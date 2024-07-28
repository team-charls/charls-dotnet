// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Diagnostics;

namespace CharLS.JpegLS.Test;

internal sealed class JpegTestStreamWriter
{
    private readonly List<byte> _buffer = [];

    public int ComponentIdOverride { get; set; }

    public void WriteByte(byte value)
    {
        _buffer.Add(value);
    }

    public void WriteUint16(int value)
    {
        WriteByte((byte)(value / 0x100));
        WriteByte((byte)(value % 0x100));
    }

    public void WriteUint24(uint value)
    {
        WriteByte((byte)(value >> 16));
        WriteByte((byte)(value >> 8));
        WriteByte((byte)(value));
    }

    public void WriteUint32(uint value)
    {
        WriteByte((byte)(value >> 24));
        WriteByte((byte)(value >> 16));
        WriteByte((byte)(value >> 8));
        WriteByte((byte)(value));
    }

    public void WriteBytes(byte[] values)
    {
        _buffer.AddRange(values);
    }

    public void WriteSegmentStart(JpegMarkerCode markerCode, int dataSize)
    {
        WriteMarker(markerCode);
        WriteUint16((ushort)(dataSize + 2));
    }

    public void WriteSegment(JpegMarkerCode markerCode, byte[] values)
    {
        WriteSegmentStart(markerCode, values.Length);
        WriteBytes(values);
    }

    public void WriteStartOfImage()
    {
        WriteMarker(JpegMarkerCode.StartOfImage);
    }

    public void WriteSpiffEndOfDirectoryEntry()
    {
        WriteSegmentStart(JpegMarkerCode.ApplicationData8, 6);

        // Note: ISO/IEC 10918-3, Annex F.2.2.3 documents that the EOD entry segment should have a length of 8
        // but only 6 data bytes. This approach allows to wrap existing bit streams\encoders with a SPIFF header.
        // In this implementation the SOI marker is added as data bytes to simplify the design.
        const byte spiffEndOfDirectoryEntryType = 1;
        byte[] endOfDirectoryEntry =
        [
            0, 0,
            0, spiffEndOfDirectoryEntryType,
            0xFF, 0xD8
        ];

        WriteBytes(endOfDirectoryEntry);
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

    public void WriteStartOfScanSegment(int componentId, int componentCount, int nearLossless, JpegLSInterleaveMode jpegLSInterleaveMode)
    {
        // Create a Scan Header as defined in T.87, C.2.3 and T.81, B.2.3
        WriteSegmentStart(JpegMarkerCode.StartOfScan, 1 + (componentCount * 2) + 3);

        WriteByte((byte)componentCount); // Nf = Number of components in scan
        for (int i = 0; i < componentCount; i++)
        {
            WriteByte((byte)componentId);
            WriteByte(0); // Mapping table selector (0 = no table)
            componentId++;
        }

        WriteByte((byte)nearLossless);// NEAR parameter
        WriteByte((byte)jpegLSInterleaveMode);// ILV parameter
        WriteByte(0); // transformation
    }

    internal void WriteJpegLSPresetParametersSegment(JpegLSPresetCodingParameters presetCodingParameters)
    {
        WriteSegmentStart(JpegMarkerCode.JpegLSPresetParameters, 11);

        WriteByte((byte)JpegLSPresetParametersType.PresetCodingParameters);
        WriteUint16(presetCodingParameters.MaximumSampleValue);
        WriteUint16(presetCodingParameters.Threshold1);
        WriteUint16(presetCodingParameters.Threshold2);
        WriteUint16(presetCodingParameters.Threshold3);
        WriteUint16(presetCodingParameters.ResetValue);
    }

    internal void WriteDefineRestartInterval(uint restartInterval, int size)
    {
        WriteSegmentStart(JpegMarkerCode.DefineRestartInterval, size);

        switch (size)
        {
            case 2:
                WriteUint16((int)restartInterval);
                break;

            case 3:
                WriteUint24(restartInterval);
                break;

            case 4:
                WriteUint32(restartInterval);
                break;

            default:
                Debug.Fail("unreachable");
                break;
        }
    }

    internal void WriteRestartMarker(byte intervalIndex)
    {
        WriteMarker((JpegMarkerCode)(Constants.JpegRestartMarkerBase + intervalIndex));
    }

    public ReadOnlyMemory<byte> GetBuffer()
    {
        return _buffer.ToArray();
    }

    public List<byte> GetModifiableBuffer()
    {
        return _buffer;
    }

    private void WriteMarker(JpegMarkerCode markerCode)
    {
        WriteByte(0xFF);
        WriteByte((byte)markerCode);
    }
}
