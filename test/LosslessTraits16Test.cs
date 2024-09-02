// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed.Test;

public class LosslessTraits16Test
{
    [Fact]
    public void TestTraits16Bit()
    {
        LosslessTraits16 losslessTraits = new();
        Traits traits = new(ushort.MaxValue, 0);

        Assert.Equal(traits.MaximumSampleValue, losslessTraits.MaximumSampleValue);

        for (int i = -4096; i <= 4096; ++i)
        {
            Assert.Equal(traits.ModuloRange(i), losslessTraits.ModuloRange(i));
            Assert.Equal(traits.ComputeErrorValue(i), losslessTraits.ComputeErrorValue(i));
        }
    }
}
