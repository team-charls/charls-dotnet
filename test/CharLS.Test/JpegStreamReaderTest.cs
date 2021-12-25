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
}
