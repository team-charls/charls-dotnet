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
}
