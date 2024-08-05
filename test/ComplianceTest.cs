// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using Xunit;

namespace CharLS.JpegLS.Test;

public class ComplianceTest
{

    [Fact]
    public void DecompressColor8BitInterleaveNoneLossless()
    {
        // ISO 14495-1: official test image 1 (T87_test-1-2-3-4-5-6.zip)
        DecompressFile("conformance/t8c0e0.jls", "conformance/test8.ppm");
    }

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

    [Fact]
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

    [Fact]
    public void DecompressMappingTableSampleAnnexH4Dot5()
    {
        // ISO 14495-1: Sample image from appendix H.4.5 "Example of a palletised image" / Figure H.10
        byte[] palettisedData =
        [
            0xFF, 0xD8, // Start of image (SOI) marker
            0xFF, 0xF7, // Start of JPEG-LS frame (SOF 55) marker - marker segment follows
            0x00, 0x0B, // Length of marker segment = 11 bytes including the length field
            0x02, // P = Precision = 2 bits per sample
            0x00, 0x04, // Y = Number of lines = 4
            0x00, 0x03, // X = Number of columns = 3
            0x01, // Nf = Number of components in the frame = 1
            0x01, // C1  = Component ID = 1 (first and only component)
            0x11, // Sub-sampling: H1 = 1, V1 = 1
            0x00, // Tq1 = 0 (this field is always 0)

            //0xFF, byte{ 0xF8},             // LSE - JPEG-LS preset parameters marker
            //0x00, byte{ 0x11},             // Length of marker segment = 17 bytes including the length field
            //0x02,                         // ID = 2, mapping table
            //0x05,                         // TID = 5 Table identifier (arbitrary)
            //0x03,                         // Wt = 3 Width of table entry
            //0xFF, byte{ 0xFF}, byte{ 0xFF}, // Entry for index 0
            //0xFF, byte{ 0x00}, byte{ 0x00}, // Entry for index 1
            //0x00, byte{ 0xFF}, byte{ 0x00}, // Entry for index 2
            //0x00, byte{ 0x00}, byte{ 0xFF}, // Entry for index 3

            0xFF, 0xDA, // Start of scan (SOS) marker
            0x00, 0x08, // Length of marker segment = 8 bytes including the length field
            0x01, // Ns = Number of components for this scan = 1
            0x01, // C1 = Component ID = 1
            0x00, // Tm 1  = Mapping table identifier = 5
            0x00, // NEAR = 0 (near-lossless max error)
            0x00, // ILV = 0 (interleave mode = non-interleaved)
            0x00, // Al = 0, Ah = 0 (no point transform)
            0xDB, 0x95, 0xF0, // 3 bytes of compressed image data
            0xFF, 0xD9 // End of image (EOI) marker
        ];

        JpegLSDecoder decoder = new JpegLSDecoder { Source = palettisedData };
        decoder.ReadHeader();

        var destination = new byte[decoder.GetDestinationSize()];
        decoder.Decode(destination);

        byte[] expected = [0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 3];
        Util.CompareBuffers(destination, expected);

        //const int32_t mapping_table_id{ decoder.mapping_table_id(0)};
        //Assert::AreEqual(5, mapping_table_id);

        //const auto optional_table_index{ decoder.mapping_table_index(mapping_table_id)};
        //Assert::IsTrue(optional_table_index.has_value());
        //const auto table_index{ optional_table_index.value_or(-1)};

        //const table_info table_info{ decoder.mapping_table_info(table_index)};
        //vector<byte> mapping_table(table_info.data_size);

        //decoder.mapping_table(table_index, mapping_table);

        //constexpr array expected_mapping_table{
        //    byte{ 0xFF}, byte{ 0xFF}, byte{ 0xFF}, byte{ 0xFF}, byte{ 0}, byte{ 0},
        //                                       byte{ 0},    byte{ 0xFF}, byte{ 0},    byte{ 0},    byte{ 0}, byte{ 0xFF}
        //};
        //compare_buffers(expected_mapping_table.data(), expected_mapping_table.size(), mapping_table.data(),
        //                mapping_table.size());
    }


    private static void DecompressFile(string encodedFilename, string rawFilename, bool checkEncode = true)
    {
        var encodedSource = Util.ReadFile(encodedFilename);

        JpegLSDecoder decoder = new(encodedSource);

        var referenceFile = Util.ReadAnymapReferenceFile(rawFilename, decoder.InterleaveMode, decoder.FrameInfo);

        Util.TestCompliance(encodedSource, referenceFile.ImageData, checkEncode);
    }


}
