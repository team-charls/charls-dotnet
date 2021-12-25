// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

// JPEG Marker codes have the pattern 0xFFaa in a JPEG byte stream.
// The valid 'aa' options are defined by several ISO/IEC, ITU standards:
// 0x00, 0x01, 0xFE, 0xC0-0xDF are defined in ISO/IEC 10918-1, ITU T.81
// 0xF0 - 0xF6 are defined in ISO/IEC 10918-3 | ITU T.84: JPEG extensions
// 0xF7 - 0xF8 are defined in ISO/IEC 14495-1 | ITU T.87: JPEG LS baseline
// 0xF9         is defined in ISO/IEC 14495-2 | ITU T.870: JPEG LS extensions
// 0x4F - 0x6F, 0x90 - 0x93 are defined in ISO/IEC 15444-1: JPEG 2000

internal enum JpegMarkerCode
{
    StartOfImage = 0xD8,          // SOI: Marks the start of an image.
    EndOfImage = 0xD9,            // EOI: Marks the end of an image.
    StartOfScan = 0xDA,           // SOS: Marks the start of scan.
    DefineRestartInterval = 0xDD, // DRI: Defines the restart interval used in succeeding scans.

    // The following markers are defined in ISO/IEC 14495-1 | ITU T.87.
    StartOfFrameJpegLS = 0xF7,       // SOF_55: Marks the start of a JPEG-LS encoded frame.
    JpegLSPresetParameters = 0xF8,   // LSE:    Marks the start of a JPEG-LS preset parameters segment.
}
