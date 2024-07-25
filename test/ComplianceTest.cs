// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using Xunit;

namespace CharLS.JpegLS.Test;

public class ComplianceTest
{

    //TEST_METHOD(decompress_color_8_bit_interleave_none_lossless) // NOLINT
    //    {
    //        // ISO 14495-1: official test image 1 (T87_test-1-2-3-4-5-6.zip)
    //        decompress_file("DataFiles/t8c0e0.jls", "DataFiles/test8.ppm");
    //    }

    //    TEST_METHOD(decompress_color_8_bit_interleave_line_lossless) // NOLINT
    //    {
    //        // ISO 14495-1: official test image 2 (T87_test-1-2-3-4-5-6.zip)
    //        decompress_file("DataFiles/t8c1e0.jls", "DataFiles/test8.ppm");
    //    }

    //    TEST_METHOD(decompress_color_8_bit_interleave_sample_lossless) // NOLINT
    //    {
    //        // ISO 14495-1: official test image 3 (T87_test-1-2-3-4-5-6.zip)
    //        decompress_file("DataFiles/t8c2e0.jls", "DataFiles/test8.ppm");
    //    }

    //    TEST_METHOD(decompress_color_8_bit_interleave_none_near_lossless_3) // NOLINT
    //    {
    //        // ISO 14495-1: official test image 4 (T87_test-1-2-3-4-5-6.zip)
    //        decompress_file("DataFiles/t8c2e3.jls", "DataFiles/test8.ppm");
    //    }

    //    TEST_METHOD(decompress_color_8_bit_interleave_line_near_lossless_3) // NOLINT
    //    {
    //        // ISO 14495-1: official test image 5 (T87_test-1-2-3-4-5-6.zip)
    //        decompress_file("DataFiles/t8c1e3.jls", "DataFiles/test8.ppm");
    //    }

    //    TEST_METHOD(decompress_color_8_bit_interleave_sample_near_lossless_3) // NOLINT
    //    {
    //        // ISO 14495-1: official test image 6 (T87_test-1-2-3-4-5-6.zip)
    //        decompress_file("DataFiles/t8c2e3.jls", "DataFiles/test8.ppm");
    //    }

    //    TEST_METHOD(decompress_color_8_bit_interleave_line_lossless_non_default) // NOLINT
    //    {
    //        // ISO 14495-1: official test image 9 (T87_test-1-2-3-4-5-6.zip)
    //        // NON-DEFAULT parameters T1=T2=T3=9,RESET=31.
    //        decompress_file("DataFiles/t8nde0.jls", "DataFiles/test8bs2.pgm");
    //    }

    //    TEST_METHOD(decompress_color_8_bit_interleave_line_near_lossless_3_non_default) // NOLINT
    //    {
    //        // ISO 14495-1: official test image 10 (T87_test-1-2-3-4-5-6.zip)
    //        // NON-DEFAULT parameters T1=T2=T3=9,RESET=31.
    //        decompress_file("DataFiles/t8nde3.jls", "DataFiles/test8bs2.pgm");
    //    }

    //    TEST_METHOD(decompress_monochrome_16_bit_lossless) // NOLINT
    //    {
    //        // ISO 14495-1: official test image 11 (T87_test-11-12.zip)
    //        // Note: test image is actually 12 bit.
    //        decompress_file("DataFiles/t16e0.jls", "DataFiles/test16.pgm");
    //    }

    //    TEST_METHOD(decompress_monochrome_16_bit_near_lossless_3) // NOLINT
    //    {
    //        // ISO 14495-1: official test image 12 (T87_test-11-12.zip)
    //        // Note: test image is actually 12 bit.
    //        decompress_file("DataFiles/t16e3.jls", "DataFiles/TEST16.pgm", false);
    //    }

    [Fact(Skip = "WIP")]
    public void TulipsMonochrome8BitLosslessHp()
    {
        // Additional, Tulips encoded with HP 1.0BETA encoder.
        DecompressFile("test-images/tulips-gray-8bit-512-512-hp-encoder.jls", "test-images/tulips-gray-8bit-512-512.pgm");
    }

    //    TEST_METHOD(decompress_color_8_bit_interleave_none_lossless_restart_7) // NOLINT
    //    {
    //        // ISO 14495-1: official test image 1 but with restart markers.
    //        decompress_file("DataFiles/test8_ilv_none_rm_7.jls", "DataFiles/test8.ppm", false);
    //    }

    //    TEST_METHOD(decompress_color_8_bit_interleave_line_lossless_restart_7) // NOLINT
    //    {
    //        // ISO 14495-1: official test image 2 but with restart markers.
    //        decompress_file("DataFiles/test8_ilv_line_rm_7.jls", "DataFiles/test8.ppm", false);
    //    }

    //    TEST_METHOD(decompress_color_8_bit_interleave_sample_lossless_restart_7) // NOLINT
    //    {
    //        // ISO 14495-1: official test image 3 but with restart markers.
    //        decompress_file("DataFiles/test8_ilv_sample_rm_7.jls", "DataFiles/test8.ppm", false);
    //    }

    //    TEST_METHOD(decompress_color_8_bit_interleave_sample_lossless_restart_300) // NOLINT
    //    {
    //        // ISO 14495-1: official test image 3 but with restart markers and restart interval 300
    //        decompress_file("DataFiles/test8_ilv_sample_rm_300.jls", "DataFiles/test8.ppm", false);
    //    }

    //    TEST_METHOD(decompress_monochrome_16_bit_restart_5) // NOLINT
    //    {
    //        // ISO 14495-1: official test image 12 but with restart markers and restart interval 5
    //        decompress_file("DataFiles/test16_rm_5.jls", "DataFiles/test16.pgm", false);
    //    }

    private static void DecompressFile(string encodedFilename, string rawFilename, bool checkEncode = true)
    {
        var encodedSource = Util.ReadFile(encodedFilename);

        JpegLSDecoder decoder = new(encodedSource);

        var referenceFile = Util.ReadAnymapReferenceFile(rawFilename, decoder.InterleaveMode, decoder.FrameInfo);

        Util.TestCompliance(encodedSource, referenceFile.ImageData, checkEncode);
    }


}
