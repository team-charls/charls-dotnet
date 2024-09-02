// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed.Test;

public class ScanEncoderTest
{
    [Fact]
    public void AppendToBitStreamZeroLength()
    {
        FrameInfo frameInfo = new(1, 1, 8, 1);

        ScanEncoder scanEncoder = new(frameInfo, JpegLSPresetCodingParameters.Default, new CodingParameters());

        byte[] data = new byte[1024];
        scanEncoder.InitializeDestination(data);

        scanEncoder.AppendToBitStream(0, 0);
        scanEncoder.Flush();

        Assert.Equal(0, data[0]);
    }

    [Fact]
    public void AppendToBitStreamFFPattern()
    {
        FrameInfo frameInfo = new(1, 1, 8, 1);
        ScanEncoder scanEncoder = new(frameInfo, JpegLSPresetCodingParameters.Default, new CodingParameters());

        byte[] destination = new byte[1024];
        scanEncoder.InitializeDestination(destination);
        destination[13] = 0x77; // marker byte to detect overruns.

        // We want _isFFWritten == true.
        scanEncoder.AppendToBitStream(0, 24);
        scanEncoder.AppendToBitStream(0xff, 8);

        // We need the buffer filled with set bits.
        scanEncoder.AppendToBitStream(0xffff, 16);
        scanEncoder.AppendToBitStream(0xffff, 16);

        // Buffer is full of FFs and _isFFWritten = true: Flush can only write 30 date bits.
        scanEncoder.AppendToBitStream(0x3, 31);

        scanEncoder.Flush();

        // Verify output.
        Assert.Equal(0, destination[0]);
        Assert.Equal(0, destination[1]);
        Assert.Equal(0, destination[2]);
        Assert.Equal(0xFF, destination[3]);
        Assert.Equal(0x7F, destination[4]); // extra 0 bit.
        Assert.Equal(0xFF, destination[5]);
        Assert.Equal(0x7F, destination[6]); // extra 0 bit.
        Assert.Equal(0xFF, destination[7]);
        Assert.Equal(0x60, destination[8]);
        Assert.Equal(0, destination[9]);
        Assert.Equal(0, destination[10]);
        Assert.Equal(0, destination[11]);
        Assert.Equal(0xC0, destination[12]);
        Assert.Equal(0x77, destination[13]);
    }
}
