// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using static CharLS.Managed.Algorithm;

namespace CharLS.Managed;

/// <summary>
/// JPEG-LS decoder that provided the functionality to decode JPEG-LS images.
/// </summary>
public sealed class JpegLSDecoder
{
    /// <summary>
    /// Special value to indicate that decoder needs to calculate the required stride.
    /// </summary>
    public const int AutoCalculateStride = Constants.AutoCalculateStride;

    private JpegStreamReader _reader;
    private ScanDecoder _scanDecoder;
    private State _state = State.Initial;

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
    /// Occurs when a comment (COM segment) is read.
    /// </summary>
    public event EventHandler<CommentEventArgs>? Comment
    {
        add => _reader.Comment += value;
        remove => _reader.Comment -= value;
    }

    /// <summary>
    /// Occurs when an application data (APPn segment) is read.
    /// </summary>
    public event EventHandler<ApplicationDataEventArgs> ApplicationData
    {
        add => _reader.ApplicationData += value;
        remove => _reader.ApplicationData -= value;
    }

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
            ThrowHelper.ThrowInvalidOperationIfFalse(_state == State.Initial);

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
            CheckStateHeaderRead();
            return _reader.FrameInfo;
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
            CheckStateHeaderRead();
            return _reader.JpegLSPresetCodingParameters ?? JpegLSPresetCodingParameters.Default;
        }
    }

    /// <summary>
    /// Returns the compressed data format of the JPEG-LS data stream.
    /// </summary>
    /// <remarks>
    /// Function can be called after reading the header or after processing the complete JPEG-LS stream.
    /// After reading the header the method may report Unknown or AbbreviatedTableSpecification.
    /// </remarks>
    public CompressedDataFormat CompressedDataFormat => _reader.CompressedDataFormat;

    /// <summary>
    /// Returns the HP color transformation that was used to encode the scan.
    /// </summary>
    /// <value>
    /// The color transformation that was used to encode the image.
    /// </value>
    public ColorTransformation ColorTransformation => _reader.ColorTransformation;

    /// <summary>
    /// Returns the count of mapping tables present in the JPEG-LS stream.
    /// </summary>
    /// <remarks>
    /// Property should be called after processing the complete JPEG-LS stream.
    /// </remarks>
    /// <value>The number of mapping tables present in the JPEG-LS stream.</value>
    public int MappingTableCount
    {
        get
        {
            CheckStateCompleted();
            return _reader.MappingTableCount;
        }
    }

    /// <summary>
    /// Gets the near lossless parameter used to encode the component.
    /// </summary>
    /// <remarks>
    /// Value should be obtained after calling <see cref="ReadHeader"/>".
    /// </remarks>
    /// <param name="componentIndex">The index of the component to retrieve the near lossless value for.</param>
    /// <returns>
    /// The near lossless parameter. A value of 0 means that the image is lossless encoded.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when this property is used before <see cref="ReadHeader(bool)"/>.</exception>
    public int GetNearLossless(int componentIndex = 0)
    {
        CheckStateHeaderRead();
        ThrowHelper.ThrowIfOutsideRange(0, _reader.ComponentCount - 1, componentIndex);
        return _reader.GetNearLossless(componentIndex);
    }

    /// <summary>
    /// Gets the interleave mode that was used to encode the component.
    /// </summary>
    /// <remarks>
    /// Value should be obtained after calling <see cref="ReadHeader"/>".
    /// </remarks>
    /// <param name="componentIndex">The index of the component to retrieve the interleave mode for.</param>
    /// <returns>The interleave mode.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this property is used before <see cref="ReadHeader(bool)"/>.</exception>
    public InterleaveMode GetInterleaveMode(int componentIndex = 0)
    {
        CheckStateHeaderRead();
        return _reader.GetInterleaveMode(componentIndex);
    }

    /// <summary>
    /// Gets the required size of the destination buffer.
    /// </summary>
    /// <param name="stride">The stride to use; byte count to the next pixel row. Pass 0 (AutoCalculateStride) for the default.</param>
    /// <returns>The size of the destination buffer in bytes.</returns>
    /// <exception cref="InvalidDataException">When the required destination size doesn't fit in an int.</exception>
    /// <exception cref="InvalidOperationException">Thrown when this method is called before <see cref="ReadHeader(bool)"/>.</exception>
    public int GetDestinationSize(int stride = AutoCalculateStride)
    {
        CheckStateHeaderRead();

        checked
        {
            try
            {
                if (stride == AutoCalculateStride)
                {
                    return FrameInfo.ComponentCount * FrameInfo.Height * FrameInfo.Width *
                           BitToByteCount(FrameInfo.BitsPerSample);
                }

                switch (GetInterleaveMode())
                {
                    case InterleaveMode.Line:
                    case InterleaveMode.Sample:
                        return stride * FrameInfo.Height;

                    default:
                        Debug.Assert(GetInterleaveMode() == InterleaveMode.None);
                        return stride * FrameInfo.ComponentCount * FrameInfo.Height;
                }
            }
            catch (OverflowException e)
            {
                throw ThrowHelper.CreateInvalidDataException(ErrorCode.ParameterValueNotSupported, e);
            }
        }
    }

    /// <summary>
    /// Returns the mapping table ID referenced by the component or 0 when no mapping table is used.
    /// </summary>
    /// <remarks>
    /// Function should be called after processing the complete JPEG-LS stream.
    /// </remarks>
    /// <param name="componentIndex">The index of the component to get the mapping table ID for.</param>
    public int GetMappingTableId(int componentIndex)
    {
        CheckStateCompleted();
        ThrowHelper.ThrowIfOutsideRange(0, _reader.ComponentCount - 1, componentIndex);
        return _reader.GetMappingTableId(componentIndex);
    }

    /// <summary>
    /// Converts the mapping table ID to a mapping table index.
    /// When the requested table is not present in the JPEG-LS stream the value -1 will be returned.
    /// </summary>
    /// <remarks>
    /// Function should be called after processing the complete JPEG-LS stream.
    /// </remarks>
    /// <param name="mappingTableId">The mapping table ID to find.</param>
    public int FindMappingTableIndex(int mappingTableId)
    {
        CheckStateCompleted();
        ThrowHelper.ThrowIfOutsideRange(Constants.MinimumMappingTableId, Constants.MaximumMappingTableId, mappingTableId);
        return _reader.FindMappingTableIndex(mappingTableId);
    }

    /// <summary>
    /// Returns information about a mapping table.
    /// </summary>
    /// <remarks>
    /// Function should be called after processing the complete JPEG-LS stream.
    /// </remarks>
    /// <param name="mappingTableIndex">The index of the mapping table to get the info for.</param>
    public MappingTableInfo GetMappingTableInfo(int mappingTableIndex)
    {
        CheckMappingTableIndex(mappingTableIndex);
        return _reader.GetMappingTableInfo(mappingTableIndex);
    }

    /// <summary>
    /// Returns a mapping table.
    /// </summary>
    /// <remarks>
    /// Function should be called after processing the complete JPEG-LS stream.
    /// </remarks>
    /// <param name="mappingTableIndex">The index of the mapping table to get the data for.</param>
    public ReadOnlyMemory<byte> GetMappingTableData(int mappingTableIndex)
    {
        CheckMappingTableIndex(mappingTableIndex);
        return _reader.GetMappingTableData(mappingTableIndex);
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
    /// Reads the header of the JPEG-LS stream.
    /// After calling this method, the informational properties can be obtained.
    /// </summary>
    /// <param name="tryReadSpiffHeader">Flag to control if the decoder should try to parse the SPIFF header (default is true).</param>
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

        _state = _reader.EndOfImage ? State.Completed : State.HeaderRead;
    }

    /// <summary>
    /// Decodes the encoded JPEG-LS data and returns the created byte buffer.
    /// </summary>
    /// <param name="stride">The stride to use, or 0 for the default.</param>
    /// <returns>A byte array with the decoded JPEG-LS data.</returns>
    /// <exception cref="InvalidDataException">Thrown when the JPEG-LS stream is not valid.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the instance is used after being disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when this method is called before <see cref="ReadHeader(bool)"/>.</exception>
    public byte[] Decode(int stride = AutoCalculateStride)
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
    public void Decode(Span<byte> destination, int stride = AutoCalculateStride)
    {
        ThrowHelper.ThrowInvalidOperationIfFalse(_state == State.HeaderRead);

        for (int component = 0; ;)
        {
            int scanStride = CheckStrideAndDestinationLength(destination.Length, stride);
            _scanDecoder = new ScanDecoder(_reader.ScanFrameInfo, _reader.GetValidatedPresetCodingParameters(), _reader.GetCodingParameters());
            int bytesRead = _scanDecoder.DecodeScan(_reader.RemainingSource(), destination, scanStride);
            _reader.AdvancePosition(bytesRead);

            component += _reader.ScanComponentCount;
            if (component == _reader.ComponentCount)
                break;

            destination = destination[(scanStride * FrameInfo.Height)..];
            _reader.ReadNextStartOfScan();
        }

        _reader.ReadEndOfImage();
        _state = State.Completed;
    }

    private void CheckStateHeaderRead()
    {
        ThrowHelper.ThrowInvalidOperationIfFalse(_state >= State.HeaderRead);
    }

    private void CheckStateCompleted()
    {
        ThrowHelper.ThrowInvalidOperationIfFalse(_state == State.Completed);
    }

    private void CheckMappingTableIndex(int mappingTableIndex)
    {
        ThrowHelper.ThrowIfOutsideRange(0, MappingTableCount - 1, mappingTableIndex);
    }

    private int CheckStrideAndDestinationLength(int destinationLength, int stride)
    {
        int minimumStride = CalculateMinimumStride();

        if (stride == AutoCalculateStride)
        {
            stride = minimumStride;
        }
        else
        {
            if (stride < minimumStride)
                ThrowHelper.ThrowArgumentException(ErrorCode.InvalidArgumentStride);
        }

        int notUsedBytesAtEnd = stride - minimumStride;
        int minimumDestinationScanLength = _reader.ScanInterleaveMode == InterleaveMode.None
            ? (stride * _reader.ScanComponentCount * FrameInfo.Height) - notUsedBytesAtEnd
            : (stride * FrameInfo.Height) - notUsedBytesAtEnd;

        if (destinationLength < minimumDestinationScanLength)
            ThrowHelper.ThrowArgumentException(ErrorCode.InvalidArgumentSize);

        return stride;
    }

    private int CalculateMinimumStride()
    {
        int componentsInPlaneCount =
            _reader.ScanInterleaveMode == InterleaveMode.None
            ? 1
            : _reader.ScanComponentCount;
        return componentsInPlaneCount * FrameInfo.Width * BitToByteCount(FrameInfo.BitsPerSample);
    }
}
