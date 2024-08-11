// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace CharLS.JpegLS.Test;

internal sealed class Util
{
    private static string DataFileDirectory
    {
        get
        {
            var assemblyLocation = new Uri(Assembly.GetExecutingAssembly().Location);
            return Path.GetDirectoryName(assemblyLocation.LocalPath)!;
        }
    }


    internal static byte[] ReadFile(string path, int bytesToSkip = 0)
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

    internal static PortableAnymapFile ReadAnymapReferenceFile(string filename, InterleaveMode interleaveMode, FrameInfo frameInfo)
    {
        PortableAnymapFile referenceFile = new(filename);

        if (interleaveMode == InterleaveMode.None && frameInfo.ComponentCount == 3)
        {
            referenceFile.ImageData = TripletToPlanar(referenceFile.ImageData, frameInfo.Width, frameInfo.Height);
        }

        return referenceFile;
    }

    internal static PortableAnymapFile ReadAnymapReferenceFile(string filename, InterleaveMode interleaveMode)
    {
        PortableAnymapFile referenceFile = new(filename);

        if (interleaveMode == InterleaveMode.None && referenceFile.ComponentCount == 3)
        {
            referenceFile.ImageData = TripletToPlanar(referenceFile.ImageData, referenceFile.Width, referenceFile.Height);
        }

        return referenceFile;
    }

    internal static void TestCompliance(byte[] encodedSource, byte[] uncompressedSource, bool checkEncode)
    {
        JpegLSDecoder decoder = new(encodedSource);

        if (checkEncode)
        {
            //Assert::IsTrue(verify_encoded_bytes(uncompressed_source, encoded_source));
        }

        var destination = new byte[decoder.GetDestinationSize()];
        decoder.Decode(destination);

        if (decoder.NearLossless == 0)
        {
            for (int i = 0; i != uncompressedSource.Length; ++i)
            {
                if (uncompressedSource[i] != destination[i])
                {
                    Assert.Equal(uncompressedSource[i], destination[i]);
                }
            }
        }
        else
        {
            var frameInfo = decoder.FrameInfo;
            int nearLossless = decoder.NearLossless;

            if (frameInfo.BitsPerSample <= 8)
            {
                for (int i = 0; i != uncompressedSource.Length; ++i)
                {
                    if (Math.Abs(uncompressedSource[i] - destination[i]) > nearLossless)
                    {
                        Assert.Equal(uncompressedSource[i], destination[i]);
                    }
                }
            }
            else
            {
                var source16 = MemoryMarshal.Cast<byte, ushort>(new ReadOnlySpan<byte>(uncompressedSource));
                var destination16 = MemoryMarshal.Cast<byte, ushort>(new ReadOnlySpan<byte>(destination));

                for (int i = 0; i != source16.Length; ++i)
                {
                    if (Math.Abs(source16[i] - destination16[i]) > nearLossless)
                    {
                        Assert.Equal(source16[i], destination16[i]);
                    }
                }
            }
        }
    }

    internal static void TestByDecoding(ReadOnlyMemory<byte> encodedSource, FrameInfo sourceFrameInfo,
        ReadOnlyMemory<byte> expectedDestination,
        InterleaveMode interleaveMode,
        ColorTransformation colorTransformation = ColorTransformation.None)
    {
        JpegLSDecoder decoder = new();
        decoder.Source = encodedSource;
        decoder.ReadHeader();

        var frameInfo = decoder.FrameInfo;

        Assert.Equal(sourceFrameInfo.Width, frameInfo.Width);
        Assert.Equal(sourceFrameInfo.Height, frameInfo.Height);
        Assert.Equal(sourceFrameInfo.BitsPerSample, frameInfo.BitsPerSample);
        Assert.Equal(sourceFrameInfo.ComponentCount, frameInfo.ComponentCount);
        Assert.Equal(interleaveMode, decoder.InterleaveMode);
        ////Assert.True(color_transformation == decoder.color_transformation());

        var destination = new byte[decoder.GetDestinationSize()];
        decoder.Decode(destination);

        Assert.Equal(destination.Length, expectedDestination.Length);

        if (decoder.NearLossless == 0)
        {
            for (int i = 0; i != expectedDestination.Length; ++i)
            {
                if (expectedDestination.Span[i] != destination[i])
                {
                    Assert.Equal(expectedDestination.Span[i], destination[i]);
                }
            }
        }
    }

    internal static ReadOnlyMemory<byte> CreateTestSpiffHeader(byte highVersion = 2, byte lowVersion = 0, bool endOfDirectory = true, byte componentCount = 3)
    {
        byte[] buffer =
        [
            0xFF,
            0xD8, // SOI.
            0xFF,
            0xE8, // ApplicationData8
            0,
            32,

            // SPIFF identifier string.
            (byte)'S',
            (byte)'P',
            (byte)'I',
            (byte)'F',
            (byte)'F',
            0,

            // Version
            highVersion,
            lowVersion,

            0, // profile id
            componentCount,

            // Height
            0,
            0,
            0x3,
            0x20,

            // Width
            0,
            0,
            0x2,
            0x58,

            10, // color space
            8, // bits per sample
            6, // compression type, 6 = JPEG-LS
            1, // resolution units

            // vertical_resolution
            0,
            0,
            0,
            96,

            // header.horizontal_resolution = 1024
            0,
            0,
            4,
            0
        ];

        JpegTestStreamWriter writer = new();
        writer.WriteBytes(buffer);
        if (endOfDirectory)
        {
            writer.WriteSpiffEndOfDirectoryEntry();
        }

        writer.WriteStartOfFrameSegment(600, 800, 8, 3);
        writer.WriteStartOfScanSegment(0, 1, 0, InterleaveMode.None);

        return writer.GetBuffer();
    }

    internal static void CompareBuffers(ReadOnlySpan<byte> buffer1, ReadOnlySpan<byte> buffer2)
    {
        Assert.Equal(buffer1.Length, buffer2.Length);

        for (int i = 0; i != buffer1.Length; ++i)
        {
            if (buffer1[i] != buffer2[i])
            {
                Assert.Equal(buffer1[i], buffer2[i]);
                break;
            }
        }
    }

    private static byte[] TripletToPlanar(byte[] tripletBuffer, int width, int height)
    {
        byte[] planarBuffer = new byte[tripletBuffer.Length];

        int byteCount = width * height;
        for (int index = 0; index != byteCount; ++index)
        {
            planarBuffer[index] = tripletBuffer[index * 3 + 0];
            planarBuffer[index + 1 * byteCount] = tripletBuffer[index * 3 + 1];
            planarBuffer[index + 2 * byteCount] = tripletBuffer[index * 3 + 2];
        }

        return planarBuffer;
    }
}
