// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Buffers.Binary;
using System.Diagnostics;

namespace CharLS.JpegLS;

internal class JpegStreamReader
{
    private enum State
    {
        BeforeStartOfImage,
        HeaderSection,
        SpiffHeaderSection,
        FrameSection,
        ScanSection,
        BitStreamSection
    }

    private const int JpegRestartMarkerBase = 0xD0; // RSTm: Marks the next restart interval (range is D0..D7)
    private const int JpegRestartMarkerRange = 8;
    private const int PcTableIdEntrySizeBytes = 3;

    private State _state;

    private int _nearLossless;
    private uint _restartInterval;
    private int _segmentDataSize;
    private int _segmentStartPosition;
    private readonly List<int> _componentIds = [];
    private readonly List<MappingTableEntry> _mappingTables = [];

    public event EventHandler<CommentEventArgs>? Comment;

    internal ReadOnlyMemory<byte> Source { get; set; }

    internal int Position { get; private set; }

    public JpegLSPresetCodingParameters? JpegLSPresetCodingParameters { get; private set; }

    public FrameInfo? FrameInfo { get; private set; }

    public SpiffHeader? SpiffHeader { get; private set; }

    internal InterleaveMode InterleaveMode { get; private set; }

    internal int MappingTableCount => _mappingTables.Count;

    private int SegmentBytesToRead => (_segmentStartPosition + _segmentDataSize) - Position;

    internal uint RestartInterval
    {
        get { return _restartInterval; }
    }

    internal int? FindMappingTableIndex(int tableId)
    {
        var index = _mappingTables.FindIndex(entry => entry.TableId == tableId);
        return index == -1 ? null : index;
    }

    internal MappingTableInfo GetMappingTableInfo(int index)
    {
        var entry = _mappingTables[index];
        return new MappingTableInfo() { EntrySize = entry.EntrySize, TableId = entry.TableId };
    }

    internal ReadOnlyMemory<byte> GetMappingTableData(int index)
    {
        var entry = _mappingTables[index];
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

        if (!JpegLSPresetCodingParameters.IsValid(Algorithm.CalculateMaximumSampleValue(FrameInfo!.BitsPerSample), _nearLossless, out var validatedCodingParameters))
            throw Util.CreateInvalidDataException(ErrorCode.InvalidParameterJpeglsPresetCodingParameters);

        return validatedCodingParameters;
    }

    internal CodingParameters GetCodingParameters()
    {
        return new CodingParameters
        {
            NearLossless = _nearLossless,
            InterleaveMode = InterleaveMode,
            RestartInterval = (int)RestartInterval
        };
    }

    internal ReadOnlyMemory<byte> RemainingSource()
    {
        //ASSERT(state_ == state::bit_stream_section);
        return Source[Position..];
    }

    internal uint MaximumSampleValue
    {
        get
        {
            if (JpegLSPresetCodingParameters != null && JpegLSPresetCodingParameters.MaximumSampleValue != 0)
            {
                return (uint)JpegLSPresetCodingParameters.MaximumSampleValue;
            }

            return (uint)Util.CalculateMaximumSampleValue(FrameInfo!.BitsPerSample);
        }
    }

    internal void ReadHeader()
    {
        Debug.Assert(_state != State.ScanSection);

        if (_state == State.BeforeStartOfImage)
        {
            if (ReadNextMarkerCode() != JpegMarkerCode.StartOfImage)
                throw Util.CreateInvalidDataException(ErrorCode.StartOfImageMarkerNotFound);

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
                    ReadMarkerSegment(markerCode);
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
            ReadMarkerSegment(markerCode);

            Debug.Assert(SegmentBytesToRead == 0); // All segment data should be processed.
        } while (_state == State.ScanSection);
    }

    private byte ReadByte()
    {
        return Position < Source.Span.Length
            ? Source.Span[Position++]
            : throw Util.CreateInvalidDataException(ErrorCode.SourceBufferTooSmall);
    }

    private void SkipByte()
    {
        if (Position == Source.Span.Length)
            throw Util.CreateInvalidDataException(ErrorCode.SourceBufferTooSmall);

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
            throw Util.CreateInvalidDataException(ErrorCode.JpegMarkerStartByteNotFound);

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
            throw Util.CreateInvalidDataException(ErrorCode.InvalidMarkerSegmentSize);

        _segmentStartPosition = Position;
    }

    private void ReadMarkerSegment(JpegMarkerCode markerCode)
    {
        switch (markerCode)
        {
            case JpegMarkerCode.StartOfFrameJpegLS:
                ReadStartOfFrameSegment();
                break;

            case JpegMarkerCode.StartOfScan:
                ReadStartOfScan();
                break;

            case JpegMarkerCode.Comment:
                ReadComment();
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
                break;

            case JpegMarkerCode.ApplicationData8:
                ReadApplicationData8Segment();
                break;

            default: // Other tags not supported (among which DNL)
                Debug.Fail("Unreachable");
                break;
        }
    }

    private void ValidateMarkerCode(JpegMarkerCode markerCode)
    {
        // ISO/IEC 14495-1, C.1.1. defines the following markers as valid for a JPEG-LS byte stream:
        // SOF55, LSE, SOI, EOI, SOS, DNL, DRI, RSTm, APPn and COM.
        // All other markers shall not be present.
        switch (markerCode)
        {
            case JpegMarkerCode.StartOfScan:
                if (_state != State.ScanSection)
                    throw Util.CreateInvalidDataException(ErrorCode.UnexpectedMarkerFound);

                return;

            case JpegMarkerCode.StartOfFrameJpegLS:
                if (_state == State.ScanSection)
                    throw Util.CreateInvalidDataException(ErrorCode.DuplicateStartOfFrameMarker);

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

            // Check explicit for one of the other common JPEG encodings.
            case JpegMarkerCode.StartOfFrameBaselineJpeg:
            case JpegMarkerCode.StartOfFrameExtendedSequential:
            case JpegMarkerCode.StartOfFrameProgressive:
            case JpegMarkerCode.StartOfFrameLossless:
            case JpegMarkerCode.StartOfFrameDifferentialSequential:
            case JpegMarkerCode.StartOfFrameDifferentialProgressive:
            case JpegMarkerCode.StartOfFrameDifferentialLossless:
            case JpegMarkerCode.StartOfFrameExtendedArithmetic:
            case JpegMarkerCode.StartOfFrameProgressiveArithmetic:
            case JpegMarkerCode.StartOfFrameLosslessArithmetic:
            case JpegMarkerCode.StartOfFrameJpeglSExtended:
                throw Util.CreateInvalidDataException(ErrorCode.EncodingNotSupported);

            case JpegMarkerCode.StartOfImage:
                throw Util.CreateInvalidDataException(ErrorCode.DuplicateStartOfImageMarker);

            case JpegMarkerCode.EndOfImage:
                throw Util.CreateInvalidDataException(ErrorCode.UnexpectedEndOfImageMarker);
        }

        if (IsRestartMarkerCode(markerCode))
            throw Util.CreateInvalidDataException(ErrorCode.UnexpectedRestartMarker);

        throw Util.CreateInvalidDataException(ErrorCode.UnknownJpegMarkerFound);
    }

    private static bool IsRestartMarkerCode(JpegMarkerCode markerCode)
    {
        return (int)markerCode is >= JpegRestartMarkerBase and
               < JpegRestartMarkerBase + JpegRestartMarkerRange;
    }

    private void ReadSpiffDirectoryEntry(JpegMarkerCode markerCode)
    {
        if (markerCode != JpegMarkerCode.ApplicationData8)
            throw Util.CreateInvalidDataException(ErrorCode.MissingEndOfSpiffDirectory);

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

        FrameInfo = new FrameInfo
        {
            BitsPerSample = ReadByte(),
            Height = ReadUint16(),
            Width = ReadUint16(),
            ComponentCount = ReadByte()
        };

        if (!Validation.IsBitsPerSampleValid(FrameInfo.BitsPerSample))
            throw Util.CreateInvalidDataException(ErrorCode.InvalidParameterBitsPerSample);

        if (FrameInfo.Height < 1 || FrameInfo.Width < 1)
            throw Util.CreateInvalidDataException(ErrorCode.ParameterValueNotSupported);

        if (FrameInfo.ComponentCount < 1)
            throw Util.CreateInvalidDataException(ErrorCode.InvalidParameterComponentCount);

        CheckSegmentSize(6 + (FrameInfo.ComponentCount * 3));

        for (int i = 0; i != FrameInfo.ComponentCount; i++)
        {
            // Component specification parameters
            AddComponent(ReadByte()); // Ci = Component identifier
            byte horizontalVerticalSamplingFactor = ReadByte(); // Hi + Vi = Horizontal sampling factor + Vertical sampling factor
            if (horizontalVerticalSamplingFactor != 0x11)
                throw Util.CreateInvalidDataException(ErrorCode.ParameterValueNotSupported);

            SkipByte(); // Tqi = Quantization table destination selector (reserved for JPEG-LS, should be set to 0)
        }

        _state = State.ScanSection;
    }

    private void ReadComment()
    {
        var comment = Comment;
        if (comment != null)
        {
            comment.Invoke(this, new CommentEventArgs(Source.Slice(Position, _segmentDataSize)));
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

            case JpegLSPresetParametersType.ExtendedWidthAndHeight:
                throw Util.CreateInvalidDataException(ErrorCode.ParameterValueNotSupported);
        }

        const byte jpegLSExtendedPresetParameterLast = 0xD; // defined in JPEG-LS Extended (ISO/IEC 14495-2) (first = 0x5)
        throw Util.CreateInvalidDataException(type <= jpegLSExtendedPresetParameterLast
            ? ErrorCode.JpeglsPresetExtendedParameterTypeNotSupported
            : ErrorCode.InvalidJpegLSPresetParameterType);
    }

    private void ReadPresetCodingParameters()
    {
        CheckSegmentSize(1 + 5 * sizeof(short));

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

    private void AddMappingTable(byte tableId, byte entrySize, ReadOnlyMemory<byte> tableData)
    {
        if (tableId == 0 || _mappingTables.FindIndex(entry => entry.TableId == tableId) != -1)
            throw Util.CreateInvalidDataException(ErrorCode.InvalidParameterMappingTableId);

        _mappingTables.Add(new MappingTableEntry(tableId, entrySize, tableData));
    }

    private void ExtendMappingTable(byte tableId, byte entrySize, ReadOnlyMemory<byte> tableData)
    {
        int index = _mappingTables.FindIndex(entry => entry.TableId == tableId);

        if (index == -1 || _mappingTables[index].EntrySize != entrySize)
            throw Util.CreateInvalidDataException(ErrorCode.InvalidParameterMappingTableContinuation);

        MappingTableEntry tableEntry = _mappingTables[index];
        tableEntry.AddFragment(tableData);
        _mappingTables[index] = tableEntry;
    }

    private void ReadDefineRestartIntervalSegment()
    {
        // Note: The JPEG-LS standard supports a 2,3 or 4 byte restart interval (see ISO/IEC 14495-1, C.2.5)
        //       The original JPEG standard only supports 2 bytes (16 bit big endian).
        _restartInterval = _segmentDataSize switch
        {
            2 => (uint)ReadUint16(),
            3 => ReadUint24(),
            4 => ReadUint32(),
            _ => throw Util.CreateInvalidDataException(ErrorCode.InvalidMarkerSegmentSize)
        };
    }

    internal void ReadStartOfScan()
    {
        CheckMinimalSegmentSize(1);

        int componentCountInScan = ReadByte();
        if (componentCountInScan != 1 && componentCountInScan != FrameInfo!.ComponentCount)
            throw Util.CreateInvalidDataException(ErrorCode.ParameterValueNotSupported);

        CheckSegmentSize(4 + (2 * componentCountInScan));

        for (int i = 0; i != componentCountInScan; i++)
        {
            SkipByte(); // Skip scan component selector
            int mappingTableSelector = ReadByte();
            if (mappingTableSelector != 0)
                throw Util.CreateInvalidDataException(ErrorCode.ParameterValueNotSupported);
        }

        _nearLossless = ReadByte(); // Read NEAR parameter
        if (_nearLossless > Util.ComputeMaximumNearLossless((int)(MaximumSampleValue)))
            throw Util.CreateInvalidDataException(ErrorCode.InvalidParameterNearLossless);

        InterleaveMode = (InterleaveMode)ReadByte(); // Read ILV parameter
        CheckInterleaveMode(InterleaveMode);

        if ((ReadByte() & 0xFU) != 0) // Read Ah (no meaning) and Al (point transform).
            throw Util.CreateInvalidDataException(ErrorCode.ParameterValueNotSupported);

        _state = State.BitStreamSection;
    }

    internal void ReadEndOfImage()
    {
        Debug.Assert(_state == State.BitStreamSection);

        var markerCode = ReadNextMarkerCode();

        if (markerCode != JpegMarkerCode.EndOfImage)
            throw Util.CreateInvalidDataException(ErrorCode.EndOfImageMarkerNotFound);

        //#ifdef DEBUG
        //        state_ = state::after_end_of_image;
        //#endif
    }

    private void ReadApplicationData8Segment()
    {
        //call_application_data_callback(jpeg_marker_code::application_data8);

        if (_segmentDataSize == 5)
        {
            //try_read_hp_color_transform_segment();
        }
        else if (_segmentDataSize >= 30)
        {
            TryReadSpiffHeaderSegment();
        }

        SkipRemainingSegmentData();
    }

    private void TryReadSpiffHeaderSegment()
    {
        //ASSERT(segment_data_.size() >= 30);

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

    private void CheckMinimalSegmentSize(int minimumSize)
    {
        if (minimumSize > _segmentDataSize)
            throw Util.CreateInvalidDataException(ErrorCode.InvalidMarkerSegmentSize);
    }

    private void CheckSegmentSize(int expectedSize)
    {
        if (expectedSize != _segmentDataSize)
            throw Util.CreateInvalidDataException(ErrorCode.InvalidMarkerSegmentSize);
    }

    private void AddComponent(int componentId)
    {
        if (_componentIds.Contains(componentId))
            throw Util.CreateInvalidDataException(ErrorCode.DuplicateComponentIdInStartOfFrameSegment);

        _componentIds.Add(componentId);
    }

    private void CheckInterleaveMode(InterleaveMode mode)
    {
        if (!Enum.IsDefined(mode) || (FrameInfo!.ComponentCount == 1 && mode != InterleaveMode.None))
            throw Util.CreateInvalidDataException(ErrorCode.InvalidParameterInterleaveMode);
    }

    private readonly struct MappingTableEntry
    {
        private readonly List<ReadOnlyMemory<byte>> _dataFragments = [];

        internal MappingTableEntry(byte tableId, byte entrySize, ReadOnlyMemory<byte> tableData)
        {
            TableId = tableId;
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

        internal byte TableId { get; }
        internal byte EntrySize { get; }
    }
}
