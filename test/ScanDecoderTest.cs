// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using Xunit;

namespace CharLS.JpegLS.Test;

internal sealed class TestScanDecoder(FrameInfo frameInfo, JpegLSPresetCodingParameters presetCodingParameters, CodingParameters codingParameters) :
    ScanDecoder(frameInfo, presetCodingParameters, codingParameters)
{
    public override int DecodeScan(ReadOnlySpan<byte> source, Span<byte> destination, int stride)
    {
        throw new NotImplementedException();
    }

    public void TestInitialize(ReadOnlySpan<byte> source)
    {
        Initialize(source);
    }

    public bool TestReadBit(ReadOnlySpan<byte> source)
    {
        return ReadBit(source);
    }

    public void TestEndScan(ReadOnlySpan<byte> source)
    {
        EndScan(source);
    }

    public byte TestPeekByte(ReadOnlySpan<byte> source)
    {
        return PeekByte(source);
    }
}

public class ScanDecoderTest
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
        scanDecoder.TestInitialize(source);

        for (int i = 0; i < 2 * 8; i++)
        {
            var actual = scanDecoder.TestReadBit(source);
            Assert.False(actual);
        }

        scanDecoder.TestEndScan(source);
    }

    [Fact]
    public void ReadBitWithRefill()
    {
        byte[] source = [0, 0, 0, 0, 0, 0, 0, 0, 0, Constants.JpegMarkerStartByte, 0xD8];
        var frameInfo = new FrameInfo(1, 1, 8, 1);
        DefaultParameters.IsValid(255, 0, out var cp);
        var codingParameters = new CodingParameters();

        var scanDecoder = new TestScanDecoder(frameInfo, cp, codingParameters);
        scanDecoder.TestInitialize(source);

        for (int i = 0; i < 9 * 8; i++)
        {
            var actual = scanDecoder.TestReadBit(source);
            Assert.False(actual);
        }

        scanDecoder.TestEndScan(source);
    }

    [Fact]
    public void PeekByte()
    {
        byte[] source = [7, 8, Constants.JpegMarkerStartByte, 0xD8];
        var frameInfo = new FrameInfo(1, 1, 8, 1);
        DefaultParameters.IsValid(255, 0, out var cp);
        var codingParameters = new CodingParameters();

        var scanDecoder = new TestScanDecoder(frameInfo, cp, codingParameters);
        scanDecoder.TestInitialize(source);

        byte peekByte1 = scanDecoder.TestPeekByte(source);
        for (int i = 0; i < 1 * 8; i++)
        {
            var actual = scanDecoder.TestReadBit(source);
        }
        byte peekByte2 = scanDecoder.TestPeekByte(source);
        for (int i = 0; i < 1 * 8; i++)
        {
            var actual = scanDecoder.TestReadBit(source);
        }
        scanDecoder.TestEndScan(source);

        Assert.Equal(7, peekByte1);
        Assert.Equal(8, peekByte2);
    }
}
