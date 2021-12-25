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

    ApplicationData0 = 0xE0,  // APP0:  Application data 0: used for JFIF header.
    ApplicationData1 = 0xE1,  // APP1:  Application data 1: used for EXIF or XMP header.
    ApplicationData2 = 0xE2,  // APP2:  Application data 2: used for ICC profile.
    ApplicationData3 = 0xE3,  // APP3:  Application data 3: used for meta info
    ApplicationData4 = 0xE4,  // APP4:  Application data 4.
    ApplicationData5 = 0xE5,  // APP5:  Application data 5.
    ApplicationData6 = 0xE6,  // APP6:  Application data 6.
    ApplicationData7 = 0xE7,  // APP7:  Application data 7: used for HP color-space info.
    ApplicationData8 = 0xE8,  // APP8:  Application data 8: used for HP color-transformation info or SPIFF header.
    ApplicationData9 = 0xE9,  // APP9:  Application data 9.
    ApplicationData10 = 0xEA, // APP10: Application data 10.
    ApplicationData11 = 0xEB, // APP11: Application data 11.
    ApplicationData12 = 0xEC, // APP12: Application data 12: used for Picture info.
    ApplicationData13 = 0xED, // APP13: Application data 13: used by PhotoShop IRB
    ApplicationData14 = 0xEE, // APP14: Application data 14: used by Adobe
    ApplicationData15 = 0xEF, // APP15: Application data 15.
}
