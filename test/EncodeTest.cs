// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using Xunit;

namespace CharLS.JpegLS.Test;

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

    //TEST_METHOD(encode_color_8_bit_interleave_line_hp1) // NOLINT
    //{
    //    encode("DataFiles/test8.ppm", 91617, interleave_mode::line, color_transformation::hp1);
    //}

    [Fact]
    public void EncodeColor8BitInterleaveSampleHp1()
    {
        Encode("conformance/test8.ppm", 91463, InterleaveMode.Sample, ColorTransformation.HP1);
    }

    //TEST_METHOD(encode_color_8_bit_interleave_line_hp2) // NOLINT
    //{
    //    encode("DataFiles/test8.ppm", 91693, interleave_mode::line, color_transformation::hp2);
    //}

    [Fact]
    public void EncodeColor8BitInterleaveSampleHp2() // NOLINT
    {
        Encode("conformance/test8.ppm", 91457, InterleaveMode.Sample, ColorTransformation.HP2);
    }

    //TEST_METHOD(encode_color_8_bit_interleave_line_hp3) // NOLINT
    //{
    //    encode("DataFiles/test8.ppm", 91993, interleave_mode::line, color_transformation::hp3);
    //}

    //TEST_METHOD(encode_color_8_bit_interleave_sample_hp3) // NOLINT
    //{
    //    encode("DataFiles/test8.ppm", 91862, interleave_mode::sample, color_transformation::hp3);
    //}

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

    //TEST_METHOD(encode_color_16_bit_interleave_sample_hp1) // NOLINT
    //{
    //    constexpr array data{ byte{ 10}, byte{ 20}, byte{ 30}, byte{ 40}, byte{ 50}, byte{ 60} };
    //    encode({ 1, 1, 16, 3}, { data.cbegin(), data.cend()}, 59, interleave_mode::sample, color_transformation::hp1);
    //}

    //TEST_METHOD(encode_color_16_bit_interleave_sample_hp2) // NOLINT
    //{
    //    constexpr array data{ byte{ 10}, byte{ 20}, byte{ 30}, byte{ 40}, byte{ 50}, byte{ 60} };
    //    encode({ 1, 1, 16, 3}, { data.cbegin(), data.cend()}, 59, interleave_mode::sample, color_transformation::hp2);
    //}

    //TEST_METHOD(encode_color_16_bit_interleave_sample_hp3) // NOLINT
    //{
    //    constexpr array data{ byte{ 10}, byte{ 20}, byte{ 30}, byte{ 40}, byte{ 50}, byte{ 60} };
    //    encode({ 1, 1, 16, 3}, { data.cbegin(), data.cend()}, 55, interleave_mode::sample, color_transformation::hp3);
    //}

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
        Util.TestByDecoding(encodedData, frameInfo, source, interleaveMode /*, color_transformation*/);
    }
}
