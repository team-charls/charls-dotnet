// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed;

/// <summary>
/// Hold information about an image frame.
/// </summary>
public readonly record struct FrameInfo
{
    private readonly int _width;
    private readonly int _height;
    private readonly int _bitsPerSample;
    private readonly int _componentCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameInfo"/> struct.
    /// </summary>
    public FrameInfo()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameInfo"/> struct.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <param name="bitsPerSample">The number of bits per sample.</param>
    /// <param name="componentCount">The number of components contained in a frame.</param>
    public FrameInfo(int width, int height, int bitsPerSample, int componentCount)
    {
        ThrowHelper.ThrowIfNegativeOrZero(width, ErrorCode.InvalidArgumentWidth);
        ThrowHelper.ThrowIfNegativeOrZero(height, ErrorCode.InvalidArgumentHeight);
        ThrowHelper.ThrowIfOutsideRange(Constants.MinimumBitsPerSample, Constants.MaximumBitsPerSample, bitsPerSample, ErrorCode.InvalidArgumentBitsPerSample);
        ThrowHelper.ThrowIfOutsideRange(Constants.MinimumComponentCount, Constants.MaximumComponentCount, componentCount, ErrorCode.InvalidArgumentComponentCount);

        _width = width;
        _height = height;
        _bitsPerSample = bitsPerSample;
        _componentCount = componentCount;
    }

    /// <summary>
    /// Gets the width of the image, valid range is [1, int.MaxValue].
    /// </summary>
    public int Width
    {
        get => _width;
        init
        {
            ThrowHelper.ThrowIfNegativeOrZero(value, ErrorCode.InvalidArgumentWidth);
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
            ThrowHelper.ThrowIfNegativeOrZero(value, ErrorCode.InvalidArgumentHeight);
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
            ThrowHelper.ThrowIfOutsideRange(Constants.MinimumBitsPerSample, Constants.MaximumBitsPerSample, value, ErrorCode.InvalidArgumentBitsPerSample);
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
            ThrowHelper.ThrowIfOutsideRange(Constants.MinimumComponentCount, Constants.MaximumComponentCount, value, ErrorCode.InvalidArgumentComponentCount);
            _componentCount = value;
        }
    }
}
