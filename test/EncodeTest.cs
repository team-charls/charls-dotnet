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

    //TEST_METHOD(encode_color_8_bit_interleave_sample_lossless) // NOLINT
    //{
    //    encode("DataFiles/test8.ppm", 99734, interleave_mode::sample);
    //}

    //TEST_METHOD(encode_color_8_bit_interleave_line_hp1) // NOLINT
    //{
    //    encode("DataFiles/test8.ppm", 91617, interleave_mode::line, color_transformation::hp1);
    //}

    //TEST_METHOD(encode_color_8_bit_interleave_sample_hp1) // NOLINT
    //{
    //    encode("DataFiles/test8.ppm", 91463, interleave_mode::sample, color_transformation::hp1);
    //}

    //TEST_METHOD(encode_color_8_bit_interleave_line_hp2) // NOLINT
    //{
    //    encode("DataFiles/test8.ppm", 91693, interleave_mode::line, color_transformation::hp2);
    //}

    //TEST_METHOD(encode_color_8_bit_interleave_sample_hp2) // NOLINT
    //{
    //    encode("DataFiles/test8.ppm", 91457, interleave_mode::sample, color_transformation::hp2);
    //}

    //TEST_METHOD(encode_color_8_bit_interleave_line_hp3) // NOLINT
    //{
    //    encode("DataFiles/test8.ppm", 91993, interleave_mode::line, color_transformation::hp3);
    //}

    //TEST_METHOD(encode_color_8_bit_interleave_sample_hp3) // NOLINT
    //{
    //    encode("DataFiles/test8.ppm", 91862, interleave_mode::sample, color_transformation::hp3);
    //}

    //TEST_METHOD(encode_color_16_bit_interleave_none) // NOLINT
    //{
    //    constexpr array data{ byte{ 10}, byte{ 20}, byte{ 30}, byte{ 40}, byte{ 50}, byte{ 60} };
    //    encode({ 1, 1, 16, 3}, { data.cbegin(), data.cend()}, 66, interleave_mode::none);
    //}

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

    //TEST_METHOD(encode_4_components_8_bit_interleave_none) // NOLINT
    //{
    //    constexpr array data{ byte{ 10}, byte{ 20}, byte{ 30}, byte{ 40} };
    //    encode({ 1, 1, 8, 4}, { data.cbegin(), data.cend()}, 75, interleave_mode::none);
    //}

    //TEST_METHOD(encode_4_components_8_bit_interleave_line) // NOLINT
    //{
    //    constexpr array data{ byte{ 10}, byte{ 20}, byte{ 30}, byte{ 40} };
    //    encode({ 1, 1, 8, 4}, { data.cbegin(), data.cend()}, 47, interleave_mode::line);
    //}

    //TEST_METHOD(encode_4_components_8_bit_interleave_sample) // NOLINT
    //{
    //    constexpr array data{ byte{ 10}, byte{ 20}, byte{ 30}, byte{ 40} };
    //    encode({ 1, 1, 8, 4}, { data.cbegin(), data.cend()}, 47, interleave_mode::sample);
    //}

    //TEST_METHOD(encode_4_components_16_bit_interleave_none) // NOLINT
    //{
    //    constexpr array data{ byte{ 10}, byte{ 20}, byte{ 30}, byte{ 40}, byte{ 50}, byte{ 60}, byte{ 70}, byte{ 80} };
    //    encode({ 1, 1, 16, 4}, { data.cbegin(), data.cend()}, 86, interleave_mode::none);
    //}

    //TEST_METHOD(encode_4_components_16_bit_interleave_line) // NOLINT
    //{
    //    constexpr array data{ byte{ 10}, byte{ 20}, byte{ 30}, byte{ 40}, byte{ 50}, byte{ 60}, byte{ 70}, byte{ 80} };
    //    encode({ 1, 1, 16, 4}, { data.cbegin(), data.cend()}, 52, interleave_mode::line);
    //}

    //TEST_METHOD(encode_4_components_16_bit_interleave_sample) // NOLINT
    //{
    //    constexpr array data{ byte{ 10}, byte{ 20}, byte{ 30}, byte{ 40}, byte{ 50}, byte{ 60}, byte{ 70}, byte{ 80} };
    //    encode({ 1, 1, 16, 4}, { data.cbegin(), data.cend()}, 52, interleave_mode::sample);
    //}

    private static void Encode(string filename, int expectedSize, InterleaveMode interleaveMode = InterleaveMode.None
    /*color_transformation color_transformation = color_transformation::none*/)
    {
        var referenceFile = Util.ReadAnymapReferenceFile(filename, interleaveMode);

        Encode(new FrameInfo(referenceFile.Width, referenceFile.Height, referenceFile.BitsPerSample, referenceFile.ComponentCount),
            referenceFile.ImageData, expectedSize, interleaveMode/*, color_transformation*/);
    }

    private static void Encode(FrameInfo frameInfo, Memory<byte> source, int expectedSize, InterleaveMode interleaveMode
    /*const color_transformation color_transformation = color_transformation::none*/)
    {
        JpegLSEncoder encoder = new() { FrameInfo = frameInfo, InterleaveMode = interleaveMode };

        Memory<byte> encodedData = new byte[encoder.EstimatedDestinationSize];
        encoder.Destination = encodedData;

        encoder.Encode(source);

        Assert.Equal(expectedSize, encoder.BytesWritten);

        encodedData = encodedData[..encoder.BytesWritten];
        Util.TestByDecoding(encodedData, frameInfo, source, interleaveMode /*, color_transformation*/);
    }
}
