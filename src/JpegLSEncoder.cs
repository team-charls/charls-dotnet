// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Reflection;
using System.Text;

namespace CharLS.JpegLS;

/// <summary>
/// JPEG-LS Encoder that uses the native CharLS implementation to encode JPEG-LS images.
/// </summary>
public sealed class JpegLSEncoder
{
    private FrameInfo? _frameInfo;
    private int _nearLossless;
    private InterleaveMode _interleaveMode;
    private ColorTransformation _colorTransformation;
    private EncodingOptions _encodingOptions;
    private JpegLSPresetCodingParameters? _userPresetCodingParameters = new JpegLSPresetCodingParameters();
    private readonly JpegStreamWriter _writer = new JpegStreamWriter();

    private enum State
    {
        Initial,
        DestinationSet,
        SpiffHeader,
        TablesAndMiscellaneous,
        Completed
    }

    private State _state;

    public static Memory<byte> Encode(ReadOnlyMemory<byte> source, FrameInfo frameInfo)
    {
        JpegLSEncoder encoder = new(frameInfo);
        encoder.Encode(source);
        return encoder.Destination[..encoder.BytesWritten];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JpegLSEncoder"/> class.
    /// </summary>
    public JpegLSEncoder()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JpegLSEncoder"/> class.
    /// </summary>
    /// <param name="width">The width of the image to encode.</param>
    /// <param name="height">The height of the image to encode.</param>
    /// <param name="bitsPerSample">The bits per sample of the image to encode.</param>
    /// <param name="componentCount">The component count of the image to encode.</param>
    /// <param name="allocateDestination">Flag to control if destination buffer should be allocated or not.</param>
    /// <param name="extraBytes">Number of extra destination bytes. Comments and tables are not included in the estimate.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when one of the arguments is invalid.</exception>
    /// <exception cref="OutOfMemoryException">Thrown when memory allocation for the destination buffer fails.</exception>
    public JpegLSEncoder(int width, int height, int bitsPerSample, int componentCount, bool allocateDestination = true, int extraBytes = 0) :
        this(new FrameInfo(width, height, bitsPerSample, componentCount), allocateDestination, extraBytes)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JpegLSEncoder"/> class.
    /// </summary>
    /// <param name="frameInfo">The frameInfo of the image to encode.</param>
    /// <param name="allocateDestination">Flag to control if destination buffer should be allocated or not.</param>
    /// <param name="extraBytes">Number of extra destination bytes. Comments and tables are not included in the estimate.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when one of the arguments is invalid.</exception>
    /// <exception cref="OutOfMemoryException">Thrown when memory allocation for the destination buffer fails.</exception>
    public JpegLSEncoder(FrameInfo frameInfo, bool allocateDestination = true, int extraBytes = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(extraBytes);

        FrameInfo = frameInfo;

        if (allocateDestination)
        {
            Destination = new byte[EstimatedDestinationSize + extraBytes];
        }
    }

    /// <summary>
    /// Gets or sets the frame information of the image.
    /// </summary>
    /// <value>
    /// The frame information of the image.
    /// </value>
    /// <exception cref="ArgumentException">Thrown when the passed FrameInfo is invalid.</exception>
    /// <exception cref="ArgumentNullException">Thrown when the passed FrameInfo instance is null.</exception>
    public FrameInfo? FrameInfo
    {
        get => _frameInfo;

        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _frameInfo = value;
        }
    }

    /// <summary>
    /// Gets or sets the near lossless parameter to be used to encode the JPEG-LS stream.
    /// </summary>
    /// <value>
    /// The near lossless parameter value, 0 means lossless.
    /// </value>
    /// <exception cref="ArgumentException">Thrown when the passed value is invalid.</exception>
    public int NearLossless
    {
        get => _nearLossless;

        set
        {
            _nearLossless = value;
        }
    }

    /// <summary>
    /// Gets or sets the interleave mode.
    /// </summary>
    /// <value>
    /// The interleave mode that should be used to encode the image. Default is None.
    /// </value>
    /// <exception cref="ArgumentException">Thrown when the passed value is invalid for the defined image.</exception>
    public InterleaveMode InterleaveMode
    {
        get => _interleaveMode;

        set
        {
            if (!value.IsValid())
                throw new ArgumentOutOfRangeException(nameof(value));

            _interleaveMode = value;
        }
    }

    /// <summary>
    /// Configures the encoding options the encoder should use.
    /// </summary>
    /// <value>
    /// Options to use. Options can be combined. Default is None.
    /// </value>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the passed enum value is invalid.</exception>
    public EncodingOptions EncodingOptions
    {
        get => _encodingOptions;

        set
        {
            if (!value.IsValid())
                throw new ArgumentOutOfRangeException(nameof(value));
            _encodingOptions = value;
        }
    }

    /// <summary>
    /// Gets or sets the JPEG-LS preset coding parameters.
    /// </summary>
    /// <value>
    /// The JPEG-LS preset coding parameters that should be used to encode the image.
    /// </value>
    /// <exception cref="ArgumentNullException">value.</exception>
    public JpegLSPresetCodingParameters? PresetCodingParameters
    {
        get => _userPresetCodingParameters;

        set
        {
            ArgumentNullException.ThrowIfNull(value);

            _userPresetCodingParameters = value;
        }
    }

    /// <summary>
    /// Gets or sets the HP color transformation the encoder should use
    /// If not set the encoder will use no color transformation.
    /// Color transformations are an HP extension and not defined by the JPEG-LS standard and can only be set for 3 component
    /// </summary>
    /// <value>
    /// The color transformation that should be used to encode the image.
    /// </value>
    /// <exception cref="ArgumentOutOfRangeException">value.</exception>
    public ColorTransformation ColorTransformation
    {
        get => _colorTransformation;

        set
        {
            if (!value.IsValid())
                throw new ArgumentOutOfRangeException(nameof(value));

            _colorTransformation = value;
        }
    }

    /// <summary>
    /// Gets the estimated size in bytes of the memory buffer that should used as output destination.
    /// </summary>
    /// <value>
    /// The size in bytes of the memory buffer.
    /// </value>
    /// <exception cref="OverflowException">When the required size doesn't fit in an int.</exception>
    public int EstimatedDestinationSize
    {
        get
        {
            Check.Operation(IsFrameInfoConfigured());

            return Util.CheckedMul(Util.CheckedMul(Util.CheckedMul(FrameInfo!.Width, FrameInfo.Height), FrameInfo.ComponentCount),
                       Algorithm.BitToByteCount(FrameInfo.BitsPerSample)) +
                   1024 + Constants.SpiffHeaderSizeInBytes;
        }
    }

    /// <summary>
    /// Gets or sets the memory region that will be the destination for the encoded JPEG-LS data.
    /// </summary>
    /// <value>
    /// The memory buffer to be used as the destination.
    /// </value>
    /// <exception cref="ArgumentException">Thrown when the passed value is an empty buffer.</exception>
    public Memory<byte> Destination
    {
        get => _writer.Destination;

        set
        {
            if (!_writer.Destination.IsEmpty)
                throw new InvalidOperationException();
            _writer.Destination = value;
        }
    }

    /// <summary>
    /// Gets the memory region with the encoded JPEG-LS data.
    /// </summary>
    /// <value>
    /// The memory region with the encoded data.
    /// </value>
    public ReadOnlyMemory<byte> EncodedData => Destination[..BytesWritten];

    /// <summary>
    /// Gets the bytes written to the destination buffer.
    /// </summary>
    /// <value>
    /// The bytes written to the destination buffer.
    /// </value>
    public int BytesWritten => _writer.BytesWritten;

    /// <summary>
    /// Encodes the passed image data into encoded JPEG-LS data.
    /// </summary>
    /// <param name="source">The memory region that is the source input to the encoding process.</param>
    /// <param name="stride">The stride of the image pixel of the source input.</param>
    public void Encode(ReadOnlyMemory<byte> source, int stride = 0)
    {
        if (source.IsEmpty)
            throw new ArgumentException("", nameof(source));
        CheckInterleaveModeAgainstComponentCount();

        int maximumSampleValue = Algorithm.CalculateMaximumSampleValue(FrameInfo!.BitsPerSample);
        if (!_userPresetCodingParameters!.IsValid(maximumSampleValue, NearLossless, out var presetCodingParameters))
            throw new ArgumentException("TODO");

        if (stride == Constants.AutoCalculateStride)
        {
            stride = CalculateStride();
        }
        else
        {
            ////check_stride(stride, source.size());
        }

        TransitionToTablesAndMiscellaneousState();
        WriteColorTransformSegment();
        WriteStartOfFrameSegment();
        WriteJpegLSPresetParametersSegment(maximumSampleValue, presetCodingParameters);

        if (InterleaveMode == InterleaveMode.None)
        {
            int byteCountComponent = stride * FrameInfo.Height;
            int lastComponent = FrameInfo.ComponentCount -1;
            for (int component = 0; component != FrameInfo.ComponentCount; ++component)
            {
                _writer.WriteStartOfScanSegment(1, NearLossless, InterleaveMode);
                EncodeScan(source, stride, 1, presetCodingParameters);

                // Synchronize the source stream (encode_scan works on a local copy)
                if (component != lastComponent)
                {
                    source = source[byteCountComponent..];
                }
            }
        }
        else
        {
            _writer.WriteStartOfScanSegment(FrameInfo.ComponentCount, NearLossless, InterleaveMode);
            EncodeScan(source, stride, FrameInfo.ComponentCount, presetCodingParameters);
        }

        WriteEndOfImage();
    }

    private void EncodeScan(ReadOnlyMemory<byte> source, int stride, int componentCount, JpegLSPresetCodingParameters codingParameters)
    {
        var encoder = ScanCodecFactory.CreateScanEncoder(
            new FrameInfo(FrameInfo!.Width, FrameInfo.Height, FrameInfo.BitsPerSample, componentCount),
            codingParameters,
            new CodingParameters
            {
                InterleaveMode = InterleaveMode, NearLossless = NearLossless, RestartInterval = 0, ColorTransformation = ColorTransformation
            });

        int bytesWritten = encoder.EncodeScan(source, _writer.GetRemainingDestination(), stride);

        // Synchronize the destination encapsulated in the writer (encode_scan works on a local copy)
        _writer.Seek(bytesWritten);
    }

    /// <summary>
    /// Writes a standard SPIFF header to the destination. The additional values are computed from the current encoder settings.
    /// A SPIFF header is optional, but recommended for standalone JPEG-LS files.
    /// It should not be used when embedding a JPEG-LS image in a DICOM file.
    /// </summary>
    /// <param name="colorSpace">The color space of the image.</param>
    /// <param name="resolutionUnit">The resolution units of the next 2 parameters.</param>
    /// <param name="verticalResolution">The vertical resolution.</param>
    /// <param name="horizontalResolution">The horizontal resolution.</param>
    public void WriteStandardSpiffHeader(SpiffColorSpace colorSpace, SpiffResolutionUnit resolutionUnit = SpiffResolutionUnit.AspectRatio,
        int verticalResolution = 1, int horizontalResolution = 1)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Writes a SPIFF header to the destination memory buffer.
    /// A SPIFF header is optional, but recommended for standalone JPEG-LS files.
    /// It should not be used when embedding a JPEG-LS image in a DICOM file.
    /// </summary>
    /// <param name="spiffHeader">Reference to a SPIFF header that will be written to the destination buffer.</param>
    public void WriteSpiffHeader(SpiffHeader spiffHeader)
    {
        throw new NotImplementedException();
    }

    private void TransitionToTablesAndMiscellaneousState()
    {
        if (_state == State.TablesAndMiscellaneous)
            return;

        if (_state == State.SpiffHeader)
        {
            ////writer_.write_spiff_end_of_directory_entry();
        }
        else
        {
            _writer.WriteStartOfImage();
        }

        if (EncodingOptions.HasFlag(EncodingOptions.IncludeVersionNumber))
        {
            var informationVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            byte[] utfBytes = Encoding.UTF8.GetBytes(informationVersion!);

            var versionNumber = "charls-dotnet "u8.ToArray().Concat(utfBytes).ToArray();
            _writer.WriteCommentSegment(versionNumber);
        }

        _state = State.TablesAndMiscellaneous;
    }

    private void WriteColorTransformSegment()
    {
        if (ColorTransformation == ColorTransformation.None)
            return;

        if (!ColorTransformations.IsPossible(FrameInfo!))
            throw new ArgumentException("TODO");

        _writer.WriteColorTransformSegment(ColorTransformation);
    }

    private void WriteStartOfFrameSegment()
    {
        if (_writer.WriteStartOfFrameSegment(FrameInfo!))
        {
            // Image dimensions are oversized and need to be written to a JPEG-LS preset parameters (LSE) segment.
            _writer.WriteJpegLSPresetParametersSegment(FrameInfo!.Height, FrameInfo.Width);
        }
    }

    private void WriteJpegLSPresetParametersSegment(int maximumSampleValue, JpegLSPresetCodingParameters presetCodingParameters)
    {

        if (!_userPresetCodingParameters!.IsDefault(maximumSampleValue, NearLossless) ||
            (EncodingOptions.HasFlag(EncodingOptions.IncludePCParametersJai) && FrameInfo!.BitsPerSample > 12))
        {
            // Write the actual used values to the stream, not zero's.
            // Explicit values reduces the risk for decoding by other implementations.
            _writer.WriteJpegLSPresetParametersSegment(presetCodingParameters);
        }
    }

    private void WriteEndOfImage()
    {
        _writer.WriteEndOfImage(EncodingOptions.HasFlag(EncodingOptions.EvenDestinationSize));
        _state = State.Completed;
    }

    private void CheckInterleaveModeAgainstComponentCount()
    {
        if (FrameInfo!.ComponentCount == 1 && InterleaveMode != InterleaveMode.None)
            throw new ArgumentException("invalid_argument_interleave_mode");
    }

    private int CalculateStride()
    {
        int stride = FrameInfo!.Width * Algorithm.BitToByteCount(FrameInfo.BitsPerSample);
        if (_interleaveMode == InterleaveMode.None)
            return stride;

        return stride * FrameInfo.ComponentCount;
    }

    private bool IsFrameInfoConfigured()
    {
        return FrameInfo != null;
    }
}
