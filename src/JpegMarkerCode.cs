// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

internal enum JpegMarkerCode
{
    StartOfImage = 0xD8,          // SOI: Marks the start of an image.
    EndOfImage = 0xD9,            // EOI: Marks the end of an image.
    StartOfScan = 0xDA,           // SOS: Marks the start of scan.
    DefineRestartInterval = 0xDD, // DRI: Defines the restart interval used in succeeding scans.

}
