// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

public sealed record MappingTableInfo
{
    /// <summary>
    /// Identifier of the mapping table, range [1, 255].
    /// </summary>
    public int TableId { get; init; }

    /// <summary>
    /// Width of a table entry in bytes, range [1, 255].
    /// </summary>
    public int EntrySize { get; init; }
}

