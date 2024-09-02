// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed.Test;

public class LosslessTraits8Test
{
    [Fact]
    public void TestTraits8Bit()
    {
        LosslessTraits8 losslessTraits = new();
        Traits traits = new(255, 0);

        Assert.Equal(traits.MaximumSampleValue, losslessTraits.MaximumSampleValue);

        for (int i = -255 ; i <= 255; ++i)
        {
            Assert.Equal(traits.ModuloRange(i), losslessTraits.ModuloRange(i));
            Assert.Equal(traits.ComputeErrorValue(i), losslessTraits.ComputeErrorValue(i));
        }
    }
}
