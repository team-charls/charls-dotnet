// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace CharLS.Managed;

internal class ThrowHelper
{
    [DoesNotReturn]
    internal static void ThrowArgumentOutOfRangeException(ErrorCode errorCode, string? paramName = null)
    {
        throw AddErrorCode(new ArgumentOutOfRangeException(paramName, GetErrorMessage(errorCode)), errorCode);
    }

    internal static void ThrowArgumentOutOfRangeExceptionIfFalse(bool condition, ErrorCode errorCode, string? paramName = null)
    {
        if (condition)
            return;

        ThrowArgumentOutOfRangeException(errorCode, paramName);
    }

    [DoesNotReturn]
    internal static void ThrowArgumentException(ErrorCode errorCode)
    {
        throw AddErrorCode(new ArgumentException(GetErrorMessage(errorCode)), errorCode);
    }

    internal static void ThrowIfNegativeOrZero<T>(T value, ErrorCode errorCode, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : INumberBase<T>
    {
        if (T.IsNegative(value) || T.IsZero(value))
            throw AddErrorCode(new ArgumentOutOfRangeException(paramName, GetErrorMessage(errorCode)), errorCode);
    }

    internal static void ThrowIfOutsideRange<T>(T min, T max, T value, ErrorCode errorCode = ErrorCode.InvalidArgument, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IBinaryInteger<T>
    {
        if (value < min || value > max)
            throw AddErrorCode(new ArgumentOutOfRangeException(paramName, GetErrorMessage(errorCode)), errorCode);
    }

    internal static void ThrowInvalidOperationIfFalse(bool condition)
    {
        if (condition)
            return;

        const ErrorCode errorCode = ErrorCode.InvalidOperation;
        throw AddErrorCode(new InvalidOperationException(GetErrorMessage(errorCode)), errorCode);
    }

    internal static void ThrowArgumentExceptionIfFalse(bool value, string? paramName = null, ErrorCode errorCode = ErrorCode.InvalidArgument)
    {
        if (value)
            return;

        throw AddErrorCode(new ArgumentException(GetErrorMessage(errorCode), paramName), errorCode);
    }

    [DoesNotReturn]
    internal static void ThrowInvalidDataException(ErrorCode errorCode)
    {
        throw CreateInvalidDataException(errorCode);
    }

    internal static InvalidDataException CreateInvalidDataException(ErrorCode errorCode)
    {
        return AddErrorCode(new InvalidDataException(GetErrorMessage(errorCode)), errorCode);
    }

    internal static InvalidDataException CreateInvalidDataException(ErrorCode errorCode, Exception innerException)
    {
        return AddErrorCode(new InvalidDataException(GetErrorMessage(errorCode), innerException), errorCode);
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
            ErrorCode.None => "",
            ErrorCode.NeedMoreData => "The source is too small, more input data was expected",
            ErrorCode.CallbackFailed => "Callback function returned a failure",
            ErrorCode.DestinationTooSmall => "The destination buffer is too small to hold all the output",
            ErrorCode.EncodingNotSupported => "Invalid JPEG-LS stream: the JPEG stream is not encoded with the JPEG-LS algorithm",
            ErrorCode.JpegLSPresetExtendedParameterTypeNotSupported => "Unsupported JPEG-LS stream: JPEG-LS preset parameters segment contains a JPEG-LS Extended (ISO/IEC 14495-2) type",
            ErrorCode.JpegMarkerStartByteNotFound => "Invalid JPEG-LS stream: first JPEG marker is not a Start Of Image (SOI) marker",
            ErrorCode.DuplicateStartOfFrameMarker => "Invalid JPEG-LS stream: more then one Start Of Frame (SOF) marker",
            ErrorCode.DuplicateStartOfImageMarker => "Invalid JPEG-LS stream: more then one Start Of Image (SOI) marker",
            ErrorCode.UnexpectedEndOfImageMarker => "Invalid JPEG-LS stream: unexpected End Of Image (EOI) marker",
            ErrorCode.UnexpectedMarkerFound => "Invalid JPEG-LS stream: unexpected marker found",
            ErrorCode.UnexpectedRestartMarker => "Invalid JPEG-LS stream: restart (RTSm) marker found outside encoded entropy data",
            ErrorCode.MissingEndOfSpiffDirectory => "Invalid JPEG-LS stream: SPIFF header without End Of Directory (EOD) entry",
            ErrorCode.DuplicateComponentIdInStartOfFrameSegment => "Invalid JPEG-LS stream: duplicate component identifier in the (SOF) segment",
            ErrorCode.InvalidData => "Invalid JPEG-LS stream, the encoded bit stream contains a general structural problem",
            ErrorCode.RestartMarkerNotFound => "Invalid JPEG-LS stream: missing expected restart (RTSm) marker",
            ErrorCode.InvalidMarkerSegmentSize => "Invalid JPEG-LS stream: segment size of a marker segment is invalid",
            ErrorCode.InvalidParameterNearLossless => "Invalid JPEG-LS stream: near-lossless is outside the range [0, min(255, MAXVAL/2)]",
            ErrorCode.InvalidParameterInterleaveMode => "Invalid JPEG-LS stream: interleave mode is outside the range [0, 2] or conflicts with component count",
            ErrorCode.InvalidOperation => "Method call is invalid for the current state",
            ErrorCode.InvalidArgumentHeight => "The height argument is outside the supported range [1, 2147483647]",
            ErrorCode.InvalidArgumentWidth => "The width argument is outside the supported range [1, 2147483647]",
            ErrorCode.InvalidArgumentBitsPerSample => "The bit per sample argument is outside the range [2, 16]",
            ErrorCode.InvalidArgumentInterleaveMode => "The interleave mode is not None, Sample, Line or invalid in combination with component count",
            ErrorCode.InvalidArgumentNearLossless => "The near lossless argument is outside the range [0, min(255, MAXVAL/2)]",
            ErrorCode.InvalidArgumentPresetCodingParameters => "The argument for the JPEG-LS preset coding parameters is not valid",
            ErrorCode.InvalidArgumentColorTransformation => "The argument for the color component is not (None, Hp1, Hp2, Hp3) or invalid in combination with component count",
            ErrorCode.InvalidArgumentSize => "The passed size is outside the valid range",
            ErrorCode.InvalidArgumentStride => "The stride argument does not match with the frame info and buffer size",
            ErrorCode.InvalidArgumentEncodingOptions => "The encoding options argument has an invalid value",
            _ => GetErrorMessageForInvalidArgument(errorCode)
        };

        static string GetErrorMessageForInvalidArgument(ErrorCode errorCode)
        {
            Debug.Assert(errorCode == ErrorCode.InvalidArgument);
            return "Invalid argument";
        }
    }
}
