// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

/// <summary>
/// Encapsulates event data for Comment event
/// </summary>
public sealed class CommentEventArgs : EventArgs
{
    internal CommentEventArgs(ReadOnlyMemory<byte> data)
    {
        Data = data;
    }

    /// <summary>
    /// Returns the data of the comment.
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; }

    /// <summary>
    /// `When set will abort the decoding process.
    /// </summary>
    public bool Failed { get; set; }
}
