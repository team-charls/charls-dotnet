// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed;

/// <summary>
/// Encapsulates event data for Comment event.
/// </summary>
public sealed class CommentEventArgs : EventArgs
{
    internal CommentEventArgs(ReadOnlyMemory<byte> data)
    {
        Data = data;
    }

    /// <summary>
    /// Gets the data of the comment.
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; }
}
