// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Diagnostics;

namespace CharLS.JpegLS;

/// <summary>
/// JPEG-LS Decoder that uses the native CharLS implementation to decode JPEG-LS images.
/// </summary>
public sealed class JpegLSDecoder
{
    private FrameInfo? _frameInfo;
    private int? _nearLossless;
    //private JpegLSInterleaveMode? _interleaveMode;
    private readonly JpegStreamReader _reader = new();

    private enum State
    {
        Initial,
        SourceSet,
        SpiffHeaderRead,
        SpiffHeaderNotFound,
        HeaderRead,
        Completed
    };

    private State _state = State.Initial;


    /// <summary>
    /// Initializes a new instance of the <see cref="JpegLSDecoder"/> class.
    /// </summary>
    public JpegLSDecoder()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JpegLSDecoder"/> class.
    /// </summary>
    /// <param name="source">The buffer containing the encoded data.</param>
    /// <param name="readHeader">When true the header from the JPEG-LS stream is parsed.</param>
    /// <exception cref="InvalidDataException">Thrown when the JPEG-LS stream is not valid.</exception>
    public JpegLSDecoder(ReadOnlyMemory<byte> source, bool readHeader = true)
    {
        Source = source;
        if (readHeader)
        {
            ReadHeader();
        }
    }

    /// <summary>
    /// Gets or sets the the source buffer that contains the encoded JPEG-LS bytes.
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
    public FrameInfo FrameInfo => _reader.FrameInfo ?? throw new InvalidOperationException("Incorrect state. ReadHeader has not called.");

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
    public int NearLossless
    {
        get
        {
            CheckHeaderRead();
            if (!_nearLossless.HasValue)
            {
                _nearLossless = 0;
            }

            return _nearLossless.Value;
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
    public JpegLSInterleaveMode InterleaveMode
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
    public JpegLSPresetCodingParameters PresetCodingParameters => _reader.JpegLSPresetCodingParameters ?? new JpegLSPresetCodingParameters();

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
                   BitToByteCount(FrameInfo.BitsPerSample);
        }

        switch (InterleaveMode)
        {
            case JpegLSInterleaveMode.None:
                return stride * FrameInfo.ComponentCount * FrameInfo.Height;

            case JpegLSInterleaveMode.Line:
            case JpegLSInterleaveMode.Sample:
                return stride * FrameInfo.Height;

            default:
                Debug.Assert(false);
                return 0;
        }
    }

    ///// <summary>
    ///// Reads the SPIFF (Still Picture Interchange File Format) header.
    ///// </summary>
    ///// <param name="spiffHeader">The header or null when no valid header was found.</param>
    ///// <returns>true if a SPIFF header was present and could be read.</returns>
    ///// <exception cref="ObjectDisposedException">Thrown when the instance is used after being disposed.</exception>
    //public bool TryReadSpiffHeader(out SpiffHeader? spiffHeader)
    //{
    //    bool found = headerFound != 0;
    //    if (found)
    //    {
    //        found = SpiffHeader.TryCreate(headerNative, out spiffHeader);
    //    }
    //    else
    //    {
    //        spiffHeader = default;
    //    }

    //    return found;
    //}

    /// <summary>
    /// Reads the header of the JPEG-LS stream.
    /// After calling this method, the informational properties can be obtained.
    /// </summary>
    /// <param name="tryReadSpiffHeader">if set to <c>true</c> try to read the SPIFF header first.</param>
    /// <exception cref="InvalidDataException">Thrown when the JPEG-LS stream is not valid.</exception>
    public void ReadHeader(bool tryReadSpiffHeader = true)
    {
        CheckOperation(_state == State.SourceSet);

        _reader.ReadHeader();
        _state = State.HeaderRead;

        //if (tryReadSpiffHeader && TryReadSpiffHeader(out SpiffHeader? spiffHeader))
        //{
        //    SpiffHeader = spiffHeader;
        //}
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
        Check.Operation(_state == State.HeaderRead);

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
        int bytesPerPlane = FrameInfo.Width * FrameInfo.Height * BitToByteCount(FrameInfo.BitsPerSample);
        int planeCount = _reader.InterleaveMode == JpegLSInterleaveMode.None ? FrameInfo.ComponentCount : 1;
        int minimumDestinationSize = (bytesPerPlane * planeCount) - (stride - minimumStride);

        if (destination.Length < minimumDestinationSize)
            throw new ArgumentException("destination buffer too small", nameof(destination));

        for (int plane = 0; ;)
        {
            var scanDecoder = ScanCodecFactory.CreateScanDecoder(FrameInfo, _reader.GetValidatedPresetCodingParameters(), _reader.GetCodingParameters());

            ////const size_t bytes_read{ scan_codec->decode_scan(reader_.remaining_source(), destination.data(), stride)};
            int bytesRead = (int)scanDecoder.DecodeScan(_reader.RemainingSource(), destination, stride);
            _reader.AdvancePosition(bytesRead);

            ++plane;
            if (plane == planeCount)
                break;

            ////_reader.read_next_start_of_scan();
            ////destination = destination.subspan(bytes_per_plane);
        }

        _reader.ReadEndOfImage();
        _state = State.Completed;
    }

    /// <summary>
    /// Computes how many bytes are needed to hold the number of bits.
    /// </summary>
    private static int BitToByteCount(int bitCount)
    {
        return (bitCount + 7) / 8;
    }

    private static void CheckOperation(bool expression)
    {
        if (!expression)
        {
            throw new InvalidOperationException("JPEG-LS Decoder in the incorrect state.");
        }
    }

    private void CheckHeaderRead()
    {
        Check.Operation(_state >= State.HeaderRead);
    }

    private int CalculateMinimumStride()
    {
        int componentsInPlaneCount = 
            _reader.InterleaveMode == JpegLSInterleaveMode.None
            ? 1
            : FrameInfo.ComponentCount;
        return componentsInPlaneCount * FrameInfo.Width * Util.BitToByteCount(FrameInfo.BitsPerSample);
    }
}
