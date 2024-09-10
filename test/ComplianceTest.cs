// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed.Test;

public class ComplianceTest
{
    [Fact]
    public void DecodeEncodeColor8BitInterleaveNoneLossless()
    {
        // ISO 14495-1: official test image 1 (T87_test-1-2-3-4-5-6.zip)
        DecodeEncodeFile("conformance/t8c0e0.jls", "conformance/test8.ppm");
    }

    [Fact]
    public void DecodeEncodeColor8BitInterleaveLineLossless()
    {
        // ISO 14495-1: official test image 2 (T87_test-1-2-3-4-5-6.zip)
        DecodeEncodeFile("conformance/t8c1e0.jls", "conformance/test8.ppm");
    }

    [Fact]
    public void DecodeEncodeColor8BitInterleaveSampleLossless()
    {
        // ISO 14495-1: official test image 3 (T87_test-1-2-3-4-5-6.zip)
        DecodeEncodeFile("conformance/t8c2e0.jls", "conformance/test8.ppm");
    }

    [Fact]
    public void DecodeEncodeColor8BitInterleaveNoneNearLossless3()
    {
        // ISO 14495-1: official test image 4 (T87_test-1-2-3-4-5-6.zip)
        DecodeEncodeFile("conformance/t8c0e3.jls", "conformance/test8.ppm");
    }

    [Fact]
    public void DecodeEncodeColor8BitInterleaveLineNearLossless3()
    {
        // ISO 14495-1: official test image 5 (T87_test-1-2-3-4-5-6.zip)
        DecodeEncodeFile("conformance/t8c1e3.jls", "conformance/test8.ppm");
    }

    [Fact]
    public void DecodeEncodeColor8BitInterleaveSampleNearLossless3()
    {
        // ISO 14495-1: official test image 6 (T87_test-1-2-3-4-5-6.zip)
        DecodeEncodeFile("conformance/t8c2e3.jls", "conformance/test8.ppm");
    }

    [Fact]
    public void DecodeEncodeColor8BitInterleaveNoneLosslessNonDefault()
    {
        // ISO 14495-1: official test image 9 (T87_test-1-2-3-4-5-6.zip)
        // NON-DEFAULT parameters T1=T2=T3=9,RESET=31.
        DecodeEncodeFile("conformance/t8nde0.jls", "conformance/test8bs2.pgm");
    }

    [Fact]
    public void DecodeEncodeColor8BitInterleaveNoneNearLossless3NonDefault()
    {
        // ISO 14495-1: official test image 10 (T87_test-1-2-3-4-5-6.zip)
        // NON-DEFAULT parameters T1=T2=T3=9,RESET=31.
        DecodeEncodeFile("conformance/t8nde3.jls", "conformance/test8bs2.pgm");
    }

    [Fact]
    public void DecodeEncodeMonochrome16BitLossless()
    {
        // ISO 14495-1: official test image 11 (T87_test-11-12.zip)
        // Note: test image is actually 12 bit.
        DecodeEncodeFile("conformance/t16e0.jls", "conformance/test16.pgm");
    }

    [Fact]
    public void DecodeMonochrome16BitNearLossless3()
    {
        // ISO 14495-1: official test image 12 (T87_test-11-12.zip)
        // Note: test image is actually 12 bit.
        DecodeEncodeFile("conformance/t16e3.jls", "conformance/test16.pgm", false);
    }

    [Fact]
    public void DecodeEncodeTulipsMonochrome8BitLosslessHp()
    {
        // Additional, Tulips encoded with HP 1.0BETA encoder.
        DecodeEncodeFile("test-images/tulips-gray-8bit-512-512-hp-encoder.jls", "test-images/tulips-gray-8bit-512-512.pgm");
    }

    [Fact]
    public void DecodeTulipsMonochrome8BitLosslessHpForMeasurement()
    {
        var encodedSource = Util.ReadFile("test-images/tulips-gray-8bit-512-512-hp-encoder.jls");

        JpegLSDecoder decoder = new(encodedSource);
        var destination = new byte[decoder.GetDestinationSize()];

        const int loopTimes = 1; // Increase to 300 for profiling sessions.
        for (int i = 0; i < loopTimes; ++i)
        {
            JpegLSDecoder decoder2 = new(encodedSource);
            decoder2.Decode(destination);
        }
    }

    [Fact]
    public void DecodeColor8BitInterleaveNoneLosslessRestart7()
    {
        // ISO 14495-1: official test image 1 but with restart markers.
        DecodeEncodeFile("test-images/test8_ilv_none_rm_7.jls", "conformance/test8.ppm", false);
    }

    [Fact]
    public void DecodeColor8BitInterleaveLineLosslessRestart7()
    {
        // ISO 14495-1: official test image 2 but with restart markers.
        DecodeEncodeFile("test-images/test8_ilv_line_rm_7.jls", "conformance/test8.ppm", false);
    }

    [Fact]
    public void DecodeColor8BitInterleaveSampleLosslessRestart7()
    {
        // ISO 14495-1: official test image 3 but with restart markers.
        DecodeEncodeFile("test-images/test8_ilv_sample_rm_7.jls", "conformance/test8.ppm", false);
    }

    [Fact]
    public void DecodeColor8BitInterleaveSampleLosslessRestart300()
    {
        // ISO 14495-1: official test image 3 but with restart markers and restart interval 300
        DecodeEncodeFile("test-images/test8_ilv_sample_rm_300.jls", "conformance/test8.ppm", false);
    }

    [Fact]
    public void DecodeMonochrome16BitRestart5()
    {
        // ISO 14495-1: official test image 12 but with restart markers and restart interval 5
        DecodeEncodeFile("test-images/test16_rm_5.jls", "conformance/test16.pgm", false);
    }

    [Fact]
    public void DecodeMappingTableSampleAnnexH4Dot5()
    {
        // ISO 14495-1: Sample image from appendix H.4.5 "Example of a palletised image" / Figure H.10
        byte[] palletisedData =
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

            0xFF,  0xF8,        // LSE - JPEG-LS preset parameters marker
            0x00,  0x11,        // Length of marker segment = 17 bytes including the length field
            0x02,               // ID = 2, mapping table
            0x05,               // TID = 5 Table identifier (arbitrary)
            0x03,               // Wt = 3 Width of table entry
            0xFF,  0xFF,  0xFF, // Entry for index 0
            0xFF,  0x00,  0x00, // Entry for index 1
            0x00,  0xFF,  0x00, // Entry for index 2
            0x00,  0x00,  0xFF, // Entry for index 3

            0xFF, 0xDA, // Start of scan (SOS) marker
            0x00, 0x08, // Length of marker segment = 8 bytes including the length field
            0x01, // Ns = Number of components for this scan = 1
            0x01, // C1 = Component ID = 1
            0x05, // Tm 1  = Mapping table identifier = 5
            0x00, // NEAR = 0 (near-lossless max error)
            0x00, // ILV = 0 (interleave mode = non-interleaved)
            0x00, // Al = 0, Ah = 0 (no point transform)
            0xDB, 0x95, 0xF0, // 3 bytes of compressed image data
            0xFF, 0xD9 // End of image (EOI) marker
        ];

        JpegLSDecoder decoder = new() { Source = palletisedData };
        decoder.ReadHeader();

        var destination = new byte[decoder.GetDestinationSize()];
        decoder.Decode(destination);

        byte[] expected = [0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 3];
        Util.CompareBuffers(destination, expected);

        int mappingTableId = decoder.GetMappingTableId(0);
        Assert.Equal(5, mappingTableId);

        int mappingTableIndex = decoder.FindMappingTableIndex(mappingTableId);
        Assert.NotEqual(-1, mappingTableId);

        var mappingTableInfo = decoder.GetMappingTableInfo(mappingTableIndex);
        Assert.Equal(5, mappingTableInfo.TableId);

        ReadOnlyMemory<byte> mappingTableData = decoder.GetMappingTableData(mappingTableIndex);
        byte[] expectedMappingTable = [0xFF, 0xFF, 0xFF, 0xFF, 0, 0, 0, 0xFF, 0, 0, 0, 0xFF];
        Assert.True(mappingTableData.Span.SequenceEqual(expectedMappingTable));
    }

    private static void DecodeEncodeFile(string encodedFilename, string rawFilename, bool checkEncode = true)
    {
        var encodedSource = Util.ReadFile(encodedFilename);

        JpegLSDecoder decoder = new(encodedSource);

        var referenceFile = Util.ReadAnymapReferenceFile(rawFilename, decoder.GetInterleaveMode(), decoder.FrameInfo);

        Util.TestCompliance(encodedSource, referenceFile.ImageData, checkEncode);
    }
}
