// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Buffers.Binary;
using System.Diagnostics;

namespace CharLS.JpegLS;

internal class JpegStreamWriter
{
    private int _position;
    private int _componentIndex;

    internal Memory<byte> Destination { get; set; }

    internal Memory<byte> GetRemainingDestination()
    {
        return Destination[_position..];
    }

    // ReSharper disable once ConvertToAutoPropertyWhenPossible
    internal int BytesWritten => _position;

    internal void Seek(int byteCount)
    {
        Debug.Assert(_position + byteCount <= Destination.Length);
        _position += byteCount;
    }

    internal void WriteStartOfImage()
    {
        WriteSegmentWithoutData(JpegMarkerCode.StartOfImage);
    }

    internal void WriteColorTransformSegment(ColorTransformation colorTransformation)
    {
        byte[] segment = [(byte)'m', (byte)'r', (byte)'f', (byte)'x', (byte)colorTransformation];
        WriteSegment(JpegMarkerCode.ApplicationData8, segment);
    }

    internal void WriteCommentSegment(ReadOnlySpan<byte> comment)
    {
        WriteSegment(JpegMarkerCode.Comment, comment);
    }

    internal void WriteJpegLSPresetParametersSegment(JpegLSPresetCodingParameters presetCodingParameters)
    {
        WriteSegmentHeader(JpegMarkerCode.JpegLSPresetParameters, 1 + 5 * sizeof(ushort));
        WriteByte((byte)JpegLSPresetParametersType.PresetCodingParameters);
        WriteUint16(presetCodingParameters.MaximumSampleValue);
        WriteUint16(presetCodingParameters.Threshold1);
        WriteUint16(presetCodingParameters.Threshold2);
        WriteUint16(presetCodingParameters.Threshold3);
        WriteUint16(presetCodingParameters.ResetValue);
    }

    public void WriteJpegLSPresetParametersSegment(int height, int width)
    {
        // Format is defined in ISO/IEC 14495-1, C.2.4.1.4
        WriteSegmentHeader(JpegMarkerCode.JpegLSPresetParameters, 1 + 1 + 2 * sizeof(uint));
        WriteByte((byte)JpegLSPresetParametersType.OversizeImageDimension);
        WriteByte(sizeof(uint)); // Wxy: number of bytes used to represent Ye and Xe [2..4]. Always 4 for simplicity.
        WriteUint32(height);     // Ye: number of lines in the image.
        WriteUint32(width);      // Xe: number of columns in the image.
    }

    internal bool WriteStartOfFrameSegment(FrameInfo frameInfo)
    {
        // Create a Frame Header as defined in ISO/IEC 14495-1, C.2.2 and T.81, B.2.2
        int dataSize = 6 + frameInfo.ComponentCount * 3;
        WriteSegmentHeader(JpegMarkerCode.StartOfFrameJpegLS, dataSize);
        WriteByte((byte)frameInfo.BitsPerSample); // P = Sample precision

        bool oversizedImage = frameInfo.Width > ushort.MaxValue || frameInfo.Height > ushort.MaxValue;
        WriteUint16(oversizedImage ? 0 : frameInfo.Height); // Y = Number of lines
        WriteUint16(oversizedImage ? 0 : frameInfo.Width);  // X = Number of samples per line

        // Components
        WriteByte((byte)frameInfo.ComponentCount); // Nf = Number of image components in frame

        // Use by default 1 as the start component identifier to remain compatible with the
        // code sample of ISO/IEC 14495-1, H.4 and the JPEG-LS ISO conformance sample files.
        for (int componentId = 1; componentId <= frameInfo.ComponentCount; ++componentId)
        {
            // Component Specification parameters
            WriteByte((byte)componentId); // Ci = Component identifier
            WriteByte(0x11);         // Hi + Vi = Horizontal sampling factor + Vertical sampling factor
            WriteByte(0); // Tqi = Quantization table destination selector (reserved for JPEG-LS, should be set to 0)
        }

        return oversizedImage;
    }

    internal void WriteStartOfScanSegment(int componentCount, int nearLossless, InterleaveMode interleaveMode)
    {
        //ASSERT(component_count > 0 && component_count <= numeric_limits<uint8_t>::max());
        //ASSERT(nearLossless >= 0 && nearLossless <= numeric_limits<uint8_t>::max());
        //ASSERT(interleave_mode == interleave_mode::none || interleave_mode == interleave_mode::line ||
        //       interleave_mode == interleave_mode::sample);

        // Create a Scan Header as defined in T.87, C.2.3 and T.81, B.2.3
        WriteSegmentHeader(JpegMarkerCode.StartOfScan, 1 + (componentCount * 2) + 3);
        WriteByte((byte)componentCount);

        for (int i = 0; i != componentCount; ++i)
        {
            WriteByte((byte)(_componentIndex + 1)); // follow the JPEG-LS standard samples and start with component ID 1.
            WriteByte(MappingTableSelector());
            ++_componentIndex;
        }

        WriteByte((byte)nearLossless);   // NEAR parameter
        WriteByte((byte)interleaveMode); // ILV parameter
        WriteByte(0);                    // transformation
    }

    internal void WriteEndOfImage(bool evenDestinationSize)
    {
        if (evenDestinationSize && BytesWritten % 2 != 0)
        {
            // Write an additional 0xFF byte to ensure that the encoded bit stream has an even size.
            WriteByte(Constants.JpegMarkerStartByte);
        }

        WriteSegmentWithoutData(JpegMarkerCode.EndOfImage);
    }

    private void WriteSegmentWithoutData(JpegMarkerCode markerCode)
    {
        if (_position + 2 > Destination.Length)
            throw Util.CreateInvalidDataException(ErrorCode.DestinationBufferTooSmall);

        WriteByte(Constants.JpegMarkerStartByte);
        WriteByte((byte)markerCode);
    }

    private void WriteSegment(JpegMarkerCode markerCode, ReadOnlySpan<byte> data)
    {
        WriteSegmentHeader(markerCode, data.Length);
        WriteBytes(data);
    }

    private void WriteSegmentHeader(JpegMarkerCode markerCode, int dataSize)
    {
        // ASSERT(data_size <= segment_max_data_size);

        // Check if there is enough room in the destination to write the complete segment.
        // Other methods assume that the checking in done here and don't check again.
        //const int markerCodeSize = 2;
        //int totalSegmentSize = markerCodeSize + Constants.SegmentLengthSize + dataSize;
        //if (const size_t total_segment_size{marker_code_size + segment_length_size + data_size};
        //UNLIKELY(byte_offset_ + total_segment_size > destination_.size()))
        //impl::throw_jpegls_error(jpegls_errc::destination_buffer_too_small);

        WriteMarker(markerCode);
        WriteUint16(Constants.SegmentLengthSize + dataSize);
    }

    private void WriteMarker(JpegMarkerCode markerCode)
    {
        WriteByte(Constants.JpegMarkerStartByte);
        WriteByte((byte)markerCode);
    }

    private void WriteByte(byte value)
    {
        //ASSERT(byte_offset_ + sizeof(std::byte) <= destination_.size());
        Destination.Span[_position++] = value;
    }

    private void WriteBytes(ReadOnlySpan<byte> data)
    {
        //ASSERT(byte_offset_ + data.size() <= destination_.size());
        data.CopyTo(Destination.Span[_position..]);
        _position += data.Length;
    }

    private void WriteUint16(int value)
    {
        Debug.Assert(value is >= 0 and <= ushort.MaxValue);
        BinaryPrimitives.WriteUInt16BigEndian(Destination.Span[_position..], (ushort)value);
        _position += sizeof(ushort);
    }

    private void WriteUint32(int value)
    {
        Debug.Assert(value >= 0);
        BinaryPrimitives.WriteUInt32BigEndian(Destination.Span[_position..], (uint)value);
        _position += sizeof(uint);
    }

    private byte MappingTableSelector()
    {
        return 0;
        // return table_ids_.empty()? 0 : table_ids_[component_index_];
    }
}
