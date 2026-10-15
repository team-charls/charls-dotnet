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

    public nuint TestReadBit()
    {
        return _scanDecoder.ReadBit();
    }

    public void TestEndScan()
    {
        _scanDecoder.EndScan();
    }

    public int TestPeekByte()
    {
        return _scanDecoder.PeekByte();
    }

    public int ReadUnaryCode()
    {
        return _scanDecoder.ReadUnaryCode();
    }
}

public class ScanDecoderTest
{
    private static readonly JpegLSPresetCodingParameters DefaultParameters = new();

    [Fact]
    public void ReadBit()
    {
        byte[] source = [0, 0, Constants.JpegMarkerStartByte, 0xD8];
        FrameInfo frameInfo = new(1, 1, 8, 1);
        _ = DefaultParameters.TryMakeExplicit(255, 0, out var cp);
        CodingParameters codingParameters = new();

        TestScanDecoder scanDecoder = new(frameInfo, cp!, codingParameters);
        scanDecoder.TestInitialize(source);

        for (int i = 0; i < 2 * 8; i++)
        {
            var actual = scanDecoder.TestReadBit();
            Assert.Equal(0UL, actual);
        }

        scanDecoder.TestEndScan();
    }

    [Fact]
    public void ReadBitWithRefill()
    {
        byte[] source = [0, 0, 0, 0, 0, 0, 0, 0, 0, Constants.JpegMarkerStartByte, 0xD8];
        FrameInfo frameInfo = new(1, 1, 8, 1);
        _ = DefaultParameters.TryMakeExplicit(255, 0, out var cp);
        CodingParameters codingParameters = new();

        TestScanDecoder scanDecoder = new(frameInfo, cp!, codingParameters);
        scanDecoder.TestInitialize(source);

        for (int i = 0; i < 9 * 8; i++)
        {
            var actual = scanDecoder.TestReadBit();
            Assert.Equal(0UL, actual);
        }

        scanDecoder.TestEndScan();
    }

    [Fact]
    public void PeekByte()
    {
        byte[] source = [7, 8, Constants.JpegMarkerStartByte, 0xD8];
        FrameInfo frameInfo = new(1, 1, 8, 1);
        _ = DefaultParameters.TryMakeExplicit(255, 0, out var cp);
        CodingParameters codingParameters = new();

        TestScanDecoder scanDecoder = new(frameInfo, cp!, codingParameters);
        scanDecoder.TestInitialize(source);

        int peekByte1 = scanDecoder.TestPeekByte();
        for (int i = 0; i < 1 * 8; i++)
        {
            _ = scanDecoder.TestReadBit();
        }
        int peekByte2 = scanDecoder.TestPeekByte();
        for (int i = 0; i < 1 * 8; i++)
        {
            _ = scanDecoder.TestReadBit();
        }
        scanDecoder.TestEndScan();

        Assert.Equal(7, peekByte1);
        Assert.Equal(8, peekByte2);
    }

    [Fact]
    public void ReadUnaryCodeWithPrefixLargerThanBufferThrowsInvalidData()
    {
        FrameInfo frameInfo = new(1, 1, 8, 1);
        CodingParameters codingParameters = new();
        _ = DefaultParameters.TryMakeExplicit(255, 0, out var cp);

        byte[] source = [0];
        TestScanDecoder scanDecoder = new(frameInfo, cp!, codingParameters);
        scanDecoder.TestInitialize(source);

        var exception = Assert.Throws<InvalidDataException>(() => _ = scanDecoder.ReadUnaryCode());

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.InvalidData, exception.GetErrorCode());
    }
}
