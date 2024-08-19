// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Buffers.Binary;
using System.Diagnostics;

using static CharLS.Managed.Algorithm;

namespace CharLS.Managed;

internal struct JpegStreamReader
{
    private readonly struct ScanInfo
    {
        internal readonly byte ComponentId;
        internal readonly byte MappingTableId;

        internal ScanInfo(byte componentId, byte mappingTableId = 0)
        {
            ComponentId = componentId;
            MappingTableId = mappingTableId;
        }
    }

    private enum State
    {
        BeforeStartOfImage,
        HeaderSection,
        SpiffHeaderSection,
        FrameSection,
        ScanSection,
        BitStreamSection
    }

    private const int JpegRestartMarkerBase = 0xD0; // RSTm: Marks the next restart interval (range is D0 - D7)
    private const int JpegRestartMarkerRange = 8;
    private const int PcTableIdEntrySizeBytes = 3;

    private State _state;
    private readonly object _eventSender;
    private int _nearLossless;
    private int _segmentDataSize;
    private int _segmentStartPosition;
    private int _width;
    private int _height;
    private int _bitsPerSample;
    private int _componentCount;
    private readonly List<ScanInfo> _scanInfos = [];
    private readonly List<MappingTableEntry> _mappingTables = [];

    public event EventHandler<CommentEventArgs>? Comment;
    public event EventHandler<ApplicationDataEventArgs>? ApplicationData;

    internal ReadOnlyMemory<byte> Source { get; set; }

    internal int Position { get; private set; }

    public JpegLSPresetCodingParameters? JpegLSPresetCodingParameters { get; private set; }

    public readonly FrameInfo FrameInfo => new(_width, _height, _bitsPerSample, _componentCount);

    public SpiffHeader? SpiffHeader { get; private set; }

    internal InterleaveMode InterleaveMode { get; private set; }

    internal ColorTransformation ColorTransformation { get; private set; }

    internal readonly int MappingTableCount => _mappingTables.Count;

    internal uint RestartInterval { get; private set; }

    internal readonly int ComponentCount => _scanInfos.Count;

    private readonly int SegmentBytesToRead => _segmentStartPosition + _segmentDataSize - Position;

    public JpegStreamReader()
    {
        _eventSender = this;
        _scanInfos = [];
    }

    internal JpegStreamReader(object? eventSender = null)
    {
        _eventSender = eventSender ?? this;
    }

    internal readonly int GetMappingTableId(int componentIndex)
    {
        return _scanInfos[componentIndex].MappingTableId;
    }

    internal readonly int FindMappingTableIndex(int mappingTableId)
    {
        return _mappingTables.FindIndex(entry => entry.MappingTableId == mappingTableId);
    }

    internal readonly MappingTableInfo GetMappingTableInfo(int mappingTableIndex)
    {
        var entry = _mappingTables[mappingTableIndex];
        return new MappingTableInfo { EntrySize = entry.EntrySize, TableId = entry.MappingTableId };
    }

    internal readonly ReadOnlyMemory<byte> GetMappingTableData(int mappingTableIndex)
    {
        var entry = _mappingTables[mappingTableIndex];
        return entry.GetData();
    }

    internal void AdvancePosition(int count)
    {
        //Debug.Assert(Position + count <= /*end_position_*/ );
        Position += count;
    }

    internal JpegLSPresetCodingParameters GetValidatedPresetCodingParameters()
    {
        JpegLSPresetCodingParameters ??= new JpegLSPresetCodingParameters();

        if (!JpegLSPresetCodingParameters.IsValid(CalculateMaximumSampleValue(FrameInfo!.BitsPerSample), _nearLossless, out var validatedCodingParameters))
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidParameterJpegLSPresetParameters);

        return validatedCodingParameters;
    }

    internal CodingParameters GetCodingParameters()
    {
        return new CodingParameters
        {
            NearLossless = _nearLossless,
            InterleaveMode = InterleaveMode,
            RestartInterval = (int)RestartInterval,
            ColorTransformation = ColorTransformation
        };
    }

    internal readonly ReadOnlyMemory<byte> RemainingSource()
    {
        //ASSERT(state_ == state::bit_stream_section);
        return Source[Position..];
    }

    internal readonly uint MaximumSampleValue
    {
        get
        {
            if (JpegLSPresetCodingParameters != null && JpegLSPresetCodingParameters.MaximumSampleValue != 0)
            {
                return (uint)JpegLSPresetCodingParameters.MaximumSampleValue;
            }

            return (uint)CalculateMaximumSampleValue(FrameInfo!.BitsPerSample);
        }
    }

    internal void ReadHeader(bool readSpiffHeader)
    {
        Debug.Assert(_state != State.ScanSection);

        if (_state == State.BeforeStartOfImage)
        {
            if (ReadNextMarkerCode() != JpegMarkerCode.StartOfImage)
                ThrowHelper.ThrowInvalidDataException(ErrorCode.StartOfImageMarkerNotFound);

            _state = State.HeaderSection;
        }

        for (; ; )
        {
            var markerCode = ReadNextMarkerCode();
            ValidateMarkerCode(markerCode);

            ReadSegmentSize();

            switch (_state)
            {
                case State.SpiffHeaderSection:
                    ReadSpiffDirectoryEntry(markerCode);
                    break;

                default:
                    ReadMarkerSegment(markerCode, readSpiffHeader);
                    break;
            }

            //Debug.Assert(_segmentData.Length);

            if (_state == State.HeaderSection && SpiffHeader != null)
            {
                _state = State.SpiffHeaderSection;
                return;
            }

            if (_state == State.BitStreamSection)
            {
                //check_frame_info();
                //check_coding_parameters();
                return;
            }
        }
    }

    internal void ReadNextStartOfScan()
    {
        Debug.Assert(_state == State.BitStreamSection);
        _state = State.ScanSection;

        do
        {
            var markerCode = ReadNextMarkerCode();
            ValidateMarkerCode(markerCode);
            ReadSegmentSize();
            ReadMarkerSegment(markerCode, false);

            Debug.Assert(SegmentBytesToRead == 0); // All segment data should be processed.
        } while (_state == State.ScanSection);
    }

    private byte ReadByte()
    {
        if (Position == Source.Span.Length)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.NeedMoreData);

        return Source.Span[Position++];
    }

    private void SkipByte()
    {
        if (Position == Source.Span.Length)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.NeedMoreData);

        Position++;
    }

    private int ReadUint16()
    {
        int value = ReadByte() * 256;
        return value + ReadByte();
    }

    private uint ReadUint24()
    {
        uint value = (uint)ReadByte() << 16;
        return value + (uint)ReadUint16();
    }

    private uint ReadUint32()
    {
        uint value = BinaryPrimitives.ReadUInt32BigEndian(Source[Position..].Span);
        Position += 4;
        return value;
    }

    private JpegMarkerCode ReadNextMarkerCode()
    {
        byte value = ReadByte();
        if (value != Constants.JpegMarkerStartByte)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.JpegMarkerStartByteNotFound);

        // Read all preceding 0xFF fill values until a non 0xFF value has been found. (see T.81, B.1.1.2)
        do
        {
            value = ReadByte();
        } while (value == Constants.JpegMarkerStartByte);

        return (JpegMarkerCode)value;
    }

    private void ReadSegmentSize()
    {
        const int segmentLength = 2; // The segment size also includes the length of the segment length bytes.
        int segmentSize = ReadUint16();
        _segmentDataSize = segmentSize - segmentLength;
        if (segmentSize < segmentLength || Position + _segmentDataSize > Source.Length)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidMarkerSegmentSize);

        _segmentStartPosition = Position;
    }

    private void ReadMarkerSegment(JpegMarkerCode markerCode, bool readSpiffHeader)
    {
        switch (markerCode)
        {
            case JpegMarkerCode.StartOfFrameJpegLS:
                ReadStartOfFrameSegment();
                break;

            case JpegMarkerCode.StartOfScan:
                ReadStartOfScanSegment();
                break;

            case JpegMarkerCode.Comment:
                ReadCommentSegment();
                break;

            case JpegMarkerCode.JpegLSPresetParameters:
                ReadPresetParametersSegment();
                break;

            case JpegMarkerCode.DefineRestartInterval:
                ReadDefineRestartIntervalSegment();
                break;

            case JpegMarkerCode.ApplicationData0:
            case JpegMarkerCode.ApplicationData1:
            case JpegMarkerCode.ApplicationData2:
            case JpegMarkerCode.ApplicationData3:
            case JpegMarkerCode.ApplicationData4:
            case JpegMarkerCode.ApplicationData5:
            case JpegMarkerCode.ApplicationData6:
            case JpegMarkerCode.ApplicationData7:
            case JpegMarkerCode.ApplicationData9:
            case JpegMarkerCode.ApplicationData10:
            case JpegMarkerCode.ApplicationData11:
            case JpegMarkerCode.ApplicationData12:
            case JpegMarkerCode.ApplicationData13:
            case JpegMarkerCode.ApplicationData14:
            case JpegMarkerCode.ApplicationData15:
                ReadApplicationDataSegment(markerCode);
                break;

            case JpegMarkerCode.ApplicationData8:
                ReadApplicationData8Segment(readSpiffHeader);
                break;

            default: // Other tags not supported (among which DNL)
                Debug.Fail("Unreachable");
                break;
        }
    }

    private readonly void ValidateMarkerCode(JpegMarkerCode markerCode)
    {
        // ISO/IEC 14495-1, C.1.1. defines the following markers as valid for a JPEG-LS byte stream:
        // SOF55, LSE, SOI, EOI, SOS, DNL, DRI, RSTm, APPn and COM.
        // All other markers shall not be present.
        switch (markerCode)
        {
            case JpegMarkerCode.StartOfScan:
                if (_state != State.ScanSection)
                    ThrowHelper.ThrowInvalidDataException(ErrorCode.UnexpectedMarkerFound);

                return;

            case JpegMarkerCode.StartOfFrameJpegLS:
                if (_state == State.ScanSection)
                    ThrowHelper.ThrowInvalidDataException(ErrorCode.DuplicateStartOfFrameMarker);

                return;

            case JpegMarkerCode.DefineRestartInterval:
            case JpegMarkerCode.JpegLSPresetParameters:
            case JpegMarkerCode.Comment:
            case JpegMarkerCode.ApplicationData0:
            case JpegMarkerCode.ApplicationData1:
            case JpegMarkerCode.ApplicationData2:
            case JpegMarkerCode.ApplicationData3:
            case JpegMarkerCode.ApplicationData4:
            case JpegMarkerCode.ApplicationData5:
            case JpegMarkerCode.ApplicationData6:
            case JpegMarkerCode.ApplicationData7:
            case JpegMarkerCode.ApplicationData8:
            case JpegMarkerCode.ApplicationData9:
            case JpegMarkerCode.ApplicationData10:
            case JpegMarkerCode.ApplicationData11:
            case JpegMarkerCode.ApplicationData12:
            case JpegMarkerCode.ApplicationData13:
            case JpegMarkerCode.ApplicationData14:
            case JpegMarkerCode.ApplicationData15:
                return;

            case JpegMarkerCode.DefineNumberOfLines: // DLN is a JPEG-LS valid marker, but not supported: handle as unknown.
                ThrowHelper.ThrowInvalidDataException(ErrorCode.UnknownJpegMarkerFound);
                break;

            case JpegMarkerCode.StartOfImage:
                ThrowHelper.ThrowInvalidDataException(ErrorCode.DuplicateStartOfImageMarker);
                break;

            case JpegMarkerCode.EndOfImage:
                ThrowHelper.ThrowInvalidDataException(ErrorCode.UnexpectedEndOfImageMarker);
                break;
        }

        // Check explicit for one of the other common JPEG encodings.
        if (IsKnownJpegSofMarker(markerCode))
            ThrowHelper.ThrowInvalidDataException(ErrorCode.EncodingNotSupported);

        if (IsRestartMarkerCode(markerCode))
            ThrowHelper.ThrowInvalidDataException(ErrorCode.UnexpectedRestartMarker);

        ThrowHelper.ThrowInvalidDataException(ErrorCode.UnknownJpegMarkerFound);
    }

    private static bool IsRestartMarkerCode(JpegMarkerCode markerCode)
    {
        return (int)markerCode is >= JpegRestartMarkerBase and
               < JpegRestartMarkerBase + JpegRestartMarkerRange;
    }

    private void ReadSpiffDirectoryEntry(JpegMarkerCode markerCode)
    {
        if (markerCode != JpegMarkerCode.ApplicationData8)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.MissingEndOfSpiffDirectory);

        CheckMinimalSegmentSize(4);
        uint spiffDirectoryType = ReadUint32();
        if (spiffDirectoryType == Constants.SpiffEndOfDirectoryEntryType)
        {
            CheckSegmentSize(6); // 4 + 2 for dummy SOI.
            _state = State.FrameSection;
        }

        SkipRemainingSegmentData();
    }

    private void ReadStartOfFrameSegment()
    {
        // A JPEG-LS Start of Frame (SOF) segment is documented in ISO/IEC 14495-1, C.2.2
        // This section references ISO/IEC 10918-1, B.2.2, which defines the normal JPEG SOF,
        // with some modifications.
        CheckMinimalSegmentSize(6);

        _bitsPerSample = ReadByte();
        if (!Validation.IsBitsPerSampleValid(_bitsPerSample))
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidParameterBitsPerSample);

        SetHeight(ReadUint16());
        SetWidth(ReadUint16());

        _componentCount = ReadByte();
        if (_componentCount == 0)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidParameterComponentCount);

        CheckSegmentSize(6 + (_componentCount * 3));

        for (int i = 0; i != _componentCount; i++)
        {
            // Component specification parameters
            AddComponent(ReadByte()); // Ci = Component identifier
            byte horizontalVerticalSamplingFactor = ReadByte(); // Hi + Vi = Horizontal sampling factor + Vertical sampling factor
            if (horizontalVerticalSamplingFactor != 0x11)
                ThrowHelper.ThrowInvalidDataException(ErrorCode.ParameterValueNotSupported);

            SkipByte(); // Tqi = Quantization table destination selector (reserved for JPEG-LS, should be set to 0)
        }

        _state = State.ScanSection;
    }

    private void ReadApplicationDataSegment(JpegMarkerCode markerCode)
    {
        try
        {
            ApplicationData?.Invoke(_eventSender,
                new ApplicationDataEventArgs(markerCode - JpegMarkerCode.ApplicationData0,
                    Source.Slice(Position, _segmentDataSize)));
        }
        catch (Exception e)
        {
            throw ThrowHelper.CreateInvalidDataException(ErrorCode.CallbackFailed, e);
        }

        SkipRemainingSegmentData();
    }

    private void ReadCommentSegment()
    {
        try
        {
            Comment?.Invoke(_eventSender, new CommentEventArgs(Source.Slice(Position, _segmentDataSize)));
        }
        catch (Exception e)
        {
            throw ThrowHelper.CreateInvalidDataException(ErrorCode.CallbackFailed, e);
        }

        SkipRemainingSegmentData();
    }

    private void ReadPresetParametersSegment()
    {
        CheckMinimalSegmentSize(1);

        byte type = ReadByte();
        switch ((JpegLSPresetParametersType)type)
        {
            case JpegLSPresetParametersType.PresetCodingParameters:
                ReadPresetCodingParameters();
                return;

            case JpegLSPresetParametersType.MappingTableSpecification:
                ReadMappingTableSpecification();
                return;

            case JpegLSPresetParametersType.MappingTableContinuation:
                ReadMappingTableContinuation();
                return;

            case JpegLSPresetParametersType.OversizeImageDimension:
                ReadOversizeImageDimension();
                return;
        }

        const byte jpegLSExtendedPresetParameterLast = 0xD; // defined in JPEG-LS Extended (ISO/IEC 14495-2) (first = 0x5)
        ThrowHelper.ThrowInvalidDataException(type <= jpegLSExtendedPresetParameterLast
            ? ErrorCode.JpegLSPresetExtendedParameterTypeNotSupported
            : ErrorCode.InvalidJpegLSPresetParameterType);
    }

    private void ReadPresetCodingParameters()
    {
        CheckSegmentSize(1 + (5 * sizeof(short)));

        // Note: validation will be done, just before decoding as more info is needed for validation.
        JpegLSPresetCodingParameters = new JpegLSPresetCodingParameters
        {
            MaximumSampleValue = ReadUint16(),
            Threshold1 = ReadUint16(),
            Threshold2 = ReadUint16(),
            Threshold3 = ReadUint16(),
            ResetValue = ReadUint16()
        };
    }

    private void ReadMappingTableSpecification()
    {
        CheckMinimalSegmentSize(PcTableIdEntrySizeBytes);

        byte tableId = ReadByte();
        byte entrySize = ReadByte();

        AddMappingTable(tableId, entrySize, Source.Slice(Position, SegmentBytesToRead));
        SkipRemainingSegmentData();
    }

    private void ReadMappingTableContinuation()
    {
        CheckMinimalSegmentSize(PcTableIdEntrySizeBytes);

        byte tableId = ReadByte();
        byte entrySize = ReadByte();

        ExtendMappingTable(tableId, entrySize, Source.Slice(Position, SegmentBytesToRead));
        SkipRemainingSegmentData();
    }

    private void ReadOversizeImageDimension()
    {
        // Note: The JPEG-LS standard supports a 2,3 or 4 bytes for the size.
        const int pcAndDimensionBytes = 2;
        CheckMinimalSegmentSize(PcTableIdEntrySizeBytes);
        byte dimensionSize = ReadByte();

        int height;
        int width;
        switch (dimensionSize)
        {
            case 2:
                CheckSegmentSize(pcAndDimensionBytes + (sizeof(ushort) * 2));
                height = ReadUint16();
                width = ReadUint16();
                break;

            case 3:
                CheckSegmentSize(pcAndDimensionBytes + ((sizeof(ushort) + 1) * 2));
                height = (int)ReadUint24();
                width = (int)ReadUint24();
                break;

            case 4:
                CheckSegmentSize(pcAndDimensionBytes + (sizeof(uint) * 2));
                height = (int)ReadUint32(); // TODO
                width = (int)ReadUint32(); // TODO
                break;

            default:
                throw ThrowHelper.CreateInvalidDataException(ErrorCode.InvalidParameterJpegLSPresetParameters);
        }

        SetHeight(height);
        SetWidth(width);
    }

    private readonly void AddMappingTable(byte tableId, byte entrySize, ReadOnlyMemory<byte> tableData)
    {
        if (tableId == 0 || _mappingTables.FindIndex(entry => entry.MappingTableId == tableId) != -1)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidParameterMappingTableId);

        _mappingTables.Add(new MappingTableEntry(tableId, entrySize, tableData));
    }

    private readonly void ExtendMappingTable(byte tableId, byte entrySize, ReadOnlyMemory<byte> tableData)
    {
        int index = _mappingTables.FindIndex(entry => entry.MappingTableId == tableId);

        if (index == -1 || _mappingTables[index].EntrySize != entrySize)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidParameterMappingTableContinuation);

        MappingTableEntry tableEntry = _mappingTables[index];
        tableEntry.AddFragment(tableData);
        _mappingTables[index] = tableEntry;
    }

    private void ReadDefineRestartIntervalSegment()
    {
        // Note: The JPEG-LS standard supports a 2, 3 or 4 byte restart interval (see ISO/IEC 14495-1, C.2.5)
        //       The original JPEG standard only supports 2 bytes (16 bit big endian).
        RestartInterval = _segmentDataSize switch
        {
            2 => (uint)ReadUint16(),
            3 => ReadUint24(),
            4 => ReadUint32(),
            _ => throw ThrowHelper.CreateInvalidDataException(ErrorCode.InvalidMarkerSegmentSize)
        };
    }

    internal void ReadStartOfScanSegment()
    {
        CheckMinimalSegmentSize(1);

        int componentCountInScan = ReadByte();
        if (componentCountInScan != 1 && componentCountInScan != FrameInfo!.ComponentCount)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.ParameterValueNotSupported);

        CheckSegmentSize(4 + (2 * componentCountInScan));

        for (int i = 0; i != componentCountInScan; i++)
        {
            byte componentId = ReadByte();
            byte mappingTableId = ReadByte();
            StoreMappingTableId(componentId, mappingTableId);
        }

        _nearLossless = ReadByte(); // Read NEAR parameter
        if (_nearLossless > ComputeMaximumNearLossless((int)MaximumSampleValue))
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidParameterNearLossless);

        InterleaveMode = (InterleaveMode)ReadByte(); // Read ILV parameter
        CheckInterleaveMode(InterleaveMode);

        if ((ReadByte() & 0xFU) != 0) // Read Ah (no meaning) and Al (point transform).
            ThrowHelper.ThrowInvalidDataException(ErrorCode.ParameterValueNotSupported);

        _state = State.BitStreamSection;
    }

    internal void ReadEndOfImage()
    {
        Debug.Assert(_state == State.BitStreamSection);

        var markerCode = ReadNextMarkerCode();

        if (markerCode != JpegMarkerCode.EndOfImage)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.EndOfImageMarkerNotFound);

        ////#ifdef DEBUG
        //        state_ = state::after_end_of_image;
        ////#endif
    }

    private void ReadApplicationData8Segment(bool readSpiffHeader)
    {
        ////call_application_data_callback(jpeg_marker_code::application_data8);

        if (_segmentDataSize == 5)
        {
            TryReadHPColorTransformSegment();
        }
        else if (readSpiffHeader && _segmentDataSize >= 30)
        {
            TryReadSpiffHeaderSegment();
        }

        SkipRemainingSegmentData();
    }

    private void TryReadHPColorTransformSegment()
    {
        Debug.Assert(SegmentBytesToRead == 5);

        var segmentData = ReadBytes(5);

        byte[] mrfxTag = [(byte)'m', (byte)'r', (byte)'f', (byte)'x'];
        if (!segmentData[..4].Span.SequenceEqual(mrfxTag))
            return;

        byte transformation = segmentData.Span[4];
        switch (transformation)
        {
            case (byte)ColorTransformation.None:
            case (byte)ColorTransformation.HP1:
            case (byte)ColorTransformation.HP2:
            case (byte)ColorTransformation.HP3:
                ColorTransformation = (ColorTransformation)transformation;
                return;

            case 4: // RgbAsYuvLossy: the standard lossy RGB to YCbCr transform used in JPEG.
            case 5: // Matrix: transformation is controlled using a matrix that is also stored in the segment.
                ThrowHelper.ThrowInvalidDataException(ErrorCode.ColorTransformNotSupported);
                break;

            default:
                ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidParameterColorTransformation);
                break;
        }
    }

    private void TryReadSpiffHeaderSegment()
    {
        Debug.Assert(_segmentDataSize >= 30);

        byte[] spiffTag =
        [
            (byte)'S', (byte)'P', (byte)'I', (byte)'F', (byte)'F', 0
        ];

        var beginBytes = ReadBytes(spiffTag.Length);
        if (beginBytes.Equals(spiffTag))
            return;

        byte highVersion = ReadByte();
        if (highVersion > Constants.SpiffMajorRevisionNumber)
            return;  // Treat unknown versions as if the SPIFF header doesn't exist.
        SkipByte();  // low version

        SpiffHeader = new SpiffHeader
        {
            ProfileId = (SpiffProfileId)ReadByte(),
            ComponentCount = ReadByte(),
            Height = (int)ReadUint32(), // TODO
            Width = (int)ReadUint32(), // TODO
            ColorSpace = (SpiffColorSpace)ReadByte(),
            BitsPerSample = ReadByte(),
            CompressionType = (SpiffCompressionType)ReadByte(),
            ResolutionUnit = (SpiffResolutionUnit)ReadByte(),
            VerticalResolution = (int)ReadUint32(), // TODO
            HorizontalResolution = (int)ReadUint32(), // TODO
        };
    }

    private ReadOnlyMemory<byte> ReadBytes(int byteCount)
    {
        var bytes = Source.Slice(Position, byteCount);
        Position += byteCount;
        return bytes;
    }

    private void SkipRemainingSegmentData()
    {
        Position += SegmentBytesToRead;
    }

    private readonly void CheckMinimalSegmentSize(int minimumSize)
    {
        if (minimumSize > _segmentDataSize)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidMarkerSegmentSize);
    }

    private readonly void CheckSegmentSize(int expectedSize)
    {
        if (expectedSize != _segmentDataSize)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidMarkerSegmentSize);
    }

    private readonly void AddComponent(byte componentId)
    {
        if (_scanInfos.Any(scan => scan.ComponentId == componentId))
            ThrowHelper.ThrowInvalidDataException(ErrorCode.DuplicateComponentIdInStartOfFrameSegment);

        _scanInfos.Add(new ScanInfo(componentId));
    }

    private void SetWidth(int value)
    {
        if (value == 0)
            return;

        if (_width != 0)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidParameterWidth);

        _width = value;
    }

    private void SetHeight(int value)
    {
        if (value == 0)
            return;

        if (_height != 0)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidParameterHeight);

        _height = value;
    }

    private readonly void StoreMappingTableId(byte componentId, byte tableId)
    {
        if (tableId == 0)
            return; // default is already 0, no need to search and update.

        int index = _scanInfos.FindIndex(scanInfo => scanInfo.ComponentId == componentId);
        if (index == -1)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.UnknownComponentId);

        _scanInfos[index] = new ScanInfo(componentId, tableId);
    }

    private readonly void CheckInterleaveMode(InterleaveMode mode)
    {
        if (!mode.IsValid() || (FrameInfo!.ComponentCount == 1 && mode != InterleaveMode.None))
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidParameterInterleaveMode);
    }

    private readonly struct MappingTableEntry
    {
        private readonly List<ReadOnlyMemory<byte>> _dataFragments = [];

        internal MappingTableEntry(byte mappingTableId, byte entrySize, ReadOnlyMemory<byte> tableData)
        {
            MappingTableId = mappingTableId;
            EntrySize = entrySize;
            _dataFragments.Add(tableData);
        }

        internal void AddFragment(ReadOnlyMemory<byte> tableData)
        {
            _dataFragments.Add(tableData);
        }

        internal ReadOnlyMemory<byte> GetData()
        {
            if (_dataFragments.Count == 1)
            {
                return _dataFragments[0];
            }

            throw new NotImplementedException();
        }

        internal byte MappingTableId { get; }
        internal byte EntrySize { get; }
    }

    private static bool IsKnownJpegSofMarker(JpegMarkerCode markerCode)
    {
        // The following start of frame (SOF) markers are defined in ISO/IEC 10918-1 | ITU T.81 (general JPEG standard).
        const byte sofBaselineJpeg = 0xC0;            // SOF_0: Baseline jpeg encoded frame.
        const byte sofExtendedSequential = 0xC1;      // SOF_1: Extended sequential Huffman encoded frame.
        const byte sofProgressive = 0xC2;             // SOF_2: Progressive Huffman encoded frame.
        const byte sofLossless = 0xC3;                // SOF_3: Lossless Huffman encoded frame.
        const byte sofDifferentialSequential = 0xC5;  // SOF_5: Differential sequential Huffman encoded frame.
        const byte sofDifferentialProgressive = 0xC6; // SOF_6: Differential progressive Huffman encoded frame.
        const byte sofDifferentialLossless = 0xC7;    // SOF_7: Differential lossless Huffman encoded frame.
        const byte sofExtendedArithmetic = 0xC9;      // SOF_9: Extended sequential arithmetic encoded frame.
        const byte sofProgressiveArithmetic = 0xCA;   // SOF_10: Progressive arithmetic encoded frame.
        const byte sofLosslessArithmetic = 0xCB;      // SOF_11: Lossless arithmetic encoded frame.
        const byte sofJpegLSExtended = 0xF9;          // SOF_57: JPEG-LS extended (ISO/IEC 14495-2) encoded frame.

        switch ((byte)markerCode)
        {
            case sofBaselineJpeg:
            case sofExtendedSequential:
            case sofProgressive:
            case sofLossless:
            case sofDifferentialSequential:
            case sofDifferentialProgressive:
            case sofDifferentialLossless:
            case sofExtendedArithmetic:
            case sofProgressiveArithmetic:
            case sofLosslessArithmetic:
            case sofJpegLSExtended:
                return true;
            default:
                return false;
        }
    }
}
