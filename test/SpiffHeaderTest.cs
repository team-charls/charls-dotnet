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

        SpiffHeader spiffHeader = new() { ProfileId = SpiffProfileId.BiLevelFacsimile };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader { ProfileId = SpiffProfileId.ContinuousToneBase };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader { ProfileId = SpiffProfileId.ContinuousToneFacsimile };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader { ProfileId = SpiffProfileId.ContinuousToneProgressive };
        Assert.False(spiffHeader.IsValid(frameInfo));
    }

    [Fact]
    public void ColorSpace()
    {
        // Use all official defined color space values.
        FrameInfo frameInfo = new(1, 1, 2, 1);

        SpiffHeader spiffHeader = new()
        {
            ProfileId = SpiffProfileId.None,
            ComponentCount = 1,
            ColorSpace = SpiffColorSpace.BiLevelBlack
        };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader
        {
            ProfileId = SpiffProfileId.None,
            ComponentCount = 1,
            ColorSpace = SpiffColorSpace.BiLevelWhite
        };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader
        {
            ProfileId = SpiffProfileId.None,
            ComponentCount = 1,
            ColorSpace = SpiffColorSpace.BiLevelWhite
        };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader
        {
            ProfileId = SpiffProfileId.None,
            ComponentCount = 1,
            ColorSpace = SpiffColorSpace.Grayscale
        };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader
        {
            ProfileId = SpiffProfileId.None,
            ComponentCount = 1,
            ColorSpace = SpiffColorSpace.YcbcrItuBT709Video
        };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader
        {
            ProfileId = SpiffProfileId.None,
            ComponentCount = 1,
            ColorSpace = SpiffColorSpace.YcbcrItuBT6011Rgb
        };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader
        {
            ProfileId = SpiffProfileId.None,
            ComponentCount = 1,
            ColorSpace = SpiffColorSpace.YcbcrItuBT6011Video
        };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader
        {
            ProfileId = SpiffProfileId.None,
            ComponentCount = 1,
            ColorSpace = SpiffColorSpace.Rgb
        };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader
        {
            ProfileId = SpiffProfileId.None,
            ComponentCount = 1,
            ColorSpace = SpiffColorSpace.Cmy
        };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader
        {
            ProfileId = SpiffProfileId.None,
            ComponentCount = 1,
            ColorSpace = SpiffColorSpace.PhotoYcc
        };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader
        {
            ProfileId = SpiffProfileId.None,
            ComponentCount = 1,
            ColorSpace = SpiffColorSpace.CieLab
        };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader
        {
            ProfileId = SpiffProfileId.None,
            ComponentCount = 1,
            ColorSpace = SpiffColorSpace.Cmyk
        };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader
        {
            ProfileId = SpiffProfileId.None,
            ComponentCount = 4,
            BitsPerSample = 2,
            Height = 1,
            Width = 1,
            ColorSpace = SpiffColorSpace.Cmyk
        };
        Assert.True(spiffHeader.IsValid(new FrameInfo(1, 1, 2, 4)));

        spiffHeader = new SpiffHeader
        {
            ProfileId = SpiffProfileId.None,
            ComponentCount = 1,
            ColorSpace = SpiffColorSpace.Ycck
        };
        Assert.False(spiffHeader.IsValid(frameInfo));

        spiffHeader = new SpiffHeader
        {
            ProfileId = SpiffProfileId.None,
            ComponentCount = 1,
            ColorSpace = (SpiffColorSpace)99
        };
        Assert.False(spiffHeader.IsValid(frameInfo));
    }

    [Fact]
    public void NondestructiveMutation()
    {
        var spiffHeader = new SpiffHeader
        {
            ProfileId = SpiffProfileId.None,
            ComponentCount = 4,
            BitsPerSample = 2,
            Height = 1,
            Width = 1,
            ColorSpace = SpiffColorSpace.Cmyk
        };

        SpiffHeader header2 = spiffHeader with { BitsPerSample = 8 };
        Assert.Equal(8, header2.BitsPerSample);
    }
}
