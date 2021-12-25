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

        var exception = Assert.Throws<InvalidDataException>(() => reader.ReadHeader());
        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(JpegLSError.SourceBufferTooSmall, exception.Data[nameof(JpegLSError)]);
    }

    [Fact]
    public void ReadHeaderFromBufferPrecededWithFillBytes()
    {
        const byte extraStartByte = 0xFF;
        JpegTestStreamWriter writer = new();

        writer.WriteByte(extraStartByte);
        writer.WriteStartOfImage();

        writer.WriteByte(extraStartByte);
        writer.WriteStartOfFrameSegment(1, 1, 2, 1);

        writer.WriteByte(extraStartByte);
        writer.WriteStartOfScanSegment(0, 1, 128, InterleaveMode.None);

        var reader = new JpegStreamReader { Source = writer.GetBuffer() };

        reader.ReadHeader();  // if it doesn't throw test is passed.
    }

    [Fact]
    public void ReadHeaderFromBufferNotStartingWithStartByteShouldThrow()
    {
        byte[] buffer = { 0x0F, 0xFF, 0xD8, 0xFF, 0xFF, 0xDA };

        var reader = new JpegStreamReader { Source = buffer };

        var exception = Assert.Throws<InvalidDataException>(() => reader.ReadHeader());
        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(JpegLSError.JpegMarkerStartByteNotFound, exception.Data[nameof(JpegLSError)]);
    }

    [Fact]
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

    private static void ReadHeaderWithApplicationDataImpl(byte dataNumber)
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();

        writer.WriteByte(0xFF);
        writer.WriteByte((byte)(0xE0 + dataNumber));
        writer.WriteByte(0x00);
        writer.WriteByte(0x02);

        writer.WriteStartOfFrameSegment(1, 1, 2, 1);
        writer.WriteStartOfScanSegment(0, 1, 128, InterleaveMode.None);

        var reader = new JpegStreamReader { Source = writer.GetBuffer() };

        reader.ReadHeader(); // if it doesn't throw test is passed.
    }

}
