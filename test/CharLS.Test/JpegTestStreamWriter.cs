// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS.Test;

internal class JpegTestStreamWriter
{
    private readonly List<byte> _buffer = new();

    public int ComponentIdOverride { get; set; }

    public void WriteByte(byte value)
    {
        _buffer.Add(value);
    }

    public void WriteUint16(ushort value)
    {
        WriteByte((byte)(value / 0x100));
        WriteByte((byte)(value % 0x100));
    }

    public void WriteSegmentStart(JpegMarkerCode markerCode, int dataSize)
    {
        WriteMarker(markerCode);
        WriteUint16((ushort)(dataSize + 2));
    }

    public void WriteStartOfImage()
    {
        WriteMarker(JpegMarkerCode.StartOfImage);
    }

    public void WriteStartOfFrameSegment(int width, int height, int bitsPerSample, int componentCount)
    {
        // Create a Frame Header as defined in T.87, C.2.2 and T.81, B.2.2
        WriteSegmentStart(JpegMarkerCode.StartOfFrameJpegLS, 6 + (componentCount * 3));

        WriteByte((byte)bitsPerSample); // P = Sample precision
        WriteUint16((ushort)height);    // Y = Number of lines
        WriteUint16((ushort)width);     // X = Number of samples per line

        // Components
        WriteByte((byte)componentCount); // Nf = Number of image components in frame
        for (int componentId = 0; componentId < componentCount; componentId++)
        {
            // Component Specification parameters
            WriteByte(ComponentIdOverride == 0 ? (byte)componentId : (byte)ComponentIdOverride); // Ci = Component identifier
            WriteByte(0x11); // Hi + Vi = Horizontal sampling factor + Vertical sampling factor
            WriteByte(0); // Tqi = Quantization table destination selector (reserved for JPEG-LS, should be set to 0)
        }
    }

    public void WriteStartOfScanSegment(int componentId, int componentCount, int nearLossless, InterleaveMode interleaveMode)
    {
        //// Create a Scan Header as defined in T.87, C.2.3 and T.81, B.2.3
        WriteSegmentStart(JpegMarkerCode.StartOfScan, 1 + (componentCount * 2) + 3);

        WriteByte((byte)componentCount); // Nf = Number of components in scan
        for (int i = 0; i < componentCount; i++)
        {
            WriteByte((byte)componentId);
            WriteByte(0); // Mapping table selector (0 = no table)
            componentId++;
        }

        WriteByte((byte)nearLossless);// NEAR parameter
        WriteByte((byte)interleaveMode);// ILV parameter
        WriteByte(0); // transformation
    }

    public ReadOnlyMemory<byte> GetBuffer()
    {
        return _buffer.ToArray();
    }

    private void WriteMarker(JpegMarkerCode markerCode)
    {
        WriteByte(0xFF);
        WriteByte((byte)markerCode);
    }
}
