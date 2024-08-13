// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace CharLS.Managed;

internal class ThrowHelper
{
    [DoesNotReturn]
    internal static void ThrowArgumentOutOfRangeException(ErrorCode errorCode)
    {
        throw AddErrorCode(new ArgumentOutOfRangeException(GetErrorMessage(errorCode)), errorCode);
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

    internal static void ThrowIfOutsideRange<T>(T min, T max, T value, ErrorCode errorCode, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IBinaryInteger<T>
    {
        if (value < min || value > max)
            throw AddErrorCode(new ArgumentOutOfRangeException(paramName, GetErrorMessage(errorCode)), errorCode);
    }

    internal static void ThrowInvalidOperationIfFalse(bool value)
    {
        if (value)
            return;

        const ErrorCode errorCode = ErrorCode.InvalidOperation;
        throw AddErrorCode(new InvalidOperationException(GetErrorMessage(errorCode)), errorCode);
    }

    internal static void ThrowArgumentExceptionIfFalse(bool value, string? paramName)
    {
        if (value)
            return;

        const ErrorCode errorCode = ErrorCode.InvalidArgument;
        throw AddErrorCode(new ArgumentException(GetErrorMessage(errorCode), paramName), errorCode);
    }

    private static Exception AddErrorCode(Exception exception, ErrorCode errorCode)
    {
        exception.AddErrorCode(errorCode);
        return exception;
    }

    private static string GetErrorMessage(ErrorCode errorCode)
    {
        return errorCode switch
        {
            ErrorCode.None => "",
            ErrorCode.InvalidArgument => "Invalid argument",
            ErrorCode.InvalidArgumentHeight => "The height argument is outside the supported range [1, 2147483647]",
            ErrorCode.InvalidArgumentWidth => "The width argument is outside the supported range [1, 2147483647]",
            ErrorCode.InvalidArgumentBitsPerSample => "The bit per sample argument is outside the range [2, 16]",
            _ => "todo",
        };
    }
}
