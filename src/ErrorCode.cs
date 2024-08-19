// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed;

/// <summary>
/// Defines the error codes that can be used as additional information in the thrown exceptions.
/// </summary>
public enum ErrorCode
{
    /// <summary>
    /// The operation completed without errors.
    /// </summary>
    None = 0,

    // Runtime errors:

    /// <summary>
    /// This error is returned when an event handler threw an exception.
    /// </summary>
    CallbackFailed = 2,

    /// <summary>
    /// The destination buffer is too small to hold all the output.
    /// </summary>
    DestinationTooSmall = 3,

    /// <summary>
    /// The source buffer is too small, more input data was expected.
    /// </summary>
    NeedMoreData = 4,

    /// <summary>
    /// This error is returned when the encoded bit stream contains a general structural problem.
    /// </summary>
    InvalidData = 5,

    /// <summary>
    /// This error is returned when an encoded frame is found that is not encoded with the JPEG-LS algorithm.
    /// </summary>
    EncodingNotSupported = 6,

    /// <summary>
    /// The parameter value not supported.
    /// </summary>
    ParameterValueNotSupported = 7,

    /// <summary>
    /// The color transform is not supported.
    /// </summary>
    ColorTransformNotSupported = 8,

    /// <summary>
    /// This error is returned when the stream contains an unsupported type parameter in the JPEG-LS segment.
    /// </summary>
    JpegLSPresetExtendedParameterTypeNotSupported = 9,

    /// <summary>
    /// This error is returned when the algorithm expect a 0xFF code (indicates start of a JPEG marker) but none was found.
    /// </summary>
    JpegMarkerStartByteNotFound = 10,

    /// <summary>
    /// This error is returned when the first JPEG marker is not the SOI (Start Of Image) marker.
    /// </summary>
    StartOfImageMarkerNotFound = 11,

    /// <summary>
    /// This error is returned when the SPIFF header is invalid.
    /// </summary>
    InvalidSpiffHeader = 12,

    /// <summary>
    /// This error is returned when an unknown JPEG marker code is detected in the encoded bit stream.
    /// </summary>
    UnknownJpegMarkerFound = 13,

    /// <summary>
    /// This error is returned when a JPEG marker is found that is not valid for the current state.
    /// </summary>
    UnexpectedMarkerFound = 14,

    /// <summary>
    /// This error is returned when the segment size of a marker segment is invalid.
    /// </summary>
    InvalidMarkerSegmentSize = 15,

    /// <summary>
    /// This error is returned when the stream contains more than one SOI (Start Of Image) marker.
    /// </summary>
    DuplicateStartOfImageMarker = 16,

    /// <summary>
    /// This error is returned when the stream contains more than one SOF (Start Of Frame) marker.
    /// </summary>
    DuplicateStartOfFrameMarker = 17,

    /// <summary>
    /// This error is returned when the stream contains duplicate component identifiers in the SOF segment.
    /// </summary>
    DuplicateComponentIdInStartOfFrameSegment = 18,

    /// <summary>
    /// This error is returned when the stream contains an unexpected EOI marker.
    /// </summary>
    UnexpectedEndOfImageMarker = 19,

    /// <summary>
    /// This error is returned when the stream contains an invalid type parameter in the JPEG-LS segment.
    /// </summary>
    InvalidJpegLSPresetParameterType = 20,

    /// <summary>
    /// This error is returned when the stream contains a SPIFF header but not an SPIFF end-of-directory entry.
    /// </summary>
    MissingEndOfSpiffDirectory = 21,

    /// <summary>
    /// This error is returned when a restart marker is found outside the encoded entropy data.
    /// </summary>
    UnexpectedRestartMarker = 22,

    /// <summary>
    /// This error is returned when an expected restart marker is not found. It may indicate data corruption in the JPEG-LS
    /// byte stream.
    /// </summary>
    RestartMarkerNotFound = 23,

    /// <summary>
    /// This error is returned when the End of Image (EOI) marker could not be found.
    /// </summary>
    EndOfImageMarkerNotFound = 24,

    /// <summary>
    /// This error is returned when an unknown component ID in a scan is detected.
    /// </summary>
    UnknownComponentId = 25,

    /// <summary>
    /// This error is returned for stream with only mapping tables and a spiff header.
    /// </summary>
    AbbreviatedFormatAndSpiffHeader = 26,

    /// <summary>
    /// This error is returned when the width parameter is defined more than once in an incompatible way.
    /// </summary>
    InvalidParameterWidth = 27,

    /// <summary>
    /// This error is returned when the height parameter is defined more than once in an incompatible way.
    /// </summary>
    InvalidParameterHeight = 28,

    /// <summary>
    /// This error is returned when the stream contains a bits per sample (sample precision) parameter outside the range [2,16]
    /// </summary>
    InvalidParameterBitsPerSample = 29,

    /// <summary>
    /// This error is returned when the stream contains a component count parameter outside the range [1,255]
    /// </summary>
    InvalidParameterComponentCount = 30,

    /// <summary>
    /// This error is returned when the stream contains an interleave mode (ILV) parameter outside the range [0, 2]
    /// </summary>
    InvalidParameterInterleaveMode = 31,

    /// <summary>
    /// This error is returned when the stream contains a near-lossless (NEAR) parameter outside the range [0, min(255, MAXVAL/2)]
    /// </summary>
    InvalidParameterNearLossless = 32,

    /// <summary>
    /// This error is returned when the stream contains an invalid JPEG-LS preset parameters segment.
    /// </summary>
    InvalidParameterJpegLSPresetParameters = 33,

    /// <summary>
    /// This error is returned when the stream contains an invalid color transformation segment or one that doesn't match with frame info.
    /// </summary>
    InvalidParameterColorTransformation = 34,

    /// <summary>
    /// This error is returned when the stream contains a mapping table with an invalid ID.
    /// </summary>
    InvalidParameterMappingTableId = 35,

    /// <summary>
    /// This error is returned when the stream contains an invalid mapping table continuation.
    /// </summary>
    InvalidParameterMappingTableContinuation = 36,

    // Logic errors:

    /// <summary>
    /// This error is returned when a method call is invalid for the current state.
    /// </summary>
    InvalidOperation = 100,

    /// <summary>
    /// This error is returned when one of the arguments is invalid and no specific reason is available.
    /// </summary>
    InvalidArgument = 101,

    /// <summary>
    /// The argument for the width parameter is outside the range [1, 2147483647].
    /// </summary>
    InvalidArgumentWidth = 102,

    /// <summary>
    /// The argument for the height parameter is outside the range [1, 2147483647].
    /// </summary>
    InvalidArgumentHeight = 103,

    /// <summary>
    /// The argument for the bit per sample parameter is outside the range [2, 16].
    /// </summary>
    InvalidArgumentBitsPerSample = 104,

    /// <summary>
    /// The argument for the component count parameter is outside the range [1, 255].
    /// </summary>
    InvalidArgumentComponentCount = 105,

    /// <summary>
    /// The argument for the interleave mode is not (None, Sample, Line) or invalid in combination with component count.
    /// </summary>
    InvalidArgumentInterleaveMode = 106,

    /// <summary>
    /// The argument for the near lossless parameter is outside the range [0, 255].
    /// </summary>
    InvalidArgumentNearLossless = 107,

    /// <summary>
    /// The argument for the JPEG-LS preset coding parameters is not valid, see ISO/IEC 14495-1,
    /// C.2.4.1.1, Table C.1 for the ranges of valid values.
    /// </summary>
    InvalidArgumentPresetCodingParameters = 108,

    /// <summary>
    /// The argument for the color component is not (None, Hp1, Hp2, Hp3) or invalid in combination with component count.
    /// </summary>
    InvalidArgumentColorTransformation = 109,

    /// <summary>
    /// The argument for the size parameter is outside the valid range.
    /// </summary>
    InvalidArgumentSize = 110,

    /// <summary>
    /// The stride argument does not match with the frame info and buffer size.
    /// </summary>
    InvalidArgumentStride = 111,

    /// <summary>
    /// The encoding options argument has an invalid value.
    /// </summary>
    InvalidArgumentEncodingOptions = 112
}
