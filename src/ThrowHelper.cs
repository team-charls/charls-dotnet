// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace CharLS.Managed;

internal static class ThrowHelper
{
    [DoesNotReturn]
    internal static void ThrowArgumentOutOfRangeException(ErrorCode errorCode, string? paramName = null)
    {
        throw AddErrorCode(new ArgumentOutOfRangeException(paramName, GetErrorMessage(errorCode)), errorCode);
    }

    [DoesNotReturn]
    internal static void ThrowArgumentException(ErrorCode errorCode, string? paramName = null)
    {
        throw AddErrorCode(new ArgumentException(GetErrorMessage(errorCode), paramName), errorCode);
    }

    internal static void ThrowArgumentOutOfRangeExceptionIfFalse([DoesNotReturnIf(false)] bool condition, ErrorCode errorCode, string? paramName = null)
    {
        if (condition)
            return;

        ThrowArgumentOutOfRangeException(errorCode, paramName);
    }

    internal static void ThrowIfNegativeOrZero<T>(T value, ErrorCode errorCode, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : INumberBase<T>
    {
        if (T.IsNegative(value) || T.IsZero(value))
            ThrowArgumentOutOfRangeException(errorCode, paramName);
    }

    internal static void ThrowIfOutsideRange<T>(T min, T max, T value, ErrorCode errorCode = ErrorCode.InvalidArgument, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IBinaryInteger<T>
    {
        if (value < min || value > max)
            ThrowArgumentOutOfRangeException(errorCode, paramName);
    }

    internal static void ThrowInvalidOperationIfFalse([DoesNotReturnIf(false)] bool condition)
    {
        if (condition)
            return;

        ThrowInvalidOperationException();
    }

    internal static void ThrowArgumentExceptionIfFalse([DoesNotReturnIf(false)] bool value, string? paramName = null, ErrorCode errorCode = ErrorCode.InvalidArgument)
    {
        if (value)
            return;

        ThrowArgumentException(errorCode, paramName);
    }

    [DoesNotReturn]
    internal static void ThrowInvalidDataException(ErrorCode errorCode)
    {
        throw CreateInvalidDataException(errorCode);
    }

    internal static ArgumentException CreateArgumentException(ErrorCode errorCode)
    {
        return AddErrorCode(new ArgumentException(GetErrorMessage(errorCode)), errorCode);
    }

    internal static InvalidDataException CreateInvalidDataException(ErrorCode errorCode)
    {
        return AddErrorCode(new InvalidDataException(GetErrorMessage(errorCode)), errorCode);
    }

    internal static InvalidDataException CreateInvalidDataException(ErrorCode errorCode, Exception innerException)
    {
        return AddErrorCode(new InvalidDataException(GetErrorMessage(errorCode), innerException), errorCode);
    }

    [DoesNotReturn]
    private static void ThrowInvalidOperationException()
    {
        const ErrorCode errorCode = ErrorCode.InvalidOperation;
        throw AddErrorCode(new InvalidOperationException(GetErrorMessage(errorCode)), errorCode);
    }

    private static T AddErrorCode<T>(T exception, ErrorCode errorCode)
        where T : Exception
    {
        exception.AddErrorCode(errorCode);
        return exception;
    }

    private static string GetErrorMessage(ErrorCode errorCode)
    {
        return errorCode switch
        {
            ErrorCode.None => string.Empty,
            ErrorCode.CallbackFailed => "Callback function returned a failure",
            ErrorCode.DestinationTooSmall => "The destination buffer is too small to hold all the output",
            ErrorCode.NeedMoreData => "The source is too small, more input data was expected",
            ErrorCode.InvalidData => "Invalid JPEG-LS stream, the encoded bit stream contains a general structural problem",
            ErrorCode.EncodingNotSupported => "Invalid JPEG-LS stream: the JPEG stream is not encoded with the JPEG-LS algorithm",
            ErrorCode.ParameterValueNotSupported => "The JPEG-LS stream is encoded with a parameter value that is not supported by this decoder",
            ErrorCode.ColorTransformNotSupported => "The HP color transform is not supported",
            ErrorCode.JpegLSPresetExtendedParameterTypeNotSupported => "Unsupported JPEG-LS stream: JPEG-LS preset parameters segment contains a JPEG-LS Extended (ISO/IEC 14495-2) type",
            ErrorCode.JpegMarkerStartByteNotFound => "Invalid JPEG-LS stream: first JPEG marker is not a Start Of Image (SOI) marker",
            ErrorCode.StartOfImageMarkerNotFound => "Invalid JPEG-LS stream: first JPEG marker is not a Start Of Image (SOI) marker",
            ErrorCode.InvalidSpiffHeader => "Invalid JPEG-LS stream: invalid SPIFF header",
            ErrorCode.UnknownJpegMarkerFound => "Invalid JPEG-LS stream: an unknown JPEG marker code was found",
            ErrorCode.UnexpectedMarkerFound => "Invalid JPEG-LS stream: unexpected marker found",
            ErrorCode.InvalidMarkerSegmentSize => "Invalid JPEG-LS stream: segment size of a marker segment is invalid",
            ErrorCode.DuplicateStartOfImageMarker => "Invalid JPEG-LS stream: more then one Start Of Image (SOI) marker",
            ErrorCode.DuplicateStartOfFrameMarker => "Invalid JPEG-LS stream: more then one Start Of Frame (SOF) marker",
            ErrorCode.DuplicateComponentIdInStartOfFrameSegment => "Invalid JPEG-LS stream: duplicate component identifier in the (SOF) segment",
            ErrorCode.UnexpectedEndOfImageMarker => "Invalid JPEG-LS stream: unexpected End Of Image (EOI) marker",
            ErrorCode.InvalidJpegLSPresetParameterType => "Invalid JPEG-LS stream: JPEG-LS preset parameters segment contains an invalid type",
            ErrorCode.MissingEndOfSpiffDirectory => "Invalid JPEG-LS stream: SPIFF header without End Of Directory (EOD) entry",
            ErrorCode.UnexpectedRestartMarker => "Invalid JPEG-LS stream: restart (RTSm) marker found outside encoded entropy data",
            ErrorCode.RestartMarkerNotFound => "Invalid JPEG-LS stream: missing expected restart (RTSm) marker",
            ErrorCode.EndOfImageMarkerNotFound => "Invalid JPEG-LS stream: missing expected restart (RTSm) marker",
            ErrorCode.UnknownComponentId => "Invalid JPEG-LS stream: unknown component ID in scan segment",
            ErrorCode.AbbreviatedFormatAndSpiffHeaderMismatch => "Invalid JPEG-LS stream: mapping tables without SOF but with spiff header",
            ErrorCode.InvalidParameterWidth => "Invalid JPEG-LS stream: the width (Number of samples per line) is already defined",
            ErrorCode.InvalidParameterHeight => "Invalid JPEG-LS stream: the height (Number of lines) is already defined",
            ErrorCode.InvalidParameterBitsPerSample => "Invalid JPEG-LS stream: the bit per sample (sample precision) parameter is not in the range [2, 16]",
            ErrorCode.InvalidParameterComponentCount => "Invalid JPEG-LS stream: component count in the SOF segment is outside the range [1, 255]",
            ErrorCode.InvalidParameterInterleaveMode => "Invalid JPEG-LS stream: interleave mode is outside the range [0, 2] or conflicts with component count",
            ErrorCode.InvalidParameterNearLossless => "Invalid JPEG-LS stream: near-lossless is outside the range [0, min(255, MAXVAL/2)]",
            ErrorCode.InvalidParameterJpegLSPresetParameters => "Invalid JPEG-LS stream: JPEG-LS preset parameters segment contains invalid values",
            ErrorCode.InvalidParameterColorTransformation => "Invalid JPEG-LS stream: Color transformation segment contains invalid values or frame info mismatch",
            ErrorCode.InvalidParameterMappingTableId => "Invalid JPEG-LS stream: mapping table ID outside valid range or duplicate",
            ErrorCode.InvalidParameterMappingTableContinuation => "Invalid JPEG-LS stream: mapping table continuation without matching mapping table specification",
            ErrorCode.InvalidOperation => "Method call is invalid for the current state",
            ErrorCode.InvalidArgument => "Invalid argument",
            ErrorCode.InvalidArgumentHeight => "The height argument is outside the supported range [1, 2147483647]",
            ErrorCode.InvalidArgumentWidth => "The width argument is outside the supported range [1, 2147483647]",
            ErrorCode.InvalidArgumentBitsPerSample => "The bit per sample argument is outside the range [2, 16]",
            ErrorCode.InvalidArgumentComponentCount => "The component count argument is outside the range [1, 255]",
            ErrorCode.InvalidArgumentInterleaveMode => "The interleave mode is not None, Sample, Line or invalid in combination with component count",
            ErrorCode.InvalidArgumentNearLossless => "The near lossless argument is outside the range [0, min(255, MAXVAL/2)]",
            ErrorCode.InvalidArgumentPresetCodingParameters => "The argument for the JPEG-LS preset coding parameters is not valid",
            ErrorCode.InvalidArgumentColorTransformation => "The argument for the color component is not (None, Hp1, Hp2, Hp3) or invalid in combination with component count",
            ErrorCode.InvalidArgumentSize => "The passed size of a buffer is outside the valid range",
            ErrorCode.InvalidArgumentStride => "The stride argument does not match with the frame info and buffer size",
            ErrorCode.InvalidArgumentEncodingOptions => "The encoding options argument has an invalid value",
            _ => string.Empty
        };
    }
}
