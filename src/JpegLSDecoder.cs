// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Diagnostics;

namespace CharLS.Managed;

/// <summary>
/// JPEG-LS decoder that provided the functionality to decode JPEG-LS images.
/// </summary>
public sealed class JpegLSDecoder
{
    private JpegStreamReader _reader;
    private ScanDecoder _scanDecoder;
    private State _state = State.Initial;

    private enum State
    {
        Initial,
        SourceSet,
        SpiffHeaderRead,
        SpiffHeaderNotFound,
        HeaderRead,
        Completed
    }

    /// <summary>
    /// Occurs when a comment (COM segment) is read.
    /// </summary>
    public event EventHandler<CommentEventArgs>? Comment
    {
        add { _reader.Comment += value; }
        remove { _reader.Comment -= value; }
    }

    /// <summary>
    /// Occurs when an application data (APPn segment) is read.
    /// </summary>
    public event EventHandler<ApplicationDataEventArgs> ApplicationData
    {
        add { _reader.ApplicationData += value; }
        remove { _reader.ApplicationData -= value; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JpegLSDecoder"/> class.
    /// </summary>
    public JpegLSDecoder()
    {
        _reader = new JpegStreamReader(this);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JpegLSDecoder"/> class.
    /// </summary>
    /// <param name="source">The buffer containing the encoded data.</param>
    /// <param name="readHeader">When true the header from the JPEG-LS stream is read parsed.</param>
    /// <param name="tryReadSpiffHeader">When true the SPIFF header from the JPEG-LS stream is read and parsed.</param>
    /// <exception cref="InvalidDataException">Thrown when the JPEG-LS stream is not valid.</exception>
    public JpegLSDecoder(ReadOnlyMemory<byte> source, bool readHeader = true, bool tryReadSpiffHeader = true)
    : this()
    {
        Source = source;
        if (readHeader)
        {
            ReadHeader(tryReadSpiffHeader);
        }
    }

    /// <summary>
    /// Gets or sets the source buffer that contains the encoded JPEG-LS bytes.
    /// </summary>
    /// <value>
    /// A region of memory that contains an encoded JPEG-LS image.
    /// </value>
    /// <exception cref="InvalidOperationException">Thrown when this property set twice./>.</exception>
    public ReadOnlyMemory<byte> Source
    {
        get => _reader.Source;

        set
        {
            if (_state != State.Initial)
                throw new InvalidOperationException("Source is already set.");

            _reader.Source = value;
            _state = State.SourceSet;
        }
    }

    /// <summary>
    /// Gets the SPIFF header that was found during reading the header.
    /// </summary>
    /// <value>
    /// The SPIFF header or null when no valid SPIFF header could be found.
    /// </value>
    public SpiffHeader? SpiffHeader { get; private set; }

    /// <summary>
    /// Gets the frame information of the image contained in the JPEG-LS stream.
    /// </summary>
    /// <remarks>
    /// Property should be obtained after calling <see cref="ReadHeader"/>".
    /// </remarks>
    /// <value>
    /// The frame information of the parsed JPEG-LS image.
    /// </value>
    /// <exception cref="InvalidOperationException">Thrown when this property is used before <see cref="ReadHeader(bool)"/>.</exception>
    public FrameInfo FrameInfo
    {
        get
        {
            if (_state < State.HeaderRead)
                throw new InvalidOperationException("Incorrect state. ReadHeader has not called.");

            return _reader.FrameInfo;
        }
    }

    /// <summary>
    /// Gets the near lossless parameter used to encode the JPEG-LS stream.
    /// </summary>
    /// <remarks>
    /// Property should be obtained after calling <see cref="ReadHeader"/>".
    /// </remarks>
    /// <value>
    /// The near lossless parameter. A value of 0 means that the image is lossless encoded.
    /// </value>
    /// <exception cref="InvalidOperationException">Thrown when this property is used before <see cref="ReadHeader(bool)"/>.</exception>
    public int NearLossless // TODO Change to method with componentIndex = 0;
    {
        get
        {
            CheckHeaderRead();
            return _reader.GetCodingParameters().NearLossless;
        }
    }

    /// <summary>
    /// Gets the interleave mode that was used to encode the scan(s).
    /// </summary>
    /// <remarks>
    /// Property should be obtained after calling <see cref="ReadHeader"/>".
    /// </remarks>
    /// <returns>The result of the operation: success or a failure code.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this property is used before <see cref="ReadHeader(bool)"/>.</exception>
    public InterleaveMode InterleaveMode
    {
        get
        {
            CheckHeaderRead();
            return _reader.InterleaveMode;
        }
    }

    /// <summary>
    /// Gets the preset coding parameters.
    /// </summary>
    /// <value>
    /// The preset coding parameters.
    /// </value>
    /// <exception cref="InvalidOperationException">Thrown when this property is used before <see cref="ReadHeader(bool)"/>.</exception>
    public JpegLSPresetCodingParameters PresetCodingParameters
    {
        get
        {
            CheckHeaderRead();
            return _reader.JpegLSPresetCodingParameters ?? new JpegLSPresetCodingParameters();
        }
    }

    /// <summary>
    /// Returns the HP color transformation that was used to encode the scan.
    /// </summary>
    /// <value>
    /// The color transformation that was used to encode the image.
    /// </value>
    public ColorTransformation ColorTransformation
    {
        get => _reader.ColorTransformation;
    }

    /// <summary>
    /// Gets the required size of the destination buffer.
    /// </summary>
    /// <param name="stride">The stride to use; byte count to the next pixel row. Pass 0 for the default.</param>
    /// <returns>The size of the destination buffer in bytes.</returns>
    /// <exception cref="OverflowException">When the required destination size doesn't fit in an int.</exception>
    /// <exception cref="InvalidOperationException">Thrown when this method is called before <see cref="ReadHeader(bool)"/>.</exception>
    public int GetDestinationSize(int stride = 0)
    {
        if (_state < State.HeaderRead)
            throw new InvalidOperationException("Source is not set.");

        if (stride == 0)
        {
            return FrameInfo.ComponentCount * FrameInfo.Height * FrameInfo.Width *
                   Algorithm.BitToByteCount(FrameInfo.BitsPerSample);
        }

        switch (InterleaveMode)
        {
            case InterleaveMode.None:
                return stride * FrameInfo.ComponentCount * FrameInfo.Height;

            case InterleaveMode.Line:
            case InterleaveMode.Sample:
                return stride * FrameInfo.Height;

            default:
                Debug.Assert(false);
                return 0;
        }
    }

    /// <summary>
    /// Returns the mapping table ID referenced by the component or 0 when no mapping table is used.
    /// </summary>
    /// <remarks>
    /// Function should be called after processing the complete JPEG-LS stream.
    /// </remarks>
    public int GetMappingTableId(int componentIndex)
    {
        CheckStateCompleted();
        ThrowHelper.ThrowIfOutsideRange(0, _reader.ComponentCount, componentIndex);
        return _reader.GetMappingTableId(componentIndex);
    }

    /// <summary>
    /// Reads the SPIFF (Still Picture Interchange File Format) header.
    /// </summary>
    /// <param name="spiffHeader">The header or null when no valid header was found.</param>
    /// <returns>true if a SPIFF header was present and could be read.</returns>
    /// <exception cref="InvalidDataException">Thrown when the JPEG-LS stream is not valid.</exception>
    public bool TryReadSpiffHeader(out SpiffHeader? spiffHeader)
    {
        ThrowHelper.ThrowInvalidOperationIfFalse(_state == State.SourceSet);
        _reader.ReadHeader(true);

        spiffHeader = _reader.SpiffHeader;
        if (spiffHeader == null)
        {
            _state = State.SpiffHeaderNotFound;
            return false;
        }

        _state = State.SpiffHeaderRead;
        return true;
    }

    /// <summary>
    /// Validates a SPIFF header with the FrameInfo.
    /// </summary>
    /// <param name="spiffHeader">Reference to a SPIFF header that will be validated.</param>
    /// <exception cref="InvalidDataException">Thrown when the SPIFF header is not valid.</exception>
    public void ValidateSpiffHeader(SpiffHeader spiffHeader)
    {
        ThrowHelper.ThrowInvalidOperationIfFalse(_state >= State.HeaderRead);
        if (!spiffHeader.IsValid(FrameInfo))
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidSpiffHeader);
    }

    /// <summary>
    /// Reads the header of the JPEG-LS stream.
    /// After calling this method, the informational properties can be obtained.
    /// </summary>
    /// <exception cref="InvalidDataException">Thrown when the JPEG-LS stream is not valid.</exception>
    public void ReadHeader(bool tryReadSpiffHeader = true)
    {
        ThrowHelper.ThrowInvalidOperationIfFalse(_state is >= State.SourceSet and < State.HeaderRead);

        if (_state != State.SpiffHeaderNotFound)
        {
            _reader.ReadHeader(tryReadSpiffHeader);
            if (_reader.SpiffHeader != null)
            {
                _reader.ReadHeader(false);
                if (_reader.SpiffHeader.IsValid(_reader.FrameInfo))
                {
                    SpiffHeader = _reader.SpiffHeader;
                }
            }
        }

        _state = State.HeaderRead;
    }

    /// <summary>
    /// Decodes the encoded JPEG-LS data and returns the created byte buffer.
    /// </summary>
    /// <param name="stride">The stride to use, or 0 for the default.</param>
    /// <returns>A byte array with the decoded JPEG-LS data.</returns>
    /// <exception cref="InvalidDataException">Thrown when the JPEG-LS stream is not valid.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the instance is used after being disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when this method is called before <see cref="ReadHeader(bool)"/>.</exception>
    public byte[] Decode(int stride = 0)
    {
        var destination = new byte[GetDestinationSize()];
        Decode(destination, stride);
        return destination;
    }

    /// <summary>
    /// Decodes the encoded JPEG-LS data to the passed byte buffer.
    /// </summary>
    /// <param name="destination">The memory region that is the destination for the decoded data.</param>
    /// <param name="stride">The stride to use, or 0 for the default.</param>
    /// <exception cref="InvalidDataException">Thrown when the JPEG-LS stream is not valid.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the instance is used after being disposed.</exception>
    public void Decode(Span<byte> destination, int stride = 0)
    {
        ThrowHelper.ThrowInvalidOperationIfFalse(_state == State.HeaderRead);

        // Compute the stride for the uncompressed destination buffer.
        int minimumStride = CalculateMinimumStride();
        if (stride == Constants.AutoCalculateStride)
        {
            stride = minimumStride;
        }
        else
        {
            if (stride < minimumStride)
                throw new ArgumentException("stride < minimumStride", nameof(stride));
        }

        // Compute the layout of the destination buffer.
        int bytesPerPlane = FrameInfo.Width * FrameInfo.Height * Algorithm.BitToByteCount(FrameInfo.BitsPerSample);
        int planeCount = _reader.InterleaveMode == InterleaveMode.None ? FrameInfo.ComponentCount : 1;
        int minimumDestinationSize = (bytesPerPlane * planeCount) - (stride - minimumStride);

        if (destination.Length < minimumDestinationSize)
            throw new ArgumentException("destination buffer too small", nameof(destination));

        for (int plane = 0; ;)
        {
            _scanDecoder = ScanCodecFactory.CreateScanDecoder(FrameInfo, _reader.GetValidatedPresetCodingParameters(), _reader.GetCodingParameters());
            int bytesRead = _scanDecoder.DecodeScan(_reader.RemainingSource(), destination, stride);
            _reader.AdvancePosition(bytesRead);

            ++plane;
            if (plane == planeCount)
                break;

            _reader.ReadNextStartOfScan();
            destination = destination[bytesPerPlane..];
        }

        _reader.ReadEndOfImage();
        _state = State.Completed;
    }

    private void CheckHeaderRead()
    {
        ThrowHelper.ThrowInvalidOperationIfFalse(_state >= State.HeaderRead);
    }

    private void CheckStateCompleted()
    {
        ThrowHelper.ThrowInvalidOperationIfFalse(_state == State.Completed);
    }

    private int CalculateMinimumStride()
    {
        int componentsInPlaneCount =
            _reader.InterleaveMode == InterleaveMode.None
            ? 1
            : FrameInfo.ComponentCount;
        return componentsInPlaneCount * FrameInfo.Width * Algorithm.BitToByteCount(FrameInfo.BitsPerSample);
    }
}
