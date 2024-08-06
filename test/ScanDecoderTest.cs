// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using Xunit;

namespace CharLS.JpegLS.Test;

internal sealed unsafe class TestScanDecoder(FrameInfo frameInfo, JpegLSPresetCodingParameters presetCodingParameters, CodingParameters codingParameters) :
    ScanDecoder(frameInfo, presetCodingParameters, codingParameters)
{
    public override int DecodeScan(ReadOnlySpan<byte> source, Span<byte> destination, int stride)
    {
        throw new NotImplementedException();
    }

    public void TestInitialize(byte* source, int length)
    {
        Initialize(source, length);
    }

    public bool TestReadBit()
    {
        return ReadBit();
    }

    public void TestEndScan()
    {
        EndScan();
    }

    public byte TestPeekByte()
    {
        return PeekByte();
    }
}

public unsafe class ScanDecoderTest
{
    private static readonly JpegLSPresetCodingParameters DefaultParameters = new JpegLSPresetCodingParameters();

    [Fact]
    public void ReadBit()
    {
        byte[] source = [0, 0, Constants.JpegMarkerStartByte, 0xD8];
        var frameInfo = new FrameInfo(1, 1, 8, 1);
        DefaultParameters.IsValid(255, 0, out var cp);
        var codingParameters = new CodingParameters();

        var scanDecoder = new TestScanDecoder(frameInfo, cp, codingParameters);
        fixed (byte* ptr = source)
        {
            scanDecoder.TestInitialize(ptr, source.Length);

            for (int i = 0; i < 2 * 8; i++)
            {
                var actual = scanDecoder.TestReadBit();
                Assert.False(actual);
            }

            scanDecoder.TestEndScan();
        }
    }

    [Fact]
    public void ReadBitWithRefill()
    {
        byte[] source = [0, 0, 0, 0, 0, 0, 0, 0, 0, Constants.JpegMarkerStartByte, 0xD8];
        var frameInfo = new FrameInfo(1, 1, 8, 1);
        DefaultParameters.IsValid(255, 0, out var cp);
        var codingParameters = new CodingParameters();

        var scanDecoder = new TestScanDecoder(frameInfo, cp, codingParameters);
        fixed (byte* ptr = source)
        {
            scanDecoder.TestInitialize(ptr, source.Length);

            for (int i = 0; i < 9 * 8; i++)
            {
                var actual = scanDecoder.TestReadBit();
                Assert.False(actual);
            }

            scanDecoder.TestEndScan();
        }
    }

    [Fact]
    public void PeekByte()
    {
        byte[] source = [7, 8, Constants.JpegMarkerStartByte, 0xD8];
        var frameInfo = new FrameInfo(1, 1, 8, 1);
        DefaultParameters.IsValid(255, 0, out var cp);
        var codingParameters = new CodingParameters();

        var scanDecoder = new TestScanDecoder(frameInfo, cp, codingParameters);
        fixed (byte* ptr = source)
        {
            scanDecoder.TestInitialize(ptr, source.Length);

            byte peekByte1 = scanDecoder.TestPeekByte();
            for (int i = 0; i < 1 * 8; i++)
            {
                var actual = scanDecoder.TestReadBit();
            }
            byte peekByte2 = scanDecoder.TestPeekByte();
            for (int i = 0; i < 1 * 8; i++)
            {
                var actual = scanDecoder.TestReadBit();
            }
            scanDecoder.TestEndScan();

            Assert.Equal(7, peekByte1);
            Assert.Equal(8, peekByte2);
        }
    }
}
