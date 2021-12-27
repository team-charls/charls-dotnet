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
    private SpiffHeader spiffHeader;
    private int _index;

    public ReadOnlyMemory<byte> Source { get; set; }

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
        return _index < Source.Span.Length
            ? Source.Span[_index++]
            : throw Util.CreateInvalidDataException(JpegLSError.SourceBufferTooSmall);
    }

    private void SkipByte()
    {
        if (_index == Source.Span.Length)
            throw Util.CreateInvalidDataException(JpegLSError.SourceBufferTooSmall);

        _index++;
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

    private int ReadMarkerSegment(JpegMarkerCode markerCode, int segment_size)
    {
        switch (markerCode)
        {
            case JpegMarkerCode.StartOfFrameJpegLS:
                //    //return read_start_of_frame_segment(segment_size);
                _state = State.ScanSection;
                return 0;

            //case JpegMarkerCode.comment:
            //    //return read_comment(segment_size);

            //case JpegMarkerCode.jpegls_preset_parameters:
            //    //return read_preset_parameters_segment(segment_size);

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
}
