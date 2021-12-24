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
}
