// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed;

internal static class Constants
{
    internal const int DefaultResetThreshold = 64; // Default RESET value as defined in ISO/IEC 14495-1, table C.2

    internal const int MinimumComponentCount = 1;
    internal const int MaximumComponentCount = 255;
    internal const int MaximumComponentCountInScan = 4;
    internal const int MinimumComponentIndex = 0;
    internal const int MaximumComponentIndex = MaximumComponentCount - 1;
    internal const int MinimumBitsPerSample = 2;
    internal const int MaximumBitsPerSample = 16;
    internal const int MinimumNearLossless = 0;
    internal const int MaximumNearLossless = 255;
    internal const int MinimumApplicationDataId = 0;
    internal const int MaximumApplicationDataId = 15;

    // The following limits for mapping tables are defined in ISO/IEC 14495-1, C.2.4.1.2, table C.4.
    internal const int MinimumMappingTableId = 1;
    internal const int MaximumMappingTableId = 255;
    internal const int MinimumMappingEntrySize = 1;
    internal const int MaximumMappingEntrySize = 255;

    internal const int AutoCalculateStride = 0;
    internal const byte JpegMarkerStartByte = 0xFF;

    // ISO/IEC 14495-1, section 4.8.1 defines the SPIFF version numbers to be used for the SPIFF header in combination with
    // JPEG-LS.
    internal const byte SpiffMajorRevisionNumber = 2;
    internal const byte SpiffMinorRevisionNumber = 0;

    internal const byte SpiffEndOfDirectoryEntryType = 1;

    internal const byte JpegRestartMarkerBase = 0xD0; // RSTm: Marks the next restart interval (range is D0 to D7)
    internal const int JpegRestartMarkerRange = 8;

    // The size in bytes of the segment length field.
    internal const int SegmentLengthSize = sizeof(ushort);

    // The size of a SPIFF header when serialized to a JPEG byte stream.
    internal const int SpiffHeaderSizeInBytes = 34;

    // The maximum size of the data bytes that fit in a segment.
    internal const int SegmentMaxDataSize = ushort.MaxValue - SegmentLengthSize;

    internal const int Int32BitCount = 32;
}
