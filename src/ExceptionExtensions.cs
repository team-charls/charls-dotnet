// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed;

/// <summary>
/// Provides helper methods for Exception objects and ErrorCode values.
/// </summary>
public static class ExceptionExtensions
{
    /// <summary>
    /// Retrieves the ErrorCode value from the Exception instance or None if no error value was present.
    /// </summary>
    /// <param name="exception">The instance of the exception to retrieve the error code from.</param>
    public static ErrorCode GetErrorCode(this Exception exception)
    {
        var value = exception.Data[nameof(ErrorCode)];
        return value != null ? (ErrorCode)value : default;
    }

    internal static void AddErrorCode(this Exception exception, ErrorCode error)
    {
        exception.Data.Add(nameof(ErrorCode), error);
    }
}
