// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed.Test;


public class ValidationTest
{
    [Fact]
    public void IsBitsPerSampleValid()
    {
        Assert.True(Validation.IsBitsPerSampleValid(2));
        Assert.True(Validation.IsBitsPerSampleValid(16));
        Assert.False(Validation.IsBitsPerSampleValid(1));
        Assert.False(Validation.IsBitsPerSampleValid(17));
    }

    [Fact]
    public void IsInterleaveModeValid()
    {
        InterleaveMode interleaveMode = InterleaveMode.None;
        Assert.True(interleaveMode.IsValid());

        interleaveMode = (InterleaveMode)(-1);
        Assert.False(interleaveMode.IsValid());

        interleaveMode = (InterleaveMode)3;
        Assert.False(interleaveMode.IsValid());
    }

    [Fact]
    public void IsColorTransformationValid()
    {
        ColorTransformation colorTransformation = ColorTransformation.None;
        Assert.True(colorTransformation.IsValid());

        colorTransformation = (ColorTransformation)(-1);
        Assert.False(colorTransformation.IsValid());

        colorTransformation = (ColorTransformation)4;
        Assert.False(colorTransformation.IsValid());
    }

    [Fact]
    public void IsEncodingOptionsValid()
    {
        EncodingOptions encodingOptions = EncodingOptions.None;
        Assert.True(encodingOptions.IsValid());

        encodingOptions = EncodingOptions.IncludeVersionNumber | EncodingOptions.IncludePCParametersJai | EncodingOptions.EvenDestinationSize;
        Assert.True(encodingOptions.IsValid());

        encodingOptions = (EncodingOptions)(-1);
        Assert.False(encodingOptions.IsValid());

        encodingOptions = (EncodingOptions)8;
        Assert.False(encodingOptions.IsValid());
    }
}
