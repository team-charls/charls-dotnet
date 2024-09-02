// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed.Test;

public class TraitsTest
{
    [Fact]
    public void Create()
    {
        var traits = new Traits((1 << 8) - 1, 0);

        Assert.Equal(255, traits.MaximumSampleValue);
        Assert.Equal(256, traits.Range);
        Assert.Equal(0, traits.NearLossless);
        Assert.Equal(8, traits.BitsPerSample);
    }

    [Fact]
    public void ModuloRange()
    {
        var traits = new Traits(24, 0);

        for (int i = -25; i != 26; ++i)
        {
            var errorValue = traits.ModuloRange(i);
            const int range = 24 + 1;
            Assert.True(errorValue is >= (-range / 2) and <= (((range + 1) / 2) - 1));
        }
    }
}
