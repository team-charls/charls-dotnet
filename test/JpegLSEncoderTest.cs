// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

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
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => _ = new JpegLSEncoder(1, 1, 1, 1));
        Assert.Equal(ErrorCode.InvalidArgumentBitsPerSample, exception.GetErrorCode());

        exception = Assert.Throws<ArgumentOutOfRangeException>(() => _ = new JpegLSEncoder(1, 1, 17, 1));
        Assert.Equal(ErrorCode.InvalidArgumentBitsPerSample, exception.GetErrorCode());
    }

    [Fact]
    public void FrameInfoBadComponentCountThrows()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => _ = new JpegLSEncoder(1, 1, 2, 0));
        Assert.Equal(ErrorCode.InvalidArgumentComponentCount, exception.GetErrorCode());

        exception = Assert.Throws<ArgumentOutOfRangeException>(() => _ = new JpegLSEncoder(1, 1, 2, 256));
        Assert.Equal(ErrorCode.InvalidArgumentComponentCount, exception.GetErrorCode());
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

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => encoder.InterleaveMode = (InterleaveMode)(-1));
        Assert.Equal(ErrorCode.InvalidArgumentInterleaveMode, exception.GetErrorCode());

        exception = Assert.Throws<ArgumentOutOfRangeException>(() => encoder.InterleaveMode = (InterleaveMode)3);
        Assert.Equal(ErrorCode.InvalidArgumentInterleaveMode, exception.GetErrorCode());
    }

    [Fact]
    public void InterleaveModeDoesNotMatchComponentCountThrows()
    {
        var frameInfo = new FrameInfo(512, 512, 8, 1);
        var source = new byte[frameInfo.Width * frameInfo.Height];

        var exception =
            Assert.Throws<ArgumentException>(() => JpegLSEncoder.Encode(source, frameInfo, InterleaveMode.Sample));
        Assert.Equal(ErrorCode.InvalidArgumentInterleaveMode, exception.GetErrorCode());
        exception = Assert.Throws<ArgumentException>(() =>
            JpegLSEncoder.Encode(source, frameInfo, InterleaveMode.Sample));
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

        _ = Assert.Throws<OverflowException>(() => encoder.EstimatedDestinationSize);
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
        var exception = Assert.Throws<InvalidOperationException>(() => encoder.Destination = destination);
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
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

        var exception =
            Assert.Throws<InvalidOperationException>(() => encoder.WriteStandardSpiffHeader(SpiffColorSpace.Cmyk));
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    [Fact]
    public void WriteStandardSpiffHeaderWithoutFrameInfoThrows()
    {
        JpegLSEncoder encoder = new();

        var destination = new byte[100];
        encoder.Destination = destination;

        var exception =
            Assert.Throws<InvalidOperationException>(() => encoder.WriteStandardSpiffHeader(SpiffColorSpace.Cmyk));
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    [Fact]
    public void WriteStandardSpiffHeaderTwiceThrows()
    {
        JpegLSEncoder encoder = new() { FrameInfo = new FrameInfo(1, 1, 2, 4) };
        var destination = new byte[encoder.EstimatedDestinationSize];
        encoder.Destination = destination;
        encoder.WriteStandardSpiffHeader(SpiffColorSpace.Cmyk);

        var exception =
            Assert.Throws<InvalidOperationException>(() => encoder.WriteStandardSpiffHeader(SpiffColorSpace.Cmyk));
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    [Fact]
    public void WriteSpiffHeader()
    {
        JpegLSEncoder encoder = new(1, 1, 2, 1);

        var spiffHeader = new SpiffHeader { Width = 1, Height = 1 };
        encoder.WriteSpiffHeader(spiffHeader);

        Assert.Equal(Constants.SpiffHeaderSizeInBytes + 2, encoder.BytesWritten);

        var destination = encoder.Destination.Span;

        // Check that SOI marker has been written.
        Assert.Equal(0xFF, destination[0]);
        Assert.Equal((byte)JpegMarkerCode.StartOfImage, destination[1]);

        // Verify that a APP8 with SPIFF has been written (details already verified by jpeg_stream_writer_test).
        Assert.Equal(0xFF, destination[2]);
        Assert.Equal((byte)JpegMarkerCode.ApplicationData8, destination[3]);
        Assert.Equal(0, destination[4]);
        Assert.Equal(32, destination[5]);
        Assert.Equal((byte)'S', destination[6]);
        Assert.Equal((byte)'P', destination[7]);
        Assert.Equal((byte)'I', destination[8]);
        Assert.Equal((byte)'F', destination[9]);
        Assert.Equal((byte)'F', destination[10]);
        Assert.Equal(0, destination[11]);
    }

    [Fact]
    public void WriteSpiffHeaderInvalidHeightThrows()
    {
        JpegLSEncoder encoder = new() { FrameInfo = new FrameInfo(1, 1, 2, 1) };

        byte[] destination = new byte[encoder.EstimatedDestinationSize];
        encoder.Destination = destination;

        SpiffHeader spiffHeader = new() { Width = 1 };

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

        SpiffHeader spiffHeader = new() { Height = 1 };

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => encoder.WriteSpiffHeader(spiffHeader));
        Assert.Equal(ErrorCode.InvalidArgumentWidth, exception.GetErrorCode());
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
        var exception =
            Assert.Throws<ArgumentException>(() => encoder.WriteSpiffEntry(endOfSpiffDirectoryTag, "test"u8));
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
        var exception =
            Assert.Throws<InvalidOperationException>(() => encoder.WriteSpiffEntry(SpiffEntryTag.ImageTitle, data));
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

    [Fact]
    public void WriteTwoComment()
    {
        JpegLSEncoder encoder = new();

        var destination = new byte[14];
        encoder.Destination = destination;

        encoder.WriteComment("123");
        encoder.WriteComment("");

        Assert.Equal(destination.Length, encoder.BytesWritten);

        // Check that SOI marker has been written.
        Assert.Equal(0xFF, destination[0]);
        Assert.Equal((byte)JpegMarkerCode.StartOfImage, destination[1]);

        // Verify that the COM segments have been written.
        Assert.Equal(0xFF, destination[2]);
        Assert.Equal((byte)JpegMarkerCode.Comment, destination[3]);
        Assert.Equal(0, destination[4]);
        Assert.Equal(2 + 4, destination[5]);
        Assert.Equal((byte)'1', destination[6]);
        Assert.Equal((byte)'2', destination[7]);
        Assert.Equal((byte)'3', destination[8]);
        Assert.Equal(0, destination[9]);

        Assert.Equal(0xFF, destination[10]);
        Assert.Equal((byte)JpegMarkerCode.Comment, destination[11]);
        Assert.Equal(0, destination[12]);
        Assert.Equal(2, destination[13]);
    }

    [Fact]
    public void WriteTooLargeCommentThrows()
    {
        JpegLSEncoder encoder = new();

        var destination = new byte[2 + 2 + ushort.MaxValue + 1];

        encoder.Destination = destination;

        const int maxSizeCommentData = ushort.MaxValue - 2;
        var data = new byte[maxSizeCommentData + 1];

        var exception = Assert.Throws<ArgumentException>(() => encoder.WriteComment(data));
        Assert.Equal(ErrorCode.InvalidArgument, exception.GetErrorCode());
    }

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
        Assert.Equal((byte)JpegMarkerCode.StartOfImage, destination[1]);

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

    [Fact]
    public void WriteMaxApplicationData()
    {
        JpegLSEncoder encoder = new();

        var destination = new byte[2 + 2 + ushort.MaxValue];
        encoder.Destination = destination;

        const int maxSizeApplicationData = ushort.MaxValue - 2;
        byte[] data = new byte[maxSizeApplicationData];
        encoder.WriteApplicationData(15, data);

        Assert.Equal(destination.Length, encoder.BytesWritten);

        // Check that SOI marker has been written.
        Assert.Equal(0xFF, destination[0]);
        Assert.Equal((byte)JpegMarkerCode.StartOfImage, destination[1]);

        // Verify that a APPn segment has been written.
        Assert.Equal(0xFF, destination[2]);
        Assert.Equal((byte)JpegMarkerCode.ApplicationData15, destination[3]);
        Assert.Equal(255, destination[4]);
        Assert.Equal(255, destination[5]);
    }

    [Fact]
    public void WriteTwoApplicationData()
    {
        JpegLSEncoder encoder = new();

        var destination = new byte[14];
        encoder.Destination = destination;

        byte[] applicationData = [1, 2, 3, 4];
        encoder.WriteApplicationData(0, applicationData);
        encoder.WriteApplicationData(8, []);

        Assert.Equal(destination.Length, encoder.BytesWritten);

        // Check that SOI marker has been written.
        Assert.Equal(0xFF, destination[0]);
        Assert.Equal((byte)JpegMarkerCode.StartOfImage, destination[1]);

        // Verify that the COM segments have been written.
        Assert.Equal(0xFF, destination[2]);
        Assert.Equal((byte)JpegMarkerCode.ApplicationData0, destination[3]);
        Assert.Equal(0, destination[4]);
        Assert.Equal(2 + 4, destination[5]);
        Assert.Equal(1, destination[6]);
        Assert.Equal(2, destination[7]);
        Assert.Equal(3, destination[8]);
        Assert.Equal(4, destination[9]);

        Assert.Equal(0xFF, destination[10]);
        Assert.Equal((byte)JpegMarkerCode.ApplicationData8, destination[11]);
        Assert.Equal(0, destination[12]);
        Assert.Equal(2, destination[13]);
    }

    [Fact]
    public void WriteTooLargeApplicationDataThrows()
    {
        JpegLSEncoder encoder = new();

        var destination = new byte[2 + 2 + ushort.MaxValue + 1];
        encoder.Destination = destination;

        const int maxSizeApplicationData = ushort.MaxValue + 2;
        byte[] data = new byte[maxSizeApplicationData + 1];

        var exception = Assert.Throws<ArgumentException>(() => encoder.WriteApplicationData(0, data));
        Assert.Equal(ErrorCode.InvalidArgument, exception.GetErrorCode());
    }

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

        encoder.WriteApplicationData(11, []);

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

        var exception = Assert.Throws<InvalidOperationException>(encoder.CreateAbbreviatedFormat);
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

        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() => encoder.ColorTransformation = (ColorTransformation)100);
        Assert.Equal(ErrorCode.InvalidArgumentColorTransformation, exception.GetErrorCode());
    }

    [Fact]
    public void UseColorTransformationIncompatibleWithFrameInfoThrows()
    {
        JpegLSEncoder encoder = new(new FrameInfo(1, 1, 8, 1)) { ColorTransformation = ColorTransformation.HP1 };

        byte[] source = [0, 1, 2, 3, 4, 5];

        var exception = Assert.Throws<ArgumentException>(() => encoder.Encode(source));
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

        Util.TestByDecoding(encoder.EncodedData, encoder.FrameInfo, source, InterleaveMode.None, ColorTransformation.HP1);
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
            0, 0, 0, 0, 200, 200, 200, 0, 0, 0, 0, 0, 0, 0
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
        ushort[] source =
        [
            100, 100, 100, 0, 0, 0, 0, 0, 0, 0, 150, 150,
            150, 0, 0, 0, 0, 0, 0, 0, 200, 200, 200, 0, 0, 0, 0, 0, 0, 0
        ];
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
        Assert.Equal(ErrorCode.InvalidArgumentSize, exception.GetErrorCode());
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
    public void Encode4Components5BitWithHighBitsSetInterleaveModeLine()
    {
        byte[] source = new byte[512 * 512 * 4];
        Array.Fill(source, (byte)0xFF);

        JpegLSEncoder encoder = new(512, 512, 5, 4) { InterleaveMode = InterleaveMode.Line };

        encoder.Encode(source);

        byte[] expectedDestination = new byte[512 * 512 * 4];
        Array.Fill(expectedDestination, (byte)31);
        Util.TestByDecoding(encoder.EncodedData, encoder.FrameInfo!, expectedDestination, InterleaveMode.Line);
    }

    [Fact]
    public void Encode4Components7BitWithHighBitsSetInterleaveModeSample()
    {
        byte[] source = new byte[512 * 512 * 4];
        Array.Fill(source, (byte)0xFF);

        JpegLSEncoder encoder = new(512, 512, 7, 4) { InterleaveMode = InterleaveMode.Sample };

        encoder.Encode(source);

        byte[] expectedDestination = new byte[512 * 512 * 4];
        Array.Fill(expectedDestination, (byte)127);
        Util.TestByDecoding(encoder.EncodedData, encoder.FrameInfo!, expectedDestination, InterleaveMode.Sample);
    }

    [Fact]
    public void Encode4Components11BitWithHighBitsSetInterleaveModeLine()
    {
        byte[] source = new byte[512 * 512 * 4 * 2];
        Array.Fill(source, (byte)0xFF);

        JpegLSEncoder encoder = new(512, 512, 11, 4) { InterleaveMode = InterleaveMode.Line };

        encoder.Encode(source);

        ushort[] expectedDestination = new ushort[512 * 512 * 4];
        Array.Fill(expectedDestination, (ushort)2047);

        Util.TestByDecoding(encoder.EncodedData, encoder.FrameInfo!,
            MemoryMarshal.AsBytes(new ReadOnlySpan<ushort>(expectedDestination)), InterleaveMode.Line);
    }

    [Fact]
    public void Encode4Components13BitWithHighBitsSetInterleaveModeSample()
    {
        byte[] source = new byte[512 * 512 * 4 * 2];
        Array.Fill(source, (byte)0xFF);

        JpegLSEncoder encoder = new(512, 512, 13, 4) { InterleaveMode = InterleaveMode.Sample };

        encoder.Encode(source);

        ushort[] expectedDestination = new ushort[512 * 512 * 4];
        Array.Fill(expectedDestination, (ushort)8191);

        Util.TestByDecoding(encoder.EncodedData, encoder.FrameInfo!,
            MemoryMarshal.AsBytes(new ReadOnlySpan<ushort>(expectedDestination)), InterleaveMode.Sample);
    }

    [Fact]
    public void Rewind()
    {
        byte[] source = [0, 1, 2, 3, 4, 5];
        JpegLSEncoder encoder = new(3, 1, 16, 1);
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

    [Fact]
    public void EncodeImageForcedVersionComment()
    {
        JpegLSEncoder encoder = new(512, 512, 8, 1) { EncodingOptions = EncodingOptions.IncludeVersionNumber };

        byte[] source = new byte[512 * 512];
        encoder.Encode(source);

        JpegLSDecoder decoder = new() { Source = encoder.EncodedData };

        ReadOnlyMemory<byte> comment = ReadOnlyMemory<byte>.Empty;
        decoder.Comment += (_, e) =>
        {
            Assert.NotNull(e);
            comment = e.Data;
        };

        decoder.ReadHeader();

        string versionString = Encoding.UTF8.GetString(comment.ToArray());
        string expectedVersion = RemoveGitHash(Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion!);

        Assert.Equal("charls-dotnet " + expectedVersion, versionString);
        return;

        static string RemoveGitHash(string version)
        {
            int index = version.IndexOf('+', StringComparison.InvariantCulture);
            return index != -1 ? version[..index] : version;
        }
    }

    [Fact]
    public void EncodeImageIncludePCParametersJai()
    {
        JpegLSEncoder encoder = new(1, 1, 16, 1) { EncodingOptions = EncodingOptions.IncludePCParametersJai };

        byte[] source = new byte[1 * 1 * 2];
        encoder.Encode(source);
        var destination = encoder.EncodedData.Span;

        Assert.Equal(43, encoder.BytesWritten);

        Assert.Equal(0xFF, destination[15]);
        Assert.Equal((byte)JpegMarkerCode.JpegLSPresetParameters, destination[16]);

        // Segment size.
        Assert.Equal(0, destination[17]);
        Assert.Equal(13, destination[18]);

        // Parameter ID.
        Assert.Equal(0x1, destination[19]);

        // MaximumSampleValue
        Assert.Equal(255, destination[20]);
        Assert.Equal(255, destination[21]);

        var expected = JpegLSPresetCodingParametersTest.ComputeDefaultsUsingReferenceImplementation(ushort.MaxValue, 0);
        int threshold1 = (destination[22] << 8) | destination[23];
        Assert.Equal(expected.T1, threshold1);
        int threshold2 = (destination[24] << 8) | destination[25];
        Assert.Equal(expected.T2, threshold2);
        int threshold3 = (destination[26] << 8) | destination[27];
        Assert.Equal(expected.T3, threshold3);
        int reset = (destination[28] << 8) | destination[29];
        Assert.Equal(expected.Reset, reset);
    }

    [Fact]
    public void EncodeImageWithIncludePCParametersJaiNotSet()
    {
        JpegLSEncoder encoder = new(1, 1, 16, 1);
        encoder.Encode(new byte[1 * 1 * 2]);

        Assert.Equal(28, encoder.BytesWritten);
    }

    [Fact]
    public void SetInvalidEncodeOptionsThrows()
    {
        JpegLSEncoder encoder = new();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => encoder.EncodingOptions = (EncodingOptions)8);
        Assert.Equal(ErrorCode.InvalidArgumentEncodingOptions, exception.GetErrorCode());
    }

    [Fact]
    public void LargeImageContainsLseForOversizeImageDimension()
    {
        JpegLSEncoder encoder = new(ushort.MaxValue + 1, 1, 16, 1);
        byte[] source = new byte[encoder.FrameInfo!.Width * encoder.FrameInfo.Height * 2];

        encoder.Encode(source);

        Assert.Equal(46, encoder.BytesWritten);

        int index = FindFirstLseSegment(encoder.EncodedData.Span);
        Assert.True(index != -1);
    }

    [Fact]
    public void EncodeOversizedImage()
    {
        JpegLSEncoder encoder = new(ushort.MaxValue + 1, 1, 8, 1);
        byte[] source = new byte[encoder.FrameInfo!.Width * encoder.FrameInfo.Height];

        encoder.Encode(source);

        Util.TestByDecoding(encoder.EncodedData, encoder.FrameInfo, source, InterleaveMode.None);
    }

    [Fact]
    public void ImageContainsNoPresetCodingParametersByDefault()
    {
        JpegLSEncoder encoder = new(512, 512, 8, 1);
        byte[] source = new byte[encoder.FrameInfo!.Width * encoder.FrameInfo.Height];

        encoder.Encode(source);
        Assert.Equal(99, encoder.BytesWritten);

        int index = FindFirstLseSegment(encoder.EncodedData.Span);
        Assert.Equal(-1, index);
    }

    [Fact]
    public void ImageContainsNoPresetCodingParametersIfConfiguredPCIsDefault()
    {
        JpegLSEncoder encoder = new(512, 512, 8, 1);
        byte[] source = new byte[encoder.FrameInfo!.Width * encoder.FrameInfo.Height];

        encoder.PresetCodingParameters = new JpegLSPresetCodingParameters(255, 3, 7, 21, 64);

        encoder.Encode(source);
        Assert.Equal(99, encoder.BytesWritten);

        int index = FindFirstLseSegment(encoder.EncodedData.Span);
        Assert.Equal(-1, index);
    }

    [Fact]
    public void ImageContainsNoPresetCodingParametersIfConfiguredPCIsNonDefault()
    {
        JpegLSEncoder encoder = new(512, 512, 8, 1);
        byte[] source = new byte[encoder.FrameInfo!.Width * encoder.FrameInfo.Height];

        encoder.PresetCodingParameters = new JpegLSPresetCodingParameters(255, 3, 7, 21, 65);

        encoder.Encode(source);
        Assert.Equal(114, encoder.BytesWritten);

        int index = FindFirstLseSegment(encoder.EncodedData.Span);
        Assert.False(index == -1);
    }

    [Fact]
    public void ImageContainsPresetCodingParametersIfConfiguredPCHasDiffMaxValue()
    {
        JpegLSEncoder encoder = new(512, 512, 8, 1);
        byte[] source = new byte[encoder.FrameInfo!.Width * encoder.FrameInfo.Height];

        encoder.PresetCodingParameters = new JpegLSPresetCodingParameters(100, 0, 0, 0, 0);

        encoder.Encode(source);
        Assert.Equal(114, encoder.BytesWritten);

        int index = FindFirstLseSegment(encoder.EncodedData.Span);
        Assert.False(index == -1);
    }

    private static void EncodeWithCustomPresetCodingParameters(JpegLSPresetCodingParameters pcParameters)
    {
        byte[] source = [0, 1, 1, 1, 0];
        FrameInfo frameInfo = new(5, 1, 8, 1);

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

    private static int FindFirstLseSegment(ReadOnlySpan<byte> data)
    {
        const byte lseMarker = 0xF8;

        for (int i = 0; i < data.Length - 1; ++i)
        {
            if (data[i] == 0xFF && data[i + 1] == lseMarker)
                return i;
        }

        return -1;
    }
}
