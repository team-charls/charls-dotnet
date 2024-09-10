// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed.Test;

public class EncodeTest
{
    [Fact]
    public void EncodeMonochrome2BitLossless()
    {
        Encode("test-images/2bit_parrot_150x200.pgm", 2866);
    }

    [Fact]
    public void EncodeMonochrome4BitLossless()
    {
        Encode("test-images/4bit-monochrome.pgm", 1596);
    }

    [Fact]
    public void EncodeMonochrome12BitLossless()
    {
        Encode("conformance/test16.pgm", 60077);
    }

    [Fact]
    public void EncodeMonochrome16BitLossless()
    {
        Encode("test-images/16-bit-640-480-many-dots.pgm", 4138);
    }

    [Fact]
    public void Encode2Components8BitInterleaveNone()
    {
        byte[] data = [10, 20, 30, 40, 50, 60, 70, 80];
        Encode(new FrameInfo(2, 2, 8, 2), data, 53, InterleaveMode.None);
    }

    [Fact]
    public void Encode2Components8BitInterleaveLine()
    {
        byte[] data = [10, 20, 30, 40, 50, 60, 70, 80];
        Encode(new FrameInfo(2, 2, 8, 2), data, 43, InterleaveMode.Line);
    }

    [Fact]
    public void Encode2Components8BitInterleaveSample()
    {
        byte[] data = [10, 20, 30, 40, 50, 60, 70, 80];
        Encode(new FrameInfo(2, 2, 8, 2), data, 43, InterleaveMode.Sample);
    }

    [Fact]
    public void Encode2Components16BitInterleaveNone()
    {
        byte[] data = [10, 1, 20, 1, 30, 1, 40, 1, 50, 1, 60, 1, 70, 1, 80, 1];
        Encode(new FrameInfo(2, 2, 16, 2), data, 52, InterleaveMode.None);
    }

    [Fact]
    public void Encode2Components16BitInterleaveLine()
    {
        byte[] data = [10, 1, 20, 1, 30, 1, 40, 1, 50, 1, 60, 1, 70, 1, 80, 1];
        Encode(new FrameInfo(2, 2, 16, 2), data, 44, InterleaveMode.Line);
    }

    [Fact]
    public void Encode2Components16BitInterleaveSample()
    {
        byte[] data = [10, 1, 20, 1, 30, 1, 40, 1, 50, 1, 60, 1, 70, 1, 80, 1];
        Encode(new FrameInfo(2, 2, 16, 2), data, 44, InterleaveMode.Sample);
    }

    [Fact]
    public void EncodeColor8BitInterleaveNoneLossless()
    {
        Encode("conformance/test8.ppm", 102248);
    }

    [Fact]
    public void EncodeColor8BitInterleaveLineLossless()
    {
        Encode("conformance/test8.ppm", 100615, InterleaveMode.Line);
    }

    [Fact]
    public void EncodeColor8BitInterleaveSampleLossless()
    {
        Encode("conformance/test8.ppm", 99734, InterleaveMode.Sample);
    }

    [Fact]
    public void EncodeColor8BitInterleaveLineHP1()
    {
        Encode("conformance/test8.ppm", 91617, InterleaveMode.Line, ColorTransformation.HP1);
    }

    [Fact]
    public void EncodeColor8BitInterleaveSampleHP1()
    {
        Encode("conformance/test8.ppm", 91463, InterleaveMode.Sample, ColorTransformation.HP1);
    }

    [Fact]
    public void EncodeColor8BitInterleaveLineHP2()
    {
        Encode("conformance/test8.ppm", 91693, InterleaveMode.Line, ColorTransformation.HP2);
    }

    [Fact]
    public void EncodeColor8BitInterleaveSampleHP2()
    {
        Encode("conformance/test8.ppm", 91457, InterleaveMode.Sample, ColorTransformation.HP2);
    }

    [Fact]
    public void EncodeColor8BitInterleaveLineHP3()
    {
        Encode("conformance/test8.ppm", 91993, InterleaveMode.Line, ColorTransformation.HP3);
    }

    [Fact]
    public void EncodeColor8BitInterleaveSampleHP3()
    {
        Encode("conformance/test8.ppm", 91862, InterleaveMode.Sample, ColorTransformation.HP3);
    }

    [Fact]
    public void EncodeMonochrome16BitInterleaveNone()
    {
        byte[] data = [0, 10, 0, 20, 0, 30, 0, 40];
        Encode(new FrameInfo(2, 2, 16, 1), data, 36, InterleaveMode.None);
    }

    [Fact]
    public void EncodeColor16BitInterleaveNone()
    {
        byte[] data = [10, 20, 30, 40, 50, 60];
        Encode(new FrameInfo(1, 1, 16, 3), data, 66, InterleaveMode.None);
    }

    [Fact]
    public void EncodeColor16BitInterleaveLine()
    {
        byte[] data = [10, 20, 30, 40, 50, 60];
        Encode(new FrameInfo(1, 1, 16, 3), data, 45, InterleaveMode.Line);
    }

    [Fact]
    public void EncodeColor16BitInterleaveSample()
    {
        byte[] data =
        [
            0, 0, 0, 0, 0, 0,    // row 0, pixel 0
            0, 0, 0, 0, 0, 0,    // row 0, pixel 1
            1, 10, 1, 20, 1, 30, // row 1, pixel 0
            1, 40, 1, 50, 1, 60  // row 1, pixel 1
        ];
        Encode(new FrameInfo(2, 2, 16, 3), data, 51, InterleaveMode.Sample);
    }

    [Fact]
    public void EncodeColor16BitInterleaveLineHP1()
    {
        byte[] data = [10, 20, 30, 40, 50, 60];
        Encode(new FrameInfo(1, 1, 16, 3), data, 59, InterleaveMode.Line, ColorTransformation.HP1);
    }

    [Fact]
    public void EncodeColor16BitInterleaveSampleHP1()
    {
        byte[] data = [10, 20, 30, 40, 50, 60];
        Encode(new FrameInfo(1, 1, 16, 3), data, 59, InterleaveMode.Sample, ColorTransformation.HP1);
    }

    [Fact]
    public void EncodeColor16BitInterleaveLineHP2()
    {
        byte[] data = [10, 20, 30, 40, 50, 60];
        Encode(new FrameInfo(1, 1, 16, 3), data, 59, InterleaveMode.Line, ColorTransformation.HP2);
    }

    [Fact]
    public void EncodeColor16BitInterleaveSampleHP2()
    {
        byte[] data = [10, 20, 30, 40, 50, 60];
        Encode(new FrameInfo(1, 1, 16, 3), data, 59, InterleaveMode.Sample, ColorTransformation.HP2);
    }

    [Fact]
    public void EncodeColor16BitInterleaveLineHP3()
    {
        byte[] data = [10, 20, 30, 40, 50, 60];
        Encode(new FrameInfo(1, 1, 16, 3), data, 55, InterleaveMode.Line, ColorTransformation.HP3);
    }

    [Fact]
    public void EncodeColor16BitInterleaveSampleHP3()
    {
        byte[] data = [10, 20, 30, 40, 50, 60];
        Encode(new FrameInfo(1, 1, 16, 3), data, 55, InterleaveMode.Sample, ColorTransformation.HP3);
    }

    [Fact]
    public void Encode4Components8BitInterleaveNone()
    {
        byte[] data = [10, 20, 30, 40];
        Encode(new FrameInfo(1, 1, 8, 4), data, 75, InterleaveMode.None);
    }

    [Fact]
    public void Encode4Components8BitInterleaveLine()
    {
        byte[] data = [10, 20, 30, 40];
        Encode(new FrameInfo(1, 1, 8, 4), data, 47, InterleaveMode.Line);
    }

    [Fact]
    public void Encode4Components8BitInterleaveSample()
    {
        byte[] data = [10, 20, 30, 40];
        Encode(new FrameInfo(1, 1, 8, 4), data, 47, InterleaveMode.Sample);
    }

    [Fact]
    public void Encode4Components16BitInterleaveNone()
    {
        byte[] data = [10, 20, 30, 40, 50, 60, 70, 80];
        Encode(new FrameInfo(1, 1, 16, 4), data, 86, InterleaveMode.None);
    }

    [Fact]
    public void Encode4Components16BitInterleaveLine()
    {
        byte[] data = [10, 20, 30, 40, 50, 60, 70, 80];
        Encode(new FrameInfo(1, 1, 16, 4), data, 52, InterleaveMode.Line);
    }

    [Fact]
    public void Encode4Components16BitInterleaveSample()
    {
        byte[] data =
        [
            0, 0, 0, 0, 0, 0, 0, 0,     // row 0, pixel 0
            0, 0, 0, 0, 0, 0, 0, 0,     // row 0, pixel 1
            1, 10, 1, 20, 1, 30, 1, 40, // row 1, pixel 0
            1, 50, 1, 60, 1, 70, 1, 80  // row 1, pixel 1
        ];

        Encode(new FrameInfo(2, 2, 16, 4), data, 61, InterleaveMode.Sample);
    }

    [Fact]
    public void EncodeWithDifferentLosslessValues()
    {
        JpegLSEncoder encoder = new() { FrameInfo = new FrameInfo(2, 2, 8, 3),
            InterleaveMode = InterleaveMode.None };

        byte[] data =
        [
            24, 23,
            22, 21
        ];

        Memory<byte> encodedData = new byte[encoder.EstimatedDestinationSize];
        encoder.Destination = encodedData;

        encoder.NearLossless = 0;
        encoder.EncodeComponents(data, 1);
        encoder.NearLossless = 2;
        encoder.EncodeComponents(data, 1);
        encoder.NearLossless = 10;
        encoder.EncodeComponents(data, 1);

        JpegLSDecoder decoder = new() { Source = encoder.EncodedData };
        decoder.ReadHeader();

        var destination = new byte[decoder.GetDestinationSize()];
        decoder.Decode(destination);

        CheckOutput(data, destination, decoder, 3, decoder.FrameInfo.Height * decoder.FrameInfo.Width);
        Assert.Equal(0, decoder.GetNearLossless());
        Assert.Equal(2, decoder.GetNearLossless(1));
        Assert.Equal(10, decoder.GetNearLossless(2));
    }

    [Fact]
    public void EncodeWithDifferentPresetCodingParameters()
    {
        JpegLSEncoder encoder = new()
        {
            FrameInfo = new FrameInfo(8, 2, 8, 3),
            InterleaveMode = InterleaveMode.None
        };

        byte[] data =
        [
            24, 23, 22, 21, 20, 19, 18, 17,
            16, 15, 14, 13, 12, 11, 10, 9
        ];

        Memory<byte> encodedData = new byte[encoder.EstimatedDestinationSize];
        encoder.Destination = encodedData;

        encoder.PresetCodingParameters = JpegLSPresetCodingParameters.Default;
        encoder.EncodeComponents(data, 1);
        encoder.PresetCodingParameters = new JpegLSPresetCodingParameters(25, 10, 20, 22, 64);
        encoder.EncodeComponents(data, 1);
        encoder.PresetCodingParameters = new JpegLSPresetCodingParameters(25, 0, 0, 0, 3);
        encoder.EncodeComponents(data, 1);

        JpegLSDecoder decoder = new() { Source = encoder.EncodedData };
        decoder.ReadHeader();

        var destination = new byte[decoder.GetDestinationSize()];
        decoder.Decode(destination);

        CheckOutput(data, destination, decoder, 3, decoder.FrameInfo.Height * decoder.FrameInfo.Width);
    }

    [Fact]
    public void EncodeWithDifferentInterleaveModesNoneFirst()
    {
        JpegLSEncoder encoder = new()
        {
            FrameInfo = new FrameInfo(8, 2, 8, 4),
            InterleaveMode = InterleaveMode.None
        };

        byte[] component0 =
        [
            24, 23, 22, 21, 20, 19, 18, 17,
            16, 15, 14, 13, 12, 11, 10, 9
        ];

        byte[] component1And2And3 =
        [
            24, 16, 23, 15, 22, 14, 21, 13, 20, 12, 19, 11, 18, 10, 17, 9,
            24, 16, 23, 15, 22, 14, 21, 13, 20, 12, 19, 11, 18, 10, 17, 9,
            24, 16, 23, 15, 22, 14, 21, 13, 20, 12, 19, 11, 18, 10, 17, 9
        ];

        Memory<byte> encodedData = new byte[encoder.EstimatedDestinationSize];
        encoder.Destination = encodedData;

        encoder.InterleaveMode = InterleaveMode.None;
        encoder.EncodeComponents(component0, 1);
        encoder.InterleaveMode = InterleaveMode.Sample;
        encoder.EncodeComponents(component1And2And3, 3);
        
        JpegLSDecoder decoder = new() { Source = encoder.EncodedData };
        decoder.ReadHeader();

        Span<byte> destination = new byte[decoder.GetDestinationSize()];
        decoder.Decode(destination);

        CheckOutput(component0, destination, decoder, 1, 8 * 2);
        CheckOutput(component1And2And3, destination[(8 * 2)..], decoder, 1, 8 * 2 * 3);
        Assert.Equal(InterleaveMode.None, decoder.GetInterleaveMode());
        Assert.Equal(InterleaveMode.Sample, decoder.GetInterleaveMode(1));
        Assert.Equal(InterleaveMode.Sample, decoder.GetInterleaveMode(2));
        Assert.Equal(InterleaveMode.Sample, decoder.GetInterleaveMode(3));
    }

    [Fact]
    public void EncodeWithDifferentInterleaveModesSampleFirst()
    {
        JpegLSEncoder encoder = new()
        {
            FrameInfo = new FrameInfo(8, 2, 8, 4),
            InterleaveMode = InterleaveMode.None
        };

        byte[] component0And1And2 =
        [
            24, 16, 23, 15, 22, 14, 21, 13, 20, 12, 19, 11, 18, 10, 17, 9,
            24, 16, 23, 15, 22, 14, 21, 13, 20, 12, 19, 11, 18, 10, 17, 9,
            24, 16, 23, 15, 22, 14, 21, 13, 20, 12, 19, 11, 18, 10, 17, 9
        ];

        byte[] component3 =
        [
            24, 23, 22, 21, 20, 19, 18, 17,
            16, 15, 14, 13, 12, 11, 10, 9
        ];

        Memory<byte> encodedData = new byte[encoder.EstimatedDestinationSize];
        encoder.Destination = encodedData;

        encoder.InterleaveMode = InterleaveMode.Sample;
        encoder.EncodeComponents(component0And1And2, 3);
        encoder.InterleaveMode = InterleaveMode.None;
        encoder.EncodeComponents(component3, 1);

        JpegLSDecoder decoder = new() { Source = encoder.EncodedData };
        decoder.ReadHeader();

        Span<byte> destination = new byte[decoder.GetDestinationSize()];
        decoder.Decode(destination);

        CheckOutput(component0And1And2, destination, decoder, 1, 8 * 2 * 3);
        CheckOutput(component3, destination[(8*2*3)..], decoder, 1, 8 * 2);
        Assert.Equal(InterleaveMode.Sample, decoder.GetInterleaveMode());
        Assert.Equal(InterleaveMode.Sample, decoder.GetInterleaveMode(1));
        Assert.Equal(InterleaveMode.Sample, decoder.GetInterleaveMode(2));
        Assert.Equal(InterleaveMode.None, decoder.GetInterleaveMode(3));
    }

    private static void CheckOutput(Span<byte> source, Span<byte> destination, JpegLSDecoder decoder, int componentCount, int componentSize)
    {
        for (int component = 0; component < componentCount; ++component)
        {
            Span<byte> componentDestination = destination.Slice(component * componentSize, componentSize);

            int nearLossless = decoder.GetNearLossless(component);
            if (nearLossless == 0)
            {
                for (int i = 0; i != source.Length; ++i)
                {
                    Assert.Equal(source[i], componentDestination[i]);
                }
            }
            else
            {
                for (int i = 0; i != source.Length; ++i)
                {
                    Assert.True(Math.Abs(source[i] - componentDestination[i]) <= nearLossless);
                }
            }
        }
    }

    private static void Encode(string filename, int expectedSize, InterleaveMode interleaveMode = InterleaveMode.None,
        ColorTransformation colorTransformation = ColorTransformation.None)
    {
        var referenceFile = Util.ReadAnymapReferenceFile(filename, interleaveMode);

        Encode(new FrameInfo(referenceFile.Width, referenceFile.Height, referenceFile.BitsPerSample, referenceFile.ComponentCount),
            referenceFile.ImageData, expectedSize, interleaveMode, colorTransformation);
    }

    private static void Encode(FrameInfo frameInfo, ReadOnlySpan<byte> source, int expectedSize, InterleaveMode interleaveMode,
        ColorTransformation colorTransformation = ColorTransformation.None)
    {
        JpegLSEncoder encoder = new() { FrameInfo = frameInfo, InterleaveMode = interleaveMode, ColorTransformation = colorTransformation };

        Memory<byte> encodedData = new byte[encoder.EstimatedDestinationSize];
        encoder.Destination = encodedData;

        encoder.Encode(source);

        Assert.Equal(expectedSize, encoder.BytesWritten);

        encodedData = encodedData[..encoder.BytesWritten];
        Util.TestByDecoding(encodedData, frameInfo, source, interleaveMode, colorTransformation);
    }
}
