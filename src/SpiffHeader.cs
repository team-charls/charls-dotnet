// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

/// <summary>
/// Holds the information for a SPIFF (Still Picture Interchange File Format) header.
/// </summary>
public sealed record SpiffHeader
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SpiffHeader"/> class.
    /// </summary>
    public SpiffHeader()
    {
        ColorSpace = SpiffColorSpace.None;
        CompressionType = SpiffCompressionType.JpegLS;
        ResolutionUnit = SpiffResolutionUnit.AspectRatio;
        VerticalResolution = 1;
        HorizontalResolution = 1;
    }

    /// <summary>
    /// Gets or sets the application profile identifier.
    /// </summary>
    /// <value>
    /// The profile identifier.
    /// </value>
    public SpiffProfileId ProfileId { get; init; }

    /// <summary>
    /// Gets or sets the number of color component count, range [1, 255].
    /// </summary>
    /// <value>
    /// The component count.
    /// </value>
    public int ComponentCount { get; init; }

    /// <summary>
    /// Gets or sets the height, range [1, 4294967295].
    /// </summary>
    /// <value>
    /// The height.
    /// </value>
    public int Height { get; init; }

    /// <summary>
    /// Gets or sets the width, range [1, 4294967295].
    /// </summary>
    /// <value>
    /// The width.
    /// </value>
    public int Width { get; init; }

    /// <summary>
    /// Gets or sets the color space.
    /// </summary>
    /// <value>
    /// The color space.
    /// </value>
    public SpiffColorSpace ColorSpace { get; init; }

    /// <summary>
    /// Gets or sets the bits per sample, range (1, 2, 4, 8, 12, 16).
    /// </summary>
    /// <value>
    /// The bits per sample.
    /// </value>
    public int BitsPerSample { get; init; }

    /// <summary>
    /// Gets or sets the type of the compression.
    /// </summary>
    /// <value>
    /// The type of the compression.
    /// </value>
    public SpiffCompressionType CompressionType { get; init; }

    /// <summary>
    /// Gets or sets the resolution unit.
    /// </summary>
    /// <value>
    /// The resolution unit.
    /// </value>
    public SpiffResolutionUnit ResolutionUnit { get; init; }

    /// <summary>
    /// Gets or sets the vertical resolution.
    /// </summary>
    /// <value>
    /// The vertical resolution.
    /// </value>
    public int VerticalResolution { get; init; }

    /// <summary>
    /// Gets or sets the horizontal resolution.
    /// </summary>
    /// <value>
    /// The horizontal resolution.
    /// </value>
    public int HorizontalResolution { get; init; }
}
