// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed.Test;

public class JpegLSEncoderTest
{
    [Fact]
    public void FrameInfoMaxAndMin()
    {
        JpegLSEncoder encoder = new() { FrameInfo = new FrameInfo(1, 1, 2, 1) }; // minimum.
        encoder.FrameInfo = new FrameInfo(int.MaxValue, int.MaxValue, 16, 255); // maximum.
    }

    [Fact]
    public void FrameInfoBadWidthThrows()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => _= new JpegLSEncoder(0, 1, 2, 1));
        Assert.Equal(ErrorCode.InvalidArgumentWidth, exception.GetErrorCode());
    }

    [Fact]
    public void FrameInfoBadHeightThrows()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => _ = new JpegLSEncoder(1, 0, 2, 1));
        Assert.Equal(ErrorCode.InvalidArgumentHeight, exception.GetErrorCode());
    }

    [Fact]
    public void FrameInfoBadBitsPerSampleThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new JpegLSEncoder(1, 1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new JpegLSEncoder(1, 1, 17, 1));
    }

    [Fact]
    public void FrameInfoBadComponentCountThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new JpegLSEncoder(1, 1, 2, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new JpegLSEncoder(1, 1, 2, 256));
    }

    [Fact]
    public void TestInterleaveMode()
    {
        JpegLSEncoder encoder = new() { InterleaveMode = InterleaveMode.None };

        encoder.InterleaveMode = InterleaveMode.Line;
        encoder.InterleaveMode = InterleaveMode.Sample;
    }

    [Fact]
    public void InterleaveModeBadThrows() 
    {
        JpegLSEncoder encoder = new();

        Assert.Throws<ArgumentOutOfRangeException>(() => encoder.InterleaveMode = (InterleaveMode)(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => encoder.InterleaveMode = (InterleaveMode)(3));
    }

    [Fact]
    public void InterleaveModeDoesNotMatchComponentCountThrows()
    {
        var frameInfo = new FrameInfo(512, 512, 8, 1);
        var source = new byte[frameInfo.Width * frameInfo.Height];

        var exception = Assert.Throws<ArgumentException>(() => JpegLSEncoder.Encode(source, frameInfo, InterleaveMode.Sample));
        Assert.Equal(ErrorCode.InvalidArgumentInterleaveMode, exception.GetErrorCode());
        exception = Assert.Throws<ArgumentException>(() => JpegLSEncoder.Encode(source, frameInfo, InterleaveMode.Sample));
        Assert.Equal(ErrorCode.InvalidArgumentInterleaveMode, exception.GetErrorCode());
    }

    [Fact]
    public void TestNearLossless()
    {
        JpegLSEncoder encoder = new() { NearLossless = 0 }; // set lowest value.
        encoder.NearLossless = 255; // set highest value.
    }

    [Fact]
    public void NearLosslessBadThrows()
    {
        JpegLSEncoder encoder = new();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => encoder.NearLossless = -1);
        Assert.Equal(ErrorCode.InvalidArgumentNearLossless, exception.GetErrorCode());
        exception = Assert.Throws<ArgumentOutOfRangeException>(() => encoder.NearLossless = 256);
        Assert.Equal(ErrorCode.InvalidArgumentNearLossless, exception.GetErrorCode());
    }

    [Fact]
    public void EstimatedDestinationSizeMinimalFrameInfo()
    {
        JpegLSEncoder encoder = new();
        var frameInfo = new FrameInfo(1, 1, 2, 1); // = minimum.

        encoder.FrameInfo = frameInfo;
        var size = encoder.EstimatedDestinationSize;
        Assert.True(size >= 1024);
    }

    //    TEST_METHOD(estimated_destination_size_maximal_frame_info) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        encoder.frame_info({ numeric_limits < uint16_t >::max(), numeric_limits < uint16_t >::max(), 8, 1}); // = maximum.
    //        const auto size{ encoder.estimated_destination_size()};
    //        constexpr auto expected{
    //            static_cast<size_t>(numeric_limits < uint16_t >::max()) * numeric_limits < uint16_t >::max() * 1 *
    //                                1};
    //        Assert.True(size >= expected);
    //    }

    [Fact]
    public void EstimatedDestinationSizeMonochrome16Bit()
    {
        JpegLSEncoder encoder = new();
        var frameInfo = new FrameInfo(100, 100, 16, 1); // = minimum.

        encoder.FrameInfo = frameInfo;
        var size = encoder.EstimatedDestinationSize;
        Assert.True(size >= 100 * 100 * 2);
    }

    //    TEST_METHOD(estimated_destination_size_color_8_bit) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        encoder.frame_info({ 2000, 2000, 8, 3});
    //        const auto size{ encoder.estimated_destination_size()};
    //        Assert.True(size >= size_t{ 2000}
    //        *2000 * 3);
    //    }

    //    TEST_METHOD(estimated_destination_size_very_wide) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        encoder.frame_info({ numeric_limits < uint16_t >::max(), 1, 8, 1});
    //        const auto size{ encoder.estimated_destination_size()};
    //        Assert.True(size >= static_cast<size_t>(numeric_limits < uint16_t >::max()) + 1024U);
    //    }

    //    TEST_METHOD(estimated_destination_size_very_high) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        encoder.frame_info({ 1, numeric_limits < uint16_t >::max(), 8, 1});
    //        const auto size{ encoder.estimated_destination_size()};
    //        Assert.True(size >= static_cast<size_t>(numeric_limits < uint16_t >::max()) + 1024U);
    //    }

    [Fact]
    public void EstimatedDestinationSizeTooSoonThrows()
    {
        JpegLSEncoder encoder = new();

        var exception = Assert.Throws<InvalidOperationException>(() => encoder.EstimatedDestinationSize);
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    //    TEST_METHOD(estimated_destination_size_thath_causes_overflow_throws) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        encoder.frame_info({ numeric_limits < uint32_t >::max(), numeric_limits < uint32_t >::max(), 8, 1});

    //#if INTPTR_MAX == INT64_MAX
    //        const auto size{ encoder.estimated_destination_size()};
    //        Assert.True(size != 0); // actual value already checked in other test functions.
    //#elif INTPTR_MAX == INT32_MAX
    //        assert_expect_exception(jpegls_errc::parameter_value_not_supported,
    //                                [&encoder] { ignore = encoder.estimated_destination_size(); });
    //#else
    //#error Unknown pointer size or missing size macros!
    //#endif
    //    }

    [Fact]
    public void TestDestination()
    {
        JpegLSEncoder encoder = new();

        var destination = new byte[20];
        encoder.Destination = destination;
    }

    [Fact]
    public void DestinationCanOnlyBeSetOnceThrows()
    {
        JpegLSEncoder encoder = new();

        var destination = new byte[20];
        encoder.Destination = destination;
        Assert.Throws<InvalidOperationException>(() => encoder.Destination = destination);
    }

    [Fact]
    public void WriteStandardSpiffHeader()
    {
        JpegLSEncoder encoder = new();

        var frameInfo = new FrameInfo(1, 1, 2, 4);
        encoder.FrameInfo = frameInfo;

        var destination = new byte[encoder.EstimatedDestinationSize];
        encoder.Destination = destination;

        encoder.WriteStandardSpiffHeader(SpiffColorSpace.Cmyk);

        Assert.Equal(Constants.SpiffHeaderSizeInBytes + 2, encoder.BytesWritten);

        // Check that SOI marker has been written.
        Assert.Equal((byte)0xFF, destination[0]);
        Assert.Equal((byte)JpegMarkerCode.StartOfImage, destination[1]);

        // Verify that a APP8 with SPIFF has been written (details already verified by jpeg_stream_writer_test).
        Assert.Equal(0xFF, destination[2]);
        Assert.Equal((byte)JpegMarkerCode.ApplicationData8, destination[3]);
        Assert.Equal(0, destination[4]);
        Assert.Equal((byte) 32, destination[5]);
        Assert.Equal((byte) 'S', destination[6]);
        Assert.Equal((byte) 'P', destination[7]);
        Assert.Equal((byte) 'I', destination[8]);
        Assert.Equal((byte) 'F', destination[9]);
        Assert.Equal((byte) 'F', destination[10]);
        Assert.Equal(0, destination[11]);
    }

    [Fact]
    public void WriteStandardSpiffHeaderWithoutDestinationThrows()
    {
        JpegLSEncoder encoder = new() { FrameInfo = new FrameInfo(1, 1, 2, 4) };

        var exception = Assert.Throws<InvalidOperationException>(() => encoder.WriteStandardSpiffHeader(SpiffColorSpace.Cmyk));
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    [Fact]
    public void WriteStandardSpiffHeaderWithoutFrameInfoThrows()
    {
        JpegLSEncoder encoder = new();

        var destination = new byte[100];
        encoder.Destination = destination;

        var exception = Assert.Throws<InvalidOperationException>(() => encoder.WriteStandardSpiffHeader(SpiffColorSpace.Cmyk));
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    [Fact]
    public void WriteStandardSpiffHeaderTwiceThrows()
    {
        JpegLSEncoder encoder = new() { FrameInfo = new FrameInfo(1, 1, 2, 4) };
        var destination = new byte[encoder.EstimatedDestinationSize];
        encoder.Destination = destination;
        encoder.WriteStandardSpiffHeader(SpiffColorSpace.Cmyk);

        var exception = Assert.Throws<InvalidOperationException>(() => encoder.WriteStandardSpiffHeader(SpiffColorSpace.Cmyk));
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    //    TEST_METHOD(write_spiff_header) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        encoder.frame_info({ 1, 1, 2, 1});

    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        spiff_header spiff_header{ };
    //        spiff_header.width = 1;
    //        spiff_header.height = 1;
    //        encoder.write_spiff_header(spiff_header);

    //        Assert.Equal(serialized_spiff_header_size + 2, encoder.bytes_written());

    //        // Check that SOI marker has been written.
    //        Assert.Equal(byte{ 0xFF}, destination[0]);
    //        Assert.Equal(static_cast<byte>(jpeg_marker_code::start_of_image), destination[1]);

    //        // Verify that a APP8 with SPIFF has been written (details already verified by jpeg_stream_writer_test).
    //        Assert.Equal(byte{ 0xFF}, destination[2]);
    //        Assert.Equal(static_cast<byte>(jpeg_marker_code::application_data8), destination[3]);
    //        Assert.Equal({ }, destination[4]);
    //        Assert.Equal(byte{ 32}, destination[5]);
    //        Assert.Equal(byte{ 'S'}, destination[6]);
    //        Assert.Equal(byte{ 'P'}, destination[7]);
    //        Assert.Equal(byte{ 'I'}, destination[8]);
    //        Assert.Equal(byte{ 'F'}, destination[9]);
    //        Assert.Equal(byte{ 'F'}, destination[10]);
    //        Assert.Equal(byte{ }, destination[11]);
    //    }

    //    TEST_METHOD(write_spiff_header_invalid_height_throws) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        encoder.frame_info({ 1, 1, 2, 1});

    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        spiff_header spiff_header{ };
    //        spiff_header.width = 1;

    //        assert_expect_exception(jpegls_errc::invalid_argument_height,
    //                                [&encoder, &spiff_header] { encoder.write_spiff_header(spiff_header); });
    //        Assert.Equal(size_t{ }, encoder.bytes_written());
    //    }

    //    TEST_METHOD(write_spiff_header_invalid_width_throws) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        encoder.frame_info({ 1, 1, 2, 1});

    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        spiff_header spiff_header{ };
    //        spiff_header.height = 1;

    //        assert_expect_exception(jpegls_errc::invalid_argument_width,
    //                                [&encoder, &spiff_header] { encoder.write_spiff_header(spiff_header); });
    //        Assert.Equal(size_t{ }, encoder.bytes_written());
    //    }

    [Fact]
    public void WriteSpiffEntry()
    {
        JpegLSEncoder encoder = new() { FrameInfo = new FrameInfo(1, 1, 2, 4) };
        var destination = new byte[encoder.EstimatedDestinationSize];
        encoder.Destination = destination;
        encoder.WriteStandardSpiffHeader(SpiffColorSpace.Cmyk);

        encoder.WriteSpiffEntry(SpiffEntryTag.ImageTitle, "test"u8);

        Assert.Equal(48, encoder.BytesWritten);
    }

    [Fact]
    public void WriteSpiffEntryTwice()
    {
        JpegLSEncoder encoder = new() { FrameInfo = new FrameInfo(1, 1, 2, 4) };
        var destination = new byte[encoder.EstimatedDestinationSize];
        encoder.Destination = destination;
        encoder.WriteStandardSpiffHeader(SpiffColorSpace.Cmyk);
        encoder.WriteSpiffEntry(SpiffEntryTag.ImageTitle, "test"u8);

        encoder.WriteSpiffEntry(SpiffEntryTag.ImageTitle, "test"u8);

        Assert.Equal(60, encoder.BytesWritten);
    }

    [Fact]
    public void WriteEmptySpiffEntry()
    {
        JpegLSEncoder encoder = new() { FrameInfo = new FrameInfo(1, 1, 2, 4) };
        var destination = new byte[encoder.EstimatedDestinationSize];
        encoder.Destination = destination;
        encoder.WriteStandardSpiffHeader(SpiffColorSpace.Cmyk);

        encoder.WriteSpiffEntry(SpiffEntryTag.ImageTitle, []);

        Assert.Equal(44, encoder.BytesWritten);
    }

    [Fact]
    public void WriteSpiffEntryWithInvalidTagThrows()
    {
        JpegLSEncoder encoder = new() { FrameInfo = new FrameInfo(1, 1, 2, 4) };
        var destination = new byte[encoder.EstimatedDestinationSize];
        encoder.Destination = destination;
        encoder.WriteStandardSpiffHeader(SpiffColorSpace.Cmyk);

        const int endOfSpiffDirectoryTag = 1;
        var exception = Assert.Throws<ArgumentException>(() => encoder.WriteSpiffEntry(endOfSpiffDirectoryTag, "test"u8));
        Assert.Equal(ErrorCode.InvalidArgument, exception.GetErrorCode());
    }

    //    TEST_METHOD(write_spiff_entry_with_invalid_size_throws) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        encoder.frame_info({ 1, 1, 2, 1});

    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);
    //        encoder.write_standard_spiff_header(spiff_color_space::cmyk);

    //        assert_expect_exception(jpegls_errc::invalid_argument_size, [&encoder] {
    //            const vector<byte> spiff_entry(65528 + 1);
    //            encoder.write_spiff_entry(spiff_entry_tag::image_title, spiff_entry.data(), spiff_entry.size());
    //        });
    //    }

    //    TEST_METHOD(write_spiff_entry_without_spiff_header_throws) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        encoder.frame_info({ 1, 1, 2, 1});

    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        assert_expect_exception(jpegls_errc::invalid_operation, [&encoder] {
    //            const vector<byte> spiff_entry(65528);
    //            encoder.write_spiff_entry(spiff_entry_tag::image_title, spiff_entry.data(), spiff_entry.size());
    //        });
    //    }

    //    TEST_METHOD(write_spiff_end_of_directory_entry) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        encoder.frame_info({ 1, 1, 2, 1});

    //        vector<byte> destination(300);
    //        encoder.destination(destination);

    //        encoder.write_standard_spiff_header(spiff_color_space::none);
    //        encoder.write_spiff_end_of_directory_entry();

    //        Assert.Equal(byte{ 0xFF}, destination[44]);
    //        Assert.Equal(byte{ 0xD8}, destination[45]); // 0xD8 = SOI: Marks the start of an image.
    //    }

    //    TEST_METHOD(write_spiff_end_of_directory_entry_before_header_throws) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        vector<byte> destination(300);
    //        encoder.destination(destination);

    //        assert_expect_exception(jpegls_errc::invalid_operation,
    //                                [&encoder] { encoder.write_spiff_end_of_directory_entry(); });
    //    }

    //    TEST_METHOD(write_spiff_end_of_directory_entry_twice_throws) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        encoder.frame_info({ 1, 1, 2, 1});

    //        vector<byte> destination(300);
    //        encoder.destination(destination);

    //        encoder.write_standard_spiff_header(spiff_color_space::none);
    //        encoder.write_spiff_end_of_directory_entry();

    //        assert_expect_exception(jpegls_errc::invalid_operation,
    //                                [&encoder] { encoder.write_spiff_end_of_directory_entry(); });
    //    }

    [Fact]
    public void WriteComment()
    {
        JpegLSEncoder encoder = new();

        var destination = new byte[10];
        encoder.Destination = destination;

        encoder.WriteComment("123");

        Assert.Equal(10, encoder.BytesWritten);

        // Check that SOI marker has been written.
        Assert.Equal(0xFF, destination[0]);
        Assert.Equal((byte)JpegMarkerCode.StartOfImage, destination[1]);

        // Verify that a COM segment has been written.
        Assert.Equal(0xFF, destination[2]);
        Assert.Equal((byte)JpegMarkerCode.Comment, destination[3]);
        Assert.Equal(0, destination[4]);
        Assert.Equal( 2 + 4, destination[5]);
        Assert.Equal((byte) '1', destination[6]);
        Assert.Equal((byte) '2', destination[7]);
        Assert.Equal((byte) '3', destination[8]);
        Assert.Equal(0, destination[9]);
    }

    [Fact]
    public void WriteEmptyComment()
    {
        JpegLSEncoder encoder = new();

        var destination = new byte[6];
        encoder.Destination = destination;

        encoder.WriteComment("");

        Assert.Equal(6, encoder.BytesWritten);

        // Check that SOI marker has been written.
        Assert.Equal(0xFF, destination[0]);
        Assert.Equal((byte)JpegMarkerCode.StartOfImage, destination[1]);

        // Verify that a COM segment has been written.
        Assert.Equal(0xFF, destination[2]);
        Assert.Equal((byte)JpegMarkerCode.Comment, destination[3]);
        Assert.Equal(0, destination[4]);
        Assert.Equal(2, destination[5]);
    }

    [Fact]
    public void WriteEmptyCommentBuffer()
    {
        JpegLSEncoder encoder = new();

        var destination = new byte[6];
        encoder.Destination = destination;

        ReadOnlySpan<byte> buffer = [];
        encoder.WriteComment(buffer);

        Assert.Equal(6, encoder.BytesWritten);

        // Check that SOI marker has been written.
        Assert.Equal(0xFF, destination[0]);
        Assert.Equal((byte)JpegMarkerCode.StartOfImage, destination[1]);

        // Verify that a COM segment has been written.
        Assert.Equal(0xFF, destination[2]);
        Assert.Equal((byte)JpegMarkerCode.Comment, destination[3]);
        Assert.Equal(0, destination[4]);
        Assert.Equal(2, destination[5]);
    }

    //    TEST_METHOD(write_max_comment) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        vector<byte> destination(2 + 2 + static_cast<size_t>(numeric_limits < uint16_t >::max()));
    //        encoder.destination(destination);

    //        constexpr size_t max_size_comment_data{ static_cast<size_t>(numeric_limits < uint16_t >::max()) - 2};
    //        const vector<byte> data(max_size_comment_data);
    //        encoder.write_comment(data.data(), data.size());

    //        Assert.Equal(destination.size(), encoder.bytes_written());

    //        // Check that SOI marker has been written.
    //        Assert.Equal(byte{ 0xFF}, destination[0]);
    //        Assert.Equal(static_cast<byte>(jpeg_marker_code::start_of_image), destination[1]);

    //        // Verify that a COM segment has been written.
    //        Assert.Equal(byte{ 0xFF}, destination[2]);
    //        Assert.Equal(static_cast<byte>(jpeg_marker_code::comment), destination[3]);
    //        Assert.Equal(byte{ 255}, destination[4]);
    //        Assert.Equal(byte{ 255}, destination[5]);
    //    }

    //    TEST_METHOD(write_two_comment) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        array < byte, 14 > destination;
    //        encoder.destination(destination);

    //        encoder.write_comment("123");
    //        encoder.write_comment("");

    //        Assert.Equal(destination.size(), encoder.bytes_written());

    //        // Check that SOI marker has been written.
    //        Assert.Equal(byte{ 0xFF}, destination[0]);
    //        Assert.Equal(static_cast<byte>(jpeg_marker_code::start_of_image), destination[1]);

    //        // Verify that the COM segments have been written.
    //        Assert.Equal(byte{ 0xFF}, destination[2]);
    //        Assert.Equal(static_cast<byte>(jpeg_marker_code::comment), destination[3]);
    //        Assert.Equal(byte{ }, destination[4]);
    //        Assert.Equal(byte{ 2 + 4}, destination[5]);
    //        Assert.Equal(byte{ '1'}, destination[6]);
    //        Assert.Equal(byte{ '2'}, destination[7]);
    //        Assert.Equal(byte{ '3'}, destination[8]);
    //        Assert.Equal(byte{ }, destination[9]);

    //        Assert.Equal(byte{ 0xFF}, destination[10]);
    //        Assert.Equal(static_cast<byte>(jpeg_marker_code::comment), destination[11]);
    //        Assert.Equal(byte{ }, destination[12]);
    //        Assert.Equal(byte{ 2}, destination[13]);
    //    }

    //    TEST_METHOD(write_too_large_comment_throws) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        vector<byte> destination(2 + 2 + static_cast<size_t>(numeric_limits < uint16_t >::max()) + 1);
    //        encoder.destination(destination);

    //        constexpr size_t max_size_comment_data{ static_cast<size_t>(numeric_limits < uint16_t >::max()) - 2};
    //        const vector<byte> data(max_size_comment_data +1);

    //        assert_expect_exception(jpegls_errc::invalid_argument_size,
    //                                [&encoder, &data] { ignore = encoder.write_comment(data.data(), data.size()); });
    //    }

    //    TEST_METHOD(write_comment_after_encode_throws) // NOLINT
    //    {
    //        const vector source{ byte{ 0}, byte{ 1}, byte{ 2}, byte{ 3}, byte{ 4}, byte{ 5} };

    //        jpegls_encoder encoder;

    //        vector<byte> destination(100);
    //        encoder.destination(destination);
    //        encoder.frame_info({ 3, 1, 16, 1});
    //        ignore = encoder.encode(source);

    //        assert_expect_exception(jpegls_errc::invalid_operation,
    //                                [&encoder] { ignore = encoder.write_comment("after-encoding"); });
    //    }

    //    TEST_METHOD(write_comment_before_encode) // NOLINT
    //    {
    //        const vector source{ byte{ 0}, byte{ 1}, byte{ 2}, byte{ 3}, byte{ 4}, byte{ 5} };
    //        constexpr frame_info frame_info{ 3, 1, 16, 1};

    //        jpegls_encoder encoder;
    //        vector<byte> encoded(100);
    //        encoder.destination(encoded);
    //        encoder.frame_info(frame_info);

    //        encoder.write_comment("my comment");

    //        encoded.resize(encoder.encode(source));
    //        test_by_decoding(encoded, frame_info, source.data(), source.size(), interleave_mode::none);
    //    }

    //    TEST_METHOD(write_application_data) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        array < byte, 10 > destination;
    //        encoder.destination(destination);

    //        constexpr array application_data{ byte{ 1}, byte{ 2}, byte{ 3}, byte{ 4} };
    //        encoder.write_application_data(1, application_data.data(), application_data.size());

    //        Assert.Equal(size_t{ 10}, encoder.bytes_written());

    //        // Check that SOI marker has been written.
    //        Assert.Equal(byte{ 0xFF}, destination[0]);
    //        Assert.Equal(static_cast<byte>(jpeg_marker_code::start_of_image), destination[1]);

    //        // Verify that a APPn segment has been written.
    //        Assert.Equal(byte{ 0xFF}, destination[2]);
    //        Assert.Equal(static_cast<byte>(jpeg_marker_code::application_data1), destination[3]);
    //        Assert.Equal(byte{ }, destination[4]);
    //        Assert.Equal(byte{ 2 + 4}, destination[5]);
    //        Assert.Equal(byte{ 1}, destination[6]);
    //        Assert.Equal(byte{ 2}, destination[7]);
    //        Assert.Equal(byte{ 3}, destination[8]);
    //        Assert.Equal(byte{ 4}, destination[9]);
    //    }

    [Fact]
    public void WriteEmptyApplicationData()
    {
        JpegLSEncoder encoder = new();

        var destination = new byte[6];
        encoder.Destination = destination;

        ReadOnlySpan<byte> buffer = [];
        encoder.WriteApplicationData(2, buffer);

        Assert.Equal(6, encoder.BytesWritten);

        // Check that SOI marker has been written.
        Assert.Equal(0xFF, destination[0]);
        Assert.Equal((byte)JpegMarkerCode.StartOfImage, destination[1]);

        // Verify that a APPn segment has been written.
        Assert.Equal(0xFF, destination[2]);
        Assert.Equal((byte)JpegMarkerCode.ApplicationData2, destination[3]);
        Assert.Equal(0, destination[4]);
        Assert.Equal(2, destination[5]);
    }

    //    TEST_METHOD(write_max_application_data) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        vector<byte> destination(2 + 2 + static_cast<size_t>(numeric_limits < uint16_t >::max()));
    //        encoder.destination(destination);

    //        constexpr size_t max_size_application_data{ static_cast<size_t>(numeric_limits < uint16_t >::max()) - 2};
    //        const vector<byte> data(max_size_application_data);
    //        encoder.write_application_data(15, data.data(), data.size());

    //        Assert.Equal(destination.size(), encoder.bytes_written());

    //        // Check that SOI marker has been written.
    //        Assert.Equal(byte{ 0xFF}, destination[0]);
    //        Assert.Equal(static_cast<byte>(jpeg_marker_code::start_of_image), destination[1]);

    //        // Verify that a APPn segment has been written.
    //        Assert.Equal(byte{ 0xFF}, destination[2]);
    //        Assert.Equal(static_cast<byte>(jpeg_marker_code::application_data15), destination[3]);
    //        Assert.Equal(byte{ 255}, destination[4]);
    //        Assert.Equal(byte{ 255}, destination[5]);
    //    }

    //    TEST_METHOD(write_two_application_data) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        array < byte, 14 > destination;
    //        encoder.destination(destination);

    //        constexpr array application_data{ byte{ 1}, byte{ 2}, byte{ 3}, byte{ 4} };
    //        encoder.write_application_data(0, application_data.data(), application_data.size());
    //        encoder.write_application_data(8, nullptr, 0);

    //        Assert.Equal(destination.size(), encoder.bytes_written());

    //        // Check that SOI marker has been written.
    //        Assert.Equal(byte{ 0xFF}, destination[0]);
    //        Assert.Equal(static_cast<byte>(jpeg_marker_code::start_of_image), destination[1]);

    //        // Verify that the COM segments have been written.
    //        Assert.Equal(byte{ 0xFF}, destination[2]);
    //        Assert.Equal(static_cast<byte>(jpeg_marker_code::application_data0), destination[3]);
    //        Assert.Equal(byte{ }, destination[4]);
    //        Assert.Equal(byte{ 2 + 4}, destination[5]);
    //        Assert.Equal(byte{ 1}, destination[6]);
    //        Assert.Equal(byte{ 2}, destination[7]);
    //        Assert.Equal(byte{ 3}, destination[8]);
    //        Assert.Equal(byte{ 4}, destination[9]);

    //        Assert.Equal(byte{ 0xFF}, destination[10]);
    //        Assert.Equal(static_cast<byte>(jpeg_marker_code::application_data8), destination[11]);
    //        Assert.Equal(byte{ }, destination[12]);
    //        Assert.Equal(byte{ 2}, destination[13]);
    //    }

    //    TEST_METHOD(write_too_large_application_data_throws) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        vector<byte> destination(2 + 2 + static_cast<size_t>(numeric_limits < uint16_t >::max()) + 1);
    //        encoder.destination(destination);

    //        constexpr size_t max_size_application_data{ static_cast<size_t>(numeric_limits < uint16_t >::max()) - 2};
    //        const vector<byte> data(max_size_application_data +1);

    //        assert_expect_exception(jpegls_errc::invalid_argument_size,
    //                                [&encoder, &data] { ignore = encoder.write_application_data(0, data.data(), data.size()); });
    //    }

    //    TEST_METHOD(write_application_data_null_pointer_with_size_throws) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        vector<byte> destination(100);
    //        encoder.destination(destination);

    //        assert_expect_exception(jpegls_errc::invalid_argument, [&encoder] {
    //            MSVC_WARNING_SUPPRESS_NEXT_LINE(6387)
    //            ignore = encoder.write_application_data(0, nullptr, 1);
    //        });
    //    }

    //    TEST_METHOD(write_application_data_after_encode_throws) // NOLINT
    //    {
    //        const vector source{ byte{ 0}, byte{ 1}, byte{ 2}, byte{ 3}, byte{ 4}, byte{ 5} };

    //        jpegls_encoder encoder;

    //        vector<byte> destination(100);
    //        encoder.destination(destination);
    //        encoder.frame_info({ 3, 1, 16, 1});
    //        ignore = encoder.encode(source);

    //        assert_expect_exception(jpegls_errc::invalid_operation,
    //                                [&encoder] { ignore = encoder.write_application_data(0, nullptr, 0); });
    //    }

    //    TEST_METHOD(write_application_data_with_bad_id_throws) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        vector<byte> destination(100);
    //        encoder.destination(destination);

    //        assert_expect_exception(jpegls_errc::invalid_argument, [&encoder] {
    //            MSVC_WARNING_SUPPRESS_NEXT_LINE(6387)
    //            ignore = encoder.write_application_data(-1, nullptr, 0);
    //        });

    //        assert_expect_exception(jpegls_errc::invalid_argument, [&encoder] {
    //            MSVC_WARNING_SUPPRESS_NEXT_LINE(6387)
    //            ignore = encoder.write_application_data(16, nullptr, 0);
    //        });
    //    }

    //    TEST_METHOD(write_application_data_before_encode) // NOLINT
    //    {
    //        constexpr array source{ byte{ 0}, byte{ 1}, byte{ 2}, byte{ 3}, byte{ 4}, byte{ 5} };
    //        constexpr frame_info frame_info{ 3, 1, 16, 1};

    //        jpegls_encoder encoder;
    //        vector<byte> encoded(100);
    //        encoder.destination(encoded);
    //        encoder.frame_info(frame_info);

    //        encoder.write_application_data(11, nullptr, 0);

    //        encoded.resize(encoder.encode(source));
    //        test_by_decoding(encoded, frame_info, source.data(), source.size(), interleave_mode::none);
    //    }

    [Fact]
    public void WriteMappingTable()
    {
        JpegLSEncoder encoder = new();

        var destination = new byte[10];
        encoder.Destination = destination;

        ReadOnlySpan<byte> buffer = [0];
        encoder.WriteMappingTable(1, 1, buffer);

        Assert.Equal(10, encoder.BytesWritten);

        // Check that SOI marker has been written.
        Assert.Equal(0xFF, destination[0]);
        Assert.Equal((byte)JpegMarkerCode.StartOfImage, destination[1]);

        // Verify that a APPn segment has been written.
        Assert.Equal(0xFF, destination[2]);
        Assert.Equal((byte)JpegMarkerCode.JpegLSPresetParameters, destination[3]);
        Assert.Equal(0, destination[4]);
        Assert.Equal(6, destination[5]);
        Assert.Equal(2, destination[6]);
        Assert.Equal(1, destination[7]);
        Assert.Equal(1, destination[8]);
        Assert.Equal(0, destination[9]);
    }

    //    TEST_METHOD(write_table_before_encode) // NOLINT
    //    {
    //        constexpr array table_data{ byte{ 0}, byte{ 1}, byte{ 2}, byte{ 3}, byte{ 4}, byte{ 5} };
    //        constexpr array source{ byte{ 0}, byte{ 1}, byte{ 2}, byte{ 3}, byte{ 4}, byte{ 5} };
    //        constexpr frame_info frame_info{ 3, 1, 16, 1};

    //        jpegls_encoder encoder;
    //        vector<byte> encoded(100);
    //        encoder.destination(encoded);
    //        encoder.frame_info(frame_info);

    //        encoder.write_table(1, 1, table_data);

    //        encoded.resize(encoder.encode(source));
    //        test_by_decoding(encoded, frame_info, source.data(), source.size(), interleave_mode::none);
    //    }

    //    TEST_METHOD(write_table_with_bad_table_id_throws) // NOLINT
    //    {
    //        constexpr array table_data{ byte{ 0}, byte{ 1}, byte{ 2}, byte{ 3}, byte{ 4}, byte{ 5} };
    //        jpegls_encoder encoder;

    //        vector<byte> destination(100);
    //        encoder.destination(destination);

    //        assert_expect_exception(jpegls_errc::invalid_argument,
    //                                [&encoder, &table_data] { ignore = encoder.write_table(0, 1, table_data); });

    //        assert_expect_exception(jpegls_errc::invalid_argument,
    //                                [&encoder, &table_data] { ignore = encoder.write_table(256, 1, table_data); });
    //    }

    //    TEST_METHOD(write_table_with_bad_entry_size_throws) // NOLINT
    //    {
    //        constexpr array table_data{ byte{ 0}, byte{ 1}, byte{ 2}, byte{ 3}, byte{ 4}, byte{ 5} };
    //        jpegls_encoder encoder;

    //        vector<byte> destination(100);
    //        encoder.destination(destination);

    //        assert_expect_exception(jpegls_errc::invalid_argument,
    //                                [&encoder, &table_data] { ignore = encoder.write_table(1, 0, table_data); });

    //        assert_expect_exception(jpegls_errc::invalid_argument,
    //                                [&encoder, &table_data] { ignore = encoder.write_table(1, 256, table_data); });
    //    }

    //    TEST_METHOD(write_table_with_too_small_table_throws) // NOLINT
    //    {
    //        constexpr array table_data{ byte{ 0} };
    //        jpegls_encoder encoder;

    //        vector<byte> destination(100);
    //        encoder.destination(destination);

    //        assert_expect_exception(jpegls_errc::invalid_argument_size, [&encoder, &table_data] {
    //            ignore = encoder.write_table(1, 2, table_data);
    //        });
    //    }

    //    TEST_METHOD(write_table_after_encode_throws) // NOLINT
    //    {
    //        constexpr array table_data{ byte{ 0} };
    //        const vector source{ byte{ 0}, byte{ 1}, byte{ 2}, byte{ 3}, byte{ 4}, byte{ 5} };

    //        jpegls_encoder encoder;

    //        vector<byte> destination(100);
    //        encoder.destination(destination);
    //        encoder.frame_info({ 3, 1, 16, 1});
    //        ignore = encoder.encode(source);

    //        assert_expect_exception(jpegls_errc::invalid_operation, [&encoder, &table_data] {
    //            ignore = encoder.write_table(1, 1, table_data);
    //        });
    //    }

    //    TEST_METHOD(create_tables_only) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        array < byte, 12 > destination;
    //        encoder.destination(destination);

    //        constexpr array table_data{ byte{ 0} };
    //        encoder.write_table(1, 1, table_data);
    //        const size_t bytes_written{ encoder.create_tables_only()};

    //        Assert.Equal(size_t{ 12}, bytes_written);

    //        // Check that SOI marker has been written.
    //        Assert.Equal(byte{ 0xFF}, destination[0]);
    //        Assert.Equal(static_cast<byte>(jpeg_marker_code::start_of_image), destination[1]);

    //        // Verify that a JPEG-LS preset segment with the table has been written.
    //        Assert.Equal(byte{ 0xFF}, destination[2]);
    //        Assert.Equal(static_cast<byte>(jpeg_marker_code::jpegls_preset_parameters), destination[3]);
    //        Assert.Equal(byte{ }, destination[4]);
    //        Assert.Equal(byte{ 6}, destination[5]);
    //        Assert.Equal(byte{ 2}, destination[6]);
    //        Assert.Equal(byte{ 1}, destination[7]);
    //        Assert.Equal(byte{ 1}, destination[8]);
    //        Assert.Equal(byte{ }, destination[9]);

    //        // Check that SOI marker has been written.
    //        Assert.Equal(byte{ 0xFF}, destination[10]);
    //        Assert.Equal(static_cast<byte>(jpeg_marker_code::end_of_image), destination[11]);
    //    }

    //    TEST_METHOD(create_tables_only_with_no_tables_throws) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        array < byte, 12 > destination;
    //        encoder.destination(destination);

    //        assert_expect_exception(jpegls_errc::invalid_operation, [&encoder] { ignore = encoder.create_tables_only(); });
    //    }

    [Fact]
    public void SetPresetCodingParameters()
    {
        JpegLSEncoder encoder = new();

        var presetCodingParameters = new JpegLSPresetCodingParameters();
        encoder.PresetCodingParameters = presetCodingParameters;

        // No explicit test possible, code should remain stable.
        Assert.True(true);
    }

    //    TEST_METHOD(set_preset_coding_parameters_bad_values_throws) // NOLINT
    //    {
    //        constexpr array source{ byte{ 0}, byte{ 1}, byte{ 1}, byte{ 1}, byte{ 0} };
    //        constexpr frame_info frame_info{ 5, 1, 8, 1};
    //        jpegls_encoder encoder;

    //        encoder.frame_info(frame_info);
    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        constexpr jpegls_pc_parameters bad_pc_parameters{ 1, 1, 1, 1, 1};
    //        encoder.preset_coding_parameters(bad_pc_parameters);

    //        assert_expect_exception(jpegls_errc::invalid_argument_jpegls_pc_parameters,
    //                                [&encoder, &source] { ignore = encoder.encode(source); });
    //    }

    //    TEST_METHOD(encode_with_preset_coding_parameters_non_default_values) // NOLINT
    //    {
    //        encode_with_custom_preset_coding_parameters({ 1, 0, 0, 0, 0});
    //        encode_with_custom_preset_coding_parameters({ 0, 1, 0, 0, 0});
    //        encode_with_custom_preset_coding_parameters({ 0, 0, 4, 0, 0});
    //        encode_with_custom_preset_coding_parameters({ 0, 0, 0, 8, 0});
    //        encode_with_custom_preset_coding_parameters({ 0, 1, 2, 3, 0});
    //        encode_with_custom_preset_coding_parameters({ 0, 0, 0, 0, 63});
    //    }

    //    TEST_METHOD(set_color_transformation_bad_value_throws) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        assert_expect_exception(jpegls_errc::invalid_argument_color_transformation,
    //                                [&encoder] { encoder.color_transformation(static_cast<color_transformation>(100)); });
    //    }

    //    TEST_METHOD(SetTableId) // NOLINT
    //    {
    //        constexpr array source{ byte{ 0}, byte{ 1}, byte{ 2}, byte{ 3}, byte{ 4}, byte{ 5} };
    //        constexpr frame_info frame_info{ 3, 1, 16, 1};
    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info);
    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        encoder.SetTableId(0, 1);

    //        const size_t bytes_written{ encoder.encode(source)};
    //        destination.resize(bytes_written);
    //        jpegls_decoder decoder(destination, true);
    //        vector<byte> destination_decoded(decoder.destination_size());
    //        decoder.decode(destination_decoded);
    //        Assert.Equal(1, decoder.mapping_table_id(0));
    //    }

    //    TEST_METHOD(set_table_id_clear_id) // NOLINT
    //    {
    //        constexpr array source{ byte{ 0}, byte{ 1}, byte{ 2}, byte{ 3}, byte{ 4}, byte{ 5} };
    //        constexpr frame_info frame_info{ 3, 1, 16, 1};
    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info);
    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        encoder.SetTableId(0, 1);
    //        encoder.SetTableId(0, 0);

    //        const size_t bytes_written{ encoder.encode(source)};
    //        destination.resize(bytes_written);
    //        jpegls_decoder decoder(destination, true);
    //        vector<byte> destination_decoded(decoder.destination_size());
    //        decoder.decode(destination_decoded);
    //        Assert.Equal(0, decoder.mapping_table_id(0));
    //    }

    //    TEST_METHOD(set_table_id_bad_component_index_throws) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        assert_expect_exception(jpegls_errc::invalid_argument, [&encoder] { encoder.SetTableId(-1, 0); });
    //    }

    //    TEST_METHOD(set_table_id_bad_id_throws) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        assert_expect_exception(jpegls_errc::invalid_argument, [&encoder] { encoder.SetTableId(0, -1); });
    //    }

    //    TEST_METHOD(encode_without_destination_throws) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        encoder.frame_info({ 1, 1, 2, 1});
    //        vector<byte> source(20);
    //        assert_expect_exception(jpegls_errc::invalid_operation, [&encoder, &source] { ignore = encoder.encode(source); });
    //    }

    //    TEST_METHOD(encode_without_frame_info_throws) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        vector<byte> destination(20);
    //        encoder.destination(destination);
    //        const vector<byte> source(20);
    //        assert_expect_exception(jpegls_errc::invalid_operation, [&encoder, &source] { ignore = encoder.encode(source); });
    //    }

    //    TEST_METHOD(encode_with_spiff_header) // NOLINT
    //    {
    //        constexpr array source{ byte{ 0}, byte{ 1}, byte{ 2}, byte{ 3}, byte{ 4} };
    //        constexpr frame_info frame_info{ 5, 1, 8, 1};

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info);
    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        encoder.write_standard_spiff_header(spiff_color_space::grayscale);

    //        const size_t bytes_written{ encoder.encode(source)};
    //        destination.resize(bytes_written);

    //        test_by_decoding(destination, frame_info, source.data(), source.size(), interleave_mode::none);
    //    }

    //    TEST_METHOD(encode_with_color_transformation) // NOLINT
    //    {
    //        constexpr array source{ byte{ 0}, byte{ 1}, byte{ 2}, byte{ 3}, byte{ 4}, byte{ 5} };
    //        constexpr frame_info frame_info{ 2, 1, 8, 3};

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info).color_transformation(color_transformation::hp1);
    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        const size_t bytes_written{ encoder.encode(source)};
    //        destination.resize(bytes_written);

    //        test_by_decoding(destination, frame_info, source.data(), source.size(), interleave_mode::none,
    //                         color_transformation::hp1);
    //    }

    //    TEST_METHOD(encode_16_bit) // NOLINT
    //    {
    //        constexpr array source{ byte{ 0}, byte{ 1}, byte{ 2}, byte{ 3}, byte{ 4}, byte{ 5} };
    //        constexpr frame_info frame_info{ 3, 1, 16, 1};

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info);

    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        const size_t bytes_written{ encoder.encode(source)};
    //        destination.resize(bytes_written);

    //        test_by_decoding(destination, frame_info, source.data(), source.size(), interleave_mode::none);
    //    }

    [Fact]
    public void SimpleEncode16Bit()
    {
        byte[] source = [0, 1, 2, 3, 4, 5];

        var frameInfo = new FrameInfo(3, 1, 16, 1);
        var encoded = JpegLSEncoder.Encode(source, frameInfo);

        Util.TestByDecoding(encoded, frameInfo, source, InterleaveMode.None);
    }

    //    TEST_METHOD(encode_with_stride_interleave_none_8_bit) // NOLINT
    //    {
    //        constexpr array<byte, 30 > source{
    //            byte{ 100}, byte{ 100}, byte{ 100}, byte{ 0},   byte{ 0},   byte{ 0},   byte{ 0},  byte{ 0},
    //                                         byte{ 0},   byte{ 0},   byte{ 150}, byte{ 150}, byte{ 150}, byte{ 0},   byte{ 0},  byte{ 0},
    //                                         byte{ 0},   byte{ 0},   byte{ 0},   byte{ 0},   byte{ 200}, byte{ 200}, byte{ 200}
    //        };
    //        constexpr frame_info frame_info{ 3, 1, 8, 3};

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info);
    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        const size_t bytes_written{ encoder.encode(source, 10)};
    //        destination.resize(bytes_written);

    //        constexpr array expected{
    //            byte{ 100}, byte{ 100}, byte{ 100}, byte{ 150}, byte{ 150},
    //                                 byte{ 150}, byte{ 200}, byte{ 200}, byte{ 200}
    //        };
    //        test_by_decoding(destination, frame_info, expected.data(), expected.size(), interleave_mode::none);
    //    }

    //    TEST_METHOD(encode_with_stride_interleave_none_8_bit_small_image) // NOLINT
    //    {
    //        constexpr array source{ byte{ 100}, byte{ 99}, byte{ 0}, byte{ 0}, byte{ 101}, byte{ 98} };
    //        constexpr frame_info frame_info{ 2, 2, 8, 1};

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info);
    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        const size_t bytes_written{ encoder.encode(source, 4)};
    //        destination.resize(bytes_written);

    //        constexpr array expected{ byte{ 100}, byte{ 99}, byte{ 101}, byte{ 98} };
    //        test_by_decoding(destination, frame_info, expected.data(), expected.size(), interleave_mode::none);
    //    }

    //    TEST_METHOD(encode_with_stride_interleave_none_16_bit) // NOLINT
    //    {
    //        constexpr array<uint16_t, 30 > source{
    //            100, 100, 100, 0, 0, 0, 0, 0, 0,   0,   150, 150,
    //                                             150, 0,   0,   0, 0, 0, 0, 0, 200, 200, 200};
    //        constexpr frame_info frame_info{ 3, 1, 16, 3};

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info);
    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        const size_t bytes_written{ encoder.encode(source, 10 * sizeof(uint16_t))};
    //        destination.resize(bytes_written);

    //        constexpr array<uint16_t, 9 > expected{ 100, 100, 100, 150, 150, 150, 200, 200, 200};
    //        test_by_decoding(destination, frame_info, expected.data(), expected.size() * sizeof(uint16_t),
    //                         interleave_mode::none);
    //    }

    //    TEST_METHOD(encode_with_stride_interleave_sample_8_bit) // NOLINT
    //    {
    //        constexpr array source{
    //            byte{ 100}, byte{ 150}, byte{ 200}, byte{ 100}, byte{ 150},
    //                               byte{ 200}, byte{ 100}, byte{ 150}, byte{ 200}, byte{ 0}
    //        };
    //        constexpr frame_info frame_info{ 3, 1, 8, 3};

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info).interleave_mode(interleave_mode::sample);
    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        const size_t bytes_written{ encoder.encode(source, 10)};
    //        destination.resize(bytes_written);

    //        constexpr array expected{
    //            byte{ 100}, byte{ 150}, byte{ 200}, byte{ 100}, byte{ 150},
    //                                 byte{ 200}, byte{ 100}, byte{ 150}, byte{ 200}
    //        };
    //        test_by_decoding(destination, frame_info, expected.data(), expected.size(), interleave_mode::sample);
    //    }

    //    TEST_METHOD(encode_with_stride_interleave_sample_16_bit) // NOLINT
    //    {
    //        constexpr array<uint16_t, 10 > source{ 100, 150, 200, 100, 150, 200, 100, 150, 200, 0};
    //        constexpr frame_info frame_info{ 3, 1, 16, 3};

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info).interleave_mode(interleave_mode::sample);
    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        const size_t bytes_written{ encoder.encode(source, 10 * sizeof(uint16_t))};
    //        destination.resize(bytes_written);

    //        constexpr array<uint16_t, 9 > expected{ 100, 150, 200, 100, 150, 200, 100, 150, 200};
    //        test_by_decoding(destination, frame_info, expected.data(), expected.size() * sizeof(uint16_t),
    //                         interleave_mode::sample);
    //    }

    //    TEST_METHOD(encode_with_bad_stride_interleave_none_throws) // NOLINT
    //    {
    //        constexpr array<byte, 21 > source{
    //            byte{ 100}, byte{ 100}, byte{ 100}, byte{ 0},   byte{ 0},   byte{ 0},   byte{ 0},
    //                                         byte{ 0},   byte{ 0},   byte{ 0},   byte{ 150}, byte{ 150}, byte{ 150}, byte{ 0},
    //                                         byte{ 0},   byte{ 0},   byte{ 0},   byte{ 0},   byte{ 0},   byte{ 0},   byte{ 200}
    //        };
    //        constexpr frame_info frame_info{ 2, 2, 8, 3};

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info);
    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        assert_expect_exception(jpegls_errc::invalid_argument_stride,
    //                                [&encoder, &source] { ignore = encoder.encode(source, 4); });
    //    }

    //    TEST_METHOD(encode_with_bad_stride_interleave_sample_throws) // NOLINT
    //    {
    //        constexpr array<byte, 12 > source{
    //            byte{ 100}, byte{ 150}, byte{ 200}, byte{ 100}, byte{ 150},
    //                                         byte{ 200}, byte{ 100}, byte{ 150}, byte{ 200}
    //        };
    //        constexpr frame_info frame_info{ 2, 2, 8, 3};

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info).interleave_mode(interleave_mode::sample);
    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        assert_expect_exception(jpegls_errc::invalid_argument_stride,
    //                                [&encoder, &source] { ignore = encoder.encode(source, 7); });
    //    }

    //    TEST_METHOD(encode_with_too_small_stride_interleave_none_throws) // NOLINT
    //    {
    //        constexpr array source{
    //            byte{ 100}, byte{ 100}, byte{ 100}, byte{ },    byte{ },    byte{ },    byte{ },
    //                               byte{ },    byte{ },    byte{ },    byte{ 150}, byte{ 150}, byte{ 150}, byte{ },
    //                               byte{ },    byte{ },    byte{ },    byte{ },    byte{ },    byte{ },    byte{ 200}
    //        };
    //        constexpr frame_info frame_info{ 2, 1, 8, 3};

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info);
    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        assert_expect_exception(jpegls_errc::invalid_argument_stride,
    //                                [&encoder, &source] { ignore = encoder.encode(source, 1); });
    //    }

    //    TEST_METHOD(encode_with_too_small_stride_interleave_sample_throws) // NOLINT
    //    {
    //        constexpr array<byte, 12 > source{
    //            byte{ 100}, byte{ 150}, byte{ 200}, byte{ 100}, byte{ 150},
    //                                         byte{ 200}, byte{ 100}, byte{ 150}, byte{ 200}
    //        };
    //        constexpr frame_info frame_info{ 2, 1, 8, 3};

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info).interleave_mode(interleave_mode::sample);
    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        assert_expect_exception(jpegls_errc::invalid_argument_stride,
    //                                [&encoder, &source] { ignore = encoder.encode(source, 5); });
    //    }

    //    TEST_METHOD(encode_1_component_4_bit_with_high_bits_set) // NOLINT
    //    {
    //        const vector source(size_t{ 512}
    //        *512, byte{ 0xFF});
    //        constexpr frame_info frame_info{ 512, 512, 4, 1};

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info);

    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        const size_t bytes_written{ encoder.encode(source)};
    //        destination.resize(bytes_written);

    //        const vector expected(size_t{ 512}
    //        *512, byte{ 15});
    //        test_by_decoding(destination, frame_info, expected.data(), expected.size(), interleave_mode::none);
    //    }

    //    TEST_METHOD(encode_1_component_12_bit_with_high_bits_set) // NOLINT
    //    {
    //        const vector source(size_t{ 512}
    //        *512 * 2, byte{ 0xFF});
    //        constexpr frame_info frame_info{ 512, 512, 12, 1};

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info);

    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        const size_t bytes_written{ encoder.encode(source)};
    //        destination.resize(bytes_written);

    //        const vector<uint16_t> expected(size_t{ 512}
    //        *512, 4095);
    //        test_by_decoding(destination, frame_info, expected.data(), expected.size() * sizeof(uint16_t),
    //                         interleave_mode::none);
    //    }

    //    TEST_METHOD(encode_3_components_6_bit_with_high_bits_set_interleave_mode_sample) // NOLINT
    //    {
    //        const vector source(size_t{ 512}
    //        *512 * 3, byte{ 0xFF});
    //        constexpr frame_info frame_info{ 512, 512, 6, 3};

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info).interleave_mode(interleave_mode::sample);

    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        const size_t bytes_written{ encoder.encode(source)};
    //        destination.resize(bytes_written);

    //        const vector expected(size_t{ 512}
    //        *512 * 3, byte{ 63});
    //        test_by_decoding(destination, frame_info, expected.data(), expected.size(), interleave_mode::sample);
    //    }

    //    TEST_METHOD(encode_3_components_6_bit_with_high_bits_set_interleave_mode_line) // NOLINT
    //    {
    //        const vector source(size_t{ 512}
    //        *512 * 3, byte{ 0xFF});
    //        constexpr frame_info frame_info{ 512, 512, 6, 3};

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info).interleave_mode(interleave_mode::line);

    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        const size_t bytes_written{ encoder.encode(source)};
    //        destination.resize(bytes_written);

    //        const vector expected(size_t{ 512}
    //        *512 * 3, byte{ 63});
    //        test_by_decoding(destination, frame_info, expected.data(), expected.size(), interleave_mode::line);
    //    }

    //    TEST_METHOD(encode_3_components_10_bit_with_high_bits_set_interleave_mode_sample) // NOLINT
    //    {
    //        const vector source(size_t{ 512}
    //        *512 * 2 * 3, byte{ 0xFF});
    //        constexpr frame_info frame_info{ 512, 512, 10, 3};

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info).interleave_mode(interleave_mode::sample);

    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        const size_t bytes_written{ encoder.encode(source)};
    //        destination.resize(bytes_written);

    //        const vector<uint16_t> expected(size_t{ 512}
    //        *512 * 3, 1023);
    //        test_by_decoding(destination, frame_info, expected.data(), expected.size() * 2, interleave_mode::sample);
    //    }

    //    TEST_METHOD(encode_3_components_10_bit_with_high_bits_set_interleave_mode_line) // NOLINT
    //    {
    //        const vector source(size_t{ 512}
    //        *512 * 2 * 3, byte{ 0xFF});
    //        constexpr frame_info frame_info{ 512, 512, 10, 3};

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info).interleave_mode(interleave_mode::line);

    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        const size_t bytes_written{ encoder.encode(source)};
    //        destination.resize(bytes_written);

    //        const vector<uint16_t> expected(size_t{ 512}
    //        *512 * 3, 1023);
    //        test_by_decoding(destination, frame_info, expected.data(), expected.size() * 2, interleave_mode::line);
    //    }

    //    TEST_METHOD(encode_4_components_6_bit_with_high_bits_set_interleave_mode_sample) // NOLINT
    //    {
    //        const vector source(size_t{ 512}
    //        *512 * 4, byte{ 0xFF});
    //        constexpr frame_info frame_info{ 512, 512, 6, 4};

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info).interleave_mode(interleave_mode::sample);

    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        const size_t bytes_written{ encoder.encode(source)};
    //        destination.resize(bytes_written);

    //        const vector expected(size_t{ 512}
    //        *512 * 4, byte{ 63});
    //        test_by_decoding(destination, frame_info, expected.data(), expected.size(), interleave_mode::sample);
    //    }

    //    TEST_METHOD(encode_4_components_6_bit_with_high_bits_set_interleave_mode_line) // NOLINT
    //    {
    //        const vector source(size_t{ 512}
    //        *512 * 4, byte{ 0xFF});
    //        constexpr frame_info frame_info{ 512, 512, 6, 4};

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info).interleave_mode(interleave_mode::line);

    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        const size_t bytes_written{ encoder.encode(source)};
    //        destination.resize(bytes_written);

    //        const vector expected(size_t{ 512}
    //        *512 * 4, byte{ 63});
    //        test_by_decoding(destination, frame_info, expected.data(), expected.size(), interleave_mode::line);
    //    }

    //    TEST_METHOD(encode_4_components_10_bit_with_high_bits_set_interleave_mode_sample) // NOLINT
    //    {
    //        const vector source(size_t{ 512}
    //        *512 * 2 * 4, byte{ 0xFF});
    //        constexpr frame_info frame_info{ 512, 512, 10, 4};

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info).interleave_mode(interleave_mode::sample);

    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        const size_t bytes_written{ encoder.encode(source)};
    //        destination.resize(bytes_written);

    //        const vector<uint16_t> expected(size_t{ 512}
    //        *512 * 4, 1023);
    //        test_by_decoding(destination, frame_info, expected.data(), expected.size() * 2, interleave_mode::sample);
    //    }

    //    TEST_METHOD(encode_4_components_10_bit_with_high_bits_set_interleave_mode_line) // NOLINT
    //    {
    //        const vector source(size_t{ 512}
    //        *512 * 2 * 4, byte{ 0xFF});
    //        constexpr frame_info frame_info{ 512, 512, 10, 4};

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info).interleave_mode(interleave_mode::line);

    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        const size_t bytes_written{ encoder.encode(source)};
    //        destination.resize(bytes_written);

    //        const vector<uint16_t> expected(size_t{ 512}
    //        *512 * 4, 1023);
    //        test_by_decoding(destination, frame_info, expected.data(), expected.size() * 2, interleave_mode::line);
    //    }

    //    TEST_METHOD(rewind) // NOLINT
    //    {
    //        constexpr array source{ byte{ 0}, byte{ 1}, byte{ 2}, byte{ 3}, byte{ 4}, byte{ 5} };
    //        constexpr frame_info frame_info{ 3, 1, 16, 1};

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info);

    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        const size_t bytes_written1{ encoder.encode(source)};
    //        destination.resize(bytes_written1);

    //        test_by_decoding(destination, frame_info, source.data(), source.size(), interleave_mode::none);

    //        const vector destination_backup(destination);

    //        encoder.rewind();
    //        const size_t bytes_written2{ encoder.encode(source)};

    //        Assert.Equal(bytes_written1, bytes_written2);
    //        Assert.True(destination_backup == destination);
    //    }

    //    TEST_METHOD(rewind_before_destination) // NOLINT
    //    {
    //        constexpr array source{ byte{ 0}, byte{ 1}, byte{ 2}, byte{ 3}, byte{ 4}, byte{ 5} };
    //        constexpr frame_info frame_info{ 3, 1, 16, 1};

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info);

    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.rewind();
    //        encoder.destination(destination);

    //        const size_t bytes_written{ encoder.encode(source)};
    //        destination.resize(bytes_written);

    //        test_by_decoding(destination, frame_info, source.data(), source.size(), interleave_mode::none);
    //    }

    //    TEST_METHOD(encode_image_odd_size) // NOLINT
    //    {
    //        constexpr frame_info frame_info{ 512, 512, 8, 1};
    //        const vector<byte> source(static_cast<size_t>(frame_info.width)* frame_info.height);

    //        const auto destination{ jpegls_encoder::encode(source, frame_info)};

    //        Assert.Equal(size_t{ 99}, destination.size());
    //        test_by_decoding(destination, frame_info, source.data(), source.size(), interleave_mode::none);
    //    }

    //    TEST_METHOD(encode_image_odd_size_forced_even) // NOLINT
    //    {
    //        constexpr frame_info frame_info{ 512, 512, 8, 1};
    //        const vector<byte> source(static_cast<size_t>(frame_info.width)* frame_info.height);

    //        const auto destination{
    //            jpegls_encoder::encode(source, frame_info, interleave_mode::none, encoding_options::even_destination_size)};

    //        Assert.Equal(size_t{ 100}, destination.size());
    //        test_by_decoding(destination, frame_info, source.data(), source.size(), interleave_mode::none);
    //    }

    //    TEST_METHOD(encode_image_forced_version_comment) // NOLINT
    //    {
    //        constexpr frame_info frame_info{ 512, 512, 8, 1};
    //        const vector<byte> source(static_cast<size_t>(frame_info.width)* frame_info.height);

    //        const auto encoded_source{
    //            jpegls_encoder::encode(source, frame_info, interleave_mode::none, encoding_options::include_version_number)};

    //        jpegls_decoder decoder;
    //        decoder.source(encoded_source);

    //        const char* actual_data{ };
    //        size_t actual_size{ };
    //        decoder.at_comment([&actual_data, &actual_size](const void* data, const size_t size) noexcept {
    //            actual_data = static_cast <const char*> (data);
    //            actual_size = size;
    //        });

    //        decoder.read_header();

    //        const std::string expected{ "charls "s + charls_get_version_string()};

    //        Assert.Equal(expected.size() + 1, actual_size);
    //        Assert.True(memcmp(expected.data(), actual_data, actual_size) == 0);
    //    }

    //    TEST_METHOD(encode_image_include_pc_parameters_jai) // NOLINT
    //    {
    //        constexpr frame_info frame_info{ 1, 1, 16, 1};
    //        const vector<uint16_t> source(static_cast<size_t>(frame_info.width)* frame_info.height);

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info);

    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination).encoding_options(encoding_options::include_pc_parameters_jai);

    //        // Note: encoding_options::include_pc_parameters_jai is enabled by default (until the next major version)

    //        const size_t bytes_written{ encoder.encode(source)};
    //        destination.resize(bytes_written);

    //        Assert.Equal(size_t{ 43}, bytes_written);

    //        Assert.Equal(byte{ 0xFF}, destination[15]);
    //        Assert.Equal(static_cast<byte>(jpeg_marker_code::jpegls_preset_parameters), destination[16]);

    //        // Segment size.
    //        Assert.Equal(byte{ }, destination[17]);
    //        Assert.Equal(byte{ 13}, destination[18]);

    //        // Parameter ID.
    //        Assert.Equal(byte{ 0x1}, destination[19]);

    //        // MaximumSampleValue
    //        Assert.Equal(byte{ 255}, destination[20]);
    //        Assert.Equal(byte{ 255}, destination[21]);

    //        constexpr thresholds expected{
    //            compute_defaults_using_reference_implementation(std::numeric_limits < uint16_t >::max(), 0)};
    //        const int32_t threshold1{ to_integer<int32_t>(destination[22]) << 8 | to_integer<int32_t>(destination[23])};
    //        Assert.Equal(expected.t1, threshold1);
    //        const int32_t threshold2{ to_integer<int32_t>(destination[24]) << 8 | to_integer<int32_t>(destination[25])};
    //        Assert.Equal(expected.t2, threshold2);
    //        const int32_t threshold3{ to_integer<int32_t>(destination[26]) << 8 | to_integer<int32_t>(destination[27])};
    //        Assert.Equal(expected.t3, threshold3);
    //        const int32_t reset{ to_integer<int32_t>(destination[28] << 8) | to_integer<int32_t>(destination[29])};
    //        Assert.Equal(expected.reset, reset);
    //    }

    //    TEST_METHOD(encode_image_with_disabled_include_pc_parameters_jai) // NOLINT
    //    {
    //        constexpr frame_info frame_info{ 1, 1, 16, 1};
    //        const vector<uint16_t> source(static_cast<size_t>(frame_info.width)* frame_info.height);

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info);

    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);
    //        encoder.encoding_options(encoding_options::none);

    //        const size_t bytes_written{ encoder.encode(source)};

    //        Assert.Equal(size_t{ 28}, bytes_written);
    //    }

    //    TEST_METHOD(set_invalid_encode_options_throws) // NOLINT
    //    {
    //        jpegls_encoder encoder;

    //        assert_expect_exception(jpegls_errc::invalid_argument_encoding_options,
    //                                [&encoder] { encoder.encoding_options(static_cast<encoding_options>(8)); });
    //    }

    //    TEST_METHOD(large_image_contains_lse_for_oversize_image_dimension) // NOLINT
    //    {
    //        constexpr frame_info frame_info{ numeric_limits < uint16_t >::max() + 1, 1, 16, 1};
    //        const vector<uint16_t> source(static_cast<size_t>(frame_info.width)* frame_info.height);

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info);

    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        const size_t bytes_written{ encoder.encode(source)};
    //        Assert.Equal(size_t{ 46}, bytes_written);

    //        destination.resize(bytes_written);
    //        const auto it{ find_first_lse_segment(destination.cbegin(), destination.cend())};
    //        Assert.True(it != destination.cend());
    //    }

    //    TEST_METHOD(encode_oversized_image) // NOLINT
    //    {
    //        constexpr frame_info frame_info{ numeric_limits < uint16_t >::max() + 1, 1, 8, 1};
    //        const vector<byte> source(static_cast<size_t>(frame_info.width)* frame_info.height);

    //        const auto encoded_source{ jpegls_encoder::encode(source, frame_info)};

    //        test_by_decoding(encoded_source, frame_info, source.data(), source.size(), interleave_mode::none);
    //    }

    //    TEST_METHOD(image_contains_no_preset_coding_parameters_by_default) // NOLINT
    //    {
    //        constexpr frame_info frame_info{ 512, 512, 8, 1};
    //        const vector<byte> source(static_cast<size_t>(frame_info.width)* frame_info.height);

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info);

    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        const size_t bytes_written{ encoder.encode(source)};
    //        Assert.Equal(size_t{ 99}, bytes_written);

    //        destination.resize(bytes_written);
    //        const auto it{ find_first_lse_segment(destination.cbegin(), destination.cend())};
    //        Assert.True(it == destination.cend());
    //    }

    //    TEST_METHOD(image_contains_no_preset_coding_parameters_if_configured_pc_is_default) // NOLINT
    //    {
    //        constexpr frame_info frame_info{ 512, 512, 8, 1};
    //        const vector<byte> source(static_cast<size_t>(frame_info.width)* frame_info.height);

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info).preset_coding_parameters({ 255, 3, 7, 21, 64});

    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        const size_t bytes_written{ encoder.encode(source)};
    //        Assert.Equal(size_t{ 99}, bytes_written);

    //        destination.resize(bytes_written);
    //        const auto it{ find_first_lse_segment(destination.cbegin(), destination.cend())};
    //        Assert.True(it == destination.cend());
    //    }

    //    TEST_METHOD(image_contains_preset_coding_parameters_if_configured_pc_is_non_default) // NOLINT
    //    {
    //        constexpr frame_info frame_info{ 512, 512, 8, 1};
    //        const vector<byte> source(static_cast<size_t>(frame_info.width)* frame_info.height);

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info).preset_coding_parameters({ 255, 3, 7, 21, 65});

    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        const size_t bytes_written{ encoder.encode(source)};
    //        Assert.Equal(size_t{ 114}, bytes_written);

    //        destination.resize(bytes_written);
    //        const auto it{ find_first_lse_segment(destination.cbegin(), destination.cend())};
    //        Assert::IsFalse(it == destination.cend());
    //    }

    //    TEST_METHOD(image_contains_preset_coding_parameters_if_configured_pc_has_diff_max_value) // NOLINT
    //    {
    //        constexpr frame_info frame_info{ 512, 512, 8, 1};
    //        const vector<byte> source(static_cast<size_t>(frame_info.width)* frame_info.height);

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info).preset_coding_parameters({ 100, 0, 0, 0, 0});

    //        vector<byte> destination(encoder.estimated_destination_size());
    //        encoder.destination(destination);

    //        const size_t bytes_written{ encoder.encode(source)};
    //        Assert.Equal(size_t{ 114}, bytes_written);

    //        destination.resize(bytes_written);
    //        const auto it{ find_first_lse_segment(destination.cbegin(), destination.cend())};
    //        Assert::IsFalse(it == destination.cend());
    //    }

    //    TEST_METHOD(encode_to_buffer_with_uint16_size_works) // NOLINT
    //    {
    //        // These are compile time checks to detect issues with overloads that have similar conversions.
    //        constexpr frame_info frame_info{ 100, 100, 8, 1};

    //        jpegls_encoder encoder;
    //        encoder.frame_info(frame_info);

    //        vector<byte> destination(encoder.estimated_destination_size());

    //        void* data1{ destination.data()};
    //        const auto size1{ static_cast<uint16_t>(destination.size())};
    //        encoder.destination(data1, size1);

    //        vector<byte> source(static_cast<size_t>(frame_info.width)* frame_info.height);
    //        void* data2{ source.data()};
    //        const auto size2{ static_cast<uint16_t>(source.size())};

    //        // Set 1 value to prevent complains about const.
    //        auto* p{ static_cast<uint8_t*>(data2)};
    //        *p = 7;

    //        // size2 is not a perfect match and needs a conversion.
    //        ignore = encoder.encode(data2, size2);
    //    }

    //    private:
    //    static void test_by_decoding(const vector<byte>& encoded_source, const frame_info& source_frame_info,
    //                                 const void* expected_destination, const size_t expected_destination_size,
    //                                 const charls::interleave_mode interleave_mode,
    //                                 const color_transformation color_transformation = color_transformation::none)
    //    {
    //        jpegls_decoder decoder;
    //    decoder.source(encoded_source);
    //        decoder.read_header();

    //        const auto& frame_info{decoder.frame_info()
    //};
    //Assert.Equal(source_frame_info.width, frame_info.width);
    //        Assert.Equal(source_frame_info.height, frame_info.height);
    //        Assert.Equal(source_frame_info.bits_per_sample, frame_info.bits_per_sample);
    //        Assert.Equal(source_frame_info.component_count, frame_info.component_count);
    //        Assert.True(interleave_mode == decoder.interleave_mode());
    //        Assert.True(color_transformation == decoder.color_transformation());

    //        vector<byte> destination(decoder.destination_size());
    //decoder.decode(destination);

    //        Assert.Equal(destination.size(), expected_destination_size);

    //        if (decoder.near_lossless() == 0)
    //        {
    //            const auto* expected_destination_byte{static_cast<const byte*>(expected_destination)};

    //for (size_t i{ }; i != expected_destination_size; ++i)
    //            {
    //    if (expected_destination_byte[i] != destination[i]) // AreEqual is very slow, pre-test to speed up 50X
    //    {
    //        Assert.Equal(expected_destination_byte[i], destination[i]);
    //    }
    //}
    //        }
    //    }

    //    static void encode_with_custom_preset_coding_parameters(const jpegls_pc_parameters& pc_parameters)
    //{
    //    constexpr array source{ byte{ 0}, byte{ 1}, byte{ 1}, byte{ 1}, byte{ 0} };
    //    constexpr frame_info frame_info{ 5, 1, 8, 1};

    //    jpegls_encoder encoder;
    //    encoder.frame_info(frame_info);
    //    vector<byte> destination(encoder.estimated_destination_size());
    //    encoder.destination(destination);

    //    encoder.preset_coding_parameters(pc_parameters);

    //    const size_t bytes_written{ encoder.encode(source)};
    //    destination.resize(bytes_written);

    //    test_by_decoding(destination, frame_info, source.data(), source.size(), interleave_mode::none);
    //}

    //// ReSharper disable CppPassValueParameterByConstReference (iterators are not simple pointers in debug builds)
    //static vector<byte>::const_iterator find_first_lse_segment(const vector<byte>::const_iterator begin,
    //                                                               const vector<byte>::const_iterator end) noexcept
    //    // ReSharper restore CppPassValueParameterByConstReference
    //    {
    //        constexpr byte lse_marker{0xF8};

    //for (auto it{ begin}; it != end; ++it)
    //        {
    //    if (*it == byte{ 0xFF} && it + 1 != end && *(it + 1) == lse_marker)
    //                return it;
    //}

    //return end;
    //    }

}
