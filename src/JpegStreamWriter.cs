// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Buffers.Binary;
using System.Diagnostics;

namespace CharLS.Managed;

internal struct JpegStreamWriter
{
    private int _position;
    private int _componentIndex;
    private byte[]? _tableIds;

    private byte MappingTableSelector => (byte)(_tableIds == null ? 0 : _tableIds[_componentIndex]);

    internal Memory<byte> Destination { get; set; }

    // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
    internal int BytesWritten => _position;

    internal void WriteStartOfImage()
    {
        WriteSegmentWithoutData(JpegMarkerCode.StartOfImage);
    }

    /// <summary>
    /// Write a JPEG SPIFF (APP8 + spiff) segment.
    /// This segment is documented in ISO/IEC 10918-3, Annex F.
    /// </summary>
    internal void WriteSpiffHeaderSegment(SpiffHeader header)
    {
        Debug.Assert(header.Height > 0);
        Debug.Assert(header.Width > 0);

        Span<byte> spiffMagicId = [(byte)'S', (byte)'P', (byte)'I', (byte)'F', (byte)'F', (byte)'\0'];

        // Create a JPEG APP8 segment in Still Picture Interchange File Format (SPIFF), v2.0
        WriteSegmentHeader(JpegMarkerCode.ApplicationData8, 30);
        WriteBytes(spiffMagicId);
        WriteByte(Constants.SpiffMajorRevisionNumber);
        WriteByte(Constants.SpiffMinorRevisionNumber);
        WriteByte((byte)header.ProfileId);
        WriteByte((byte)header.ComponentCount);
        WriteUint32(header.Height);
        WriteUint32(header.Width);
        WriteByte((byte)header.ColorSpace);
        WriteByte((byte)header.BitsPerSample);
        WriteByte((byte)header.CompressionType);
        WriteByte((byte)header.ResolutionUnit);
        WriteUint32(header.VerticalResolution);
        WriteUint32(header.HorizontalResolution);
    }

    internal void WriteSpiffDirectoryEntry(int entryTag, ReadOnlySpan<byte> entryData)
    {
        WriteSegmentHeader(JpegMarkerCode.ApplicationData8, sizeof(int) + entryData.Length);
        WriteUint32(entryTag);
        WriteBytes(entryData);
    }

    internal void WriteSpiffEndOfDirectoryEntry()
    {
        // Note: ISO/IEC 10918-3, Annex F.2.2.3 documents that the EOD entry segment should have a length of 8
        // but only 6 data bytes. This approach allows to wrap existing bit streams\encoders with a SPIFF header.
        // In this implementation the SOI marker is added as data bytes to simplify the stream writer design.
        Span<byte> spiffEndOfDirectory =
        [
            0, 0,
            0, Constants.SpiffEndOfDirectoryEntryType,
            0xFF, (byte) JpegMarkerCode.StartOfImage
        ];

        WriteSegment(JpegMarkerCode.ApplicationData8, spiffEndOfDirectory);
    }

    internal void WriteColorTransformSegment(ColorTransformation colorTransformation)
    {
        byte[] segment = [(byte)'m', (byte)'r', (byte)'f', (byte)'x', (byte)colorTransformation];
        WriteSegment(JpegMarkerCode.ApplicationData8, segment);
    }

    internal void WriteCommentSegment(ReadOnlySpan<byte> comment)
    {
        WriteSegment(JpegMarkerCode.Comment, comment);
    }

    internal void WriteApplicationDataSegment(int applicationDataId, ReadOnlySpan<byte> applicationData)
    {
        Debug.Assert(applicationDataId is >= Constants.MinimumApplicationDataId and <= Constants.MaximumApplicationDataId);
        WriteSegment(JpegMarkerCode.ApplicationData0 + applicationDataId, applicationData);
    }

    internal void WriteJpegLSPresetParametersSegment(JpegLSPresetCodingParameters presetCodingParameters)
    {
        WriteSegmentHeader(JpegMarkerCode.JpegLSPresetParameters, 1 + 5 * sizeof(ushort));
        WriteByte((byte)JpegLSPresetParametersType.PresetCodingParameters);
        WriteUint16(presetCodingParameters.MaximumSampleValue);
        WriteUint16(presetCodingParameters.Threshold1);
        WriteUint16(presetCodingParameters.Threshold2);
        WriteUint16(presetCodingParameters.Threshold3);
        WriteUint16(presetCodingParameters.ResetValue);
    }

    internal void WriteJpegLSPresetParametersSegment(int tableId, int entrySize,
        ReadOnlySpan<byte> tableData)
    {
        // Write the first 65530 bytes as mapping table specification LSE segment.
        const int maxTableDataSize = Constants.SegmentMaxDataSize - 3;

        int tableSizeToWrite = Math.Min(tableData.Length, maxTableDataSize);
        WriteJpegLSPresetParametersSegment(JpegLSPresetParametersType.MappingTableSpecification, tableId,
            entrySize,
            tableData[..tableSizeToWrite]);

        // Write the remaining bytes as mapping table continuation LSE segments.
        int tablePosition = tableSizeToWrite;
        while (tablePosition < tableData.Length)
        {
            tableSizeToWrite = Math.Min(tableData.Length - tablePosition, maxTableDataSize);
            WriteJpegLSPresetParametersSegment(JpegLSPresetParametersType.MappingTableContinuation, tableId,
                entrySize, tableData[tablePosition..]);
            tablePosition += tableSizeToWrite;
        }
    }

    internal void WriteJpegLSPresetParametersSegment(int height, int width)
    {
        // Format is defined in ISO/IEC 14495-1, C.2.4.1.4
        WriteSegmentHeader(JpegMarkerCode.JpegLSPresetParameters, 1 + 1 + 2 * sizeof(uint));
        WriteByte((byte)JpegLSPresetParametersType.OversizeImageDimension);
        WriteByte(sizeof(uint)); // Wxy: number of bytes used to represent Ye and Xe [2..4]. Always 4 for simplicity.
        WriteUint32(height);     // Ye: number of lines in the image.
        WriteUint32(width);      // Xe: number of columns in the image.
    }

    internal bool WriteStartOfFrameSegment(FrameInfo frameInfo)
    {
        // Create a Frame Header as defined in ISO/IEC 14495-1, C.2.2 and T.81, B.2.2
        int dataSize = 6 + frameInfo.ComponentCount * 3;
        WriteSegmentHeader(JpegMarkerCode.StartOfFrameJpegLS, dataSize);
        WriteByte((byte)frameInfo.BitsPerSample); // P = Sample precision

        bool oversizedImage = frameInfo.Width > ushort.MaxValue || frameInfo.Height > ushort.MaxValue;
        WriteUint16(oversizedImage ? 0 : frameInfo.Height); // Y = Number of lines
        WriteUint16(oversizedImage ? 0 : frameInfo.Width);  // X = Number of samples per line

        // Components
        WriteByte((byte)frameInfo.ComponentCount); // Nf = Number of image components in frame

        // Use by default 1 as the start component identifier to remain compatible with the
        // code sample of ISO/IEC 14495-1, H.4 and the JPEG-LS ISO conformance sample files.
        for (int componentId = 1; componentId <= frameInfo.ComponentCount; ++componentId)
        {
            // Component Specification parameters
            WriteByte((byte)componentId); // Ci = Component identifier
            WriteByte(0x11);              // Hi + Vi = Horizontal sampling factor + Vertical sampling factor
            WriteByte(0);                 // Tqi = Quantization table destination selector (reserved for JPEG-LS, should be set to 0)
        }

        return oversizedImage;
    }

    internal void WriteStartOfScanSegment(int componentCount, int nearLossless, InterleaveMode interleaveMode)
    {
        Debug.Assert(componentCount is > 0 and <= byte.MaxValue);
        Debug.Assert(nearLossless is >= 0 and <= byte.MaxValue);
        Debug.Assert(interleaveMode.IsValid());

        // Create a Scan Header as defined in T.87, C.2.3 and T.81, B.2.3
        WriteSegmentHeader(JpegMarkerCode.StartOfScan, 1 + (componentCount * 2) + 3);
        WriteByte((byte)componentCount);

        for (int i = 0; i != componentCount; ++i)
        {
            WriteByte((byte)(_componentIndex + 1)); // follow the JPEG-LS standard samples and start with component ID 1.
            WriteByte(MappingTableSelector);
            ++_componentIndex;
        }

        WriteByte((byte)nearLossless);   // NEAR parameter
        WriteByte((byte)interleaveMode); // ILV parameter
        WriteByte(0);                    // transformation
    }

    internal void WriteEndOfImage(bool evenDestinationSize)
    {
        if (evenDestinationSize && BytesWritten % 2 != 0)
        {
            // Write an additional 0xFF byte to ensure that the encoded bit stream has an even size.
            WriteByte(Constants.JpegMarkerStartByte);
        }

        WriteSegmentWithoutData(JpegMarkerCode.EndOfImage);
    }

    internal Memory<byte> GetRemainingDestination()
    {
        return Destination[_position..];
    }

    internal void AdvancePosition(int byteCount)
    {
        Debug.Assert(byteCount >= 0);
        Debug.Assert(_position + byteCount <= Destination.Length);
        _position += byteCount;
    }

    internal void Rewind()
    {
        _position = 0;
        _componentIndex = 0;
    }

    internal void SetTableId(int componentIndex, int tableId)
    {
        Debug.Assert(componentIndex < Constants.MaximumComponentCount);
        Debug.Assert(tableId is >= 0 and <= Constants.MaximumTableId);

        // Usage of mapping tables is rare: use lazy initialization.
        _tableIds ??= new byte[Constants.MaximumComponentCount];

        _tableIds[componentIndex] = (byte)tableId;
    }

    private void WriteJpegLSPresetParametersSegment(JpegLSPresetParametersType presetParametersType,
    int tableId, int entrySize, ReadOnlySpan<byte> tableData)
    {
        Debug.Assert(presetParametersType is JpegLSPresetParametersType.MappingTableSpecification or JpegLSPresetParametersType.MappingTableContinuation);
        Debug.Assert(tableId > 0);
        Debug.Assert(entrySize > 0);
        Debug.Assert(tableData.Length >= entrySize); // Need to contain at least 1 entry.
        Debug.Assert(tableData.Length <= Constants.SegmentMaxDataSize - 3);

        // Format is defined in ISO/IEC 14495-1, C.2.4.1.2 and C.2.4.1.3
        WriteSegmentHeader(JpegMarkerCode.JpegLSPresetParameters, 1 + 1 + 1 + tableData.Length);
        WriteByte((byte)presetParametersType);
        WriteByte((byte)tableId);
        WriteByte((byte)entrySize);
        WriteBytes(tableData);
    }

    private void WriteSegmentWithoutData(JpegMarkerCode markerCode)
    {
        if (_position + 2 > Destination.Length)
            ThrowHelper.ThrowArgumentOutOfRangeException(ErrorCode.DestinationBufferTooSmall);

        WriteByte(Constants.JpegMarkerStartByte);
        WriteByte((byte)markerCode);
    }

    private void WriteSegment(JpegMarkerCode markerCode, ReadOnlySpan<byte> data)
    {
        WriteSegmentHeader(markerCode, data.Length);
        WriteBytes(data);
    }

    private void WriteSegmentHeader(JpegMarkerCode markerCode, int dataSize)
    {
        Debug.Assert(dataSize <= Constants.SegmentMaxDataSize);

        // Check if there is enough room in the destination to write the complete segment.
        // Other methods assume that the checking in done here and don't check again.
        const int markerCodeSize = 2;
        int totalSegmentSize = markerCodeSize + Constants.SegmentLengthSize + dataSize;
        if (_position + totalSegmentSize > Destination.Length)
            ThrowHelper.ThrowArgumentOutOfRangeException(ErrorCode.DestinationBufferTooSmall);

        WriteMarker(markerCode);
        WriteUint16(Constants.SegmentLengthSize + dataSize);
    }

    private void WriteMarker(JpegMarkerCode markerCode)
    {
        WriteByte(Constants.JpegMarkerStartByte);
        WriteByte((byte)markerCode);
    }

    private void WriteByte(byte value)
    {
        Destination.Span[_position++] = value;
    }

    private void WriteBytes(ReadOnlySpan<byte> data)
    {
        data.CopyTo(Destination.Span[_position..]);
        _position += data.Length;
    }

    private void WriteUint16(int value)
    {
        Debug.Assert(value is >= 0 and <= ushort.MaxValue);
        BinaryPrimitives.WriteUInt16BigEndian(Destination.Span[_position..], (ushort)value);
        _position += sizeof(ushort);
    }

    private void WriteUint32(int value)
    {
        Debug.Assert(value >= 0);
        BinaryPrimitives.WriteUInt32BigEndian(Destination.Span[_position..], (uint)value);
        _position += sizeof(uint);
    }
}
