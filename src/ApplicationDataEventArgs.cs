// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Diagnostics;

namespace CharLS.Managed;

/// <summary>
/// Encapsulates event data for ApplicationData event
/// </summary>
public sealed class ApplicationDataEventArgs : EventArgs
{
    internal ApplicationDataEventArgs(int applicationDataId, ReadOnlyMemory<byte> data)
    {
        Debug.Assert(applicationDataId is >= Constants.MinimumApplicationDataId and <= Constants.MaximumApplicationDataId);

        Id = applicationDataId;
        Data = data;
    }

    /// <summary>
    /// Identifies the type of application data, has the range [0,15]
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Returns the application data.
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; }
}
