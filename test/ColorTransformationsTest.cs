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
        Assert.False(ColorTransformations.IsPossible(new FrameInfo(1, 1, 10, 3)));
        Assert.False(ColorTransformations.IsPossible(new FrameInfo(1, 1, 16, 4)));
    }

    [Fact]
    public void TransformHP1RoundTrip()
    {
        // For the normal unit test keep the range small for a quick test.
        // For a complete test which will take a while set the start and end to 0 and 255.
        const int startValue = 123;
        const int endValue = 124;

        for (int red =  startValue; red != endValue; ++red)
        {
            for (int green = 0; green != 255; ++green)
            {
                for (int blue = 0; blue != 255; ++blue)
                {
                    var sampleByte = ColorTransformations.TransformHP1((byte)red, (byte)green, (byte)blue);
                    var sampleUInt16 = ColorTransformations.TransformHP1((ushort)red, (ushort)green, (ushort)blue);
                    var roundTripByte = ColorTransformations.ReverseTransformHP1(sampleByte.V1, sampleByte.V2, sampleByte.V3);
                    var roundTripUInt16 = ColorTransformations.ReverseTransformHP1(sampleUInt16.V1, sampleUInt16.V2, sampleUInt16.V3);

                    Assert.Equal(red, roundTripByte.V1);
                    Assert.Equal(green, roundTripByte.V2);
                    Assert.Equal(blue, roundTripByte.V3);
                    Assert.Equal(red, roundTripUInt16.V1);
                    Assert.Equal(green, roundTripUInt16.V2);
                    Assert.Equal(blue, roundTripUInt16.V3);
                }
            }
        }
    }

    [Fact]
    public void TransformHP2RoundTrip()
    {
        // For the normal unit test keep the range small for a quick test.
        // For a complete test which will take a while set the start and end to 0 and 255.
        const int startValue = 123;
        const int endValue = 124;

        for (int red = startValue; red != endValue; ++red)
        {
            for (int green = 0; green != 255; ++green)
            {
                for (int blue = 0; blue != 255; ++blue)
                {
                    var sampleByte = ColorTransformations.TransformHP2((byte)red, (byte)green, (byte)blue);
                    var sampleUInt16 = ColorTransformations.TransformHP2((ushort)red, (ushort)green, (ushort)blue);
                    var roundTripByte = ColorTransformations.ReverseTransformHP2(sampleByte.V1, sampleByte.V2, sampleByte.V3);
                    var roundTripUInt16 = ColorTransformations.ReverseTransformHP2(sampleUInt16.V1, sampleUInt16.V2, sampleUInt16.V3);

                    Assert.Equal(red, roundTripByte.V1);
                    Assert.Equal(green, roundTripByte.V2);
                    Assert.Equal(blue, roundTripByte.V3);
                    Assert.Equal(red, roundTripUInt16.V1);
                    Assert.Equal(green, roundTripUInt16.V2);
                    Assert.Equal(blue, roundTripUInt16.V3);
                }
            }
        }
    }

    [Fact]
    public void TransformHP3RoundTrip()
    {
        // For the normal unit test keep the range small for a quick test.
        // For a complete test which will take a while set the start and end to 0 and 255.
        const int startValue = 123;
        const int endValue = 124;

        for (int red = startValue; red != endValue; ++red)
        {
            for (int green = 0; green != 255; ++green)
            {
                for (int blue = 0; blue != 255; ++blue)
                {
                    var sampleByte = ColorTransformations.TransformHP3((byte)red, (byte)green, (byte)blue);
                    var sampleUInt16 = ColorTransformations.TransformHP3((ushort)red, (ushort)green, (ushort)blue);
                    var roundTripByte = ColorTransformations.ReverseTransformHP3(sampleByte.V1, sampleByte.V2, sampleByte.V3);
                    var roundTripUInt16 = ColorTransformations.ReverseTransformHP3(sampleUInt16.V1, sampleUInt16.V2, sampleUInt16.V3);

                    Assert.Equal(red, roundTripByte.V1);
                    Assert.Equal(green, roundTripByte.V2);
                    Assert.Equal(blue, roundTripByte.V3);
                    Assert.Equal(red, roundTripUInt16.V1);
                    Assert.Equal(green, roundTripUInt16.V2);
                    Assert.Equal(blue, roundTripUInt16.V3);
                }
            }
        }
    }
}
