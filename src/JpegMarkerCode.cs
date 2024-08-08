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
    // The following markers are defined in ISO/IEC 10918-1 | ITU T.81 (general JPEG standard).

    /// <summary>SOI: Marks the start of an image.</summary>
    StartOfImage = 0xD8,

    /// <summary>EOI: Marks the end of an image.</summary>
    EndOfImage = 0xD9,

    /// <summary>SOS: Marks the start of scan.</summary>
    StartOfScan = 0xDA,

    /// <summary>DNL: Defines the number of lines in a scan.</summary>
    DefineNumberOfLines = 0xDC,

    /// <summary>DRI: Defines the restart interval used in succeeding scans.</summary>
    DefineRestartInterval = 0xDD,

    /// <summary>APP0: Application data 0: used for JFIF header.</summary>
    ApplicationData0 = 0xE0,

    /// <summary>APP1: Application data 1: used for EXIF or XMP header.</summary>
    ApplicationData1 = 0xE1,

    /// <summary>APP2: Application data 2: used for ICC profile.</summary>
    ApplicationData2 = 0xE2,

    /// <summary>APP3: Application data 3: used for meta info</summary>
    ApplicationData3 = 0xE3,

    /// <summary>APP4: Application data 4.</summary>
    ApplicationData4 = 0xE4,

    /// <summary>APP5: Application data 5.</summary>
    ApplicationData5 = 0xE5,

    /// <summary>APP6: Application data 6.</summary>
    ApplicationData6 = 0xE6,

    /// <summary>APP7: Application data 7: used for HP color-space info.</summary>
    ApplicationData7 = 0xE7,

    /// <summary>APP8: Application data 8: used for HP color-transformation info or SPIFF header.</summary>
    ApplicationData8 = 0xE8,

    /// <summary>APP9: Application data 9.</summary>
    ApplicationData9 = 0xE9,

    /// <summary>APP10: Application data 10.</summary>
    ApplicationData10 = 0xEA,

    /// <summary>APP11: Application data 11.</summary>
    ApplicationData11 = 0xEB,

    /// <summary>APP12: Application data 12: used for Picture info.</summary>
    ApplicationData12 = 0xEC,

    /// <summary>APP13: Application data 13: used by PhotoShop IRB</summary>
    ApplicationData13 = 0xED,

    /// <summary>APP14: Application data 14: used by Adobe</summary>
    ApplicationData14 = 0xEE,

    /// <summary>APP15: Application data 15.</summary>
    ApplicationData15 = 0xEF,

    /// <summary>COM: Comment block.</summary>
    Comment = 0xFE,

    // The following markers are defined in ISO/IEC 14495-1 | ITU T.87.

    /// <summary>SOF_55: Marks the start of a JPEG-LS encoded frame.</summary>
    StartOfFrameJpegLS = 0xF7,

    /// <summary>LSE: Marks the start of a JPEG-LS preset parameters segment.</summary>
    JpegLSPresetParameters = 0xF8,
}
