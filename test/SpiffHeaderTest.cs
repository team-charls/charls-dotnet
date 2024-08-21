// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed.Test;

public class SpiffHeaderTest
{
    [Fact]
    public void DefaultIsValid()
    {
        FrameInfo frameInfo = new(1, 1, 2, 1);
        SpiffHeader spiffHeader = new()
        {
            Width = 1,
            Height = 1,
            BitsPerSample = 2,
            ComponentCount = 1,
            ColorSpace = SpiffColorSpace.Grayscale
        };
        Assert.True(spiffHeader.IsValid(frameInfo));
    }

    [Fact]
    public void IsValidDetectsInvalidValues()
    {
        FrameInfo frameInfo = new(1, 1, 2, 1);

        var spiffHeader = new SpiffHeader { CompressionType = SpiffCompressionType.Uncompressed };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader
        {
            Width = 1,
            Height = 1,
            BitsPerSample = 2,
            ComponentCount = 1,
            ProfileId = SpiffProfileId.BiLevelFacsimile
        };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader
        {
            Width = 1,
            Height = 1,
            BitsPerSample = 2,
            ComponentCount = 1,
            ResolutionUnit = (SpiffResolutionUnit)4
        };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader
        {
            Width = 1,
            Height = 1,
            BitsPerSample = 2,
            ComponentCount = 1,
            HorizontalResolution = -1
        };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader
        {
            Width = 1,
            Height = 1,
            BitsPerSample = 2,
            ComponentCount = 1,
            VerticalResolution = -1
        };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader { Width = 1, Height = 1, BitsPerSample = 2, ComponentCount = 3 };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader { Width = 1, Height = 1, BitsPerSample = 7, ComponentCount = 1 };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader { Width = 1, Height = 9, BitsPerSample = 2, ComponentCount = 1 };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader { Width = 11, Height = 1, BitsPerSample = 2, ComponentCount = 1 };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader
        {
            Width = 1,
            Height = 1,
            BitsPerSample = 2,
            ComponentCount = 1,
            ColorSpace = SpiffColorSpace.Cmyk
        };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader
        {
            Width = 1,
            Height = 1,
            BitsPerSample = 2,
            ComponentCount = 1,
            ColorSpace = SpiffColorSpace.BiLevelWhite
        };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader
        {
            Width = 1,
            Height = 1,
            BitsPerSample = 2,
            ComponentCount = 1,
            ColorSpace = SpiffColorSpace.CieLab
        };
        Assert.False(spiffHeader.IsValid(frameInfo));
    }

    [Fact]
    public void CompressionType()
    {
        // Use all official defined compression types.
        FrameInfo frameInfo = new(1, 1, 2, 1);

        SpiffHeader spiffHeader = new() { CompressionType = SpiffCompressionType.JBig };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader { CompressionType = SpiffCompressionType.ModifiedHuffman };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader { CompressionType = SpiffCompressionType.ModifiedModifiedRead };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader { CompressionType = SpiffCompressionType.Jpeg };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader { CompressionType = SpiffCompressionType.ModifiedRead };
        Assert.False(spiffHeader.IsValid(frameInfo));
    }

    [Fact]
    public void ProfileId()
    {
        // Use all official defined profile IDs.
        FrameInfo frameInfo = new(1, 1, 2, 1);

        SpiffHeader spiffHeader = new() { ProfileId = SpiffProfileId.BiLevelFacsimile};
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new() { ProfileId = SpiffProfileId.ContinuousToneBase };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new() { ProfileId = SpiffProfileId.ContinuousToneFacsimile };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new() { ProfileId = SpiffProfileId.ContinuousToneProgressive };
        Assert.False(spiffHeader.IsValid(frameInfo));
    }
}
