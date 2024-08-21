// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed;

/// <summary>
/// Defines the information that describes a mapping table.
/// </summary>
public readonly record struct MappingTableInfo
{
    /// <summary>
    /// Identifier of the mapping table, range [1, 255].
    /// </summary>
    public byte TableId { get; init; }

    /// <summary>
    /// Width of a table entry in bytes, range [1, 255].
    /// </summary>
    public byte EntrySize { get; init; }

    /// <summary>
    /// Size of the table in bytes, range [1, 16711680]
    /// </summary>
    public int DataSize { get; init; }
}

