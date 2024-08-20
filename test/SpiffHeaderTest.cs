// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed.Test;

public class SpiffHeaderTest
{
    [Fact]
    public void DefaultIsValid()
    {
        FrameInfo frameInfo = new FrameInfo(1, 1, 2, 1);
        var spiffHeader = new SpiffHeader() { Width = 1, Height = 1, BitsPerSample = 2, ComponentCount = 1, ColorSpace = SpiffColorSpace.Grayscale};
        Assert.True(spiffHeader.IsValid(frameInfo));
    }

    [Fact]
    public void IsValidDetectsInvalidValues()
    {
        FrameInfo frameInfo = new FrameInfo(1, 1, 2, 1);

        var spiffHeader = new SpiffHeader { CompressionType = SpiffCompressionType.Uncompressed };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader { Width = 1, Height = 1, BitsPerSample = 2, ComponentCount = 1, ProfileId = SpiffProfileId.BiLevelFacsimile };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader { Width = 1, Height = 1, BitsPerSample = 2, ComponentCount = 1, ResolutionUnit = (SpiffResolutionUnit)4 };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader { Width = 1, Height = 1, BitsPerSample = 2, ComponentCount = 1, HorizontalResolution = -1 };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader { Width = 1, Height = 1, BitsPerSample = 2, ComponentCount = 1, VerticalResolution = -1 };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader { Width = 1, Height = 1, BitsPerSample = 2, ComponentCount = 3 };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader { Width = 1, Height = 1, BitsPerSample = 7, ComponentCount = 1};
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader { Width = 1, Height = 9, BitsPerSample = 2, ComponentCount = 1};
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader { Width = 11, Height = 1, BitsPerSample = 2, ComponentCount = 1};
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader { Width = 1, Height = 1, BitsPerSample = 2, ComponentCount = 1, ColorSpace = SpiffColorSpace.Cmyk };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader { Width = 1, Height = 1, BitsPerSample = 2, ComponentCount = 1, ColorSpace = SpiffColorSpace.BiLevelWhite };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader { Width = 1, Height = 1, BitsPerSample = 2, ComponentCount = 1, ColorSpace = SpiffColorSpace.CieLab };
        Assert.False(spiffHeader.IsValid(frameInfo));
    }
}
