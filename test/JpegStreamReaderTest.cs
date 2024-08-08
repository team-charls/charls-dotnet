// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using Xunit;

namespace CharLS.JpegLS.Test;

public class JpegStreamReaderTest
{
    [Fact]
    public void ReadHeaderFromToSmallInputBuffer()
    {
        var buffer = Array.Empty<byte>();

        var reader = new JpegStreamReader { Source = buffer };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);
        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.SourceBufferTooSmall, exception.Data[nameof(ErrorCode)]);
    }

    [Fact]
    public void ReadHeaderFromBufferPrecededWithFillBytes()
    {
        const byte extraStartByte = 0xFF;
        JpegTestStreamWriter writer = new();

        writer.WriteByte(extraStartByte);
        writer.WriteStartOfImage();

        writer.WriteByte(extraStartByte);
        writer.WriteStartOfFrameSegment(1, 1, 2, 1);

        writer.WriteByte(extraStartByte);
        writer.WriteStartOfScanSegment(0, 1, 1, InterleaveMode.None);

        var reader = new JpegStreamReader { Source = writer.GetBuffer() };

        reader.ReadHeader();  // if it doesn't throw test is passed.
    }

    [Fact]
    public void ReadHeaderFromBufferNotStartingWithStartByteShouldThrow()
    {
        byte[] buffer = [0x0F, 0xFF, 0xD8, 0xFF, 0xFF, 0xDA];

        var reader = new JpegStreamReader { Source = buffer };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);
        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.JpegMarkerStartByteNotFound, exception.Data[nameof(ErrorCode)]);
    }

    [Fact]
    public void ReadHeaderWithApplicationData()
    {
        ReadHeaderWithApplicationDataImpl(0);
        ReadHeaderWithApplicationDataImpl(1);
        ReadHeaderWithApplicationDataImpl(2);
        ReadHeaderWithApplicationDataImpl(3);
        ReadHeaderWithApplicationDataImpl(4);
        ReadHeaderWithApplicationDataImpl(5);
        ReadHeaderWithApplicationDataImpl(6);
        ReadHeaderWithApplicationDataImpl(7);
        ReadHeaderWithApplicationDataImpl(8);
        ReadHeaderWithApplicationDataImpl(9);
        ReadHeaderWithApplicationDataImpl(10);
        ReadHeaderWithApplicationDataImpl(11);
        ReadHeaderWithApplicationDataImpl(12);
        ReadHeaderWithApplicationDataImpl(13);
        ReadHeaderWithApplicationDataImpl(14);
        ReadHeaderWithApplicationDataImpl(15);
    }

    [Fact]
    public void ReadHeaderWithJpegLSExtendedFrameShouldThrow()
    {
        // 0xF9 = SOF_57: Marks the start of a JPEG-LS extended (ISO/IEC 14495-2) encoded frame.
        byte[] buffer = [0xFF, 0xD8, 0xFF, 0xF9];

        var reader = new JpegStreamReader { Source = buffer };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);
        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.EncodingNotSupported, exception.Data[nameof(ErrorCode)]);
    }

    [Fact]
    public void ReadHeaderJpegLSPresetParameterSegment()
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();

        JpegLSPresetCodingParameters presets = new(1, 2, 3, 4, 5);
        writer.WriteJpegLSPresetParametersSegment(presets);
        writer.WriteStartOfFrameSegment(1, 1, 2, 1);
        writer.WriteStartOfScanSegment(1, 1, 0, InterleaveMode.None);

        var reader = new JpegStreamReader { Source = writer.GetBuffer() };

        reader.ReadHeader();

        JpegLSPresetCodingParameters? actual = reader.JpegLSPresetCodingParameters;
        Assert.NotNull(actual);

        Assert.Equal(presets.MaximumSampleValue, actual.MaximumSampleValue);
        Assert.Equal(presets.ResetValue, actual.ResetValue);
        Assert.Equal(presets.Threshold1, actual.Threshold1);
        Assert.Equal(presets.Threshold2, actual.Threshold2);
        Assert.Equal(presets.Threshold3, actual.Threshold3);
    }

    [Fact]
    public void ReadHeaderWithTooSmallJpegLSPresetParameterSegmentShouldThrow()
    {
        byte[] buffer =
        [
            0xFF, 0xD8, 0xFF,
            0xF8, // LSE: Marks the start of a JPEG-LS preset parameters segment.
            0x00, 0x02, 0x01
        ];

        var reader = new JpegStreamReader { Source = buffer };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);
        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidMarkerSegmentSize, exception.Data[nameof(ErrorCode)]);
    }

    [Fact]
    public void ReadHeaderWithTooSmallJpegLSPresetParameterSegmentWithCodingParametersShouldThrow()
    {
        var buffer = new byte[]{
            0xFF, 0xD8, 0xFF,
            0xF8, // LSE: Marks the start of a JPEG-LS preset parameters segment.
            0x00, 0x0A, 0x01};

        var reader = new JpegStreamReader { Source = buffer };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);
        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidMarkerSegmentSize, exception.Data[nameof(ErrorCode)]);
    }

    [Fact]
    public void ReadHeaderWithTooLargeJpegLSPresetParameterSegmentWithCodingParametersShouldThrow()
    {
        var buffer = new byte[]{
            0xFF, 0xD8, 0xFF,
            0xF8, // LSE: Marks the start of a JPEG-LS preset parameters segment.
            0x00, 0x0C, 0x01};

        var reader = new JpegStreamReader { Source = buffer };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);
        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidMarkerSegmentSize, exception.Data[nameof(ErrorCode)]);
    }

    [Fact]
    public void ReadHeaderWithJpegLSPresetParameterWithExtendedIdShouldThrow()
    {
        var ids = new byte[] { 0x5, 0x6, 0x7, 0x8, 0x9, 0xA, 0xC, 0xD };

        foreach (var id in ids)
        {
            ReadHeaderWithJpegLSPresetParameterWithExtendedIdShouldThrowImpl(id);
        }
    }

    [Fact]
    public void ReadHeaderWithTooSmallSegmentSizeShouldThrow()
    {
        var buffer = new byte[]{
            0xFF, 0xD8, 0xFF,
            0xF7,                    // SOF_55: Marks the start of JPEG-LS extended scan.
            0x00, 0x01, 0xFF, 0xDA}; // SOS: Marks the start of scan.

        var reader = new JpegStreamReader { Source = buffer };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);
        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidMarkerSegmentSize, exception.Data[nameof(ErrorCode)]);
    }

    [Fact]
    public void ReadHeaderWithTooSmallStartOfFrameShouldThrow()
    {
        byte[] buffer =
        [
            0xFF, 0xD8, 0xFF,
            0xF7, // SOF_55: Marks the start of JPEG-LS extended scan.
            0x00, 0x07
        ];

        var reader = new JpegStreamReader { Source = buffer };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);
        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidMarkerSegmentSize, exception.Data[nameof(ErrorCode)]);
    }

    [Fact]
    public void ReadHeaderWithTooSmallStartOfFrameInComponentInfoShouldThrow()
    {
        byte[] buffer =
        [
            0xFF, 0xD8, 0xFF,
            0xF7, // SOF_55: Marks the start of JPEG-LS extended scan.
            0x00, 0x07
        ];

        var reader = new JpegStreamReader { Source = buffer };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);
        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidMarkerSegmentSize, exception.Data[nameof(ErrorCode)]);
    }

    [Fact]
    public void ReadHeaderWithTooLargeStartOfFrameShouldThrow()
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteStartOfFrameSegment(512, 512, 8, 3);
        writer.WriteByte(0);
        var buffer = writer.GetModifiableBuffer();
        buffer[5]++;
        var reader = new JpegStreamReader { Source = buffer.ToArray() };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidMarkerSegmentSize, exception.Data[nameof(ErrorCode)]);
    }

    [Fact]
    public void ReadHeaderSosBeforeSofShouldThrow()
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteStartOfScanSegment(0, 1, 128, InterleaveMode.None);
        var reader = new JpegStreamReader { Source = writer.GetBuffer() };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.UnexpectedMarkerFound, exception.Data[nameof(ErrorCode)]);
    }

    [Fact]
    public void ReadHeaderExtraSofShouldThrow()
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteStartOfFrameSegment(512, 512, 8, 3);
        writer.WriteStartOfFrameSegment(512, 512, 8, 3);
        var reader = new JpegStreamReader { Source = writer.GetBuffer() };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.DuplicateStartOfFrameMarker, exception.Data[nameof(ErrorCode)]);
    }

    [Fact]
    public void ReadHeaderTooLargeNearLosslessInSosShouldThrow()
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteStartOfFrameSegment(512, 512, 8, 3);
        writer.WriteStartOfScanSegment(0, 1, 128, InterleaveMode.None);
        var reader = new JpegStreamReader { Source = writer.GetBuffer() };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidParameterNearLossless, exception.Data[nameof(ErrorCode)]);
    }

    [Fact]
    public void ReadHeaderTooLargeNearLosslessInSosShouldThrow2()
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteStartOfFrameSegment(512, 512, 8, 3);
        writer.WriteJpegLSPresetParametersSegment(new JpegLSPresetCodingParameters(200, 0, 0, 0, 0));

        const int badNearLossless = (200 / 2) + 1;
        writer.WriteStartOfScanSegment(0, 1, badNearLossless, InterleaveMode.None);
        var reader = new JpegStreamReader { Source = writer.GetBuffer() };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidParameterNearLossless, exception.Data[nameof(ErrorCode)]);
    }

    [Fact]
    public void ReadHeaderLineInterleaveInSosForSingleComponentThrows()
    {
        ReadHeaderIncorrectInterleaveInSosForSingleComponentThrows(InterleaveMode.Line);
    }

    [Fact]
    public void ReadHeaderSampleInterleaveInSosForSingleComponentThrows()
    {
        ReadHeaderIncorrectInterleaveInSosForSingleComponentThrows(InterleaveMode.Sample);
    }

    [Fact]
    public void ReadHeaderWithDuplicateComponentIdInStartOfFrameSegmentShouldThrow()
    {
        JpegTestStreamWriter writer = new() { ComponentIdOverride = 7 };
        writer.WriteStartOfImage();
        writer.WriteStartOfFrameSegment(512, 512, 8, 3);
        var reader = new JpegStreamReader { Source = writer.GetBuffer() };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.DuplicateComponentIdInStartOfFrameSegment, exception.Data[nameof(ErrorCode)]);
    }

    [Fact]
    public void ReadHeaderWithTooSmallStartOfScanShouldThrow()
    {
        byte[] buffer =
        [
            0xFF, 0xD8, 0xFF,
            0xF7, // SOF_55: Marks the start of JPEG-LS extended scan.
            0x00,
            0x08, // size
            0x08, // bits per sample
            0x00,
            0x01, // width
            0x00,
            0x01, // height
            0x01, // component count
            0xFF,
            0xDA, // SOS
            0x00, 0x03
        ];
        var reader = new JpegStreamReader { Source = buffer };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidMarkerSegmentSize, exception.Data[nameof(ErrorCode)]);
    }

    [Fact]
    public void ReadHeaderWithTooSmallStartOfScanComponentCountShouldThrow()
    {
        byte[] buffer =
        [
            0xFF, 0xD8, 0xFF,
            0xF7, // SOF_55: Marks the start of JPEG-LS extended scan.
            0x00,
            0x08, // size
            0x08, // bits per sample
            0x00,
            0x01, // width
            0x00,
            0x01, // height
            0x01, // component count
            0xFF,
            0xDA, // SOS
            0x00, 0x07, 0x01
        ];
        var reader = new JpegStreamReader { Source = buffer };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidMarkerSegmentSize, exception.Data[nameof(ErrorCode)]);
    }

    [Fact]
    public void ReadHeaderWithDirectlyEndOfImageShouldThrow()
    {
        byte[] buffer = [0xFF, 0xD8, 0xFF, 0xD9]; // 0xD9 = EOI
        var reader = new JpegStreamReader { Source = buffer };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.UnexpectedEndOfImageMarker, exception.Data[nameof(ErrorCode)]);
    }

    [Fact]
    public void ReadHeaderWithDuplicateStartOfImageShouldThrow()
    {
        byte[] buffer = [0xFF, 0xD8, 0xFF, 0xD8]; // 0xD8 = SOI.
        var reader = new JpegStreamReader { Source = buffer };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.DuplicateStartOfImageMarker, exception.Data[nameof(ErrorCode)]);
    }

    [Fact]
    public void ReadSpiffHeader()
    {
        ReadSpiffHeaderImpl(0);
    }

    [Fact]
    public void ReadSpiffHeaderLowVersionNewer()
    {
        ReadSpiffHeaderImpl(1);
    }

    [Fact]
    public void ReadSpiffHeaderHighVersionToNew()
    {
        var buffer = Util.CreateTestSpiffHeader(3);
        var reader = new JpegStreamReader { Source = buffer };

        reader.ReadHeader();

        Assert.Null(reader.SpiffHeader);
    }

    [Fact]
    public void ReadSpiffHeaderWithoutEndOfDirectory()
    {
        var buffer = Util.CreateTestSpiffHeader(2, 0, false);
        var reader = new JpegStreamReader { Source = buffer };

        reader.ReadHeader();
        Assert.NotNull(reader.SpiffHeader);

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.MissingEndOfSpiffDirectory, exception.Data[nameof(ErrorCode)]);
    }

    [Fact]
    public void ReadHeaderWithDefineRestartInterval16Bit()
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteStartOfFrameSegment(512, 512, 8, 3);
        const uint expectedRestartInterval = ushort.MaxValue - 5;
        writer.WriteDefineRestartInterval(expectedRestartInterval, 2);
        writer.WriteStartOfScanSegment(0, 1, 0, InterleaveMode.None);
        var reader = new JpegStreamReader { Source = writer.GetBuffer() };

        reader.ReadHeader();

        Assert.Equal(expectedRestartInterval, reader.RestartInterval);
    }

    [Fact]
    public void ReadHeaderWithDefineRestartInterval24Bit()
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteStartOfFrameSegment(512, 512, 8, 3);
        const uint expectedRestartInterval = ushort.MaxValue + 5;
        writer.WriteDefineRestartInterval(expectedRestartInterval, 3);
        writer.WriteStartOfScanSegment(0, 1, 0, InterleaveMode.None);
        var reader = new JpegStreamReader { Source = writer.GetBuffer() };

        reader.ReadHeader();

        Assert.Equal(expectedRestartInterval, reader.RestartInterval);
    }

    [Fact]
    public void ReadHeaderWithDefineRestartInterval32Bit()
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteStartOfFrameSegment(512, 512, 8, 3);
        const uint expectedRestartInterval = uint.MaxValue - 7;
        writer.WriteDefineRestartInterval(expectedRestartInterval, 4);
        writer.WriteStartOfScanSegment(0, 1, 0, InterleaveMode.None);
        var reader = new JpegStreamReader { Source = writer.GetBuffer() };

        reader.ReadHeader();

        Assert.Equal(expectedRestartInterval, reader.RestartInterval);
    }

    [Fact]
    public void ReadHeaderWith2DefineRestartIntervals()
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteDefineRestartInterval(uint.MaxValue, 4);
        writer.WriteStartOfFrameSegment(512, 512, 8, 3);
        writer.WriteDefineRestartInterval(0, 3);
        writer.WriteStartOfScanSegment(0, 1, 0, InterleaveMode.None);
        var reader = new JpegStreamReader { Source = writer.GetBuffer() };

        reader.ReadHeader();

        Assert.Equal(0U, reader.RestartInterval);
    }

    [Fact]
    public void ReadHeaderWithBadDefineRestartInterval()
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteStartOfFrameSegment(512, 512, 8, 3);
        writer.WriteSegmentStart(JpegMarkerCode.DefineRestartInterval, 1);
        writer.WriteByte(0);
        writer.WriteStartOfScanSegment(0, 1, 0, InterleaveMode.None);
        var reader = new JpegStreamReader { Source = writer.GetBuffer() };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidMarkerSegmentSize, exception.Data[nameof(ErrorCode)]);
    }

    [Fact]
    public void ReadJpegLSStreamWithRestartMarkerOutsideEntropyData()
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteRestartMarker(0);
        var reader = new JpegStreamReader { Source = writer.GetBuffer() };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.UnexpectedRestartMarker, exception.Data[nameof(ErrorCode)]);
    }

    [Fact]
    public void ReadComment()
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        byte[] comment = [(byte)'h', (byte)'e', (byte)'l', (byte)'l', (byte)'o'];
        writer.WriteSegment(JpegMarkerCode.Comment, comment);
        writer.WriteStartOfFrameSegment(512, 512, 8, 3);
        writer.WriteStartOfScanSegment(0, 1, 0, InterleaveMode.None);

        var reader = new JpegStreamReader { Source = writer.GetBuffer() };

        byte[]? receivedComment = null;
        reader.Comment += (_, e) =>
        {
            receivedComment = e.Data.ToArray();
        };

        reader.ReadHeader();

        Assert.NotNull(receivedComment);
        Assert.Equal(comment, receivedComment);
    }

    [Fact]
    public void ReadEmptyComment()
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        byte[] comment = [];
        writer.WriteSegment(JpegMarkerCode.Comment, comment);
        writer.WriteStartOfFrameSegment(512, 512, 8, 3);
        writer.WriteStartOfScanSegment(0, 1, 0, InterleaveMode.None);

        var reader = new JpegStreamReader { Source = writer.GetBuffer() };

        byte[]? receivedComment = null;
        reader.Comment += (_, e) =>
        {
            receivedComment = e.Data.ToArray();
        };

        reader.ReadHeader();

        Assert.NotNull(receivedComment);
        Assert.Equal(comment, receivedComment);
    }

    [Fact]
    public void ReadBadComment()
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        byte[] comment = [];
        writer.WriteSegment(JpegMarkerCode.Comment, comment);

        var reader = new JpegStreamReader { Source = writer.GetBuffer()[..(writer.GetBuffer().Length - 1)] };

        bool eventHandlerCalled = false;
        reader.Comment += (_, _) =>
        {
            eventHandlerCalled = true;
        };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.SourceBufferTooSmall, exception.Data[nameof(ErrorCode)]);
        Assert.False(eventHandlerCalled);
    }

    [Fact]
    public void ReadMappingTable()
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        var tableData = new byte[2];
        tableData[0] = 2;
        writer.WriteJpegLSPresetParametersSegment(1, 1, tableData);
        writer.WriteStartOfFrameSegment(1, 1, 2, 1);
        writer.WriteStartOfScanSegment(0, 1, 0, InterleaveMode.None);

        var reader = new JpegStreamReader { Source = writer.GetBuffer() };
        reader.ReadHeader();

        Assert.Equal(1, reader.MappingTableCount);
        Assert.Equal(0, reader.FindMappingTableIndex(1).GetValueOrDefault(-1));

        var info = reader.GetMappingTableInfo(0);
        Assert.Equal(1, info.TableId);
        Assert.Equal(1, info.EntrySize);

        var mappingTableData = reader.GetMappingTableData(0);
        Assert.Equal(2, mappingTableData.Span[0]);
    }

    private static void ReadSpiffHeaderImpl(byte lowVersion)
    {
        var buffer = Util.CreateTestSpiffHeader(2, lowVersion);
        var reader = new JpegStreamReader { Source = buffer };

        reader.ReadHeader();

        var spiffHeader = reader.SpiffHeader;
        Assert.NotNull(spiffHeader);
        Assert.Equal(SpiffProfileId.None, spiffHeader.ProfileId);
        Assert.Equal(3, spiffHeader.ComponentCount);
        Assert.Equal(800, spiffHeader.Height);
        Assert.Equal(600, spiffHeader.Width);
        Assert.Equal(SpiffColorSpace.Rgb, spiffHeader.ColorSpace);
        Assert.Equal(8, spiffHeader.BitsPerSample);
        Assert.Equal(SpiffCompressionType.JpegLS, spiffHeader.CompressionType);
        Assert.Equal(SpiffResolutionUnit.DotsPerInch, spiffHeader.ResolutionUnit);
        Assert.Equal(96, spiffHeader.VerticalResolution);
        Assert.Equal(1024, spiffHeader.HorizontalResolution);
    }

    private static void ReadHeaderWithApplicationDataImpl(byte dataNumber)
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();

        writer.WriteByte(0xFF);
        writer.WriteByte((byte)(0xE0 + dataNumber));
        writer.WriteByte(0x00);
        writer.WriteByte(0x02);

        writer.WriteStartOfFrameSegment(1, 1, 2, 1);
        writer.WriteStartOfScanSegment(0, 1, 1, InterleaveMode.None);

        var reader = new JpegStreamReader { Source = writer.GetBuffer() };

        reader.ReadHeader(); // if it doesn't throw test is passed.
    }

    private static void ReadHeaderIncorrectInterleaveInSosForSingleComponentThrows(InterleaveMode mode)
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteStartOfFrameSegment(512, 512, 8, 1);
        writer.WriteStartOfScanSegment(0, 1, 0, mode);
        var reader = new JpegStreamReader { Source = writer.GetBuffer() };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidParameterInterleaveMode, exception.Data[nameof(ErrorCode)]);
    }

    private static void ReadHeaderWithJpegLSPresetParameterWithExtendedIdShouldThrowImpl(int id)
    {
        var buffer = new byte[]
            {0xFF, 0xD8, 0xFF,
            0xF8, // LSE: Marks the start of a JPEG-LS preset parameters segment.
            0x00, 0x03, (byte)id
        };

        var reader = new JpegStreamReader { Source = buffer };

        var exception = Assert.Throws<InvalidDataException>(reader.ReadHeader);
        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.JpeglsPresetExtendedParameterTypeNotSupported, exception.Data[nameof(ErrorCode)]);
    }
}
