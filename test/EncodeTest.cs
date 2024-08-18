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
    public void EncodeColor16BitInterleaveNone()
    {
        byte[] data = [ 10, 20, 30, 40, 50, 60 ];
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
        byte[] data = [10, 20, 30, 40, 50, 60];
        Encode(new FrameInfo(1, 1, 16, 3), data, 45, InterleaveMode.Sample);
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
        byte[] data = [ 10, 20, 30, 40];
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
        byte[] data = [10, 20, 30, 40, 50, 60, 70,  80];
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
        byte[] data = [10, 20, 30, 40, 50, 60, 70, 80];
        Encode(new FrameInfo(1, 1, 16, 4), data, 52, InterleaveMode.Sample);
    }

    private static void Encode(string filename, int expectedSize, InterleaveMode interleaveMode = InterleaveMode.None,
        ColorTransformation colorTransformation = ColorTransformation.None)
    {
        var referenceFile = Util.ReadAnymapReferenceFile(filename, interleaveMode);

        Encode(new FrameInfo(referenceFile.Width, referenceFile.Height, referenceFile.BitsPerSample, referenceFile.ComponentCount),
            referenceFile.ImageData, expectedSize, interleaveMode, colorTransformation);
    }

    private static void Encode(FrameInfo frameInfo, ReadOnlyMemory<byte> source, int expectedSize, InterleaveMode interleaveMode,
        ColorTransformation colorTransformation = ColorTransformation.None)
    {
        JpegLSEncoder encoder = new() { FrameInfo = frameInfo, InterleaveMode = interleaveMode, ColorTransformation = colorTransformation};

        Memory<byte> encodedData = new byte[encoder.EstimatedDestinationSize];
        encoder.Destination = encodedData;

        encoder.Encode(source);

        Assert.Equal(expectedSize, encoder.BytesWritten);

        encodedData = encodedData[..encoder.BytesWritten];
        Util.TestByDecoding(encodedData, frameInfo, source.Span, interleaveMode /*, color_transformation*/);
    }
}
