// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using Xunit;

namespace CharLS.JpegLS.Test;

public class JpegStreamWriterTest
{
    [Fact]
    public void RemainingDestinationWillBeZeroAfterCreateWithDefault()
    {
        JpegStreamWriter writer = new JpegStreamWriter();

        Assert.Equal(0, writer.GetRemainingDestination().Length);
    }

    [Fact]
    public void WriteStartOfImage()
    {
        var buffer = new byte[2];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        writer.WriteStartOfImage();

        Assert.Equal(2, writer.BytesWritten);
        Assert.Equal(0xFF, buffer[0]);
        Assert.Equal((byte)JpegMarkerCode.StartOfImage, buffer[1]);
    }

    [Fact]
    public void WriteStartOfImageInTooSmallBufferThrows()
    {
        var buffer = new byte[1];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        var exception = Assert.Throws<ArgumentOutOfRangeException>(writer.WriteStartOfImage);
        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.DestinationBufferTooSmall, exception.Data[nameof(ErrorCode)]);
    }

    [Fact]
    public void WriteEndOfImage()
    {
        var buffer = new byte[2];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        writer.WriteEndOfImage(false);

        Assert.Equal(2, writer.BytesWritten);
        Assert.Equal((byte)0xFF, buffer[0]);
        Assert.Equal((byte)JpegMarkerCode.EndOfImage, buffer[1]);
    }

    [Fact]
    public void WriteEndOfImageEvenNoExtraByteNeeded()
    {
        var buffer = new byte[2];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        writer.WriteEndOfImage(true);

        Assert.Equal(2, writer.BytesWritten);
        Assert.Equal((byte)0xFF, buffer[0]);
        Assert.Equal((byte)JpegMarkerCode.EndOfImage, buffer[1]);
    }

    [Fact]
    public void WriteEndOfImageEvenExtraByteNeeded()
    {
        var buffer = new byte[5 + 3];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        // writer.
        byte[] comment = [99];
        writer.WriteCommentSegment(comment);
        writer.WriteEndOfImage(true);

        Assert.Equal(8, writer.BytesWritten);
        Assert.Equal(0xFF, buffer[0]);
        Assert.Equal((byte)JpegMarkerCode.Comment, buffer[1]);
        Assert.Equal(0, buffer[2]);
        Assert.Equal(3, buffer[3]);
        Assert.Equal(99, buffer[4]);
        Assert.Equal(0xFF, buffer[5]);
        Assert.Equal(0xFF, buffer[6]);
        Assert.Equal((byte)JpegMarkerCode.EndOfImage, buffer[7]);
    }

    [Fact]
    public void WriteEndOfImageEvenExtraByteNeededNotEnabled()
    {
        var buffer = new byte[5 + 2];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        // writer.
        byte[] comment = [99];
        writer.WriteCommentSegment(comment);
        writer.WriteEndOfImage(false);

        Assert.Equal(7, writer.BytesWritten);
        Assert.Equal(0xFF, buffer[0]);
        Assert.Equal((byte)JpegMarkerCode.Comment, buffer[1]);
        Assert.Equal(0, buffer[2]);
        Assert.Equal(3, buffer[3]);
        Assert.Equal(99, buffer[4]);
        Assert.Equal(0xFF, buffer[5]);
        Assert.Equal((byte)JpegMarkerCode.EndOfImage, buffer[6]);
    }

    [Fact]
    public void WriteEndOfImageInTooSmallBufferThrows()
    {
        var buffer = new byte[1];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteEndOfImage(false));
        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.DestinationBufferTooSmall, exception.Data[nameof(ErrorCode)]);
    }

    [Fact]
    public void WriteSpiffSegment()
    {
        var buffer = new byte[34];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        var header = new SpiffHeader
        {
            ProfileId = SpiffProfileId.None,
            ComponentCount = 3,
            Height = 800,
            Width = 600,
            ColorSpace = SpiffColorSpace.Rgb,
            BitsPerSample = 8,
            CompressionType = SpiffCompressionType.JpegLS,
            ResolutionUnit = SpiffResolutionUnit.DotsPerInch,
            VerticalResolution = 96,
            HorizontalResolution = 1024
        };

        writer.WriteSpiffHeaderSegment(header);

        Assert.Equal(34, writer.BytesWritten);

        Assert.Equal(0xFF, buffer[0]);
        Assert.Equal((byte)JpegMarkerCode.ApplicationData8, buffer[1]);

        Assert.Equal(0, buffer[2]);
        Assert.Equal(32, buffer[3]);

        // Verify SPIFF identifier string.
        Assert.Equal((byte)'S', buffer[4]);
        Assert.Equal((byte)'P', buffer[5]);
        Assert.Equal((byte)'I', buffer[6]);
        Assert.Equal((byte)'F', buffer[7]);
        Assert.Equal((byte)'F', buffer[8]);
        Assert.Equal(0, buffer[9]);

        // Verify version
        Assert.Equal(2, buffer[10]);
        Assert.Equal(0, buffer[11]);

        Assert.Equal((byte)header.ProfileId, buffer[12]);
        Assert.Equal((byte)header.ComponentCount, buffer[13]);

        // Height
        Assert.Equal(0, buffer[14]);
        Assert.Equal(0, buffer[15]);
        Assert.Equal(0x3, buffer[16]);
        Assert.Equal(0x20, buffer[17]);

        // Width
        Assert.Equal(0, buffer[18]);
        Assert.Equal(0, buffer[19]);
        Assert.Equal(0x2, buffer[20]);
        Assert.Equal(0x58, buffer[21]);

        Assert.Equal((byte)header.ColorSpace, buffer[22]);
        Assert.Equal((byte)header.BitsPerSample, buffer[23]);
        Assert.Equal((byte)header.CompressionType, buffer[24]);
        Assert.Equal((byte)header.ResolutionUnit, buffer[25]);

        // vertical_resolution
        Assert.Equal(0, buffer[26]);
        Assert.Equal(0, buffer[27]);
        Assert.Equal(0, buffer[28]);
        Assert.Equal(96, buffer[29]);

        // header.horizontal_resolution = 1024
        Assert.Equal(0, buffer[30]);
        Assert.Equal(0, buffer[31]);
        Assert.Equal(4, buffer[32]);
        Assert.Equal(0, buffer[33]);
    }

    [Fact]
    public void WriteSpiffSegmentInTooSmallBufferThrows()
    {
        var buffer = new byte[33];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        var header = new SpiffHeader
        {
            ProfileId = SpiffProfileId.None,
            ComponentCount = 3,
            Height = 800,
            Width = 600,
            ColorSpace = SpiffColorSpace.Rgb,
            BitsPerSample = 8,
            CompressionType = SpiffCompressionType.JpegLS,
            ResolutionUnit = SpiffResolutionUnit.DotsPerInch,
            VerticalResolution = 96,
            HorizontalResolution = 1024
        };

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteSpiffHeaderSegment(header));
        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.DestinationBufferTooSmall, exception.Data[nameof(ErrorCode)]);
    }

    [Fact]
    public void WriteSpiffEndOfDirectorySegment()
    {
        var buffer = new byte[10];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        writer.WriteSpiffEndOfDirectoryEntry();

        Assert.Equal(10, writer.BytesWritten);

        // Verify Entry Magic Number (EMN)
        Assert.Equal(0xFF, buffer[0]);
        Assert.Equal((byte)JpegMarkerCode.ApplicationData8, buffer[1]);

        // Verify EOD Entry Length (EOD = End Of Directory)
        Assert.Equal(0, buffer[2]);
        Assert.Equal(8, buffer[3]);

        // Verify EOD Tag
        Assert.Equal(0, buffer[4]);
        Assert.Equal(0, buffer[5]);
        Assert.Equal(0, buffer[6]);
        Assert.Equal(1, buffer[7]);

        // Verify embedded SOI tag
        Assert.Equal(0xFF, buffer[8]);
        Assert.Equal((byte)JpegMarkerCode.StartOfImage, buffer[9]);
    }

    [Fact]
    public void WriteSpiffDirectoryEntry()
    {
        var buffer = new byte[10];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        Span<byte> data = [0x77, 0x66];

        writer.WriteSpiffDirectoryEntry(2, data);

        // Verify Entry Magic Number (EMN)
        Assert.Equal(0xFF, buffer[0]);
        Assert.Equal((byte)JpegMarkerCode.ApplicationData8, buffer[1]);

        // Verify Entry Length
        Assert.Equal(0, buffer[2]);
        Assert.Equal(8, buffer[3]);

        // Verify Entry Tag
        Assert.Equal(0, buffer[4]);
        Assert.Equal(0, buffer[5]);
        Assert.Equal(0, buffer[6]);
        Assert.Equal(2, buffer[7]);

        // Verify embedded data
        Assert.Equal(data[0], buffer[8]);
        Assert.Equal(data[1], buffer[9]);
    }

    [Fact]
    public void TestWriteStartOfFrameSegment()
    {
        const int bitsPerSample = 8;
        const int componentCount = 3;

        var buffer = new byte[19];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        bool oversizedImage =
            writer.WriteStartOfFrameSegment(new FrameInfo(100, ushort.MaxValue, bitsPerSample, componentCount));

        Assert.False(oversizedImage);
        Assert.Equal(19, writer.BytesWritten);

        Assert.Equal(0xFF, buffer[0]);
        Assert.Equal(0xF7, buffer[1]); // JPEG_SOF_55
        Assert.Equal(0, buffer[2]);     // 6 + (3 * 3) + 2 (in big endian)
        Assert.Equal(17, buffer[3]);   // 6 + (3 * 3) + 2 (in big endian)
        Assert.Equal(bitsPerSample, buffer[4]);
        Assert.Equal(255, buffer[5]); // height (in big endian)
        Assert.Equal(255, buffer[6]); // height (in big endian)
        Assert.Equal(0, buffer[7]);    // width (in big endian)
        Assert.Equal(100, buffer[8]); // width (in big endian)
        Assert.Equal(componentCount, buffer[9]);

        Assert.Equal(1, buffer[10]);
        Assert.Equal(0x11, buffer[11]);
        Assert.Equal(0, buffer[12]);

        Assert.Equal(2, buffer[13]);
        Assert.Equal(0x11, buffer[14]);
        Assert.Equal(0, buffer[15]);

        Assert.Equal(3, buffer[16]);
        Assert.Equal(0x11, buffer[17]);
        Assert.Equal(0, buffer[18]);
    }

    [Fact]
    public void WriteStartOfFrameSegmentLargeImageWidth()
    {
        const int bitsPerSample = 8;
        const int componentCount = 3;

        var buffer = new byte[19];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        bool oversizedImage =
            writer.WriteStartOfFrameSegment(new FrameInfo(ushort.MaxValue + 1, 100 , bitsPerSample, componentCount));

        Assert.True(oversizedImage);
        Assert.Equal(19, writer.BytesWritten);

        Assert.Equal(0xFF, buffer[0]);
        Assert.Equal(0xF7, buffer[1]); // JPEG_SOF_55
        Assert.Equal(0, buffer[2]);     // 6 + (3 * 3) + 2 (in big endian)
        Assert.Equal(17, buffer[3]);   // 6 + (3 * 3) + 2 (in big endian)
        Assert.Equal(bitsPerSample, buffer[4]);
        Assert.Equal(0, buffer[5]); // height (in big endian)
        Assert.Equal(0, buffer[6]); // height (in big endian)
        Assert.Equal(0, buffer[7]);    // width (in big endian)
        Assert.Equal(0, buffer[8]); // width (in big endian)
        Assert.Equal(componentCount, buffer[9]);

        Assert.Equal(1, buffer[10]);
        Assert.Equal(0x11, buffer[11]);
        Assert.Equal(0, buffer[12]);

        Assert.Equal(2, buffer[13]);
        Assert.Equal(0x11, buffer[14]);
        Assert.Equal(0, buffer[15]);

        Assert.Equal(3, buffer[16]);
        Assert.Equal(0x11, buffer[17]);
        Assert.Equal(0, buffer[18]);
    }

    [Fact]
    public void WriteStartOfFrameSegmentLargeImageHeight()
    {
        const int bitsPerSample = 8;
        const int componentCount = 3;

        var buffer = new byte[19];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        bool oversizedImage =
            writer.WriteStartOfFrameSegment(new FrameInfo(100, ushort.MaxValue + 1, bitsPerSample, componentCount));

        Assert.True(oversizedImage);
        Assert.Equal(19, writer.BytesWritten);

        Assert.Equal(0xFF, buffer[0]);
        Assert.Equal(0xF7, buffer[1]); // JPEG_SOF_55
        Assert.Equal(0, buffer[2]);     // 6 + (3 * 3) + 2 (in big endian)
        Assert.Equal(17, buffer[3]);   // 6 + (3 * 3) + 2 (in big endian)
        Assert.Equal(bitsPerSample, buffer[4]);
        Assert.Equal(0, buffer[5]); // height (in big endian)
        Assert.Equal(0, buffer[6]); // height (in big endian)
        Assert.Equal(0, buffer[7]);    // width (in big endian)
        Assert.Equal(0, buffer[8]); // width (in big endian)
        Assert.Equal(componentCount, buffer[9]);

        Assert.Equal(1, buffer[10]);
        Assert.Equal(0x11, buffer[11]);
        Assert.Equal(0, buffer[12]);

        Assert.Equal(2, buffer[13]);
        Assert.Equal(0x11, buffer[14]);
        Assert.Equal(0, buffer[15]);

        Assert.Equal(3, buffer[16]);
        Assert.Equal(0x11, buffer[17]);
        Assert.Equal(0, buffer[18]);
    }

    [Fact]
    public void WriteStartOfFrameMarkerSegmentWithLowBoundaryValues()
    {
        const int bitsPerSample = 2;
        const int componentCount = 1;
        var buffer = new byte[13];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        writer.WriteStartOfFrameSegment(new FrameInfo(1, 1, bitsPerSample, componentCount));

        Assert.Equal(buffer.Length, writer.BytesWritten);
        Assert.Equal(bitsPerSample, buffer[4]);
        Assert.Equal(componentCount, buffer[9]);
    }

    [Fact]
    public void WriteStartOfFrameMarkerSegmentWithHighBoundaryValuesAndSerialize()
    {
        var buffer = new byte[775];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        writer.WriteStartOfFrameSegment(new FrameInfo(ushort.MaxValue, ushort.MaxValue, 16, byte.MaxValue));

        Assert.Equal(buffer.Length, writer.BytesWritten);
        Assert.Equal(16, buffer[4]);
        Assert.Equal(byte.MaxValue, buffer[9]);
        Assert.Equal(byte.MaxValue, buffer[^3]); // Last component index.
    }

    [Fact]
    public void WriteColorTransformSegment()
    {
        const ColorTransformation transformation = ColorTransformation.HP1;
        var buffer = new byte[9];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        writer.WriteColorTransformSegment(transformation);
        Assert.Equal(buffer.Length, writer.BytesWritten);

        // Verify mrfx identifier string.
        Assert.Equal((byte)'m', buffer[4]);
        Assert.Equal((byte)'r', buffer[5]);
        Assert.Equal((byte)'f', buffer[6]);
        Assert.Equal((byte)'x', buffer[7]);

        Assert.Equal((byte)transformation, buffer[8]);
    }

    [Fact]
    public void WriteJpegLSExtendedParametersMarkerAndSerialize()
    {
        var parameters = new JpegLSPresetCodingParameters(2, 1, 2, 3, 7);
        var buffer = new byte[15];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        writer.WriteJpegLSPresetParametersSegment(parameters);

        Assert.Equal(buffer.Length, writer.BytesWritten);

        // Parameter ID.
        Assert.Equal(0x1, buffer[4]);

        // MaximumSampleValue
        Assert.Equal(0, buffer[5]);
        Assert.Equal(2, buffer[6]);

        // Threshold1
        Assert.Equal(0, buffer[7]);
        Assert.Equal(1, buffer[8]);

        // Threshold2
        Assert.Equal(0, buffer[9]);
        Assert.Equal(2, buffer[10]);

        // Threshold3
        Assert.Equal(0, buffer[11]);
        Assert.Equal(3, buffer[12]);

        // ResetValue
        Assert.Equal(0, buffer[13]);
        Assert.Equal(7, buffer[14]);
    }

    [Fact]
    public void WriteJpegLSPresetParametersSegmentForOversizedImageDimensions()
    {
        var buffer = new byte[14];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        writer.WriteJpegLSPresetParametersSegment(100, int.MaxValue);
        Assert.Equal(buffer.Length, writer.BytesWritten);

        // Parameter ID.
        Assert.Equal(0x4, buffer[4]);

        // Wxy
        Assert.Equal(4, buffer[5]);

        // Height (in big endian)
        Assert.Equal(0, buffer[6]);
        Assert.Equal(0, buffer[7]);
        Assert.Equal(0, buffer[8]);
        Assert.Equal(100, buffer[9]);

        // Width (in big endian)
        Assert.Equal(127, buffer[10]);
        Assert.Equal(255, buffer[11]);
        Assert.Equal(255, buffer[12]);
        Assert.Equal(255, buffer[13]);
    }

    [Fact]
    public void WriteStartOfScanSegment()
    {
        var buffer = new byte[10];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        writer.WriteStartOfScanSegment(1, 2, InterleaveMode.None);

        Assert.Equal(buffer.Length, writer.BytesWritten);
        Assert.Equal(1, buffer[4]); // component count.
        Assert.Equal(1, buffer[5]); // component index.
        Assert.Equal(0, buffer[6]); // table ID.
        Assert.Equal(2, buffer[7]); // NEAR parameter.
        Assert.Equal(0, buffer[8]); // ILV parameter.
        Assert.Equal(0, buffer[9]); // transformation.
    }

    [Fact]
    public void WriteStartOfScanSegmentWithTableId()
    {
        var buffer = new byte[10];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };
        writer.SetTableId(0, 77);

        writer.WriteStartOfScanSegment(1, 2, InterleaveMode.None);

        Assert.Equal(buffer.Length, writer.BytesWritten);
        Assert.Equal(1, buffer[4]);  // component count.
        Assert.Equal(1, buffer[5]);  // component index.
        Assert.Equal(77, buffer[6]); // table ID.
        Assert.Equal(2, buffer[7]);  // NEAR parameter.
        Assert.Equal(0, buffer[8]);  // ILV parameter.
        Assert.Equal(0, buffer[9]);  // transformation.
    }

    [Fact]
    public void WriteStartOfScanSegmentWithTableIdAfterRewind()
    {
        var buffer = new byte[10];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };
        writer.SetTableId(0, 77);
        writer.Rewind();

        writer.WriteStartOfScanSegment(1, 2, InterleaveMode.None);

        Assert.Equal(buffer.Length, writer.BytesWritten);
        Assert.Equal(1, buffer[4]);  // component count.
        Assert.Equal(1, buffer[5]);  // component index.
        Assert.Equal(77, buffer[6]); // table ID.
        Assert.Equal(2, buffer[7]);  // NEAR parameter.
        Assert.Equal(0, buffer[8]);  // ILV parameter.
        Assert.Equal(0, buffer[9]);  // transformation.
    }

    [Fact]
    public void AdvancePosition()
    {
        var buffer = new byte[2];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        writer.AdvancePosition(2);
        Assert.Equal(2, writer.BytesWritten);
    }

    [Fact]
    public void Rewind()
    {
        var buffer = new byte[10];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };
        writer.WriteStartOfScanSegment(1, 2, InterleaveMode.None);

        writer.Rewind();

        buffer[4] = 0;
        writer.WriteStartOfScanSegment(1, 2, InterleaveMode.None);

        Assert.Equal(buffer.Length, writer.BytesWritten);
        Assert.Equal(1, buffer[4]); // component count.
    }

    [Fact]
    public void WriteMinimalTable()
    {
        var buffer = new byte[8];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        byte[] tableData = [77];
        writer.WriteJpegLSPresetParametersSegment(100, 1, tableData);

        Assert.Equal(buffer.Length, writer.BytesWritten);
        Assert.Equal(0xFF, buffer[0]);
        Assert.Equal(0xF8, buffer[1]); // LSE
        Assert.Equal(0, buffer[2]);
        Assert.Equal(6, buffer[3]);
        Assert.Equal(2, buffer[4]);   // type = table
        Assert.Equal(100, buffer[5]); // table ID
        Assert.Equal(1, buffer[6]);   // size of entry
        Assert.Equal(77, buffer[7]);  // table content
    }

    [Fact]
    public void WriteTableMaxEntrySize()
    {
        var buffer = new byte[7 + 255];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        var tableData = new byte[255];
        writer.WriteJpegLSPresetParametersSegment(255, 255, tableData);

        Assert.Equal(buffer.Length, writer.BytesWritten);
        Assert.Equal(0xFF, buffer[0]);
        Assert.Equal(0xF8, buffer[1]); // LSE
        Assert.Equal(1, buffer[2]);
        Assert.Equal(4, buffer[3]);
        Assert.Equal(2, buffer[4]);   // type = table
        Assert.Equal(255, buffer[5]); // table ID
        Assert.Equal(255, buffer[6]); // size of entry
        Assert.Equal(0, buffer[7]);   // table content
    }

    [Fact]
    public void WriteTableFitsInSingleSegment()
    {
        var buffer = new byte[2 + ushort.MaxValue];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        var tableData = new byte[ushort.MaxValue - 5];
        writer.WriteJpegLSPresetParametersSegment(255, 1, tableData);

        Assert.Equal(buffer.Length, writer.BytesWritten);
        Assert.Equal(0xFF, buffer[0]);
        Assert.Equal(0xF8, buffer[1]); // LSE
        Assert.Equal(255, buffer[2]);
        Assert.Equal(255, buffer[3]);
        Assert.Equal(2, buffer[4]);   // type = table
        Assert.Equal(255, buffer[5]); // table ID
        Assert.Equal(1, buffer[6]);   // size of entry
        Assert.Equal(0, buffer[7]);   // table content (first entry)
    }

    [Fact]
    public void WriteTableThatRequiresTwoSegment()
    {
        var buffer = new byte[2 + ushort.MaxValue + 8];
        JpegStreamWriter writer = new JpegStreamWriter { Destination = buffer };

        var tableData = new byte[ushort.MaxValue - 5 + 1];
        writer.WriteJpegLSPresetParametersSegment(255, 1, tableData);

        Assert.Equal(buffer.Length, writer.BytesWritten);
        Assert.Equal(0xFF, buffer[0]);
        Assert.Equal(0xF8, buffer[1]); // LSE
        Assert.Equal(255, buffer[2]);
        Assert.Equal(255, buffer[3]);
        Assert.Equal(2, buffer[4]);   // type = table
        Assert.Equal(255, buffer[5]); // table ID
        Assert.Equal(1, buffer[6]);   // size of entry
        Assert.Equal(0, buffer[7]);   // table content (first entry)

        // Validate second segment.
        Assert.Equal(0xFF, buffer[65537]);
        Assert.Equal(0xF8, buffer[65538]); // LSE
        Assert.Equal(0, buffer[65539]);
        Assert.Equal(6, buffer[65540]);
        Assert.Equal(3, buffer[65541]);   // type = table
        Assert.Equal(255, buffer[65542]); // table ID
        Assert.Equal(1, buffer[65543]);   // size of entry
        Assert.Equal(0, buffer[65544]);   // table content (last entry)
    }
}
