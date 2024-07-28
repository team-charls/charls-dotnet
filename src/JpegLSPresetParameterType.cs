// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

public enum JpegLSPresetParametersType
{
    PresetCodingParameters = 0x1,    // JPEG-LS Baseline (ISO/IEC 14495-1): Preset coding parameters.
    MappingTableSpecification = 0x2, // JPEG-LS Baseline (ISO/IEC 14495-1): Mapping table specification.
    MappingTableContinuation = 0x3,  // JPEG-LS Baseline (ISO/IEC 14495-1): Mapping table continuation.
    ExtendedWidthAndHeight = 0x4, // JPEG-LS Baseline (ISO/IEC 14495-1): X and Y parameters greater than 16 bits are defined.
}
