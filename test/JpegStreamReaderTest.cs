// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using Xunit;

namespace CharLS.JpegLS.Test;

public class JpegStreamReaderTest
{
    [Fact]
    public void ReadHeaderFromToSmallInputBuffer()
    {
        var buffer = Array.Empty<byte>();

        var reader = new JpegStreamReader { Source = buffer };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);
        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(JpegLSError.SourceBufferTooSmall, exception.Data[nameof(JpegLSError)]);
    }

    [Fact(Skip = "WIP")]
    public void ReadHeaderFromBufferPrecededWithFillBytes()
    {
        const byte extraStartByte = 0xFF;
        JpegTestStreamWriter writer = new();

        writer.WriteByte(extraStartByte);
        writer.WriteStartOfImage();

        writer.WriteByte(extraStartByte);
        writer.WriteStartOfFrameSegment(1, 1, 2, 1);

        writer.WriteByte(extraStartByte);
        writer.WriteStartOfScanSegment(0, 1, 128, JpegLSInterleaveMode.None);

        var reader = new JpegStreamReader { Source = writer.GetBuffer() };

        reader.ReadHeader();  // if it doesn't throw test is passed.
    }

    [Fact]
    public void ReadHeaderFromBufferNotStartingWithStartByteShouldThrow()
    {
        byte[] buffer = [0x0F, 0xFF, 0xD8, 0xFF, 0xFF, 0xDA];

        var reader = new JpegStreamReader { Source = buffer };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);
        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(JpegLSError.JpegMarkerStartByteNotFound, exception.Data[nameof(JpegLSError)]);
    }

    [Fact(Skip = "WIP")]
    public void ReadHeaderWithApplicationData()
    {
        ReadHeaderWithApplicationDataImpl(0);
        ReadHeaderWithApplicationDataImpl(1);
        ReadHeaderWithApplicationDataImpl(2);
        ReadHeaderWithApplicationDataImpl(3);
        ReadHeaderWithApplicationDataImpl(4);
        ReadHeaderWithApplicationDataImpl(5);
        ReadHeaderWithApplicationDataImpl(6);
        ReadHeaderWithApplicationDataImpl(7);
        ReadHeaderWithApplicationDataImpl(8);
        ReadHeaderWithApplicationDataImpl(9);
        ReadHeaderWithApplicationDataImpl(10);
        ReadHeaderWithApplicationDataImpl(11);
        ReadHeaderWithApplicationDataImpl(12);
        ReadHeaderWithApplicationDataImpl(13);
        ReadHeaderWithApplicationDataImpl(14);
        ReadHeaderWithApplicationDataImpl(15);
    }

    [Fact]
    public void ReadHeaderWithJpegLSExtendedFrameShouldThrow()
    {
        // 0xF9 = SOF_57: Marks the start of a JPEG-LS extended (ISO/IEC 14495-2) encoded frame.
        byte[] buffer = [0xFF, 0xD8, 0xFF, 0xF9];

        var reader = new JpegStreamReader { Source = buffer };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);
        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(JpegLSError.EncodingNotSupported, exception.Data[nameof(JpegLSError)]);
    }

    [Fact(Skip = "WIP")]
    public void ReadHeaderJpegLSPresetParameterSegment()
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();

        JpegLSPresetCodingParameters presets = new(1, 2, 3, 4, 5);
        writer.WriteJpegLSPresetParametersSegment(presets);
        writer.WriteStartOfFrameSegment(1, 1, 2, 1);
        writer.WriteStartOfScanSegment(1, 0, 0, JpegLSInterleaveMode.None);

        var reader = new JpegStreamReader { Source = writer.GetBuffer() };

        reader.ReadHeader();

        JpegLSPresetCodingParameters? actual = reader.JpegLSPresetCodingParameters;
        Assert.NotNull(actual);

        Assert.Equal(presets.MaximumSampleValue, actual.MaximumSampleValue);
        Assert.Equal(presets.ResetValue, actual.ResetValue);
        Assert.Equal(presets.Threshold1, actual.Threshold1);
        Assert.Equal(presets.Threshold2, actual.Threshold2);
        Assert.Equal(presets.Threshold3, actual.Threshold3);
    }

    [Fact]
    public void ReadHeaderWithTooSmallJpegLSPresetParameterSegmentShouldThrow()
    {
        byte[] buffer =
        [
            0xFF, 0xD8, 0xFF,
            0xF8, // LSE: Marks the start of a JPEG-LS preset parameters segment.
            0x00, 0x02, 0x01
        ];

        var reader = new JpegStreamReader { Source = buffer };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);
        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(JpegLSError.InvalidMarkerSegmentSize, exception.Data[nameof(JpegLSError)]);
    }

    [Fact]
    public void ReadHeaderWithTooSmallJpegLSPresetParameterSegmentWithCodingParametersShouldThrow()
    {
        var buffer = new byte[]{
            0xFF, 0xD8, 0xFF,
            0xF8, // LSE: Marks the start of a JPEG-LS preset parameters segment.
            0x00, 0x0A, 0x01};

        var reader = new JpegStreamReader { Source = buffer };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);
        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(JpegLSError.InvalidMarkerSegmentSize, exception.Data[nameof(JpegLSError)]);
    }

    [Fact]
    public void ReadHeaderWithTooLargeJpegLSPresetParameterSegmentWithCodingParametersShouldThrow()
    {
        var buffer = new byte[]{
            0xFF, 0xD8, 0xFF,
            0xF8, // LSE: Marks the start of a JPEG-LS preset parameters segment.
            0x00, 0x0C, 0x01};

        var reader = new JpegStreamReader { Source = buffer };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);
        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(JpegLSError.InvalidMarkerSegmentSize, exception.Data[nameof(JpegLSError)]);
    }

    [Fact]
    public void ReadHeaderWithJpegLSPresetParameterWithExtendedIdShouldThrow()
    {
        var ids = new byte[] { 0x5, 0x6, 0x7, 0x8, 0x9, 0xA, 0xC, 0xD };

        foreach (var id in ids)
        {
            ReadHeaderWithJpegLSPresetParameterWithExtendedIdShouldThrowImpl(id);
        }
    }

    [Fact]
    public void ReadHeaderWithTooSmallSegmentSizeShouldThrow()
    {
        var buffer = new byte[]{
            0xFF, 0xD8, 0xFF,
            0xF7,                    // SOF_55: Marks the start of JPEG-LS extended scan.
            0x00, 0x01, 0xFF, 0xDA}; // SOS: Marks the start of scan.

        var reader = new JpegStreamReader { Source = buffer };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);
        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(JpegLSError.InvalidMarkerSegmentSize, exception.Data[nameof(JpegLSError)]);
    }

    [Fact]
    public void ReadHeaderWithTooSmallStartOfFrameShouldThrow()
    {
        byte[] buffer =
        [
            0xFF, 0xD8, 0xFF,
            0xF7, // SOF_55: Marks the start of JPEG-LS extended scan.
            0x00, 0x07
        ];

        var reader = new JpegStreamReader { Source = buffer };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);
        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(JpegLSError.InvalidMarkerSegmentSize, exception.Data[nameof(JpegLSError)]);
    }

    [Fact]
    public void ReadHeaderWithTooSmallStartOfFrameInComponentInfoShouldThrow()
    {
        byte[] buffer =
        [
            0xFF, 0xD8, 0xFF,
            0xF7, // SOF_55: Marks the start of JPEG-LS extended scan.
            0x00, 0x07
        ];

        var reader = new JpegStreamReader { Source = buffer };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);
        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(JpegLSError.InvalidMarkerSegmentSize, exception.Data[nameof(JpegLSError)]);
    }

    [Fact]
    public void ReadHeaderWithTooLargeStartOfFrameShouldThrow()
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteStartOfFrameSegment(512, 512, 8, 3);
        writer.WriteByte(0);
        var buffer = writer.GetModifiableBuffer();
        buffer[5]++;
        var reader = new JpegStreamReader { Source = buffer.ToArray() };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(JpegLSError.InvalidMarkerSegmentSize, exception.Data[nameof(JpegLSError)]);
    }

    [Fact]
    public void ReadHeaderSosBeforeSofShouldThrow()
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteStartOfScanSegment(0, 1, 128, JpegLSInterleaveMode.None);
        var reader = new JpegStreamReader { Source = writer.GetBuffer() };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(JpegLSError.UnexpectedMarkerFound, exception.Data[nameof(JpegLSError)]);
    }

    [Fact]
    public void ReadHeaderExtraSofShouldThrow()
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteStartOfFrameSegment(512, 512, 8, 3);
        writer.WriteStartOfFrameSegment(512, 512, 8, 3);
        var reader = new JpegStreamReader { Source = writer.GetBuffer() };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(JpegLSError.DuplicateStartOfFrameMarker, exception.Data[nameof(JpegLSError)]);
    }

    [Fact]
    public void ReadHeaderTooLargeNearLosslessInSosShouldThrow()
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteStartOfFrameSegment(512, 512, 8, 3);
        writer.WriteStartOfScanSegment(0, 1, 128, JpegLSInterleaveMode.None);
        var reader = new JpegStreamReader { Source = writer.GetBuffer() };

        // TODO: enable
        //var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);

        //Assert.False(string.IsNullOrEmpty(exception.Message));
        //Assert.Equal(JpegLSError.InvalidParameterNearLossless, exception.Data[nameof(JpegLSError)]);
    }

    //    TEST_METHOD(read_header_too_large_near_lossless_in_sos_should_throw2) // NOLINT
    //    {
    //        constexpr jpegls_pc_parameters preset_coding_parameters{ 200, 0, 0, 0, 0};

    //        jpeg_test_stream_writer writer;
    //        writer.write_start_of_image();
    //        writer.write_jpegls_preset_parameters_segment(preset_coding_parameters);
    //        writer.write_start_of_frame_segment(512, 512, 8, 3);

    //        constexpr int bad_near_lossless = (200 / 2) + 1;
    //        writer.write_start_of_scan_segment(0, 1, bad_near_lossless, interleave_mode::none);

    //        jpeg_stream_reader reader;
    //        reader.source({ writer.buffer.data(), writer.buffer.size()});
    //        reader.read_header();

    //        assert_expect_exception(jpegls_errc::invalid_parameter_near_lossless, [&reader] { reader.read_start_of_scan(); });
    //    }

    //    TEST_METHOD(read_header_line_interleave_in_sos_for_single_component_should_throw) // NOLINT
    //    {
    //        read_header_incorrect_interleave_in_sos_for_single_component_should_throw(interleave_mode::line);
    //    }

    //    TEST_METHOD(read_header_sample_interleave_in_sos_for_single_component_should_throw) // NOLINT
    //    {
    //        read_header_incorrect_interleave_in_sos_for_single_component_should_throw(interleave_mode::sample);
    //    }

    //    TEST_METHOD(read_header_with_duplicate_component_id_in_start_of_frame_segment_should_throw) // NOLINT
    //    {
    //        jpeg_test_stream_writer writer;
    //        writer.componentIdOverride = 7;
    //        writer.write_start_of_image();
    //        writer.write_start_of_frame_segment(512, 512, 8, 3);

    //        jpeg_stream_reader reader;
    //        reader.source({ writer.buffer.data(), writer.buffer.size()});

    //        assert_expect_exception(jpegls_errc::duplicate_component_id_in_sof_segment, [&reader] { reader.read_header(); });
    //    }

    //    TEST_METHOD(read_header_with_too_small_start_of_scan_should_throw) // NOLINT
    //    {
    //        array < uint8_t, 16 > buffer{
    //            0xFF, 0xD8, 0xFF,
    //                                  0xF7, // SOF_55: Marks the start of JPEG-LS extended scan.
    //                                  0x00,
    //                                  0x08, // size
    //                                  0x08, // bits per sample
    //                                  0x00,
    //                                  0x01, // width
    //                                  0x00,
    //                                  0x01, // height
    //                                  0x01, // component count
    //                                  0xFF,
    //                                  0xDA, // SOS
    //                                  0x00, 0x03};

    //        jpeg_stream_reader reader;
    //        reader.source({ buffer.data(), buffer.size()});

    //        assert_expect_exception(jpegls_errc::invalid_marker_segment_size, [&reader] { reader.read_header(); });
    //    }

    //    TEST_METHOD(read_header_with_too_small_start_of_scan_component_count_should_throw) // NOLINT
    //    {
    //        array < uint8_t, 17 > buffer{
    //            0xFF, 0xD8, 0xFF,
    //                                  0xF7, // SOF_55: Marks the start of JPEG-LS extended scan.
    //                                  0x00,
    //                                  0x08, // size
    //                                  0x08, // bits per sample
    //                                  0x00,
    //                                  0x01, // width
    //                                  0x00,
    //                                  0x01, // height
    //                                  0x01, // component count
    //                                  0xFF,
    //                                  0xDA, // SOS
    //                                  0x00, 0x07, 0x01};

    //        jpeg_stream_reader reader;
    //        reader.source({ buffer.data(), buffer.size()});

    //        assert_expect_exception(jpegls_errc::invalid_marker_segment_size, [&reader] { reader.read_header(); });
    //    }

    //    TEST_METHOD(read_header_with_directly_end_of_image_should_throw) // NOLINT
    //    {
    //        array < uint8_t, 4 > buffer{ 0xFF, 0xD8, 0xFF, 0xD9}; // 0xD9 = EOI

    //        jpeg_stream_reader reader;
    //        reader.source({ buffer.data(), buffer.size()});

    //        assert_expect_exception(jpegls_errc::unexpected_end_of_image_marker, [&reader] { reader.read_header(); });
    //    }

    //    TEST_METHOD(read_header_with_duplicate_start_of_image_should_throw) // NOLINT
    //    {
    //        array < uint8_t, 4 > buffer{ 0xFF, 0xD8, 0xFF, 0xD8}; // 0xD8 = SOI.

    //        jpeg_stream_reader reader;
    //        reader.source({ buffer.data(), buffer.size()});

    //        assert_expect_exception(jpegls_errc::duplicate_start_of_image_marker, [&reader] { reader.read_header(); });
    //    }

    //    TEST_METHOD(read_spiff_header) // NOLINT
    //    {
    //        read_spiff_header(0);
    //    }

    //    TEST_METHOD(read_spiff_header_low_version_newer) // NOLINT
    //    {
    //        read_spiff_header(1);
    //    }

    //    TEST_METHOD(read_spiff_header_high_version_to_new) // NOLINT
    //    {
    //        vector<uint8_t> buffer{ create_test_spiff_header(3)};
    //        jpeg_stream_reader reader;
    //        reader.source({ buffer.data(), buffer.size()});

    //        spiff_header spiff_header{ };
    //        bool spiff_header_found{ };

    //        reader.read_header(&spiff_header, &spiff_header_found);

    //        Assert::IsFalse(spiff_header_found);
    //    }

    //    TEST_METHOD(read_spiff_header_without_end_of_directory) // NOLINT
    //    {
    //        vector<uint8_t> buffer = create_test_spiff_header(2, 0, false);
    //        jpeg_stream_reader reader;
    //        reader.source({ buffer.data(), buffer.size()});

    //        spiff_header spiff_header{ };
    //        bool spiff_header_found{ };

    //        reader.read_header(&spiff_header, &spiff_header_found);
    //        Assert::IsTrue(spiff_header_found);

    //        assert_expect_exception(jpegls_errc::missing_end_of_spiff_directory, [&reader] { reader.read_header(); });
    //    }

    //    TEST_METHOD(read_header_with_define_restart_interval_16_bit) // NOLINT
    //    {
    //        jpeg_test_stream_writer writer;
    //        writer.write_start_of_image();
    //        writer.write_start_of_frame_segment(512, 512, 8, 3);
    //        writer.write_define_restart_interval(numeric_limits < uint16_t >::max() - 5, 2);
    //        writer.write_start_of_scan_segment(0, 1, 0, interleave_mode::none);

    //        jpeg_stream_reader reader;
    //        reader.source({ writer.buffer.data(), writer.buffer.size()});
    //        reader.read_header();

    //        Assert::AreEqual(static_cast<uint32_t>(numeric_limits < uint16_t >::max() - 5), reader.parameters().restart_interval);
    //    }

    //    TEST_METHOD(read_header_with_define_restart_interval_24_bit) // NOLINT
    //    {
    //        jpeg_test_stream_writer writer;
    //        writer.write_start_of_image();
    //        writer.write_start_of_frame_segment(512, 512, 8, 3);
    //        writer.write_define_restart_interval(numeric_limits < uint16_t >::max() + 5, 3);
    //        writer.write_start_of_scan_segment(0, 1, 0, interleave_mode::none);

    //        jpeg_stream_reader reader;
    //        reader.source({ writer.buffer.data(), writer.buffer.size()});
    //        reader.read_header();

    //        Assert::AreEqual(static_cast<uint32_t>(numeric_limits < uint16_t >::max() + 5), reader.parameters().restart_interval);
    //    }

    //    TEST_METHOD(read_header_with_define_restart_interval_32_bit) // NOLINT
    //    {
    //        jpeg_test_stream_writer writer;
    //        writer.write_start_of_image();
    //        writer.write_start_of_frame_segment(512, 512, 8, 3);
    //        writer.write_define_restart_interval(numeric_limits < uint32_t >::max() - 7, 4);
    //        writer.write_start_of_scan_segment(0, 1, 0, interleave_mode::none);

    //        jpeg_stream_reader reader;
    //        reader.source({ writer.buffer.data(), writer.buffer.size()});
    //        reader.read_header();

    //        Assert::AreEqual(numeric_limits < uint32_t >::max() - 7, reader.parameters().restart_interval);
    //    }

    //    TEST_METHOD(read_header_with_2_define_restart_intervals) // NOLINT
    //    {
    //        jpeg_test_stream_writer writer;
    //        writer.write_start_of_image();
    //        writer.write_define_restart_interval(numeric_limits < uint32_t >::max(), 4);
    //        writer.write_start_of_frame_segment(512, 512, 8, 3);
    //        writer.write_define_restart_interval(0, 3);
    //        writer.write_start_of_scan_segment(0, 1, 0, interleave_mode::none);

    //        jpeg_stream_reader reader;
    //        reader.source({ writer.buffer.data(), writer.buffer.size()});
    //        reader.read_header();

    //        Assert::AreEqual(0U, reader.parameters().restart_interval);
    //    }

    //    TEST_METHOD(read_header_with_bad_define_restart_interval) // NOLINT
    //    {
    //        jpeg_test_stream_writer writer;
    //        writer.write_start_of_image();
    //        writer.write_start_of_frame_segment(512, 512, 8, 3);

    //        constexpr array<uint8_t, 1 > buffer{ };
    //        writer.write_segment(jpeg_marker_code::define_restart_interval, buffer.data(), buffer.size());
    //        writer.write_start_of_scan_segment(0, 1, 0, interleave_mode::none);

    //        jpeg_stream_reader reader;
    //        reader.source({ writer.buffer.data(), writer.buffer.size()});

    //        assert_expect_exception(jpegls_errc::invalid_marker_segment_size, [&reader] { reader.read_header(); });
    //    }

    //    TEST_METHOD(read_jpegls_stream_with_restart_marker_outside_entropy_data) // NOLINT
    //    {
    //        jpeg_test_stream_writer writer;
    //        writer.write_start_of_image();
    //        writer.write_restart_marker(0);

    //        jpeg_stream_reader reader;
    //        reader.source({ writer.buffer.data(), writer.buffer.size()});

    //        assert_expect_exception(jpegls_errc::unexpected_restart_marker, [&reader] { reader.read_header(); });
    //    }

    //    TEST_METHOD(read_comment) // NOLINT
    //    {
    //        jpeg_test_stream_writer writer;
    //        writer.write_start_of_image();
    //        writer.write_segment(jpeg_marker_code::comment, "hello", 5);
    //        writer.write_start_of_frame_segment(512, 512, 8, 3);
    //        writer.write_start_of_scan_segment(0, 1, 0, interleave_mode::none);

    //        jpeg_stream_reader reader;
    //        reader.source({ writer.buffer.data(), writer.buffer.size()});

    //        struct callback_output
    //    {
    //        const void* data{};
    //    size_t size { };
    //};
    //callback_output actual;

    //reader.at_comment(

    //    [](const void* data, const size_t size, void* user_context) noexcept -> int32_t {
    //                auto* actual_output = static_cast<callback_output*>(user_context);
    //actual_output->data = data;
    //                actual_output->size = size;
    //                return 0;
    //            },
    //            &actual);

    //reader.read_header();

    //Assert::AreEqual(static_cast<size_t>(5), actual.size);
    //Assert::IsTrue(memcmp("hello", actual.data, actual.size) == 0);
    //    }

    //    TEST_METHOD(read_empty_comment) // NOLINT
    //    {
    //    jpeg_test_stream_writer writer;
    //    writer.write_start_of_image();
    //    writer.write_segment(jpeg_marker_code::comment, "", 0);
    //    writer.write_start_of_frame_segment(512, 512, 8, 3);
    //    writer.write_start_of_scan_segment(0, 1, 0, interleave_mode::none);

    //    jpeg_stream_reader reader;
    //    reader.source({ writer.buffer.data(), writer.buffer.size()});

    //        struct callback_output
    //{
    //    const void* data{};
    //size_t size { };
    //        };
    //callback_output actual;

    //reader.at_comment(

    //    [](const void* data, const size_t size, void* user_context) noexcept->int32_t {
    //    auto* actual_output = static_cast<callback_output*>(user_context);
    //    actual_output->data = data;
    //    actual_output->size = size;
    //    return 0;
    //},
    //            &actual);

    //reader.read_header();

    //Assert::AreEqual(static_cast<size_t>(0), actual.size);
    //Assert::IsNull(actual.data);
    //    }

    //    TEST_METHOD(read_bad_comment) // NOLINT
    //    {
    //    jpeg_test_stream_writer writer;
    //    writer.write_start_of_image();
    //    writer.write_segment(jpeg_marker_code::comment, "", 10);

    //    jpeg_stream_reader reader;
    //    reader.source({ writer.buffer.data(), writer.buffer.size() - 1});

    //    bool called{ };
    //    reader.at_comment(

    //        [](const void*, const size_t, void* user_context) noexcept->int32_t {
    //        auto* actual_called = static_cast<bool*>(user_context);
    //        *actual_called = true;
    //        return 0;
    //    },
    //            &called);

    //    assert_expect_exception(jpegls_errc::source_buffer_too_small, [&reader] { reader.read_header(); });
    //    Assert::IsFalse(called);
    //}



    private static void ReadHeaderWithApplicationDataImpl(byte dataNumber)
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();

        writer.WriteByte(0xFF);
        writer.WriteByte((byte)(0xE0 + dataNumber));
        writer.WriteByte(0x00);
        writer.WriteByte(0x02);

        writer.WriteStartOfFrameSegment(1, 1, 2, 1);
        writer.WriteStartOfScanSegment(0, 1, 128, JpegLSInterleaveMode.None);

        var reader = new JpegStreamReader { Source = writer.GetBuffer() };

        reader.ReadHeader(); // if it doesn't throw test is passed.
    }

    private static void ReadHeaderWithJpegLSPresetParameterWithExtendedIdShouldThrowImpl(int id)
    {
        var buffer = new byte[]
            {0xFF, 0xD8, 0xFF,
            0xF8, // LSE: Marks the start of a JPEG-LS preset parameters segment.
            0x00, 0x03, (byte)id
        };

        var reader = new JpegStreamReader { Source = buffer };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);
        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(JpegLSError.JpeglsPresetExtendedParameterTypeNotSupported, exception.Data[nameof(JpegLSError)]);
    }
}
