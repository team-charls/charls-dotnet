// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed;

/// <summary>
/// Official defined SPIFF tags defined in Table F.5 (ISO/IEC 10918-3)
/// </summary>
public enum SpiffEntryTag
{
    /// <summary>
    /// This entry describes the opto-electronic transfer characteristics of the source image.
    /// </summary>
    TransferCharacteristics = 2,

    /// <summary>
    /// This entry specifies component registration, the spatial positioning of samples within components relative to the
    /// samples of other components.
    /// </summary>
    ComponentRegistration = 3,

    /// <summary>
    /// This entry specifies the image orientation (rotation, flip).
    /// </summary>
    ImageOrientation = 4,

    /// <summary>
    /// This entry specifies a reference to a thumbnail.
    /// </summary>
    Thumbnail = 5,

    /// <summary>
    /// This entry describes in textual form a title for the image.
    /// </summary>
    ImageTitle = 6,

    /// <summary>
    /// This entry refers to data in textual form containing additional descriptive information about the image.
    /// </summary>
    ImageDescription = 7,

    /// <summary>
    /// This entry describes the date and time of the last modification of the image.
    /// </summary>
    TimeStamp = 8,

    /// <summary>
    /// This entry describes in textual form a version identifier which refers to the number of revisions of the image.
    /// </summary>
    VersionIdentifier = 9,

    /// <summary>
    /// This entry describes in textual form the creator of the image.
    /// </summary>
    CreatorIdentification = 10,

    /// <summary>
    /// The presence of this entry, indicates that the image’s owner has retained copyright protection and usage rights for
    /// the image.
    /// </summary>
    ProtectionIndicator = 11,

    /// <summary>
    /// This entry describes in textual form copyright information for the image.
    /// </summary>
    CopyrightInformation = 12,

    /// <summary>
    /// This entry describes in textual form contact information for use of the image.
    /// </summary>
    ContactInformation = 13,

    /// <summary>
    /// This entry refers to data containing a list of offsets into the file.
    /// </summary>
    TileIndex = 14,

    /// <summary>
    /// This entry refers to data containing the scan list.
    /// </summary>
    ScanIndex = 15,

    /// <summary>
    /// This entry contains a 96-bit reference number intended to relate images stored in separate files.
    /// </summary>
    SetReference = 16
};
