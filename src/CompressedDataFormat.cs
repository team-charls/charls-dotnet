// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed;

/// <summary>
/// JPEG-LS defines 3 compressed data formats. (see Annex C).
/// </summary>
public enum CompressedDataFormat
{
    /// <summary>
    /// Not enough information has been decoded to determine the data format.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// All data to decode the image is contained in the file. This is the typical format.
    /// </summary>
    Interchange = 1,

    /// <summary>
    /// The file has references to mapping tables that need to be provided by
    /// the application environment.
    /// </summary>
    AbbreviatedImageData = 2,

    /// <summary>
    /// The file only contains mapping tables, no image is present.
    /// </summary>
    AbbreviatedTableSpecification = 3
}
