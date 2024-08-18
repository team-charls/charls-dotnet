// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Runtime.InteropServices;

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
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => _ = new JpegLSEncoder(0, 1, 2, 1));
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

    [Fact]
    public void EstimatedDestinationSizeMaximalFrameInfoThrows()
    {
        JpegLSEncoder encoder = new();
        var frameInfo = new FrameInfo(int.MaxValue, int.MaxValue, 8, 1);
        encoder.FrameInfo = frameInfo;

        Assert.Throws<OverflowException>(() => encoder.EstimatedDestinationSize);
    }

    [Fact]
    public void EstimatedDestinationSizeMonochrome16Bit()
    {
        JpegLSEncoder encoder = new();
        var frameInfo = new FrameInfo(100, 100, 16, 1);

        encoder.FrameInfo = frameInfo;
        var size = encoder.EstimatedDestinationSize;
        Assert.True(size >= 100 * 100 * 2);
    }

    [Fact]
    public void EstimatedDestinationSizeColor8Bit()
    {
        JpegLSEncoder encoder = new();
        var frameInfo = new FrameInfo(2000, 2000, 8, 3);

        encoder.FrameInfo = frameInfo;

        var size = encoder.EstimatedDestinationSize;
        Assert.True(size >= 2000 * 2000 * 3);
    }

    [Fact]
    public void EstimatedDestinationSizeVeryWide()
    {
        JpegLSEncoder encoder = new();
        var frameInfo = new FrameInfo(ushort.MaxValue, 1, 8, 1);

        encoder.FrameInfo = frameInfo;

        var size = encoder.EstimatedDestinationSize;
        Assert.True(size >= ushort.MaxValue + 1024U);
    }

    [Fact]
    public void EstimatedDestinationSizeVeryHigh()
    {
        JpegLSEncoder encoder = new();
        var frameInfo = new FrameInfo(1, ushort.MaxValue, 8, 1);

        encoder.FrameInfo = frameInfo;

        var size = encoder.EstimatedDestinationSize;
        Assert.True(size >= ushort.MaxValue + 1024U);
    }

    [Fact]
    public void EstimatedDestinationSizeTooSoonThrows()
    {
        JpegLSEncoder encoder = new();

        var exception = Assert.Throws<InvalidOperationException>(() => encoder.EstimatedDestinationSize);
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

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
        Assert.Equal((byte)32, destination[5]);
        Assert.Equal((byte)'S', destination[6]);
        Assert.Equal((byte)'P', destination[7]);
        Assert.Equal((byte)'I', destination[8]);
        Assert.Equal((byte)'F', destination[9]);
        Assert.Equal((byte)'F', destination[10]);
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

    [Fact]
    public void WriteSpiffHeaderInvalidHeightThrows()
    {
        JpegLSEncoder encoder = new() { FrameInfo = new FrameInfo(1, 1, 2, 1) };

        byte[] destination = new byte[encoder.EstimatedDestinationSize];
        encoder.Destination = destination;

        SpiffHeader spiffHeader = new SpiffHeader() { Width = 1 };

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => encoder.WriteSpiffHeader(spiffHeader));
        Assert.Equal(ErrorCode.InvalidArgumentHeight, exception.GetErrorCode());
        Assert.Equal(0, encoder.BytesWritten);
    }

    [Fact]
    public void WriteSpiffHeaderInvalidWidthThrows()
    {
        JpegLSEncoder encoder = new() { FrameInfo = new FrameInfo(1, 1, 2, 1) };

        byte[] destination = new byte[encoder.EstimatedDestinationSize];
        encoder.Destination = destination;

        SpiffHeader spiffHeader = new SpiffHeader() { Height = 1 };

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => encoder.WriteSpiffHeader(spiffHeader));
        Assert.Equal(ErrorCode.InvalidParameterWidth, exception.GetErrorCode());
        Assert.Equal(0, encoder.BytesWritten);
    }

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

    [Fact]
    public void WriteSpiffEntryWithInvalidSizeThrows()
    {
        JpegLSEncoder encoder = new() { FrameInfo = new FrameInfo(1, 1, 2, 4) };

        var destination = new byte[encoder.EstimatedDestinationSize];
        encoder.Destination = destination;
        encoder.WriteStandardSpiffHeader(SpiffColorSpace.Cmyk);

        var data = new byte[655281];
        var exception = Assert.Throws<ArgumentException>(() => encoder.WriteSpiffEntry(SpiffEntryTag.ImageTitle, data));
        Assert.Equal(ErrorCode.InvalidArgumentSize, exception.GetErrorCode());
    }

    [Fact]
    public void WriteSpiffEntryWithoutSpiffHeaderThrows()
    {
        JpegLSEncoder encoder = new() { FrameInfo = new FrameInfo(1, 1, 2, 1) };
        var destination = new byte[encoder.EstimatedDestinationSize];
        encoder.Destination = destination;

        var data = new byte[100];
        var exception = Assert.Throws<InvalidOperationException>(() => encoder.WriteSpiffEntry(SpiffEntryTag.ImageTitle, data));
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    [Fact]
    public void WriteSpiffEndOfDirectoryEntry()
    {
        JpegLSEncoder encoder = new() { FrameInfo = new FrameInfo(1, 1, 2, 1) };

        var destination = new byte[300];
        encoder.Destination = destination;

        encoder.WriteStandardSpiffHeader(SpiffColorSpace.None);
        encoder.WriteSpiffEndOfDirectoryEntry();

        Assert.Equal(0xFF, destination[44]);
        Assert.Equal(0xD8, destination[45]); // 0xD8 = SOI: Marks the start of an image.
    }

    [Fact]
    public void WriteSpiffEndOfDirectoryEntryBeforeHeaderThrows()
    {
        JpegLSEncoder encoder = new();

        var destination = new byte[300];
        encoder.Destination = destination;

        var exception = Assert.Throws<InvalidOperationException>(encoder.WriteSpiffEndOfDirectoryEntry);
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    [Fact]
    public void WriteSpiffEndOfDirectoryEntryTwiceThrows()
    {
        JpegLSEncoder encoder = new() { FrameInfo = new FrameInfo(1, 1, 2, 1) };

        var destination = new byte[300];
        encoder.Destination = destination;

        encoder.WriteStandardSpiffHeader(SpiffColorSpace.None);
        encoder.WriteSpiffEndOfDirectoryEntry();

        var exception = Assert.Throws<InvalidOperationException>(encoder.WriteSpiffEndOfDirectoryEntry);
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

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
        Assert.Equal(2 + 4, destination[5]);
        Assert.Equal((byte)'1', destination[6]);
        Assert.Equal((byte)'2', destination[7]);
        Assert.Equal((byte)'3', destination[8]);
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

    [Fact]
    public void WriteMaxComment()
    {
        JpegLSEncoder encoder = new();

        var destination = new byte[2 + 2 + ushort.MaxValue];
        encoder.Destination = destination;

        const int maxSizeCommentData = ushort.MaxValue - 2;
        byte[] data = new byte[maxSizeCommentData];
        encoder.WriteComment(data);

        Assert.Equal(destination.Length, encoder.BytesWritten);

        // Check that SOI marker has been written.
        Assert.Equal(0xFF, destination[0]);
        Assert.Equal((byte)JpegMarkerCode.StartOfImage, destination[1]);

        // Verify that a COM segment has been written.
        Assert.Equal(0xFF, destination[2]);
        Assert.Equal((byte)JpegMarkerCode.Comment, destination[3]);
        Assert.Equal(255, destination[4]);
        Assert.Equal(255, destination[5]);
    }

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

    [Fact]
    public void WriteCommentAfterEncodeThrows()
    {
        byte[] source = [0, 1, 2, 3, 4, 5];

        JpegLSEncoder encoder = new();

        var destination = new byte[2 + 2 + ushort.MaxValue];
        encoder.Destination = destination;

        encoder.FrameInfo = new FrameInfo(3, 1, 16, 1);
        encoder.Encode(source);

        var exception = Assert.Throws<InvalidOperationException>(() => encoder.WriteComment("after-encoding"));
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    [Fact]
    public void WriteCommentBeforeEncode()
    {
        byte[] source = [0, 1, 2, 3, 4, 5];
        var frameInfo = new FrameInfo(3, 1, 16, 1);

        JpegLSEncoder encoder = new();
        var encoded = new byte[100];
        encoder.Destination = encoded;
        encoder.FrameInfo = frameInfo;

        encoder.WriteComment("my comment");

        encoder.Encode(source);
        Util.TestByDecoding(encoder.EncodedData, frameInfo, source, InterleaveMode.None);
    }

    [Fact]
    public void WriteApplicationData()
    {
        JpegLSEncoder encoder = new();

        var destination = new byte[10];
        encoder.Destination = destination;

        byte[] applicationData = [1, 2, 3, 4];
        encoder.WriteApplicationData(1, applicationData);

        Assert.Equal(10, encoder.BytesWritten);

        // Check that SOI marker has been written.
        Assert.Equal(0xFF, destination[0]);
        Assert.Equal((byte)(JpegMarkerCode.StartOfImage), destination[1]);

        // Verify that a APPn segment has been written.
        Assert.Equal(0xFF, destination[2]);
        Assert.Equal((byte)JpegMarkerCode.ApplicationData1, destination[3]);
        Assert.Equal(0, destination[4]);
        Assert.Equal(2 + 4, destination[5]);
        Assert.Equal(1, destination[6]);
        Assert.Equal(2, destination[7]);
        Assert.Equal(3, destination[8]);
        Assert.Equal(4, destination[9]);
    }

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

    [Fact]
    public void WriteApplicationDataAfterEncodeThrows()
    {
        byte[] source = [0, 1, 2, 3, 4, 5];

        JpegLSEncoder encoder = new();

        var destination = new byte[2 + 2 + ushort.MaxValue];
        encoder.Destination = destination;

        encoder.FrameInfo = new FrameInfo(3, 1, 16, 1);
        encoder.Encode(source);

        var exception = Assert.Throws<InvalidOperationException>(() => encoder.WriteApplicationData(0, []));
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    [Fact]
    public void WriteApplicationDataWithBadIdThrows()
    {
        JpegLSEncoder encoder = new();

        var destination = new byte[100];
        encoder.Destination = destination;

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => encoder.WriteApplicationData(-1, []));
        Assert.Equal(ErrorCode.InvalidArgument, exception.GetErrorCode());
        exception = Assert.Throws<ArgumentOutOfRangeException>(() => encoder.WriteApplicationData(16, []));
        Assert.Equal(ErrorCode.InvalidArgument, exception.GetErrorCode());
    }

    [Fact]
    public void WriteApplicationDataBeforeEncode()
    {
        byte[] source = [0, 1, 2, 3, 4, 5];
        var frameInfo = new FrameInfo(3, 1, 16, 1);

        JpegLSEncoder encoder = new();
        var encoded = new byte[100];
        encoder.Destination = encoded;
        encoder.FrameInfo = frameInfo;

        encoder.WriteApplicationData(11, ReadOnlySpan<byte>.Empty);

        encoder.Encode(source);
        Util.TestByDecoding(encoder.EncodedData, frameInfo, source, InterleaveMode.None);
    }

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

    [Fact]
    public void WriteMappingTableBeforeEncode()
    {
        byte[] tableData = [0, 1, 2, 3, 4, 5];
        byte[] source = [0, 1, 2, 3, 4, 5];
        var frameInfo = new FrameInfo(3, 1, 16, 1);

        JpegLSEncoder encoder = new();
        var encoded = new byte[100];
        encoder.Destination = encoded;
        encoder.FrameInfo = frameInfo;

        encoder.WriteMappingTable(1, 1, tableData);

        encoder.Encode(source);
        Util.TestByDecoding(encoder.EncodedData, frameInfo, source, InterleaveMode.None);
    }

    [Fact]
    public void WriteTableWithBadTableIdThrows()
    {
        byte[] tableData = [0, 1, 2, 3, 4, 5];
        JpegLSEncoder encoder = new();

        var destination = new byte[100];
        encoder.Destination = destination;

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => encoder.WriteMappingTable(0, 1, tableData));
        Assert.Equal(ErrorCode.InvalidArgument, exception.GetErrorCode());

        exception = Assert.Throws<ArgumentOutOfRangeException>(() => encoder.WriteMappingTable(256, 1, tableData));
        Assert.Equal(ErrorCode.InvalidArgument, exception.GetErrorCode());
    }

    [Fact]
    public void WriteTableWithBadEntrySizeThrows()
    {
        byte[] tableData = [0, 1, 2, 3, 4, 5];
        JpegLSEncoder encoder = new();

        var destination = new byte[100];
        encoder.Destination = destination;

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => encoder.WriteMappingTable(1, 0, tableData));
        Assert.Equal(ErrorCode.InvalidArgument, exception.GetErrorCode());

        exception = Assert.Throws<ArgumentOutOfRangeException>(() => encoder.WriteMappingTable(1, 256, tableData));
        Assert.Equal(ErrorCode.InvalidArgument, exception.GetErrorCode());
    }

    [Fact]
    public void WriteTableWithTooSmallTableThrows()
    {
        byte[] tableData = [0];
        JpegLSEncoder encoder = new();

        var destination = new byte[100];
        encoder.Destination = destination;

        var exception = Assert.Throws<ArgumentException>(() => encoder.WriteMappingTable(1, 2, tableData));
        Assert.Equal(ErrorCode.InvalidArgumentSize, exception.GetErrorCode());
    }

    [Fact]
    public void WriteTableAfterEncodeThrows()
    {
        byte[] tableData = [0];
        byte[] source = [0, 1, 2, 3, 4, 5];

        JpegLSEncoder encoder = new();

        var destination = new byte[100];
        encoder.Destination = destination;
        encoder.FrameInfo = new FrameInfo(3, 1, 16, 1);
        encoder.Encode(source);

        var exception = Assert.Throws<InvalidOperationException>(() => encoder.WriteMappingTable(1, 1, tableData));
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    [Fact]
    public void CreateAbbreviatedFormat()
    {
        JpegLSEncoder encoder = new();

        var destination = new byte[12];
        encoder.Destination = destination;

        byte[] tableData = [0];
        encoder.WriteMappingTable(1, 1, tableData);

        encoder.CreateAbbreviatedFormat();

        Assert.Equal(12, encoder.BytesWritten);

        // Check that SOI marker has been written.
        Assert.Equal(0xFF, destination[0]);
        Assert.Equal((byte)JpegMarkerCode.StartOfImage, destination[1]);

        // Verify that a JPEG-LS preset segment with the table has been written.
        Assert.Equal(0xFF, destination[2]);
        Assert.Equal((byte)JpegMarkerCode.JpegLSPresetParameters, destination[3]);
        Assert.Equal(0, destination[4]);
        Assert.Equal(6, destination[5]);
        Assert.Equal(2, destination[6]);
        Assert.Equal(1, destination[7]);
        Assert.Equal(1, destination[8]);
        Assert.Equal(0, destination[9]);

        // Check that SOI marker has been written.
        Assert.Equal(0xFF, destination[10]);
        Assert.Equal((byte)JpegMarkerCode.EndOfImage, destination[11]);
    }

    [Fact]
    public void CreateTablesOnlyWithNoTablesThrows()
    {
        JpegLSEncoder encoder = new();

        var destination = new byte[12];
        encoder.Destination = destination;

        var exception = Assert.Throws<InvalidOperationException>(() => encoder.CreateAbbreviatedFormat());
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    [Fact]
    public void SetPresetCodingParameters()
    {
        JpegLSEncoder encoder = new();

        var presetCodingParameters = new JpegLSPresetCodingParameters();
        encoder.PresetCodingParameters = presetCodingParameters;

        // No explicit test possible, code should remain stable.
        Assert.True(true);
    }

    [Fact]
    public void SetPresetCodingParametersBadValuesThrows()
    {
        byte[] source = [0, 1, 1, 1, 0];
        JpegLSEncoder encoder = new() { FrameInfo = new FrameInfo(5, 1, 8, 1) };
        var destination = new byte[encoder.EstimatedDestinationSize];
        encoder.Destination = destination;
        var jpegLSPresetCodingParameters = new JpegLSPresetCodingParameters(1, 1, 1, 1, 1);
        encoder.PresetCodingParameters = jpegLSPresetCodingParameters;

        var exception = Assert.Throws<ArgumentException>(() => encoder.Encode(source));
        Assert.Equal(ErrorCode.InvalidArgumentPresetCodingParameters, exception.GetErrorCode());
    }

    [Fact]
    public void EncodeWithPresetCodingParametersNonDefaultValues()
    {
        EncodeWithCustomPresetCodingParameters(new JpegLSPresetCodingParameters(1, 0, 0, 0, 0));
        EncodeWithCustomPresetCodingParameters(new JpegLSPresetCodingParameters(0, 1, 0, 0, 0));
        EncodeWithCustomPresetCodingParameters(new JpegLSPresetCodingParameters(0, 0, 4, 0, 0));
        EncodeWithCustomPresetCodingParameters(new JpegLSPresetCodingParameters(0, 0, 0, 8, 0));
        EncodeWithCustomPresetCodingParameters(new JpegLSPresetCodingParameters(0, 1, 2, 3, 0));
        EncodeWithCustomPresetCodingParameters(new JpegLSPresetCodingParameters(0, 0, 0, 0, 63));
    }

    [Fact]
    public void SetColorTransformationBadValueThrows()
    {
        JpegLSEncoder encoder = new();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => encoder.ColorTransformation = (ColorTransformation)100);
        Assert.Equal(ErrorCode.InvalidArgumentColorTransformation, exception.GetErrorCode());
    }

    [Fact]
    public void SetMappingTableId()
    {
        byte[] source = [0, 1, 2, 3, 4, 5];
        JpegLSEncoder encoder = new() { FrameInfo = new FrameInfo(2, 1, 16, 1) };
        var destination = new byte[encoder.EstimatedDestinationSize];
        encoder.Destination = destination;

        encoder.SetMappingTableId(0, 1);
        encoder.Encode(source);

        JpegLSDecoder decoder = new(encoder.EncodedData);

        byte[] destinationDecoded = new byte[decoder.GetDestinationSize()];
        decoder.Decode(destinationDecoded);
        Assert.Equal(1, decoder.GetMappingTableId(0));
    }

    [Fact]
    public void SetMappingTableIdClearId()
    {
        byte[] source = [0, 1, 2, 3, 4, 5];
        JpegLSEncoder encoder = new() { FrameInfo = new FrameInfo(2, 1, 16, 1) };
        var destination = new byte[encoder.EstimatedDestinationSize];
        encoder.Destination = destination;

        encoder.SetMappingTableId(0, 7);
        encoder.SetMappingTableId(0, 0);

        encoder.Encode(source);
        JpegLSDecoder decoder = new(encoder.EncodedData);
        byte[] destinationDecoded = new byte[decoder.GetDestinationSize()];
        decoder.Decode(destinationDecoded);
        Assert.Equal(0, decoder.GetMappingTableId(0));
    }

    [Fact]
    public void SetMappingTableIdBadComponentIndexThrows()
    {
        JpegLSEncoder encoder = new();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => encoder.SetMappingTableId(-1, 0));
        Assert.Equal(ErrorCode.InvalidArgument, exception.GetErrorCode());
    }

    [Fact]
    public void SetMappingTableIdBadIdThrows()
    {
        JpegLSEncoder encoder = new();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => encoder.SetMappingTableId(0, -1));
        Assert.Equal(ErrorCode.InvalidArgument, exception.GetErrorCode());
    }

    [Fact]
    public void EncodeWithoutDestinationThrows()
    {
        JpegLSEncoder encoder = new() { FrameInfo = new FrameInfo(1, 1, 2, 1) };
        byte[] source = new byte[20];

        var exception = Assert.Throws<InvalidOperationException>(() => encoder.Encode(source));
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    [Fact]
    public void EncodeWithoutFrameInfoThrows()
    {
        JpegLSEncoder encoder = new();
        byte[] source = new byte[20];
        byte[] destination = new byte[20];
        encoder.Destination = destination;

        var exception = Assert.Throws<InvalidOperationException>(() => encoder.Encode(source));
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    [Fact]
    public void EncodeWithSpiffHeader()
    {
        byte[] source = [0, 1, 2, 3, 4];

        JpegLSEncoder encoder = new() { FrameInfo = new FrameInfo(5, 1, 8, 1) };
        var destination = new byte[encoder.EstimatedDestinationSize];
        encoder.Destination = destination;

        encoder.WriteStandardSpiffHeader(SpiffColorSpace.Grayscale);
        encoder.Encode(source);

        Util.TestByDecoding(encoder.EncodedData, encoder.FrameInfo, source, InterleaveMode.None);
    }

    [Fact]
    public void EncodeWithColorTransformation()
    {
        byte[] source = [0, 1, 2, 3, 4, 5];

        JpegLSEncoder encoder = new() { FrameInfo = new FrameInfo(2, 1, 8, 3) };
        var destination = new byte[encoder.EstimatedDestinationSize];
        encoder.Destination = destination;

        encoder.ColorTransformation = ColorTransformation.HP1;
        encoder.Encode(source);

        Util.TestByDecoding(encoder.EncodedData, encoder.FrameInfo, source, InterleaveMode.None);
    }

    [Fact]
    public void Encode16Bit()
    {
        byte[] source = [0, 1, 2, 3, 4, 5];

        JpegLSEncoder encoder = new() { FrameInfo = new FrameInfo(3, 1, 16, 1) };
        encoder.Destination = new byte[encoder.EstimatedDestinationSize];

        encoder.Encode(source);

        Util.TestByDecoding(encoder.EncodedData, encoder.FrameInfo, source, InterleaveMode.None);
    }

    [Fact]
    public void SimpleEncode16Bit()
    {
        byte[] source = [0, 1, 2, 3, 4, 5];

        var frameInfo = new FrameInfo(3, 1, 16, 1);
        var encoded = JpegLSEncoder.Encode(source, frameInfo);

        Util.TestByDecoding(encoded, frameInfo, source, InterleaveMode.None);
    }

    [Fact]
    public void EncodeWithStrideInterleaveNone8Bit()
    {
        byte[] source =
        [
            100, 100, 100, 0, 0, 0, 0, 0,
            0, 0, 150, 150, 150, 0, 0, 0,
            0, 0, 0, 0, 200, 200, 200,0,0,0,0,0,0,0
        ];
        JpegLSEncoder encoder = new(3, 1, 8, 3);

        encoder.Encode(source, 10);

        byte[] expectedDestination = [100, 100, 100, 150, 150, 150, 200, 200, 200];
        Util.TestByDecoding(encoder.EncodedData, encoder.FrameInfo!, expectedDestination, InterleaveMode.None);
    }

    [Fact]
    public void EncodeWithStrideInterleaveNone8BitSmallImage()
    {
        byte[] source = [100, 99, 0, 0, 101, 98];
        JpegLSEncoder encoder = new(2, 2, 8, 1);

        encoder.Encode(source, 4);

        byte[] expectedDestination = [100, 99, 101, 98];
        Util.TestByDecoding(encoder.EncodedData, encoder.FrameInfo!, expectedDestination, InterleaveMode.None);
    }

    [Fact]
    public void EncodeWithStrideInterleaveNone16Bit()
    {
        ushort[] source = [
            100, 100, 100, 0, 0, 0, 0, 0, 0,   0,   150, 150,
            150, 0,   0,   0, 0, 0, 0, 0, 200, 200, 200, 0, 0, 0, 0, 0, 0, 0];
        JpegLSEncoder encoder = new(3, 1, 16, 3);

        encoder.Encode(ConvertToByteArray(source), 10 * sizeof(ushort));

        ushort[] expectedDestination = [100, 100, 100, 150, 150, 150, 200, 200, 200];
        Util.TestByDecoding(encoder.EncodedData, encoder.FrameInfo!,
            MemoryMarshal.AsBytes(new ReadOnlySpan<ushort>(expectedDestination)), InterleaveMode.None);
    }

    [Fact]
    public void EncodeWithStrideInterleaveSample8Bit()
    {
        byte[] source = [100, 150, 200, 100, 150, 200, 100, 150, 200, 0];
        JpegLSEncoder encoder = new(3, 1, 8, 3) { InterleaveMode = InterleaveMode.Sample };

        encoder.Encode(source, 10);

        byte[] expectedDestination = [100, 150, 200, 100, 150, 200, 100, 150, 200];
        Util.TestByDecoding(encoder.EncodedData, encoder.FrameInfo!, expectedDestination, InterleaveMode.Sample);
    }

    [Fact]
    public void EncodeWithStrideInterleaveSample16Bit()
    {
        ushort[] source = [100, 150, 200, 100, 150, 200, 100, 150, 200, 0];
        JpegLSEncoder encoder = new(3, 1, 16, 3) { InterleaveMode = InterleaveMode.Sample };

        encoder.Encode(ConvertToByteArray(source), 10 * sizeof(ushort));

        ushort[] expectedDestination = [100, 150, 200, 100, 150, 200, 100, 150, 200];
        Util.TestByDecoding(encoder.EncodedData, encoder.FrameInfo!,
            MemoryMarshal.AsBytes(new ReadOnlySpan<ushort>(expectedDestination)), InterleaveMode.Sample);
    }

    [Fact]
    public void EncodeWithBadStrideInterleaveNoneThrows()
    {
        byte[] source = [100, 100, 100, 0, 0, 0, 0, 0, 0, 0, 150, 150, 150, 0, 0, 0, 0, 0, 0, 0, 200];
        JpegLSEncoder encoder = new(2, 2, 8, 3) { InterleaveMode = InterleaveMode.None };

        var exception = Assert.Throws<ArgumentException>(() => encoder.Encode(source, 4));
        Assert.Equal(ErrorCode.InvalidArgumentStride, exception.GetErrorCode());
    }

    [Fact]
    public void EncodeWithBadStrideInterleaveSampleThrows()
    {
        byte[] source = [100, 150, 200, 100, 150, 200, 100, 150, 200, 0, 0, 0];
        JpegLSEncoder encoder = new(2, 2, 8, 3) { InterleaveMode = InterleaveMode.Sample };

        var exception = Assert.Throws<ArgumentException>(() => encoder.Encode(source, 4));
        Assert.Equal(ErrorCode.InvalidArgumentStride, exception.GetErrorCode());
    }


    [Fact]
    public void EncodeWithTooSmallStrideInterleaveNoneThrows()
    {
        byte[] source = [100, 100, 100, 0, 0, 0, 0, 0, 0, 0, 150, 150, 150, 0, 0, 0, 0, 0, 0, 0, 200];
        JpegLSEncoder encoder = new(2, 1, 8, 3);

        var exception = Assert.Throws<ArgumentException>(() => encoder.Encode(source, 1));
        Assert.Equal(ErrorCode.InvalidArgumentStride, exception.GetErrorCode());
    }

    [Fact]
    public void EncodeWithTooSmallStrideInterleaveSampleThrows()
    {
        byte[] source = [100, 150, 200, 100, 150, 200, 100, 150, 200];
        JpegLSEncoder encoder = new(2, 1, 8, 3) { InterleaveMode = InterleaveMode.Sample };

        var exception = Assert.Throws<ArgumentException>(() => encoder.Encode(source, 5));
        Assert.Equal(ErrorCode.InvalidArgumentStride, exception.GetErrorCode());
    }

    [Fact]
    public void Encode1Component4BitWithHighBitsSet()
    {
        byte[] source = new byte[512 * 512];
        Array.Fill(source, (byte)0xFF);

        JpegLSEncoder encoder = new(512, 512, 4, 1);

        encoder.Encode(source);

        byte[] expected = new byte[512 * 512];
        Array.Fill(expected, (byte)15);
        Util.TestByDecoding(encoder.EncodedData, encoder.FrameInfo!, expected, InterleaveMode.None);
    }

    [Fact]
    public void Encode1Component12BitWithHighBitsSet()
    {
        byte[] source = new byte[512 * 512 * 2];
        Array.Fill(source, (byte)0xFF);

        JpegLSEncoder encoder = new(512, 512, 12, 1);

        encoder.Encode(source);

        ushort[] expectedDestination = new ushort[512 * 512];
        Array.Fill(expectedDestination, (ushort)4095);

        Util.TestByDecoding(encoder.EncodedData, encoder.FrameInfo!,
            MemoryMarshal.AsBytes(new ReadOnlySpan<ushort>(expectedDestination)), InterleaveMode.None);
    }

    [Fact]
    public void Encode3Components6BitWithHighBitsSetInterleaveModeSample()
    {
        byte[] source = new byte[512 * 512 * 3];
        Array.Fill(source, (byte)0xFF);

        JpegLSEncoder encoder = new(512, 512, 6, 3) { InterleaveMode = InterleaveMode.Sample };

        encoder.Encode(source);

        byte[] expectedDestination = new byte[512 * 512 * 3];
        Array.Fill(expectedDestination, (byte)63);
        Util.TestByDecoding(encoder.EncodedData, encoder.FrameInfo!, expectedDestination, InterleaveMode.Sample);
    }

    [Fact]
    public void Encode3Components6BitWithHighBitsSetInterleaveModeLine()
    {
        byte[] source = new byte[512 * 512 * 3];
        Array.Fill(source, (byte)0xFF);

        JpegLSEncoder encoder = new(512, 512, 6, 3) { InterleaveMode = InterleaveMode.Line };

        encoder.Encode(source);

        byte[] expectedDestination = new byte[512 * 512 * 3];
        Array.Fill(expectedDestination, (byte)63);
        Util.TestByDecoding(encoder.EncodedData, encoder.FrameInfo!, expectedDestination, InterleaveMode.Line);
    }

    [Fact]
    public void Encode3Components10BitWithHighBitsSetInterleaveModeSample()
    {
        byte[] source = new byte[512 * 512 * 3 * 2];
        Array.Fill(source, (byte)0xFF);

        JpegLSEncoder encoder = new(512, 512, 10, 3) { InterleaveMode = InterleaveMode.Sample };

        encoder.Encode(source);

        ushort[] expectedDestination = new ushort[512 * 512 * 3];
        Array.Fill(expectedDestination, (ushort)1023);

        Util.TestByDecoding(encoder.EncodedData, encoder.FrameInfo!,
            MemoryMarshal.AsBytes(new ReadOnlySpan<ushort>(expectedDestination)), InterleaveMode.Sample);
    }

    [Fact]
    public void Encode3Components10BitWithHighBitsSetInterleaveModeLine()
    {
        byte[] source = new byte[512 * 512 * 3 * 2];
        Array.Fill(source, (byte)0xFF);

        JpegLSEncoder encoder = new(512, 512, 10, 3) { InterleaveMode = InterleaveMode.Line };

        encoder.Encode(source);

        ushort[] expectedDestination = new ushort[512 * 512 * 3];
        Array.Fill(expectedDestination, (ushort)1023);

        Util.TestByDecoding(encoder.EncodedData, encoder.FrameInfo!,
            MemoryMarshal.AsBytes(new ReadOnlySpan<ushort>(expectedDestination)), InterleaveMode.Line);
    }

    [Fact]
    public void Encode4Components6BitWithHighBitsSetInterleaveModeLine()
    {
        byte[] source = new byte[512 * 512 * 4];
        Array.Fill(source, (byte)0xFF);

        JpegLSEncoder encoder = new(512, 512, 6, 4) { InterleaveMode = InterleaveMode.Line }; // TODO: change in 5 bits

        encoder.Encode(source);

        byte[] expectedDestination = new byte[512 * 512 * 4];
        Array.Fill(expectedDestination, (byte)63);
        Util.TestByDecoding(encoder.EncodedData, encoder.FrameInfo!, expectedDestination, InterleaveMode.Line);
    }

    [Fact]
    public void Encode4Components6BitWithHighBitsSetInterleaveModeSample()
    {
        byte[] source = new byte[512 * 512 * 4];
        Array.Fill(source, (byte)0xFF);

        JpegLSEncoder encoder = new(512, 512, 6, 4) { InterleaveMode = InterleaveMode.Sample }; // TODO: change in 7 bits

        encoder.Encode(source);

        byte[] expectedDestination = new byte[512 * 512 * 4];
        Array.Fill(expectedDestination, (byte)63);
        Util.TestByDecoding(encoder.EncodedData, encoder.FrameInfo!, expectedDestination, InterleaveMode.Sample);
    }

    [Fact]
    public void Encode4Components10BitWithHighBitsSetInterleaveModeLine()
    {
        byte[] source = new byte[512 * 512 * 4 * 2];
        Array.Fill(source, (byte)0xFF);

        JpegLSEncoder encoder = new(512, 512, 10, 4) { InterleaveMode = InterleaveMode.Line }; // TODO change to 11 bits.

        encoder.Encode(source);

        ushort[] expectedDestination = new ushort[512 * 512 * 4];
        Array.Fill(expectedDestination, (ushort)1023);

        Util.TestByDecoding(encoder.EncodedData, encoder.FrameInfo!,
            MemoryMarshal.AsBytes(new ReadOnlySpan<ushort>(expectedDestination)), InterleaveMode.Line);
    }

    //[Fact]
    //public void Encode4Components10BitWithHighBitsSetInterleaveModeSample()
    //{
    //    byte[] source = new byte[512 * 512 * 4 * 2];
    //    //Array.Fill(source, (byte)0xFF);

    //    JpegLSEncoder encoder = new(512, 512, 12, 4) { InterleaveMode = InterleaveMode.Sample }; // TODO change to 13 bits

    //    encoder.Encode(source);

    //    ushort[] expectedDestination = new ushort[512 * 512 * 4];
    //    Array.Fill(expectedDestination, (ushort)1023);

    //    Util.TestByDecoding(encoder.EncodedData, encoder.FrameInfo!,
    //        MemoryMarshal.AsBytes(new ReadOnlySpan<ushort>(expectedDestination)), InterleaveMode.Sample);
    //}

    [Fact]
    public void Rewind()
    {
        byte[] source = [0, 1, 2, 3, 4, 5];
        JpegLSEncoder encoder = new(3, 1, 16, 1); // TODO: passing 3 components causes a crash: improve source size checking.
        encoder.Encode(source);
        Util.TestByDecoding(encoder.EncodedData, encoder.FrameInfo!, source, InterleaveMode.None);
        byte[] destinationBackup = new byte[encoder.BytesWritten];
        encoder.EncodedData.Span.CopyTo(destinationBackup);
        int bytesWritten1 = encoder.BytesWritten;

        encoder.Rewind();

        encoder.Encode(source);

        Assert.Equal(bytesWritten1, encoder.BytesWritten);
        Assert.True(destinationBackup.SequenceEqual(encoder.EncodedData.ToArray()));
    }

    [Fact]
    public void RewindBeforeDestination()
    {
        byte[] source = [0, 1, 2, 3, 4, 5];
        JpegLSEncoder encoder = new(3, 1, 16, 1, false);

        byte[] destination = new byte[encoder.EstimatedDestinationSize];
        encoder.Rewind();
        encoder.Destination = destination;
        encoder.Encode(source);

        Util.TestByDecoding(encoder.EncodedData, encoder.FrameInfo!, source, InterleaveMode.None);
    }

    [Fact]
    public void EncodeImageOddSize()
    {
        JpegLSEncoder encoder = new(512, 512, 8, 1);

        byte[] source = new byte[512 * 512];
        encoder.Encode(source);

        Assert.Equal(99, encoder.EncodedData.Length);
        Util.TestByDecoding(encoder.EncodedData, encoder.FrameInfo!, source, InterleaveMode.None);
    }

    [Fact]
    public void EncodeImageOddSizeForcedEven()
    {
        JpegLSEncoder encoder = new(512, 512, 8, 1) { EncodingOptions = EncodingOptions.EvenDestinationSize };

        byte[] source = new byte[512 * 512];
        encoder.Encode(source);

        Assert.Equal(100, encoder.EncodedData.Length);
        Util.TestByDecoding(encoder.EncodedData, encoder.FrameInfo!, source, InterleaveMode.None);
    }

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


    private static void EncodeWithCustomPresetCodingParameters(JpegLSPresetCodingParameters pcParameters)
    {
        byte[] source = [0, 1, 1, 1, 0];
        FrameInfo frameInfo = new FrameInfo(5, 1, 8, 1);

        JpegLSEncoder encoder = new() { FrameInfo = frameInfo };
        var destination = new byte[encoder.EstimatedDestinationSize];
        encoder.Destination = destination;

        encoder.PresetCodingParameters = pcParameters;

        encoder.Encode(source);

        Util.TestByDecoding(encoder.EncodedData, frameInfo, source, InterleaveMode.None);
    }

    private static byte[] ConvertToByteArray(ushort[] source)
    {
        byte[] destination = new byte[source.Length * 2];
        Buffer.BlockCopy(source, 0, destination, 0, source.Length * 2);
        return destination;
    }

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
