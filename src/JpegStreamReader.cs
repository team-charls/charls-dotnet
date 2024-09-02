// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Buffers.Binary;

using static CharLS.Managed.Algorithm;

namespace CharLS.Managed;

internal struct JpegStreamReader
{
    private const int JpegRestartMarkerBase = 0xD0; // RSTm: Marks the next restart interval (range is D0 - D7)
    private const int JpegRestartMarkerRange = 8;
    private const int PcTableIdEntrySizeBytes = 3;

    private readonly object _eventSender;
    private readonly List<ScanInfo> _scanInfos = [];
    private readonly List<MappingTableEntry> _mappingTables = [];
    private State _state;
    private int _nearLossless;
    private int _segmentDataSize;
    private int _segmentStartPosition;
    private int _width;
    private int _height;
    private int _bitsPerSample;
    private int _componentCount;
    private bool _componentWithMappingTableExists;

    public JpegStreamReader()
    {
        _eventSender = this;
        _scanInfos = [];
    }

    internal JpegStreamReader(object eventSender)
    {
        _eventSender = eventSender;
        _scanInfos = [];
    }

    public event EventHandler<CommentEventArgs>? Comment;

    public event EventHandler<ApplicationDataEventArgs>? ApplicationData;

    private enum State
    {
        BeforeStartOfImage,
        HeaderSection,
        SpiffHeaderSection,
        FrameSection,
        ScanSection,
        BitStreamSection,
        AfterEndOfImage
    }

    internal readonly int ComponentCount => _scanInfos.Count;

    internal readonly bool EndOfImage => _state == State.AfterEndOfImage;

    internal readonly FrameInfo FrameInfo => new(_width, _height, _bitsPerSample, _componentCount);

    internal ReadOnlyMemory<byte> Source { get; set; }

    internal int Position { get; private set; }

    internal JpegLSPresetCodingParameters? JpegLSPresetCodingParameters { get; private set; }

    internal SpiffHeader? SpiffHeader { get; private set; }

    internal InterleaveMode CurrentInterleaveMode { get; private set; }

    internal CompressedDataFormat CompressedDataFormat { get; private set; }

    internal ColorTransformation ColorTransformation { get; private set; }

    internal readonly int MappingTableCount => _mappingTables.Count;

    internal int RestartInterval { get; private set; }

    internal readonly uint MaximumSampleValue
    {
        get
        {
            if (JpegLSPresetCodingParameters != null && JpegLSPresetCodingParameters.MaximumSampleValue != 0)
            {
                return (uint)JpegLSPresetCodingParameters.MaximumSampleValue;
            }

            return (uint)CalculateMaximumSampleValue(FrameInfo.BitsPerSample);
        }
    }

    private readonly int SegmentBytesToRead => _segmentStartPosition + _segmentDataSize - Position;

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
        return new MappingTableInfo { EntrySize = entry.EntrySize, TableId = entry.MappingTableId, DataSize = entry.DataSize };
    }

    internal readonly ReadOnlyMemory<byte> GetMappingTableData(int mappingTableIndex)
    {
        var entry = _mappingTables[mappingTableIndex];
        return entry.GetData();
    }

    internal void AdvancePosition(int count)
    {
        Debug.Assert(count >= 0);
        Debug.Assert(Position + count <= Source.Length);
        Position += count;
    }

    internal JpegLSPresetCodingParameters GetValidatedPresetCodingParameters()
    {
        JpegLSPresetCodingParameters ??= new JpegLSPresetCodingParameters();

        if (!JpegLSPresetCodingParameters.TryMakeExplicit(CalculateMaximumSampleValue(FrameInfo.BitsPerSample), _nearLossless, out var validatedCodingParameters))
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidParameterJpegLSPresetParameters);

        return validatedCodingParameters;
    }

    internal readonly int GetNearLossless(int componentIndex)
    {
        return _scanInfos[componentIndex].NearLossless;
    }

    internal readonly InterleaveMode GetInterleaveMode(int componentIndex)
    {
        return _scanInfos[componentIndex].InterleaveMode;
    }

    internal CodingParameters GetCodingParameters()
    {
        return new CodingParameters
        {
            NearLossless = _nearLossless,
            InterleaveMode = CurrentInterleaveMode,
            RestartInterval = RestartInterval,
            ColorTransformation = ColorTransformation
        };
    }

    internal readonly ReadOnlyMemory<byte> RemainingSource()
    {
        Debug.Assert(_state == State.BitStreamSection);
        return Source[Position..];
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
            if (markerCode == JpegMarkerCode.EndOfImage)
            {
                if (IsAbbreviatedFormatForTableSpecificationData())
                {
                    _state = State.AfterEndOfImage;
                    CompressedDataFormat = CompressedDataFormat.AbbreviatedTableSpecification;
                    return;
                }

                ThrowHelper.ThrowInvalidDataException(ErrorCode.UnexpectedEndOfImageMarker);
            }

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

            Debug.Assert(SegmentBytesToRead == 0);

            if (_state == State.HeaderSection && SpiffHeader != null)
            {
                _state = State.SpiffHeaderSection;
                return;
            }

            if (_state == State.BitStreamSection)
            {
                CheckCodingParameters();
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
        }
        while (_state == State.ScanSection);
    }

    internal void ReadEndOfImage()
    {
        Debug.Assert(_state == State.BitStreamSection);

        var markerCode = ReadNextMarkerCode();
        if (markerCode != JpegMarkerCode.EndOfImage)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.EndOfImageMarkerNotFound);

        Debug.Assert(CompressedDataFormat == CompressedDataFormat.Unknown);
        CompressedDataFormat = _componentWithMappingTableExists && HasExternalMappingTableIds() ?
            CompressedDataFormat.AbbreviatedImageData : CompressedDataFormat.Interchange;

        _state = State.AfterEndOfImage;
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

    private int ReadUint24()
    {
        uint value = (uint)ReadByte() << 16;
        return (int)(value + (uint)ReadUint16());
    }

    private int ReadUint32()
    {
        uint value = BinaryPrimitives.ReadUInt32BigEndian(Source[Position..].Span);
        Position += 4;

        if (value > int.MaxValue)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.ParameterValueNotSupported);

        return (int)value;
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
        }
        while (value == Constants.JpegMarkerStartByte);

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
                CheckHeightAndWidth();
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
        int spiffDirectoryType = ReadUint32();
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
        RaiseApplicationDataEvent(markerCode);
        SkipRemainingSegmentData();
    }

    private void RaiseApplicationDataEvent(JpegMarkerCode markerCode)
    {
        try
        {
            ApplicationData?.Invoke(
                _eventSender, new ApplicationDataEventArgs(markerCode - JpegMarkerCode.ApplicationData0, Source.Slice(Position, _segmentDataSize)));
        }
        catch (Exception e)
        {
            throw ThrowHelper.CreateInvalidDataException(ErrorCode.CallbackFailed, e);
        }
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
                height = ReadUint24();
                width = ReadUint24();
                break;

            case 4:
                CheckSegmentSize(pcAndDimensionBytes + (sizeof(uint) * 2));
                height = ReadUint32();
                width = ReadUint32();
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
            2 => ReadUint16(),
            3 => ReadUint24(),
            4 => ReadUint32(),
            _ => throw ThrowHelper.CreateInvalidDataException(ErrorCode.InvalidMarkerSegmentSize)
        };
    }

    private void ReadStartOfScanSegment()
    {
        CheckMinimalSegmentSize(1);

        int componentCountInScan = ReadByte();
        if (componentCountInScan != 1 && componentCountInScan != FrameInfo.ComponentCount)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.ParameterValueNotSupported);

        Span<byte> componentIds = stackalloc byte[componentCountInScan];
        Span<byte> mappingTableIds = stackalloc byte[componentCountInScan];

        CheckSegmentSize(4 + (2 * componentCountInScan));

        for (int i = 0; i != componentCountInScan; ++i)
        {
            componentIds[i] = ReadByte();
            mappingTableIds[i] = ReadByte();
        }

        _nearLossless = ReadByte(); // Read NEAR parameter
        if (_nearLossless > ComputeMaximumNearLossless((int)MaximumSampleValue))
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidParameterNearLossless);

        CurrentInterleaveMode = (InterleaveMode)ReadByte(); // Read ILV parameter
        CheckInterleaveMode(CurrentInterleaveMode);

        for (int i = 0; i != componentCountInScan; ++i)
        {
            StoreScanInfo(componentIds[i], mappingTableIds[i], (byte)_nearLossless, CurrentInterleaveMode);
        }

        if ((ReadByte() & 0xFU) != 0) // Read Ah (no meaning) and Al (point transform).
            ThrowHelper.ThrowInvalidDataException(ErrorCode.ParameterValueNotSupported);

        _state = State.BitStreamSection;
    }

    private void ReadApplicationData8Segment(bool readSpiffHeader)
    {
        RaiseApplicationDataEvent(JpegMarkerCode.ApplicationData8);

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

        Span<byte> mrfxTag = [(byte)'m', (byte)'r', (byte)'f', (byte)'x'];
        var tagBytes = ReadBytes(mrfxTag.Length);
        if (!mrfxTag.SequenceEqual(tagBytes.Span))
            return;

        byte transformation = ReadByte();
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

        Span<byte> spiffTag = [(byte)'S', (byte)'P', (byte)'I', (byte)'F', (byte)'F', 0];
        var tagBytes = ReadBytes(spiffTag.Length);
        if (!spiffTag.SequenceEqual(tagBytes.Span))
            return;

        byte highVersion = ReadByte();
        if (highVersion > Constants.SpiffMajorRevisionNumber)
            return;  // Treat unknown versions as if the SPIFF header doesn't exist.
        SkipByte();  // low version

        SpiffHeader = new SpiffHeader
        {
            ProfileId = (SpiffProfileId)ReadByte(),
            ComponentCount = ReadByte(),
            Height = ReadUint32(),
            Width = ReadUint32(),
            ColorSpace = (SpiffColorSpace)ReadByte(),
            BitsPerSample = ReadByte(),
            CompressionType = (SpiffCompressionType)ReadByte(),
            ResolutionUnit = (SpiffResolutionUnit)ReadByte(),
            VerticalResolution = ReadUint32(),
            HorizontalResolution = ReadUint32()
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
        if (_scanInfos.Exists(scan => scan.ComponentId == componentId))
            ThrowHelper.ThrowInvalidDataException(ErrorCode.DuplicateComponentIdInStartOfFrameSegment);

        _scanInfos.Add(new ScanInfo(componentId));
    }

    private readonly void CheckHeightAndWidth()
    {
        if (_height < 1)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidParameterHeight);

        if (_width < 1)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidParameterWidth);
    }

    private readonly void CheckCodingParameters()
    {
        if (ColorTransformation != ColorTransformation.None && !ColorTransformations.IsPossible(FrameInfo))
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidParameterColorTransformation);
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

    private void StoreScanInfo(byte componentId, byte tableId, int nearLossless, InterleaveMode interleaveMode)
    {
        // Ignore when info is default, prevent search and ID mismatch issues.
        if (tableId == 0 && nearLossless == 0 && interleaveMode == InterleaveMode.None)
            return;

        int index = _scanInfos.FindIndex(scanInfo => scanInfo.ComponentId == componentId);
        if (index == -1)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.UnknownComponentId);

        if (tableId != 0)
        {
            _componentWithMappingTableExists = true;
        }

        _scanInfos[index] = new ScanInfo(componentId, tableId, nearLossless, interleaveMode);
    }

    private readonly void CheckInterleaveMode(InterleaveMode mode)
    {
        if (!mode.IsValid() || (FrameInfo.ComponentCount == 1 && mode != InterleaveMode.None))
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidParameterInterleaveMode);
    }

    /// <summary>
    /// ISO/IEC 14495-1, Annex C defines 3 data formats.
    /// Annex C.4 defines the format that only contains mapping tables.
    /// </summary>
    private readonly bool IsAbbreviatedFormatForTableSpecificationData()
    {
        if (MappingTableCount == 0)
            return false;

        if (_state == State.FrameSection)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.AbbreviatedFormatAndSpiffHeaderMismatch);

        return _state == State.HeaderSection;
    }

    private readonly bool HasExternalMappingTableIds()
    {
        foreach (var scanInfo in _scanInfos)
        {
            if (scanInfo.MappingTableId != 0 && FindMappingTableIndex(scanInfo.MappingTableId) == -1)
                return true;
        }

        return false;
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

        return (byte)markerCode switch
        {
            sofBaselineJpeg or sofExtendedSequential or sofProgressive or sofLossless or sofDifferentialSequential
                or sofDifferentialProgressive or sofDifferentialLossless or sofExtendedArithmetic
                or sofProgressiveArithmetic or sofLosslessArithmetic or sofJpegLSExtended => true,
            _ => false
        };
    }

    private readonly struct ScanInfo
    {
        internal readonly int NearLossless;
        internal readonly InterleaveMode InterleaveMode;
        internal readonly byte ComponentId;
        internal readonly byte MappingTableId;

        internal ScanInfo(byte componentId, byte mappingTableId = 0, int nearLossless = 0, InterleaveMode interleaveMode = InterleaveMode.None)
        {
            ComponentId = componentId;
            MappingTableId = mappingTableId;
            NearLossless = nearLossless;
            InterleaveMode = interleaveMode;
        }
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

        internal byte MappingTableId { get; }

        internal byte EntrySize { get; }

        internal int DataSize => _dataFragments.Sum(fragment => fragment.Length);

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

            byte[] buffer = new byte[DataSize];
            CopyFragmentsIntoBuffer(buffer);
            return buffer;
        }

        private void CopyFragmentsIntoBuffer(Span<byte> buffer)
        {
            foreach (var fragment in _dataFragments)
            {
                fragment.Span.CopyTo(buffer);
                buffer = buffer[fragment.Length..];
            }
        }
    }
}
