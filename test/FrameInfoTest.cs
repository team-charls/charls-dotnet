// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed.Test;

public class FrameInfoTest
{
    [Fact]
    public void ConstructDefault()
    {
        FrameInfo frameInfo = new(256, 1024, 8, 3);

        Assert.Equal(256, frameInfo.Width);
        Assert.Equal(1024, frameInfo.Height);
        Assert.Equal(8, frameInfo.BitsPerSample);
        Assert.Equal(3, frameInfo.ComponentCount);
    }

    [Fact]
    public void SetInvalidWidthThrows()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new FrameInfo(0, 1, 1, 1));

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidArgumentWidth, exception.GetErrorCode());

        exception = Assert.Throws<ArgumentOutOfRangeException>(() => new FrameInfo { Width = 0});

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidArgumentWidth, exception.GetErrorCode());
    }

    [Fact]
    public void SetInvalidHeightThrows()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new FrameInfo(1, 0, 1, 1));

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidArgumentHeight, exception.GetErrorCode());

        exception = Assert.Throws<ArgumentOutOfRangeException>(() => new FrameInfo { Height = 0 });

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidArgumentHeight, exception.GetErrorCode());
    }

    [Fact]
    public void SetInvalidBitsPerSampleThrows()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new FrameInfo(1, 1, 0, 1));

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidArgumentBitsPerSample, exception.GetErrorCode());

        exception = Assert.Throws<ArgumentOutOfRangeException>(() => new FrameInfo { BitsPerSample = 0 });

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidArgumentBitsPerSample, exception.GetErrorCode());
    }

    [Fact]
    public void SetInvalidComponentCountThrows()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new FrameInfo(1, 1, 2, 0));

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidArgumentComponentCount, exception.GetErrorCode());

        exception = Assert.Throws<ArgumentOutOfRangeException>(() => new FrameInfo { ComponentCount = 0 });

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidArgumentComponentCount, exception.GetErrorCode());
    }
}
