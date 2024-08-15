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
    }

    [Fact]
    public void ReadHeaderWithoutSource()
    {
        JpegLSDecoder decoder = new();

        var exception = Assert.Throws<InvalidOperationException>(() => decoder.ReadHeader());
        Assert.False(string.IsNullOrEmpty(exception.Message));
    }

    [Fact]
    public void DestinationSizeWithoutReadingHeader()
    {
        JpegLSDecoder decoder = new();

        var exception = Assert.Throws<InvalidOperationException>(() => decoder.GetDestinationSize());
        Assert.False(string.IsNullOrEmpty(exception.Message));
    }

    [Fact]
    public void ReadHeaderWithoutSourceThrows()
    {
        JpegLSDecoder decoder = new();

        var exception = Assert.Throws<InvalidOperationException>(() => decoder.ReadHeader());
        Assert.False(string.IsNullOrEmpty(exception.Message));
    }

    [Fact]
    public void ReadHeaderFromNonJpegLSData()
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
    }

    [Fact]
    public void InterleaveModeWithoutReadHeader()
    {
        var buffer = new byte[2000];
        JpegLSDecoder decoder = new() { Source = buffer };

        var exception = Assert.Throws<InvalidOperationException>(() => decoder.InterleaveMode);
        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidOperation, exception.GetErrorCode());
    }

    [Fact]
    public void NearLosslessWithoutReadHeader()
    {
        var buffer = new byte[2000];
        JpegLSDecoder decoder = new() { Source = buffer };

        var exception = Assert.Throws<InvalidOperationException>(() => decoder.NearLossless);
        Assert.False(string.IsNullOrEmpty(exception.Message));
    }

    [Fact]
    public void PresetCodingParametersWithoutReadHeader()
    {
        var buffer = new byte[2000];
        JpegLSDecoder decoder = new() { Source = buffer };

        var exception = Assert.Throws<InvalidOperationException>(() => decoder.PresetCodingParameters);
        Assert.False(string.IsNullOrEmpty(exception.Message));
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

        var referenceFile = Util.ReadAnymapReferenceFile("conformance/test8.ppm", decoder.InterleaveMode, decoder.FrameInfo);
        var referenceImageData = referenceFile.ImageData;
        for (int i = 0; i != destination.Length; ++i)
        {
            Assert.Equal(referenceImageData[i], destination[i]);
        }
    }

    [Fact]
    public void DecodeWithDefaultPcParametersBeforeEachSos()
    {
        var source = ReadAllBytes("conformance/t8c0e0.jls");
        source = InsertPCParametersSegments(source, 3);

        JpegLSDecoder decoder = new(source);
        byte[] destination = new byte[decoder.GetDestinationSize()];
        decoder.Decode(destination);

        var referenceFile = Util.ReadAnymapReferenceFile("conformance/test8.ppm", decoder.InterleaveMode, decoder.FrameInfo);
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

        var referenceFile = Util.ReadAnymapReferenceFile("conformance/test8.ppm", decoder.InterleaveMode, decoder.FrameInfo);
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
        var referenceFile = Util.ReadAnymapReferenceFile("conformance/test8.ppm", decoder.InterleaveMode, decoder.FrameInfo);
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
    public void ReadCommentThrowException()
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
        Assert.IsType<ArgumentNullException>(exception.InnerException);
        return;

        void EventHandler(object? sender, CommentEventArgs e)
        {
            throw new ArgumentNullException();
        }
    }

    [Fact]
    public void ApplicationDataHandlerReceivesApplicationDataBytes()
    {
        JpegLSEncoder encoder = new(1, 1, 8, 1, true, 100);

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
}
