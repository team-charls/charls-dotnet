// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Diagnostics.CodeAnalysis;

namespace CharLS.JpegLS;

internal class ThrowHelper
{
    [DoesNotReturn]
    internal static void ThrowArgumentOutOfRangeException(ErrorCode errorCode)
    {
        throw AddErrorCode(new ArgumentOutOfRangeException(GetErrorMessage(errorCode)), errorCode);
    }

    private static Exception AddErrorCode(Exception exception, ErrorCode errorCode)
    {
        exception.Data.Add(nameof(ErrorCode), errorCode);
        return exception;
    }

    private static string GetErrorMessage(ErrorCode errorCode)
    {
        return errorCode switch
        {
            ErrorCode.None => "",
            _ => "todo",
        };
    }
}
