// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS;

internal static class Constants
{
    internal const int AutoCalculateStride = 0;
    internal const byte JpegMarkerStartByte = 0xFF;
    internal const int MaximumNearLossless = 255;

    // ISO/IEC 14495-1, section 4.8.1 defines the SPIFF version numbers to be used for the SPIFF header in combination with
    // JPEG-LS.
    internal const byte SpiffMajorRevisionNumber = 2;
    internal const byte SpiffMinorRevisionNumber = 0;

    internal const byte SpiffEndOfDirectoryEntryType = 1;

    internal const byte JpegRestartMarkerBase = 0xD0; // RSTm: Marks the next restart interval (range is D0 to D7)
    internal const uint JpegRestartMarkerRange = 8;
}
