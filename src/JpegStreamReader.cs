// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Diagnostics;

namespace CharLS.JpegLS;

internal class JpegStreamReader
{
    private enum State
    {
        BeforeStartOfImage,
        HeaderSection,
        SpiffHeaderSection,
        ImageSection,
        FrameSection,
        ScanSection,
        BitStreamSection 
    }

    private const int JpegRestartMarkerBase = 0xD0; // RSTm: Marks the next restart interval (range is D0..D7)
    private const int JpegRestartMarkerRange = 8;

    private State _state;
    ////private SpiffHeader spiffHeader;
    private int _nearLossless;
    private JpegLSInterleaveMode _interleaveMode;

    internal ReadOnlyMemory<byte> Source { get; set; }

    internal int Position { get; private set; }

    public JpegLSPresetCodingParameters? JpegLSPresetCodingParameters { get; private set; }

    public FrameInfo? FrameInfo { get; private set; }

    internal void ReadHeader()
    {
        Debug.Assert(_state != State.ScanSection);

        if (_state == State.BeforeStartOfImage)
        {
            if (ReadNextMarkerCode() != JpegMarkerCode.StartOfImage)
                throw Util.CreateInvalidDataException(JpegLSError.StartOfImageMarkerNotFound);

            _state = State.HeaderSection;
        }

        for (; ; )
        {
            var markerCode = ReadNextMarkerCode();
            ValidateMarkerCode(markerCode);

            if (markerCode == JpegMarkerCode.StartOfScan)
            {
                _state = State.ScanSection;
                return;
            }

            int segmentSize = ReadSegmentSize();

            int bytesRead;
            switch (_state)
            {
                case State.SpiffHeaderSection:
                    bytesRead = 0; //read_spiff_directory_entry(markerCode, segment_size - 2) + 2;
                    break;

                default:
                    bytesRead = ReadMarkerSegment(markerCode, segmentSize - 2) + 2;
                    break;
            }

            int paddingToRead = segmentSize - bytesRead;
            if (paddingToRead < 0)
                throw Util.CreateInvalidDataException(JpegLSError.InvalidMarkerSegmentSize);

            for (int i = 0; i != paddingToRead; ++i)
            {
                SkipByte();
            }

            //if (state_ == state::header_section && spiff_header_found && *spiff_header_found)
            //{
            //    state_ = state::spiff_header_section;
            //    return;
            //}
        }
    }

    private byte ReadByte()
    {
        return Position < Source.Span.Length
            ? Source.Span[Position++]
            : throw Util.CreateInvalidDataException(JpegLSError.SourceBufferTooSmall);
    }

    private void SkipByte()
    {
        if (Position == Source.Span.Length)
            throw Util.CreateInvalidDataException(JpegLSError.SourceBufferTooSmall);

        Position++;
    }

    private int ReadUint16()
    {
        int value = ReadByte() * 256;
        return value + ReadByte();
    }


    private JpegMarkerCode ReadNextMarkerCode()
    {
        const byte jpegMarkerStartByte = 0xFF;

        byte value = ReadByte();
        if (value != jpegMarkerStartByte)
            throw Util.CreateInvalidDataException(JpegLSError.JpegMarkerStartByteNotFound);

        // Read all preceding 0xFF fill values until a non 0xFF value has been found. (see T.81, B.1.1.2)
        do
        {
            value = ReadByte();
        } while (value == jpegMarkerStartByte);

        return (JpegMarkerCode)value;
    }

    private int ReadSegmentSize()
    {
        int segmentSize = ReadUint16();
        return segmentSize < 2 ? throw Util.CreateInvalidDataException(JpegLSError.InvalidMarkerSegmentSize) : segmentSize;
    }

    private int ReadMarkerSegment(JpegMarkerCode markerCode, int segmentSize)
    {
        switch (markerCode)
        {
            case JpegMarkerCode.StartOfFrameJpegLS:
                return ReadStartOfFrameSegment(segmentSize);

            //case JpegMarkerCode.comment:
            //    //return read_comment(segment_size);

            case JpegMarkerCode.JpegLSPresetParameters:
                return ReadPresetParametersSegment(segmentSize);

            //case JpegMarkerCode.define_restart_interval:
            //    //return read_define_restart_interval(segment_size);

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
                return 0;

            case JpegMarkerCode.ApplicationData8:
                return 0;
            default: // Other tags not supported (among which DNL DRI)
                return 0;
                ////return try_read_ApplicationData8_segment(segment_size, header, spiff_header_found);
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
                    throw Util.CreateInvalidDataException(JpegLSError.UnexpectedMarkerFound);

                return;

            case JpegMarkerCode.StartOfFrameJpegLS:
                if (_state == State.ScanSection)
                    throw Util.CreateInvalidDataException(JpegLSError.DuplicateStartOfFrameMarker);

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
                throw Util.CreateInvalidDataException(JpegLSError.EncodingNotSupported);

            case JpegMarkerCode.StartOfImage:
                throw Util.CreateInvalidDataException(JpegLSError.DuplicateStartOfImageMarker);

            case JpegMarkerCode.EndOfImage:
                throw Util.CreateInvalidDataException(JpegLSError.UnexpectedEndOfImageMarker);
        }

        if (IsRestartMarkerCode(markerCode))
            throw Util.CreateInvalidDataException(JpegLSError.UnexpectedRestartMarker);

        throw Util.CreateInvalidDataException(JpegLSError.UnknownJpegMarkerFound);
    }

    private static bool IsRestartMarkerCode(JpegMarkerCode markerCode)
    {
        return (int)markerCode is >= JpegRestartMarkerBase and
               < JpegRestartMarkerBase + JpegRestartMarkerRange;
    }

    private int ReadStartOfFrameSegment(int segmentSize)
    {
        // A JPEG-LS Start of Frame (SOF) segment is documented in ISO/IEC 14495-1, C.2.2
        // This section references ISO/IEC 10918-1, B.2.2, which defines the normal JPEG SOF,
        // with some modifications.

        if (segmentSize < 6)
            throw Util.CreateInvalidDataException(JpegLSError.InvalidMarkerSegmentSize);

        FrameInfo = new FrameInfo
        {
            BitsPerSample = ReadByte(),
            Height = ReadUint16(),
            Width = ReadUint16(),
            ComponentCount = ReadByte()
        };

        if (!Validation.IsBitsPerSampleValid(FrameInfo.BitsPerSample))
            throw Util.CreateInvalidDataException(JpegLSError.InvalidParameterBitsPerSample);

        if (FrameInfo.Height < 1 || FrameInfo.Width < 1)
            throw Util.CreateInvalidDataException(JpegLSError.ParameterValueNotSupported);

        if (FrameInfo.ComponentCount < 1)
            throw Util.CreateInvalidDataException(JpegLSError.InvalidParameterComponentCount);

        if (segmentSize != 6 + (FrameInfo.ComponentCount * 3))
            throw Util.CreateInvalidDataException(JpegLSError.InvalidMarkerSegmentSize);

        for (int i = 0; i != FrameInfo.ComponentCount; i++)
        {
            // Component specification parameters
            add_component(ReadByte()); // Ci = Component identifier
            byte horizontal_vertical_sampling_factor = ReadByte(); // Hi + Vi = Horizontal sampling factor + Vertical sampling factor
            if (horizontal_vertical_sampling_factor != 0x11)
                throw Util.CreateInvalidDataException(JpegLSError.ParameterValueNotSupported);

            SkipByte(); // Tqi = Quantization table destination selector (reserved for JPEG-LS, should be set to 0)
        }

        _state = State.ScanSection;

        return segmentSize;
    }

    private int ReadPresetParametersSegment(int segmentSize)
    {
        if (segmentSize < 1)
            throw Util.CreateInvalidDataException(JpegLSError.InvalidMarkerSegmentSize);

        var type = (JpegLSPresetParametersType)ReadByte();
        switch (type)
        {
            case JpegLSPresetParametersType.PresetCodingParameters:
                const int coding_parameter_segment_size = 11;
                if (segmentSize != coding_parameter_segment_size)
                    throw Util.CreateInvalidDataException(JpegLSError.InvalidMarkerSegmentSize);

                // Note: validation will be done, just before decoding as more info is needed for validation.
                JpegLSPresetCodingParameters = new JpegLSPresetCodingParameters
                {
                    MaximumSampleValue = ReadUint16(),
                    Threshold1 = ReadUint16(),
                    Threshold2 = ReadUint16(),
                    Threshold3 = ReadUint16(),
                    ResetValue = ReadUint16(),
                };

                return coding_parameter_segment_size;

            case JpegLSPresetParametersType.MappingTableSpecification:
            case JpegLSPresetParametersType.MappingTableContinuation:
            case JpegLSPresetParametersType.ExtendedWidthAndHeight:
                throw Util.CreateInvalidDataException(JpegLSError.ParameterValueNotSupported);

            case JpegLSPresetParametersType.CodingMethodSpecification:
            case JpegLSPresetParametersType.NearLosslessErrorReSpecification:
            case JpegLSPresetParametersType.VisuallyOrientedQuantizationSpecification:
            case JpegLSPresetParametersType.ExtendedPredictionSpecification:
            case JpegLSPresetParametersType.StartOfFixedLengthCoding:
            case JpegLSPresetParametersType.EndOfFixedLengthCoding:
            case JpegLSPresetParametersType.ExtendedPresetCodingParameters:
            case JpegLSPresetParametersType.InverseColorTransformSpecification:
                throw Util.CreateInvalidDataException(JpegLSError.JpeglsPresetExtendedParameterTypeNotSupported);
        }

        throw Util.CreateInvalidDataException(JpegLSError.InvalidJpegLSPresetParameterType);
    }

    internal void ReadStartOfScan()
    {
        int segmentSize = ReadSegmentSize();
        if (segmentSize < 3)
            throw Util.CreateInvalidDataException(JpegLSError.InvalidMarkerSegmentSize);

        int componentCountInScan = ReadByte();
        if (componentCountInScan != 1 && componentCountInScan != FrameInfo!.ComponentCount)
            throw Util.CreateInvalidDataException(JpegLSError.ParameterValueNotSupported);

        if (segmentSize != 6 + (2 * componentCountInScan))
            throw Util.CreateInvalidDataException(JpegLSError.InvalidMarkerSegmentSize);

        for (int i = 0; i != componentCountInScan; i++)
        {
            SkipByte(); // Skip scan component selector
            int mapping_table_selector = ReadByte();
            if (mapping_table_selector != 0)
                throw Util.CreateInvalidDataException(JpegLSError.ParameterValueNotSupported);
        }

        _nearLossless = ReadByte(); // Read NEAR parameter
        //if (parameters_.near_lossless > compute_maximum_near_lossless(static_cast<int>(maximum_sample_value())))
        //    throw_jpegls_error(jpegls_errc::invalid_parameter_near_lossless);

        _interleaveMode = (JpegLSInterleaveMode)ReadByte(); // Read ILV parameter
        //check_interleave_mode(mode);
        //parameters_.interleave_mode = mode;

        if ((ReadByte() & 0xFU) != 0) // Read Ah (no meaning) and Al (point transform).
            throw Util.CreateInvalidDataException(JpegLSError.ParameterValueNotSupported);

        _state = State.BitStreamSection;
    }

    private void add_component(int component_id)
    {
        //if (find(component_ids_.cbegin(), component_ids_.cend(), component_id) != component_ids_.cend())
        //    throw_jpegls_error(jpegls_errc::duplicate_component_id_in_sof_segment);

        //component_ids_.push_back(component_id);
    }


}
