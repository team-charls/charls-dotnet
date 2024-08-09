// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

/// <summary>
/// Hold information about an image frame.
/// </summary>
public sealed record FrameInfo
{
    private readonly int _width;
    private readonly int _height;
    private readonly int _bitsPerSample;
    private readonly int _componentCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameInfo"/> record.
    /// </summary>
    public FrameInfo()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameInfo"/> record.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <param name="bitsPerSample">The number of bits per sample.</param>
    /// <param name="componentCount">The number of components contained in a frame.</param>
    public FrameInfo(int width, int height, int bitsPerSample, int componentCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfLessThan(bitsPerSample, 2);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(componentCount);

        Width = width;
        Height = height;
        BitsPerSample = bitsPerSample;
        ComponentCount = componentCount;
    }

    /// <summary>
    /// Gets the width of the image, valid range is [1, int.MaxValue].
    /// </summary>
    public int Width
    {
        get => _width;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            _width = value;
        }
    }

    /// <summary>
    /// Gets the height of the image, valid range is [1, int.MaxValue].
    /// </summary>
    public int Height
    {
        get => _height;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            _height = value;
        }
    }

    /// <summary>
    /// Gets the number of bits per sample, valid range is [2, 16].
    /// </summary>
    public int BitsPerSample
    {
        get => _bitsPerSample;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, Constants.MinimumBitsPerSample);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, Constants.MaximumBitsPerSample);
            _bitsPerSample = value;
        }
    }

    /// <summary>
    /// Gets the number of components contained in the frame, valid range is [1, 255].
    /// </summary>
    public int ComponentCount
    {
        get => _componentCount;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, Constants.MaximumComponentCount);
            _componentCount = value;
        }
    }
}
