// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed.Test;

public class ColorTransformationsTest
{
    [Fact]
    public void IsPossible()
    {
        Assert.True(ColorTransformations.IsPossible(new FrameInfo(1, 1, 8, 3)));
        Assert.True(ColorTransformations.IsPossible(new FrameInfo(1, 1, 16, 3)));
        Assert.False(ColorTransformations.IsPossible(new FrameInfo(1, 1, 16, 4)));
    }
}
