// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

internal enum JpegLSPresetParametersType
{
    /// <summary>
    /// JPEG-LS Baseline (ISO/IEC 14495-1): Preset coding parameters (defined in C.2.4.1.1).
    /// </summary>
    PresetCodingParameters = 0x1,

    /// <summary>
    /// JPEG-LS Baseline (ISO/IEC 14495-1): Mapping table specification (defined in C.2.4.1.2).
    /// </summary>
    MappingTableSpecification = 0x2,

    /// <summary>
    /// JPEG-LS Baseline (ISO/IEC 14495-1): Mapping table continuation (defined in C.2.4.1.3).
    /// </summary>
    MappingTableContinuation = 0x3,

    /// <summary>
    /// JPEG-LS Baseline (ISO/IEC 14495-1): X and Y parameters are defined (defined in C.2.4.1.4).
    /// </summary>
    OversizeImageDimension = 0x4
}
