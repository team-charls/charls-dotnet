// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed.Test;

internal sealed class TestScanDecoder
{
    private ScanDecoder _scanDecoder;

    internal TestScanDecoder(FrameInfo frameInfo, JpegLSPresetCodingParameters presetCodingParameters, CodingParameters codingParameters)
    {
        _scanDecoder = new ScanDecoder(frameInfo, presetCodingParameters, codingParameters);
    }

    public void TestInitialize(ReadOnlyMemory<byte> source)
    {
        _scanDecoder.Initialize(source);
    }

    public bool TestReadBit()
    {
        return _scanDecoder.ReadBit();
    }

    public void TestEndScan()
    {
        _scanDecoder.EndScan();
    }

    public byte TestPeekByte()
    {
        return _scanDecoder.PeekByte();
    }
}

public class ScanDecoderTest
{
    private static readonly JpegLSPresetCodingParameters DefaultParameters = new();

    [Fact]
    public void ReadBit()
    {
        byte[] source = [0, 0, Constants.JpegMarkerStartByte, 0xD8];
        var frameInfo = new FrameInfo(1, 1, 8, 1);
        _ = DefaultParameters.IsValid(255, 0, out var cp);
        var codingParameters = new CodingParameters();

        var scanDecoder = new TestScanDecoder(frameInfo, cp, codingParameters);
        scanDecoder.TestInitialize(source);

        for (int i = 0; i < 2 * 8; i++)
        {
            var actual = scanDecoder.TestReadBit();
            Assert.False(actual);
        }

        scanDecoder.TestEndScan();
    }

    [Fact]
    public void ReadBitWithRefill()
    {
        byte[] source = [0, 0, 0, 0, 0, 0, 0, 0, 0, Constants.JpegMarkerStartByte, 0xD8];
        var frameInfo = new FrameInfo(1, 1, 8, 1);
        _ = DefaultParameters.IsValid(255, 0, out var cp);
        var codingParameters = new CodingParameters();

        var scanDecoder = new TestScanDecoder(frameInfo, cp, codingParameters);
        scanDecoder.TestInitialize(source);

        for (int i = 0; i < 9 * 8; i++)
        {
            var actual = scanDecoder.TestReadBit();
            Assert.False(actual);
        }

        scanDecoder.TestEndScan();
    }

    [Fact]
    public void PeekByte()
    {
        byte[] source = [7, 8, Constants.JpegMarkerStartByte, 0xD8];
        var frameInfo = new FrameInfo(1, 1, 8, 1);
        _ = DefaultParameters.IsValid(255, 0, out var cp);
        var codingParameters = new CodingParameters();

        var scanDecoder = new TestScanDecoder(frameInfo, cp, codingParameters);
        scanDecoder.TestInitialize(source);

        byte peekByte1 = scanDecoder.TestPeekByte();
        for (int i = 0; i < 1 * 8; i++)
        {
            _ = scanDecoder.TestReadBit();
        }
        byte peekByte2 = scanDecoder.TestPeekByte();
        for (int i = 0; i < 1 * 8; i++)
        {
            _ = scanDecoder.TestReadBit();
        }
        scanDecoder.TestEndScan();

        Assert.Equal(7, peekByte1);
        Assert.Equal(8, peekByte2);
    }
}
