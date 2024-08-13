// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS;

internal static class Constants
{
    // Default threshold values for JPEG-LS statistical modeling as defined in ISO/IEC 14495-1, table C.3
    // for the case MAXVAL = 255 and NEAR = 0.
    internal const int DefaultThreshold1 = 3;  // BASIC_T1
    internal const int DefaultThreshold2 = 7;  // BASIC_T2
    internal const int DefaultThreshold3 = 21; // BASIC_T3

    internal const int DefaultResetThreshold = 64; // Default RESET value as defined in ISO/IEC 14495-1, table C.2

    internal const int MaximumComponentCount = 255;
    internal const int MinimumBitsPerSample = 2;
    internal const int MaximumBitsPerSample = 16;

    // The following limits for mapping tables are defined in ISO/IEC 14495-1, C.2.4.1.2, table C.4.
    internal const int MinimumTableId = 1;
    internal const int MaximumTableId = 255;
    internal const int MinimumEntrySize = 1;
    internal const int MaximumEntrySize = 255;

    internal const int AutoCalculateStride = 0;
    internal const byte JpegMarkerStartByte = 0xFF;
    internal const int MaximumNearLossless = 255;

    internal const int MaxKValue = 16; // This is an implementation limit (theoretical limit is 32)

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
