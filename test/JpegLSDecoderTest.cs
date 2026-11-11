// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Reflection;

namespace CharLS.Managed.Test;

public class JpegLSDecoderTest
{
    [Fact]
    public void SetSourceTwiceThrows()
    {
        var buffer = new byte[2000];
        JpegLSDecoder decoder = new() { Source = buffer };

        var exception = Assert.Throws<InvalidOperationException>(() => decoder.Source = buffer);

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    [Fact]
    public void ReadHeaderWithoutSourceThrows()
    {
        JpegLSDecoder decoder = new();

        var exception = Assert.Throws<InvalidOperationException>(() => decoder.ReadHeader());

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    [Fact]
    public void DestinationSizeWithoutReadingHeaderThrows()
    {
        JpegLSDecoder decoder = new();

        var exception = Assert.Throws<InvalidOperationException>(() => decoder.GetDestinationSize());

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    [Fact]
    public void ReadHeaderFromNonJpegLSDataThrows()
    {
        var buffer = new byte[100];
        JpegLSDecoder decoder = new() { Source = buffer };

        var exception = Assert.Throws<InvalidDataException>(() => decoder.ReadHeader());

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.JpegMarkerStartByteNotFound, exception.GetErrorCode());
    }

    [Fact]
    public void FrameInfoWithoutReadHeaderThrows()
    {
        var buffer = new byte[2000];
        JpegLSDecoder decoder = new() { Source = buffer };

        var exception = Assert.Throws<InvalidOperationException>(() => decoder.FrameInfo);

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    [Fact]
    public void InterleaveModeWithoutReadHeaderThrows()
    {
        var buffer = new byte[2000];
        JpegLSDecoder decoder = new() { Source = buffer };

        var exception = Assert.Throws<InvalidOperationException>(() => decoder.GetInterleaveMode());

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    [Fact]
    public void GetNearLosslessWithoutReadHeaderThrows()
    {
        var buffer = new byte[2000];
        JpegLSDecoder decoder = new() { Source = buffer };

        var exception = Assert.Throws<InvalidOperationException>(() => decoder.GetNearLossless());

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    [Fact]
    public void PresetCodingParametersWithoutReadHeaderThrows()
    {
        var buffer = new byte[2000];
        JpegLSDecoder decoder = new() { Source = buffer };

        var exception = Assert.Throws<InvalidOperationException>(() => decoder.PresetCodingParameters);

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    [Fact]
    public void DestinationSize()
    {
        JpegLSDecoder decoder = new(ReadAllBytes("conformance/t8c0e0.jls"));

        const int expectedDestinationSize = 256 * 256 * 3;
        Assert.Equal(expectedDestinationSize, decoder.GetDestinationSize());
    }

    [Fact]
    public void DestinationSizeStrideInterleaveNone()
    {
        JpegLSDecoder decoder = new(ReadAllBytes("conformance/t8c0e0.jls"));

        const int stride = 512;
        const int expectedDestinationSize = stride * 256 * 3;
        Assert.Equal(expectedDestinationSize, decoder.GetDestinationSize(stride));
    }

    [Fact]
    public void DestinationSizeStrideInterleaveLine()
    {
        JpegLSDecoder decoder = new(ReadAllBytes("conformance/t8c1e0.jls"));

        const int stride = 1024;
        const int expectedDestinationSize = stride * 256;
        Assert.Equal(expectedDestinationSize, decoder.GetDestinationSize(stride));
    }

    [Fact]
    public void DestinationSizeStrideInterleaveSample()
    {
        JpegLSDecoder decoder = new(ReadAllBytes("conformance/t8c2e0.jls"));

        const int stride = 1024;
        const int expectedDestinationSize = stride * 256;
        Assert.Equal(expectedDestinationSize, decoder.GetDestinationSize(stride));
    }

    [Fact]
    public void DecodeReferenceFileFromBuffer()
    {
        JpegLSDecoder decoder = new(ReadAllBytes("conformance/t8c0e0.jls"));
        byte[] destination = new byte[decoder.GetDestinationSize()];

        decoder.Decode(destination);

        var referenceFile = Util.ReadAnymapReferenceFile("conformance/test8.ppm", decoder.GetInterleaveMode(), decoder.FrameInfo);
        var referenceImageData = referenceFile.ImageData;
        for (int i = 0; i != destination.Length; ++i)
        {
            Assert.Equal(referenceImageData[i], destination[i]);
        }
    }

    [Fact]
    public void DecodeFuzzyInputNoValidBitsAtTheEndThrows()
    {
        JpegLSDecoder decoder = new(ReadAllBytes("test-images/fuzzy-input-no-valid-bits-at-the-end.jls"));
        byte[] destination = new byte[decoder.GetDestinationSize()];

        var exception = Assert.Throws<InvalidDataException>(() => decoder.Decode(destination));

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidData, exception.GetErrorCode());
    }

    [Fact]
    public void DecodeFuzzyInputBadRunModeGolombCodeThrows()
    {
        JpegLSDecoder decoder = new(ReadAllBytes("test-images/fuzzy-input-bad-run-mode-golomb-code.jls"));
        byte[] destination = new byte[decoder.GetDestinationSize()];

        var exception = Assert.Throws<InvalidDataException>(() => decoder.Decode(destination));

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidData, exception.GetErrorCode());
    }

    [Fact]
    public void GetDestinationSizeReturnsZeroForAbbreviatedTableSpecification()
    {
        byte[] tableData = new byte[255];
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteJpegLSPresetParametersSegment(1, 1, tableData, false);
        writer.WriteMarker(JpegMarkerCode.EndOfImage);
        JpegLSDecoder decoder = new(writer.GetBuffer());

        int size = decoder.GetDestinationSize();

        Assert.Equal(0, size);
    }

    [Fact]
    public void DecodeDestinationSizeOverflowThrows()
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteStartOfFrameSegment(ushort.MaxValue, ushort.MaxValue, 2, 1);
        writer.WriteStartOfScanSegment(1, 1, 0, InterleaveMode.None);
        JpegLSDecoder decoder = new(writer.GetBuffer());

        var exception = Assert.Throws<InvalidDataException>(() => decoder.GetDestinationSize());

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.ParameterValueNotSupported, exception.GetErrorCode());
    }

    [Fact]
    public void DecodeWithDefaultPcParametersBeforeEachSos()
    {
        var source = ReadAllBytes("conformance/t8c0e0.jls");
        source = InsertPCParametersSegments(source, 3);

        JpegLSDecoder decoder = new(source);
        byte[] destination = new byte[decoder.GetDestinationSize()];
        decoder.Decode(destination);

        var referenceFile = Util.ReadAnymapReferenceFile("conformance/test8.ppm", decoder.GetInterleaveMode(), decoder.FrameInfo);
        var referenceImageData = referenceFile.ImageData;
        for (int i = 0; i != destination.Length; ++i)
        {
            Assert.Equal(referenceImageData[i], destination[i]);
        }
    }

    [Fact]
    public void DecodeWithDestinationAsReturn()
    {
        var source = ReadAllBytes("conformance/t8c0e0.jls");
        JpegLSDecoder decoder = new(source);

        var destination = decoder.Decode();

        var referenceFile = Util.ReadAnymapReferenceFile("conformance/test8.ppm", decoder.GetInterleaveMode(), decoder.FrameInfo);
        var referenceImageData = referenceFile.ImageData;
        for (int i = 0; i != destination.Length; ++i)
        {
            Assert.Equal(referenceImageData[i], destination[i]);
        }
    }

    [Fact]
    public void DecodeWithoutReadingHeaderThrows()
    {
        JpegLSDecoder decoder = new();
        byte[] buffer = new byte[1000];

        var exception = Assert.Throws<InvalidOperationException>(() => decoder.Decode(buffer));

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    [Fact]
    public void DecodeColorInterleaveNoneWithTooSmallBufferThrows()
    {
        DecodeImageWithTooSmallBufferThrows("conformance/t8c0e0.jls");
    }

    [Fact]
    public void DecodeColorInterleaveSampleWithTooSmallBufferThrows()
    {
        DecodeImageWithTooSmallBufferThrows("conformance/t8c2e0.jls");
    }

    [Fact]
    public void DecodeColorInterleaveNoneCustomStrideWithTooSmallBufferThrows()
    {
        DecodeImageWithTooSmallBufferThrows("conformance/t8c0e0.jls", 256 + 1, 1 + 1);
    }

    [Fact]
    public void DecodeColorInterleaveSampleCustomStrideWithTooSmallBufferThrows()
    {
        DecodeImageWithTooSmallBufferThrows("conformance/t8c2e0.jls", (256 * 3) + 1, 1 + 1);
    }

    [Fact]
    public void DecodeColorInterleaveNoneWithTooSmallStrideThrows()
    {
        byte[] source = Util.ReadFile("conformance/t8c0e0.jls");

        JpegLSDecoder decoder = new(source);
        byte[] destination = new byte[decoder.GetDestinationSize()];

        const int correctStride = 256;
        var exception = Assert.Throws<ArgumentException>(() => decoder.Decode(destination, correctStride - 1));

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidArgumentStride, exception.GetErrorCode());
    }

    [Fact]
    public void DecodeColorInterleaveSampleWithTooSmallStrideThrows()
    {
        byte[] source = Util.ReadFile("conformance/t8c2e0.jls");

        JpegLSDecoder decoder = new(source);
        byte[] destination = new byte[decoder.GetDestinationSize()];

        const int correctStride = 256;
        var exception = Assert.Throws<ArgumentException>(() => decoder.Decode(destination, correctStride - 1));

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidArgumentStride, exception.GetErrorCode());
    }

    [Fact]
    public void DecodeColorInterleaveNoneWithStandardStrideWorks()
    {
        byte[] source = Util.ReadFile("conformance/t8c0e0.jls");

        JpegLSDecoder decoder = new(source);
        byte[] destination = new byte[decoder.GetDestinationSize()];
        int standardStride = decoder.FrameInfo.Width;

        decoder.Decode(destination, standardStride);

        Util.VerifyDecodedBytes(decoder.GetInterleaveMode(), decoder.FrameInfo, destination, standardStride, "conformance/test8.ppm");
    }

    [Fact]
    public void DecodeColorInterleaveSampleWithStandardStrideWorks()
    {
        byte[] source = Util.ReadFile("conformance/t8c2e0.jls");

        JpegLSDecoder decoder = new(source);
        byte[] destination = new byte[decoder.GetDestinationSize()];
        int standardStride = decoder.FrameInfo.Width * 3;

        decoder.Decode(destination, standardStride);

        Util.VerifyDecodedBytes(decoder.GetInterleaveMode(), decoder.FrameInfo, destination, standardStride, "conformance/test8.ppm");
    }

    [Fact]
    public void DecodeColorInterleaveNoneWithCustomStrideWorks()
    {
        const int customStride = 256 + 1;
        byte[] source = Util.ReadFile("conformance/t8c0e0.jls");

        JpegLSDecoder decoder = new(source);
        byte[] destination = new byte[decoder.GetDestinationSize(customStride)];

        decoder.Decode(destination, customStride);

        Util.VerifyDecodedBytes(decoder.GetInterleaveMode(), decoder.FrameInfo, destination, customStride, "conformance/test8.ppm");
    }

    [Fact]
    public void DecodeColorInterleaveSampleWithCustomStrideWorks()
    {
        const int customStride = (256 * 3) + 1;
        byte[] source = Util.ReadFile("conformance/t8c2e0.jls");

        JpegLSDecoder decoder = new(source);
        byte[] destination = new byte[decoder.GetDestinationSize(customStride)];

        decoder.Decode(destination, customStride);

        Util.VerifyDecodedBytes(decoder.GetInterleaveMode(), decoder.FrameInfo, destination, customStride, "conformance/test8.ppm");
    }

    [Fact]
    public void ReadSpiffHeader()
    {
        var source = Util.CreateTestSpiffHeader();
        JpegLSDecoder decoder = new(source);

        Assert.NotNull(decoder.SpiffHeader);

        var header = decoder.SpiffHeader;
        Assert.Equal(SpiffProfileId.None, header.ProfileId);
        Assert.Equal(3, header.ComponentCount);
        Assert.Equal(800, header.Height);
        Assert.Equal(600, header.Width);
        Assert.Equal(SpiffColorSpace.Rgb, header.ColorSpace);
        Assert.Equal(8, header.BitsPerSample);
        Assert.Equal(SpiffCompressionType.JpegLS, header.CompressionType);
        Assert.Equal(SpiffResolutionUnit.DotsPerInch, header.ResolutionUnit);
        Assert.Equal(96, header.VerticalResolution);
        Assert.Equal(1024, header.HorizontalResolution);
    }

    [Fact]
    public void ReadSpiffHeaderFromNonJpegLSData()
    {
        byte[] source = new byte[100];
        JpegLSDecoder decoder = new(source, false);

        var exception = Assert.Throws<InvalidDataException>(() => decoder.TryReadSpiffHeader(out _));

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.JpegMarkerStartByteNotFound, exception.GetErrorCode());
    }

    [Fact]
    public void ReadSpiffHeaderFromJpegLSWithoutSpiff()
    {
        var source = ReadAllBytes("conformance/t8c0e0.jls");
        JpegLSDecoder decoder = new(source);

        Assert.Null(decoder.SpiffHeader);

        var frameInfo = decoder.FrameInfo;

        Assert.Equal(3, frameInfo.ComponentCount);
        Assert.Equal(8, frameInfo.BitsPerSample);
        Assert.Equal(256, frameInfo.Height);
        Assert.Equal(256, frameInfo.Width);
    }

    [Fact]
    public void ReadHeaderTwice()
    {
        var source = ReadAllBytes("conformance/t8c0e0.jls");
        JpegLSDecoder decoder = new(source);

        var exception = Assert.Throws<InvalidOperationException>(() => decoder.ReadHeader());

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    [Fact]
    public void DecodeFileWithFFInEntropyData()
    {
        var source = ReadAllBytes("test-images/ff_in_entropy_data.jls");
        JpegLSDecoder decoder = new(source);

        var frameInfo = decoder.FrameInfo;
        Assert.Equal(1, frameInfo.ComponentCount);
        Assert.Equal(12, frameInfo.BitsPerSample);
        Assert.Equal(1216, frameInfo.Height);
        Assert.Equal(968, frameInfo.Width);

        var destination = new byte[decoder.GetDestinationSize()];

        var exception = Assert.Throws<InvalidDataException>(() => decoder.Decode(destination));

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidData, exception.GetErrorCode());
    }

    [Fact]
    public void DecodeFileWithGolombLargeThenKMax()
    {
        var source = ReadAllBytes("test-images/fuzzy_input_golomb_16.jls");
        JpegLSDecoder decoder = new(source);

        var frameInfo = decoder.FrameInfo;
        Assert.Equal(3, frameInfo.ComponentCount);
        Assert.Equal(16, frameInfo.BitsPerSample);
        Assert.Equal(65516, frameInfo.Height);
        Assert.Equal(1, frameInfo.Width);

        var destination = new byte[decoder.GetDestinationSize()];

        var exception = Assert.Throws<InvalidDataException>(() => decoder.Decode(destination));

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidData, exception.GetErrorCode());
    }

    [Fact]
    public void DecodeFileWithMissingRestartMarker()
    {
        var source = ReadAllBytes("conformance/t8c0e0.jls");

        // Insert a DRI marker segment to trigger that restart markers are used.
        JpegTestStreamWriter streamWriter = new();
        streamWriter.WriteDefineRestartInterval(10, 3);

        source = ArrayInsert(2, source, streamWriter.GetBuffer().ToArray());

        JpegLSDecoder decoder = new(source);
        var destination = new byte[decoder.GetDestinationSize()];

        var exception = Assert.Throws<InvalidDataException>(() => decoder.Decode(destination));

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.RestartMarkerNotFound, exception.GetErrorCode());
    }

    [Fact]
    public void DecodeFileWithIncorrectRestartMarker()
    {
        var source = ReadAllBytes("test-images/test8_ilv_none_rm_7.jls");

        // Change the first restart marker to the second.
        int position = FindScanHeader(0, source);
        position = FindFirstRestartMarker(position + 1, source);
        ++position;
        source[position] = 0xD1;

        JpegLSDecoder decoder = new(source);
        var destination = new byte[decoder.GetDestinationSize()];

        var exception = Assert.Throws<InvalidDataException>(() => decoder.Decode(destination));

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.RestartMarkerNotFound, exception.GetErrorCode());
    }

    [Fact]
    public void DecodeFileWithExtraBeginBytesForRestartMarkerCode()
    {
        var source = ReadAllBytes("test-images/test8_ilv_none_rm_7.jls");

        // Add additional 0xFF marker begin bytes
        int position = FindScanHeader(0, source);
        position = FindFirstRestartMarker(position + 1, source);
        byte[] extraBeginBytes = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];
        source = ArrayInsert(position, source, extraBeginBytes);

        JpegLSDecoder decoder = new(source);
        var referenceFile = Util.ReadAnymapReferenceFile("conformance/test8.ppm", decoder.GetInterleaveMode(), decoder.FrameInfo);
        Util.TestCompliance(source, referenceFile.ImageData, false);
    }

    [Fact]
    public void ReadComment()
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteSegment(JpegMarkerCode.Comment, "hello"u8);
        writer.WriteStartOfFrameSegment(512, 512, 8, 3);
        writer.WriteStartOfScanSegment(0, 1, 0, InterleaveMode.None);

        JpegLSDecoder decoder = new(writer.GetBuffer(), false);
        decoder.Comment += (sender, e) =>
        {
            Assert.Equal(decoder, sender);

            Assert.NotNull(e);
            Assert.Equal(5, e.Data.Length);

            Assert.True(e.Data.Span.SequenceEqual("hello"u8));
        };

        decoder.ReadHeader();
    }

    [Fact]
    public void ReadCommentWhileAlreadyUnregistered()
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteSegment(JpegMarkerCode.Comment, "hello"u8);
        writer.WriteStartOfFrameSegment(512, 512, 8, 3);
        writer.WriteStartOfScanSegment(0, 1, 0, InterleaveMode.None);
        JpegLSDecoder decoder = new(writer.GetBuffer(), false);
        decoder.Comment += EventHandler;
        decoder.Comment -= EventHandler;

        decoder.ReadHeader();

        bool eventCalled = false;
        Assert.False(eventCalled);
        return;

        void EventHandler(object? sender, CommentEventArgs e)
        {
            eventCalled = true;
        }
    }

    [Fact]
    public void CommentHandlerThatThrowsExceptionReturnsCallbackFailedError()
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteSegment(JpegMarkerCode.Comment, "hello"u8);
        writer.WriteStartOfFrameSegment(512, 512, 8, 3);
        writer.WriteStartOfScanSegment(0, 1, 0, InterleaveMode.None);

        JpegLSDecoder decoder = new(writer.GetBuffer(), false);
        decoder.Comment += EventHandler;

        var exception = Assert.Throws<InvalidDataException>(() => decoder.ReadHeader());
        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.CallbackFailed, exception.GetErrorCode());
        _ = Assert.IsType<ArgumentNullException>(exception.InnerException);
        return;

        static void EventHandler(object? sender, CommentEventArgs e)
        {
            throw new ArgumentNullException();
        }
    }

    [Fact]
    public void ApplicationDataHandlerReceivesApplicationDataBytes()
    {
        JpegLSEncoder encoder = new(1, 1, 8, 1, InterleaveMode.None, true, 100);

        var applicationData1 = new byte[] { 1, 2, 3, 4 };
        encoder.WriteApplicationData(12, applicationData1);
        encoder.Encode(new byte[1]);

        byte[]? applicationData2 = null;
        JpegLSDecoder decoder = new(encoder.EncodedData, false);

        decoder.ApplicationData += ApplicationDataHandler;
        decoder.ApplicationData -= ApplicationDataHandler;
        decoder.ApplicationData += ApplicationDataHandler;
        decoder.ReadHeader();

        Assert.NotNull(applicationData2);
        Assert.Equal(4, applicationData2.Length);
        Assert.Equal(1, applicationData2![0]);
        Assert.Equal(2, applicationData2![1]);
        Assert.Equal(3, applicationData2![2]);
        Assert.Equal(4, applicationData2![3]);
        return;

        void ApplicationDataHandler(object? _, ApplicationDataEventArgs e)
        {
            applicationData2 = e.Data.ToArray();
        }
    }

    [Fact]
    public void OversizeImageDimensionBeforeStartOfFrame()
    {
        const int width = 99;
        const int height = ushort.MaxValue + 1;

        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteOversizeImageDimension(3, width, height);
        writer.WriteStartOfFrameSegment(0, 0, 8, 3);
        writer.WriteStartOfScanSegment(0, 1, 0, InterleaveMode.None);

        JpegLSDecoder decoder = new(writer.GetBuffer());

        Assert.Equal(height, decoder.FrameInfo.Height);
        Assert.Equal(width, decoder.FrameInfo.Width);
    }

    [Fact]
    public void OversizeImageDimensionZeroBeforeStartOfFrame()
    {
        const int width = 99;
        const int height = ushort.MaxValue;

        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteOversizeImageDimension(2, 0, 0);
        writer.WriteStartOfFrameSegment(width, height, 8, 3);
        writer.WriteStartOfScanSegment(0, 1, 0, InterleaveMode.None);

        JpegLSDecoder decoder = new(writer.GetBuffer());

        Assert.Equal(height, decoder.FrameInfo.Height);
        Assert.Equal(width, decoder.FrameInfo.Width);
    }

    [Fact]
    public void OversizeImageDimensionWithInvalidNumberOfBytesThrows()
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteOversizeImageDimension(1, 1, 1);
        writer.WriteStartOfFrameSegment(512, 512, 8, 3);
        writer.WriteStartOfScanSegment(0, 1, 0, InterleaveMode.None);

        JpegLSDecoder decoder = new(writer.GetBuffer(), false);

        var exception = Assert.Throws<InvalidDataException>(() => decoder.ReadHeader());
        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidParameterJpegLSPresetParameters, exception.GetErrorCode());
    }

    [Fact]
    public void OversizeImageDimensionChangeWidthAfterStartOfFrameThrows()
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteStartOfFrameSegment(99, ushort.MaxValue, 8, 3);
        writer.WriteOversizeImageDimension(2, 10, 0);
        writer.WriteStartOfScanSegment(0, 1, 0, InterleaveMode.None);

        JpegLSDecoder decoder = new(writer.GetBuffer(), false);

        var exception = Assert.Throws<InvalidDataException>(() => decoder.ReadHeader());

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidParameterWidth, exception.GetErrorCode());
    }

    [Fact]
    public void StartOfFrameChangesHeightThrows()
    {
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteOversizeImageDimension(2, 0, 10);
        writer.WriteStartOfFrameSegment(0, ushort.MaxValue, 8, 3);
        writer.WriteStartOfScanSegment(0, 1, 0, InterleaveMode.None);
        JpegLSDecoder decoder = new(writer.GetBuffer(), false);

        var exception = Assert.Throws<InvalidDataException>(() => decoder.ReadHeader());

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidParameterHeight, exception.GetErrorCode());
    }

    [Fact]
    public void TestOversizeImageDimensionBadSegmentSizeThrows()
    {
        OversizeImageDimensionBadSegmentSizeThrows(2);
        OversizeImageDimensionBadSegmentSizeThrows(3);
        OversizeImageDimensionBadSegmentSizeThrows(4);
    }

    [Fact]
    public void OversizeImageDimensionThatCausesOverflowThrows()
    {
        const uint width = uint.MaxValue;
        const uint height = uint.MaxValue;
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteOversizeImageDimension(4, width, height);
        writer.WriteStartOfFrameSegment(0, 0, 8, 2);
        writer.WriteStartOfScanSegment(0, 1, 0, InterleaveMode.None);
        JpegLSDecoder decoder = new(writer.GetBuffer(), false);

        var exception = Assert.Throws<InvalidDataException>(() => decoder.ReadHeader());

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.ParameterValueNotSupported, exception.GetErrorCode());
    }

    [Fact]
    public void AbbreviatedFormatMappingTableCountAfterReadHeader()
    {
        byte[] tableData = new byte[255];
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteJpegLSPresetParametersSegment(1, 1, tableData, false);
        writer.WriteMarker(JpegMarkerCode.EndOfImage);

        JpegLSDecoder decoder = new(writer.GetBuffer(), false);
        Assert.Equal(CompressedDataFormat.Unknown, decoder.CompressedDataFormat);

        decoder.ReadHeader();
        int count = decoder.MappingTableCount;

        Assert.Equal(1, count);
        Assert.Equal(CompressedDataFormat.AbbreviatedTableSpecification, decoder.CompressedDataFormat);
    }

    [Fact]
    public void CompressedDataFormatAbbreviatedImageData()
    {
        JpegLSDecoder decoder = new(ReadAllBytes("conformance/t8c0e0.jls"));
        byte[] destination = new byte[decoder.GetDestinationSize()];

        decoder.Decode(destination);
        Assert.Equal(CompressedDataFormat.Interchange, decoder.CompressedDataFormat);
    }

    [Fact]
    public void AbbreviatedFormatWithSpiffHeaderThrows()
    {
        byte[] tableData = new byte[255];

        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();

        SpiffHeader header = new()
        {
            BitsPerSample = 8,
            ColorSpace = SpiffColorSpace.Rgb,
            ComponentCount = 3,
            Height = 1,
            Width = 1
        };

        writer.WriteSpiffHeaderSegment(header);
        writer.WriteSpiffEndOfDirectoryEntry();
        writer.WriteJpegLSPresetParametersSegment(1, 1, tableData, false);
        writer.WriteMarker(JpegMarkerCode.EndOfImage);

        JpegLSDecoder decoder = new(writer.GetBuffer(), false);
        _ = decoder.TryReadSpiffHeader(out _);

        var exception = Assert.Throws<InvalidDataException>(() => decoder.ReadHeader());

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.AbbreviatedFormatAndSpiffHeaderMismatch, exception.GetErrorCode());
    }

    [Fact]
    public void MappingTableCountAfterDecodeTableAfterFirstScan()
    {
        byte[] dataH10 =
        [
                 0xFF,  0xD8, // Start of image (SOI) marker
                 0xFF,  0xF7, // Start of JPEG-LS frame (SOF 55) marker - marker segment follows
                 0x00,  0x0E, // Length of marker segment = 14 bytes including the length field
                 0x02,             // P = Precision = 2 bits per sample
                 0x00,  0x04, // Y = Number of lines = 4
                 0x00,  0x03, // X = Number of columns = 3
                 0x02,             // Nf = Number of components in the frame = 2
                 0x01,             // C1  = Component ID = 1 (first and only component)
                 0x11,             // Sub-sampling: H1 = 1, V1 = 1
                 0x00,             // Tq1 = 0 (this field is always 0)
                 0x02,             // C2  = Component ID = 2 (first and only component)
                 0x11,             // Sub-sampling: H1 = 1, V1 = 1
                 0x00,             // Tq1 = 0 (this field is always 0)

                 0xFF,  0xF8,             // LSE - JPEG-LS preset parameters marker
                 0x00,  0x11,             // Length of marker segment = 17 bytes including the length field
                 0x02,                         // ID = 2, mapping table
                 0x05,                         // TID = 5 Table identifier (arbitrary)
                 0x03,                         // Wt = 3 Width of table entry
                 0xFF,  0xFF,  0xFF, // Entry for index 0
                 0xFF,  0x00,  0x00, // Entry for index 1
                 0x00,  0xFF,  0x00, // Entry for index 2
                 0x00,  0x00,  0xFF, // Entry for index 3

                 0xFF,  0xDA,             // Start of scan (SOS) marker
                 0x00,  0x08,             // Length of marker segment = 8 bytes including the length field
                 0x01,                         // Ns = Number of components for this scan = 1
                 0x01,                         // C1 = Component ID = 1
                 0x05,                         // Tm 1  = Mapping table identifier = 5
                 0x00,                         // NEAR = 0 (near-lossless max error)
                 0x00,                         // ILV = 0 (interleave mode = non-interleaved)
                 0x00,                         // Al = 0, Ah = 0 (no point transform)
                 0xDB,  0x95,  0xF0, // 3 bytes of compressed image data

                 0xFF,  0xF8,             // LSE - JPEG-LS preset parameters marker
                 0x00,  0x11,             // Length of marker segment = 17 bytes including the length field
                 0x02,                         // ID = 2, mapping table
                 0x06,                         // TID = 6 Table identifier (arbitrary)
                 0x03,                         // Wt = 3 Width of table entry
                 0xFF,  0xFF,  0xFF, // Entry for index 0
                 0xFF,  0x00,  0x00, // Entry for index 1
                 0x00,  0xFF,  0x00, // Entry for index 2
                 0x00,  0x00,  0xFF, // Entry for index 3

                 0xFF,  0xDA,             // Start of scan (SOS) marker
                 0x00,  0x08,             // Length of marker segment = 8 bytes including the length field
                 0x01,                         // Ns = Number of components for this scan = 1
                 0x02,                         // C1 = Component ID = 2
                 0x06,                         // Tm 1  = Mapping table identifier = 6
                 0x00,                         // NEAR = 0 (near-lossless max error)
                 0x00,                         // ILV = 0 (interleave mode = non-interleaved)
                 0x00,                         // Al = 0, Ah = 0 (no point transform)
                 0xDB,  0x95,  0xF0, // 3 bytes of compressed image data

                 0xFF,  0xD9 // End of image (EOI) marker
        ];

        JpegLSDecoder decoder = new(dataH10);
        var destination = new byte[decoder.GetDestinationSize()];
        decoder.Decode(destination);

        int count = decoder.MappingTableCount;
        Assert.Equal(2, count);

        Assert.Equal(5, decoder.GetMappingTableId(0));
        Assert.Equal(6, decoder.GetMappingTableId(1));
    }

    [Fact]
    public void InvalidTableIdThrows()
    {
        byte[] tableData = new byte[255];

        JpegTestStreamWriter writer = new();

        writer.WriteStartOfImage();
        writer.WriteJpegLSPresetParametersSegment(0, 1, tableData, false);

        JpegLSDecoder decoder = new(writer.GetBuffer(), false);

        var exception = Assert.Throws<InvalidDataException>(() => decoder.ReadHeader());

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidParameterMappingTableId, exception.GetErrorCode());
    }

    [Fact]
    public void DuplicateTableIdThrows()
    {
        byte[] tableData = new byte[255];

        JpegTestStreamWriter writer = new();

        writer.WriteStartOfImage();
        writer.WriteJpegLSPresetParametersSegment(1, 1, tableData, false);
        writer.WriteStartOfFrameSegment(1, 1, 8, 3);
        writer.WriteJpegLSPresetParametersSegment(1, 1, tableData, false);
        writer.WriteStartOfScanSegment(0, 1, 0, InterleaveMode.None);

        JpegLSDecoder decoder = new(writer.GetBuffer(), false);

        var exception = Assert.Throws<InvalidDataException>(() => decoder.ReadHeader());

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidParameterMappingTableId, exception.GetErrorCode());
    }

    [Fact]
    public void MappingTableIdReturnsZero()
    {
        JpegLSDecoder decoder = new(ReadAllBytes("conformance/t8c0e0.jls"));

        byte[] destination = new byte[decoder.GetDestinationSize()];
        decoder.Decode(destination);

        Assert.Equal(0, decoder.GetMappingTableId(0));
        Assert.Equal(0, decoder.GetMappingTableId(1));
        Assert.Equal(0, decoder.GetMappingTableId(2));
    }

    [Fact]
    public void MappingTableIdForInvalidComponentThrows()
    {
        JpegLSDecoder decoder = new(ReadAllBytes("conformance/t8c0e0.jls"));
        byte[] destination = new byte[decoder.GetDestinationSize()];
        decoder.Decode(destination);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => decoder.GetMappingTableId(3));

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidArgument, exception.GetErrorCode());
    }

    [Fact]
    public void MappingTableIdBeforeDecodeThrows()
    {
        JpegLSDecoder decoder = new(ReadAllBytes("conformance/t8c0e0.jls"));

        var exception = Assert.Throws<InvalidOperationException>(() => decoder.GetMappingTableId(0));

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    [Fact]
    public void MappingTableIndexBeforeDecodeThrows()
    {
        JpegLSDecoder decoder = new(ReadAllBytes("conformance/t8c0e0.jls"));

        var exception = Assert.Throws<InvalidOperationException>(() => decoder.FindMappingTableIndex(3));

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    [Fact]
    public void MappingTableIndexInvalidIndexThrows()
    {
        JpegLSDecoder decoder = new(ReadAllBytes("conformance/t8c0e0.jls"));
        byte[] destination = new byte[decoder.GetDestinationSize()];
        decoder.Decode(destination);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => decoder.FindMappingTableIndex(0));

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidArgument, exception.GetErrorCode());

        exception = Assert.Throws<ArgumentOutOfRangeException>(() => decoder.FindMappingTableIndex(256));

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidArgument, exception.GetErrorCode());
    }

    [Fact]
    public void MappingTableCountBeforeDecodeThrows()
    {
        JpegLSDecoder decoder = new(ReadAllBytes("conformance/t8c0e0.jls"));

        var exception = Assert.Throws<InvalidOperationException>(() => decoder.MappingTableCount);

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    [Fact]
    public void MappingTableInfoBeforeDecodeThrows()
    {
        JpegLSDecoder decoder = new(ReadAllBytes("conformance/t8c0e0.jls"));

        var exception = Assert.Throws<InvalidOperationException>(() => decoder.GetMappingTableInfo(0));

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    [Fact]
    public void MappingTableBeforeDecodeThrows()
    {
        JpegLSDecoder decoder = new(ReadAllBytes("conformance/t8c0e0.jls"));

        var exception = Assert.Throws<InvalidOperationException>(() => decoder.GetMappingTableData(0));

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    [Fact]
    public void MappingTableInvalidIndexThrows()
    {
        JpegLSDecoder decoder = new(ReadAllBytes("conformance/t8c0e0.jls"));
        byte[] destination = new byte[decoder.GetDestinationSize()];
        decoder.Decode(destination);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => decoder.GetMappingTableData(0));

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidArgument, exception.GetErrorCode());
    }

    [Fact]
    public void ReadHeaderNon8Or16BitWithColorTransformationThrows()
    {
        JpegLSDecoder decoder = new(ReadAllBytes("test-images/land10-10bit-rgb-hp3-invalid.jls"), false);

        var exception = Assert.Throws<InvalidDataException>(() => decoder.ReadHeader());

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidParameterColorTransformation, exception.GetErrorCode());
    }

    private static void OversizeImageDimensionBadSegmentSizeThrows(int numberOfBytes)
    {
        const int width = 0;
        const int height = ushort.MaxValue;
        JpegTestStreamWriter writer = new();
        writer.WriteStartOfImage();
        writer.WriteOversizeImageDimension(numberOfBytes, width, 10, true);
        writer.WriteStartOfFrameSegment(width, height, 8, 3);
        writer.WriteStartOfScanSegment(0, 1, 0, InterleaveMode.None);
        JpegLSDecoder decoder = new(writer.GetBuffer(), false);

        var exception = Assert.Throws<InvalidDataException>(() => decoder.ReadHeader());

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidMarkerSegmentSize, exception.GetErrorCode());
    }

    private static byte[] ReadAllBytes(string path, int bytesToSkip = 0)
    {
        var fullPath = Path.Join(DataFileDirectory, path);

        if (bytesToSkip == 0)
            return File.ReadAllBytes(fullPath);

        using var stream = File.OpenRead(fullPath);
        var result = new byte[new FileInfo(fullPath).Length - bytesToSkip];

        _ = stream.Seek(bytesToSkip, SeekOrigin.Begin);
        _ = stream.Read(result, 0, result.Length);
        return result;
    }

    private static string DataFileDirectory
    {
        get
        {
            var assemblyLocation = new Uri(Assembly.GetExecutingAssembly().Location);
            return Path.GetDirectoryName(assemblyLocation.LocalPath)!;
        }
    }

    private static byte[] InsertPCParametersSegments(byte[] source, int componentCount)
    {
        var pcpSegment = CreateDefaultPCParametersSegment();

        int position = 0;
        for (int i = 0; i != componentCount; ++i)
        {
            position = FindScanHeader(position, source);
            source = ArrayInsert(position, source, pcpSegment);
            position += pcpSegment.Length + 2;
        }

        return source;
    }

    private static byte[] CreateDefaultPCParametersSegment()
    {
        JpegTestStreamWriter writer = new();

        var a = new JpegLSPresetCodingParameters();
        writer.WriteJpegLSPresetParametersSegment(a);
        return writer.GetBuffer().ToArray();
    }

    private static int FindScanHeader(int startPosition, byte[] data)
    {
        const byte startOfScan = 0xDA;

        for (int i = startPosition; startPosition < data.Length - 1; ++i)
        {
            if (data[i] == 0xFF && data[i + 1] == startOfScan)
                return i;
        }

        return -1;
    }

    private static int FindFirstRestartMarker(int startPosition, byte[] data)
    {
        const byte firstRestartMarker = 0xD0;

        for (int i = startPosition; i < data.Length; ++i)
        {
            if (data[i] == 0xFF && data[i + 1] == firstRestartMarker)
                return i;
        }

        return -1;
    }

    private static byte[] ArrayInsert(int insertIndex, byte[] array1, byte[] array2)
    {
        var result = new byte[array1.Length + array2.Length];

        Array.Copy(array1, 0, result, 0, insertIndex);
        Array.Copy(array2, 0, result, insertIndex, array2.Length);
        Array.Copy(array1, insertIndex, result, insertIndex + array2.Length, array1.Length - insertIndex);

        return result;
    }

    private static void DecodeImageWithTooSmallBufferThrows(string path, int stride = 0, int tooSmallByteCount = 1)
    {
        byte[] source = Util.ReadFile(path);
        JpegLSDecoder decoder = new(source);

        byte[] destination = new byte[decoder.GetDestinationSize(stride) - tooSmallByteCount];

        var exception = Assert.Throws<ArgumentException>(() => decoder.Decode(destination, stride));

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidArgumentSize, exception.GetErrorCode());
    }
}
