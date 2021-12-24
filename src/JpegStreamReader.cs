// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

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

    private State _state;
    private SpiffHeader spiffHeader;
    private int _index;

    public ReadOnlyMemory<byte> Source { get; set; }

    internal void ReadHeader()
    {
        if (_state == State.BeforeStartOfImage)
        {
            if (ReadNextMarkerCode() != JpegMarkerCode.StartOfImage)
                throw Util.CreateInvalidDataException(JpegLSError.StartOfImageMarkerNotFound);

            _state = State.HeaderSection;
        }

        for (;;)
        {
            var markerCode = ReadNextMarkerCode();
            //validate_marker_code(marker_code);

            if (markerCode == JpegMarkerCode.StartOfScan)
            {
                _state = State.ScanSection;
                return;
            }

            //const int32_t segment_size{ read_segment_size()};
            //int bytes_read;
            //switch (state_)
            //{
            //    case state::spiff_header_section:
            //        bytes_read = read_spiff_directory_entry(marker_code, segment_size - 2) + 2;
            //        break;

            //    default:
            //        bytes_read = read_marker_segment(marker_code, segment_size - 2, header, spiff_header_found) + 2;
            //        break;
            //}

            //const int padding_to_read{ segment_size - bytes_read};
            //if (padding_to_read < 0)
            //    throw_jpegls_error(jpegls_errc::invalid_marker_segment_size);

            //for (int i{ }; i != padding_to_read; ++i)
            //{
            //    skip_byte();
            //}

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

    private JpegMarkerCode ReadNextMarkerCode()
    {
        const byte jpegMarkerStartByte = 0xFF;

        byte value = ReadByte();
        if (value != jpegMarkerStartByte)
            throw Util.CreateInvalidDataException(JpegLSError.StartOfImageMarkerNotFound);

        // Read all preceding 0xFF fill values until a non 0xFF value has been found. (see T.81, B.1.1.2)
        do
        {
            value = ReadByte();
        } while (value == jpegMarkerStartByte);

        return (JpegMarkerCode) value;
    }
}
