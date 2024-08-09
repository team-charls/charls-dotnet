// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using Xunit;

namespace CharLS.JpegLS.Test;

public class JpegStreamWriterTest
{
    [Fact]
    public void RemainingDestinationWillBeZeroAfterCreateWithDefault()
    {
        JpegStreamWriter writer = new JpegStreamWriter();

        Assert.Equal(0, writer.GetRemainingDestination().Length);
    }

    [Fact]
    public void WriteStartOfImage()
    {
        var buffer = new byte[2];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        writer.WriteStartOfImage();

        Assert.Equal(2, writer.BytesWritten);
        Assert.Equal(0xFF, buffer[0]);
        Assert.Equal((byte)JpegMarkerCode.StartOfImage, buffer[1]);
    }

    //TEST_METHOD(write_start_of_image_in_too_small_buffer_throws) // NOLINT
    //{
    //    array < byte, 1 > buffer{ };
    //    jpeg_stream_writer writer({ buffer.data(), buffer.size()});

    //    assert_expect_exception(jpegls_errc::destination_buffer_too_small, [&writer] { writer.write_start_of_image(); });
    //    Assert::AreEqual(size_t{ }, writer.bytes_written());
    //}

    [Fact]
    public void WriteEndOfImage()
    {
        var buffer = new byte[2];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        writer.WriteEndOfImage(false);

        Assert.Equal(2, writer.BytesWritten);
        Assert.Equal((byte)0xFF, buffer[0]);
        Assert.Equal((byte)JpegMarkerCode.EndOfImage, buffer[1]);
    }

    [Fact]
    public void WriteEndOfImageEvenNoExtraByteNeeded()
    {
        var buffer = new byte[2];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        writer.WriteEndOfImage(true);

        Assert.Equal(2, writer.BytesWritten);
        Assert.Equal((byte)0xFF, buffer[0]);
        Assert.Equal((byte)JpegMarkerCode.EndOfImage, buffer[1]);
    }

    [Fact]
    public void WriteEndOfImageEvenExtraByteNeeded()
    {
        var buffer = new byte[5 + 3];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        // writer.
        byte[] comment = [99];
        writer.WriteCommentSegment(comment);
        writer.WriteEndOfImage(true);

        Assert.Equal(8, writer.BytesWritten);
        Assert.Equal(0xFF, buffer[0]);
        Assert.Equal((byte)JpegMarkerCode.Comment, buffer[1]);
        Assert.Equal(0, buffer[2]);
        Assert.Equal(3, buffer[3]);
        Assert.Equal(99, buffer[4]);
        Assert.Equal(0xFF, buffer[5]);
        Assert.Equal(0xFF, buffer[6]);
        Assert.Equal((byte)JpegMarkerCode.EndOfImage, buffer[7]);
    }

    [Fact]
    public void WriteEndOfImageEvenExtraByteNeededNotEnabled()
    {
        var buffer = new byte[5 + 2];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        // writer.
        byte[] comment = [99];
        writer.WriteCommentSegment(comment);
        writer.WriteEndOfImage(false);

        Assert.Equal(7, writer.BytesWritten);
        Assert.Equal(0xFF, buffer[0]);
        Assert.Equal((byte)JpegMarkerCode.Comment, buffer[1]);
        Assert.Equal(0, buffer[2]);
        Assert.Equal(3, buffer[3]);
        Assert.Equal(99, buffer[4]);
        Assert.Equal(0xFF, buffer[5]);
        Assert.Equal((byte)JpegMarkerCode.EndOfImage, buffer[6]);
    }

    //TEST_METHOD(write_end_of_image_in_too_small_buffer_throws) // NOLINT
    //{
    //    array < byte, 1 > buffer{ };
    //    jpeg_stream_writer writer({ buffer.data(), buffer.size()});

    //    assert_expect_exception(jpegls_errc::destination_buffer_too_small, [&writer] { writer.write_end_of_image(false); });
    //    Assert::AreEqual(size_t{ }, writer.bytes_written());
    //}

    //TEST_METHOD(write_spiff_segment) // NOLINT
    //{
    //    array < byte, 34 > buffer{ };
    //    jpeg_stream_writer writer({ buffer.data(), buffer.size()});

    //    constexpr spiff_header header{
    //        spiff_profile_id::none,
    //                                  3,
    //                                  800,
    //                                  600,
    //                                  spiff_color_space::rgb,
    //                                  8,
    //                                  spiff_compression_type::jpeg_ls,
    //                                  spiff_resolution_units::dots_per_inch,
    //                                  96,
    //                                  1024};

    //    writer.write_spiff_header_segment(header);

    //    Assert::AreEqual(size_t{ 34}, writer.bytes_written());

    //    Assert::AreEqual(byte{ 0xFF}, buffer[0]);
    //    Assert::AreEqual(static_cast<byte>(jpeg_marker_code::application_data8), buffer[1]);

    //    Assert::AreEqual({ }, buffer[2]);
    //    Assert::AreEqual(byte{ 32}, buffer[3]);

    //    // Verify SPIFF identifier string.
    //    Assert::AreEqual(byte{ 'S'}, buffer[4]);
    //    Assert::AreEqual(byte{ 'P'}, buffer[5]);
    //    Assert::AreEqual(byte{ 'I'}, buffer[6]);
    //    Assert::AreEqual(byte{ 'F'}, buffer[7]);
    //    Assert::AreEqual(byte{ 'F'}, buffer[8]);
    //    Assert::AreEqual({ }, buffer[9]);

    //    // Verify version
    //    Assert::AreEqual(byte{ 2}, buffer[10]);
    //    Assert::AreEqual({ }, buffer[11]);

    //    Assert::AreEqual(static_cast<byte>(header.profile_id), buffer[12]);
    //    Assert::AreEqual(static_cast<byte>(header.component_count), buffer[13]);

    //    // Height
    //    Assert::AreEqual({ }, buffer[14]);
    //    Assert::AreEqual({ }, buffer[15]);
    //    Assert::AreEqual(byte{ 0x3}, buffer[16]);
    //    Assert::AreEqual(byte{ 0x20}, buffer[17]);

    //    // Width
    //    Assert::AreEqual({ }, buffer[18]);
    //    Assert::AreEqual({ }, buffer[19]);
    //    Assert::AreEqual(byte{ 0x2}, buffer[20]);
    //    Assert::AreEqual(byte{ 0x58}, buffer[21]);

    //    Assert::AreEqual(static_cast<byte>(header.color_space), buffer[22]);
    //    Assert::AreEqual(static_cast<byte>(header.bits_per_sample), buffer[23]);
    //    Assert::AreEqual(static_cast<byte>(header.compression_type), buffer[24]);
    //    Assert::AreEqual(static_cast<byte>(header.resolution_units), buffer[25]);

    //    // vertical_resolution
    //    Assert::AreEqual({ }, buffer[26]);
    //    Assert::AreEqual({ }, buffer[27]);
    //    Assert::AreEqual({ }, buffer[28]);
    //    Assert::AreEqual(byte{ 96}, buffer[29]);

    //    // header.horizontal_resolution = 1024
    //    Assert::AreEqual({ }, buffer[30]);
    //    Assert::AreEqual({ }, buffer[31]);
    //    Assert::AreEqual(byte{ 4}, buffer[32]);
    //    Assert::AreEqual(byte{ }, buffer[33]);
    //}

    //TEST_METHOD(write_spiff_segment_in_too_small_buffer_throws) // NOLINT
    //{
    //    array < byte, 33 > buffer{ };
    //    jpeg_stream_writer writer({ buffer.data(), buffer.size()});

    //    spiff_header header{
    //        spiff_profile_id::none,
    //                        3,
    //                        800,
    //                        600,
    //                        spiff_color_space::rgb,
    //                        8,
    //                        spiff_compression_type::jpeg_ls,
    //                        spiff_resolution_units::dots_per_inch,
    //                        96,
    //                        1024};

    //    assert_expect_exception(jpegls_errc::destination_buffer_too_small,
    //                            [&writer, &header] { writer.write_spiff_header_segment(header); });
    //    Assert::AreEqual(size_t{ }, writer.bytes_written());
    //}

    //TEST_METHOD(write_spiff_end_of_directory_segment) // NOLINT
    //{
    //    array < byte, 10 > buffer{ };
    //    jpeg_stream_writer writer({ buffer.data(), buffer.size()});

    //    writer.write_spiff_end_of_directory_entry();

    //    Assert::AreEqual(size_t{ 10}, writer.bytes_written());

    //    // Verify Entry Magic Number (EMN)
    //    Assert::AreEqual(byte{ 0xFF}, buffer[0]);
    //    Assert::AreEqual(static_cast<byte>(jpeg_marker_code::application_data8), buffer[1]);

    //    // Verify EOD Entry Length (EOD = End Of Directory)
    //    Assert::AreEqual(byte{ }, buffer[2]);
    //    Assert::AreEqual(byte{ 8}, buffer[3]);

    //    // Verify EOD Tag
    //    Assert::AreEqual({ }, buffer[4]);
    //    Assert::AreEqual(byte{ }, buffer[5]);
    //    Assert::AreEqual(byte{ }, buffer[6]);
    //    Assert::AreEqual(byte{ 1}, buffer[7]);

    //    // Verify embedded SOI tag
    //    Assert::AreEqual(byte{ 0xFF}, buffer[8]);
    //    Assert::AreEqual(static_cast<byte>(jpeg_marker_code::start_of_image), buffer[9]);
    //}

    //TEST_METHOD(write_spiff_directory_entry) // NOLINT
    //{
    //    array < byte, 10 > buffer{ };
    //    jpeg_stream_writer writer{ { buffer.data(), buffer.size()} };

    //    constexpr array data{ byte{ 0x77}, byte{ 0x66} };

    //    writer.write_spiff_directory_entry(2, data);

    //    // Verify Entry Magic Number (EMN)
    //    Assert::AreEqual(byte{ 0xFF}, buffer[0]);
    //    Assert::AreEqual(static_cast<byte>(jpeg_marker_code::application_data8), buffer[1]);

    //    // Verify Entry Length
    //    Assert::AreEqual({ }, buffer[2]);
    //    Assert::AreEqual(byte{ 8}, buffer[3]);

    //    // Verify Entry Tag
    //    Assert::AreEqual({ }, buffer[4]);
    //    Assert::AreEqual({ }, buffer[5]);
    //    Assert::AreEqual({ }, buffer[6]);
    //    Assert::AreEqual(byte{ 2}, buffer[7]);

    //    // Verify embedded data
    //    Assert::AreEqual(data[0], buffer[8]);
    //    Assert::AreEqual(data[1], buffer[9]);
    //}

    [Fact]
    public void TestWriteStartOfFrameSegment()
    {
        const int bitsPerSample = 8;
        const int componentCount = 3;

        var buffer = new byte[19];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        bool oversizedImage =
            writer.WriteStartOfFrameSegment(new FrameInfo(100, ushort.MaxValue, bitsPerSample, componentCount));

        Assert.False(oversizedImage);
        Assert.Equal(19, writer.BytesWritten);

        Assert.Equal(0xFF, buffer[0]);
        Assert.Equal(0xF7, buffer[1]); // JPEG_SOF_55
        Assert.Equal(0, buffer[2]);     // 6 + (3 * 3) + 2 (in big endian)
        Assert.Equal(17, buffer[3]);   // 6 + (3 * 3) + 2 (in big endian)
        Assert.Equal(bitsPerSample, buffer[4]);
        Assert.Equal(255, buffer[5]); // height (in big endian)
        Assert.Equal(255, buffer[6]); // height (in big endian)
        Assert.Equal(0, buffer[7]);    // width (in big endian)
        Assert.Equal(100, buffer[8]); // width (in big endian)
        Assert.Equal(componentCount, buffer[9]);

        Assert.Equal(1, buffer[10]);
        Assert.Equal(0x11, buffer[11]);
        Assert.Equal(0, buffer[12]);

        Assert.Equal(2, buffer[13]);
        Assert.Equal(0x11, buffer[14]);
        Assert.Equal(0, buffer[15]);

        Assert.Equal(3, buffer[16]);
        Assert.Equal(0x11, buffer[17]);
        Assert.Equal(0, buffer[18]);
    }

    //TEST_METHOD(write_start_of_frame_segment_large_image) // NOLINT
    //{
    //    constexpr int bits_per_sample{ 8};
    //    constexpr int component_count{ 3};

    //    array < byte, 19 > buffer{ };
    //    jpeg_stream_writer writer({ buffer.data(), buffer.size()});

    //    const bool oversized_image{
    //        writer.write_start_of_frame_segment(
    //        { 100, numeric_limits < uint16_t >::max() + 1U, bits_per_sample, component_count})};

    //    Assert::IsTrue(oversized_image);
    //    Assert::AreEqual(size_t{ 19}, writer.bytes_written());

    //    Assert::AreEqual(byte{ 0xFF}, buffer[0]);
    //    Assert::AreEqual(byte{ 0xF7}, buffer[1]); // JPEG_SOF_55
    //    Assert::AreEqual(byte{ }, buffer[2]);     // 6 + (3 * 3) + 2 (in big endian)
    //    Assert::AreEqual(byte{ 17}, buffer[3]);   // 6 + (3 * 3) + 2 (in big endian)
    //    Assert::AreEqual(static_cast<byte>(bits_per_sample), buffer[4]);
    //    Assert::AreEqual(byte{ }, buffer[5]); // height (in big endian)
    //    Assert::AreEqual(byte{ }, buffer[6]); // height (in big endian)
    //    Assert::AreEqual(byte{ }, buffer[7]); // width (in big endian)
    //    Assert::AreEqual(byte{ }, buffer[8]); // width (in big endian)
    //    Assert::AreEqual(static_cast<byte>(component_count), buffer[9]);

    //    Assert::AreEqual(byte{ 1}, buffer[10]);
    //    Assert::AreEqual(byte{ 0x11}, buffer[11]);
    //    Assert::AreEqual(byte{ }, buffer[12]);

    //    Assert::AreEqual(byte{ 2}, buffer[13]);
    //    Assert::AreEqual(byte{ 0x11}, buffer[14]);
    //    Assert::AreEqual(byte{ }, buffer[15]);

    //    Assert::AreEqual(byte{ 3}, buffer[16]);
    //    Assert::AreEqual(byte{ 0x11}, buffer[17]);
    //    Assert::AreEqual(byte{ }, buffer[18]);
    //}

    [Fact]
    public void WriteStartOfFrameMarkerSegmentWithLowBoundaryValues()
    {
        const int bitsPerSample = 2;
        const int componentCount = 1;
        var buffer = new byte[13];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        writer.WriteStartOfFrameSegment(new FrameInfo(1, 1, bitsPerSample, componentCount));

        Assert.Equal(buffer.Length, writer.BytesWritten);
        Assert.Equal(bitsPerSample, buffer[4]);
        Assert.Equal(componentCount, buffer[9]);
    }

    //TEST_METHOD(write_start_of_frame_marker_segment_with_high_boundary_values_and_serialize) // NOLINT
    //{
    //    array < byte, 775 > buffer{ };
    //    jpeg_stream_writer writer({ buffer.data(), buffer.size()});
    //    writer.write_start_of_frame_segment(
    //        { numeric_limits < uint16_t >::max(), numeric_limits < uint16_t >::max(), 16, numeric_limits < uint8_t >::max()});

    //    Assert::AreEqual(buffer.size(), writer.bytes_written());
    //    Assert::AreEqual(byte{ 16}, buffer[4]);
    //    Assert::AreEqual(numeric_limits < uint8_t >::max(), to_integer<uint8_t>(buffer[9]));
    //    Assert::AreEqual(numeric_limits < uint8_t >::max(),
    //                     to_integer<uint8_t>(buffer[buffer.size() - 3])); // Last component index.
    //}

    //TEST_METHOD(write_color_transform_segment) // NOLINT
    //{
    //    constexpr color_transformation transformation = color_transformation::hp1;

    //    array < byte, 9 > buffer{ };
    //    jpeg_stream_writer writer({ buffer.data(), buffer.size()});

    //    writer.write_color_transform_segment(transformation);
    //    Assert::AreEqual(buffer.size(), writer.bytes_written());

    //    // Verify mrfx identifier string.
    //    Assert::AreEqual(byte{ 'm'}, buffer[4]);
    //    Assert::AreEqual(byte{ 'r'}, buffer[5]);
    //    Assert::AreEqual(byte{ 'f'}, buffer[6]);
    //    Assert::AreEqual(byte{ 'x'}, buffer[7]);

    //    Assert::AreEqual(static_cast<byte>(transformation), buffer[8]);
    //}

    //TEST_METHOD(write_jpegls_extended_parameters_marker_and_serialize) // NOLINT
    //{
    //    constexpr jpegls_pc_parameters presets{ 2, 1, 2, 3, 7};

    //    array < byte, 15 > buffer{ };
    //    jpeg_stream_writer writer({ buffer.data(), buffer.size()});

    //    writer.write_jpegls_preset_parameters_segment(presets);
    //    Assert::AreEqual(buffer.size(), writer.bytes_written());

    //    // Parameter ID.
    //    Assert::AreEqual(byte{ 0x1}, buffer[4]);

    //    // MaximumSampleValue
    //    Assert::AreEqual({ }, buffer[5]);
    //    Assert::AreEqual(byte{ 2}, buffer[6]);

    //    // Threshold1
    //    Assert::AreEqual({ }, buffer[7]);
    //    Assert::AreEqual(byte{ 1}, buffer[8]);

    //    // Threshold2
    //    Assert::AreEqual({ }, buffer[9]);
    //    Assert::AreEqual(byte{ 2}, buffer[10]);

    //    // Threshold3
    //    Assert::AreEqual({ }, buffer[11]);
    //    Assert::AreEqual(byte{ 3}, buffer[12]);

    //    // ResetValue
    //    Assert::AreEqual({ }, buffer[13]);
    //    Assert::AreEqual(byte{ 7}, buffer[14]);
    //}

    //TEST_METHOD(write_jpegls_preset_parameters_segment_for_oversized_image_dimensions) // NOLINT
    //{
    //    array < byte, 14 > buffer{ };
    //    jpeg_stream_writer writer({ buffer.data(), buffer.size()});

    //    writer.write_jpegls_preset_parameters_segment(100, numeric_limits < uint32_t >::max());
    //    Assert::AreEqual(buffer.size(), writer.bytes_written());

    //    // Parameter ID.
    //    Assert::AreEqual(byte{ 0x4}, buffer[4]);

    //    // Wxy
    //    Assert::AreEqual(byte{ 4}, buffer[5]);

    //    // Height (in big endian)
    //    Assert::AreEqual({ }, buffer[6]);
    //    Assert::AreEqual({ }, buffer[7]);
    //    Assert::AreEqual({ }, buffer[8]);
    //    Assert::AreEqual(byte{ 100}, buffer[9]);

    //    // Width (in big endian)
    //    Assert::AreEqual(byte{ 255}, buffer[10]);
    //    Assert::AreEqual(byte{ 255}, buffer[11]);
    //    Assert::AreEqual(byte{ 255}, buffer[12]);
    //    Assert::AreEqual(byte{ 255}, buffer[13]);
    //}

    [Fact]
    public void WriteStartOfScanMarker()
    {
        var buffer = new byte[10];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        writer.WriteStartOfScanSegment(1, 2, InterleaveMode.None);

        Assert.Equal(buffer.Length, writer.BytesWritten);
        Assert.Equal(1, buffer[4]); // component count.
        Assert.Equal(1, buffer[5]); // component index.
        Assert.Equal(0, buffer[6]); // table ID.
        Assert.Equal(2, buffer[7]); // NEAR parameter.
        Assert.Equal(0, buffer[8]); // ILV parameter.
        Assert.Equal(0, buffer[9]); // transformation.
    }

    //TEST_METHOD(rewind) // NOLINT
    //{
    //    array < byte, 10 > buffer{ };
    //    jpeg_stream_writer writer({ buffer.data(), buffer.size()});

    //    writer.write_start_of_scan_segment(1, 2, interleave_mode::none);
    //    writer.rewind();
    //    buffer[4] = { };
    //    writer.write_start_of_scan_segment(1, 2, interleave_mode::none);

    //    Assert::AreEqual(buffer.size(), writer.bytes_written());
    //    Assert::AreEqual(byte{ 1}, buffer[4]); // component count.
    //}

    //TEST_METHOD(write_minimal_table) // NOLINT
    //{
    //    array < byte, 8 > buffer{ };
    //    jpeg_stream_writer writer({ buffer.data(), buffer.size()});

    //    constexpr array table_data{ byte{ 77} };
    //    writer.write_jpegls_preset_parameters_segment(100, 1, table_data);

    //    Assert::AreEqual(buffer.size(), writer.bytes_written());
    //    Assert::AreEqual(byte{ 0xFF}, buffer[0]);
    //    Assert::AreEqual(byte{ 0xF8}, buffer[1]); // LSE
    //    Assert::AreEqual(byte{ 0}, buffer[2]);
    //    Assert::AreEqual(byte{ 6}, buffer[3]);
    //    Assert::AreEqual(byte{ 2}, buffer[4]);   // type = table
    //    Assert::AreEqual(byte{ 100}, buffer[5]); // table ID
    //    Assert::AreEqual(byte{ 1}, buffer[6]);   // size of entry
    //    Assert::AreEqual(byte{ 77}, buffer[7]);  // table content
    //}

    //TEST_METHOD(write_table_max_entry_size) // NOLINT
    //{
    //    array < byte, 7 + 255 > buffer{ };
    //    jpeg_stream_writer writer({ buffer.data(), buffer.size()});

    //    constexpr array<byte, 255 > table_data{ };
    //    writer.write_jpegls_preset_parameters_segment(100, 255, table_data);

    //    Assert::AreEqual(buffer.size(), writer.bytes_written());
    //    Assert::AreEqual(byte{ 0xFF}, buffer[0]);
    //    Assert::AreEqual(byte{ 0xF8}, buffer[1]); // LSE
    //    Assert::AreEqual(byte{ 1}, buffer[2]);
    //    Assert::AreEqual(byte{ 4}, buffer[3]);
    //    Assert::AreEqual(byte{ 2}, buffer[4]);   // type = table
    //    Assert::AreEqual(byte{ 100}, buffer[5]); // table ID
    //    Assert::AreEqual(byte{ 255}, buffer[6]); // size of entry
    //    Assert::AreEqual(byte{ 0}, buffer[7]);   // table content
    //}

    //TEST_METHOD(write_table_fits_in_single_segment) // NOLINT
    //{
    //    std::vector<byte> buffer(size_t{ 2}
    //    +std::numeric_limits < uint16_t >::max());
    //    jpeg_stream_writer writer({ buffer.data(), buffer.size()});

    //    std::vector<byte> table_data(std::numeric_limits<uint16_t>::max() -5);
    //    writer.write_jpegls_preset_parameters_segment(255, 1, { table_data.data(), table_data.size()});

    //    Assert::AreEqual(buffer.size(), writer.bytes_written());
    //    Assert::AreEqual(byte{ 0xFF}, buffer[0]);
    //    Assert::AreEqual(byte{ 0xF8}, buffer[1]); // LSE
    //    Assert::AreEqual(byte{ 255}, buffer[2]);
    //    Assert::AreEqual(byte{ 255}, buffer[3]);
    //    Assert::AreEqual(byte{ 2}, buffer[4]);   // type = table
    //    Assert::AreEqual(byte{ 255}, buffer[5]); // table ID
    //    Assert::AreEqual(byte{ 1}, buffer[6]);   // size of entry
    //    Assert::AreEqual(byte{ 0}, buffer[7]);   // table content (first entry)
    //}

    //TEST_METHOD(write_table_that_requires_two_segment) // NOLINT
    //{
    //    std::vector<byte> buffer(size_t{ 2}
    //    +std::numeric_limits < uint16_t >::max() + 8);
    //    jpeg_stream_writer writer({ buffer.data(), buffer.size()});

    //    std::vector<byte> table_data(static_cast<size_t>(std::numeric_limits<uint16_t>::max()) -5 + 1);
    //    writer.write_jpegls_preset_parameters_segment(255, 1, { table_data.data(), table_data.size()});

    //    Assert::AreEqual(buffer.size(), writer.bytes_written());
    //    Assert::AreEqual(byte{ 0xFF}, buffer[0]);
    //    Assert::AreEqual(byte{ 0xF8}, buffer[1]); // LSE
    //    Assert::AreEqual(byte{ 255}, buffer[2]);
    //    Assert::AreEqual(byte{ 255}, buffer[3]);
    //    Assert::AreEqual(byte{ 2}, buffer[4]);   // type = table
    //    Assert::AreEqual(byte{ 255}, buffer[5]); // table ID
    //    Assert::AreEqual(byte{ 1}, buffer[6]);   // size of entry
    //    Assert::AreEqual(byte{ 0}, buffer[7]);   // table content (first entry)

    //    // Validate second segment.
    //    Assert::AreEqual(byte{ 0xFF}, buffer[65537]);
    //    Assert::AreEqual(byte{ 0xF8}, buffer[65538]); // LSE
    //    Assert::AreEqual(byte{ }, buffer[65539]);
    //    Assert::AreEqual(byte{ 6}, buffer[65540]);
    //    Assert::AreEqual(byte{ 3}, buffer[65541]);   // type = table
    //    Assert::AreEqual(byte{ 255}, buffer[65542]); // table ID
    //    Assert::AreEqual(byte{ 1}, buffer[65543]);   // size of entry
    //    Assert::AreEqual(byte{ 0}, buffer[65544]);   // table content (last entry)
    //}

}