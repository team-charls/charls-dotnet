// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace CharLS.Managed;

/// <summary>
/// JPEG-LS encoder that provided the functionality to encode JPEG-LS images.
/// </summary>
public sealed class JpegLSEncoder
{
    private JpegStreamWriter _writer;
    private ScanEncoder _scanEncoder;
    private FrameInfo? _frameInfo;
    private int _nearLossless;
    private InterleaveMode _interleaveMode;
    private ColorTransformation _colorTransformation;
    private EncodingOptions _encodingOptions;
    private JpegLSPresetCodingParameters? _userPresetCodingParameters = new();
    private State _state = State.Initial;

    private enum State
    {
        Initial,
        DestinationSet,
        SpiffHeader,
        TablesAndMiscellaneous,
        Completed
    }

    /// <summary>
    /// Encodes the passed image data into encoded JPEG-LS data.
    /// </summary>
    public static Memory<byte> Encode(ReadOnlyMemory<byte> source, FrameInfo frameInfo,
        InterleaveMode interleaveMode = InterleaveMode.None, EncodingOptions encodingOptions = EncodingOptions.None)
    {
        JpegLSEncoder encoder = new(frameInfo) { InterleaveMode = interleaveMode, EncodingOptions = encodingOptions };
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
            ThrowHelper.ThrowIfOutsideRange(Constants.MinimumNearLossless, Constants.MaximumNearLossless, value,
                ErrorCode.InvalidArgumentNearLossless);
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
            ThrowHelper.ThrowArgumentOutOfRangeExceptionIfFalse(value.IsValid(), ErrorCode.InvalidArgumentInterleaveMode, nameof(value));
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
            ThrowHelper.ThrowArgumentOutOfRangeExceptionIfFalse(value.IsValid(), ErrorCode.InvalidArgumentEncodingOptions, nameof(value));
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
            ThrowHelper.ThrowArgumentOutOfRangeExceptionIfFalse(value.IsValid(),
                ErrorCode.InvalidArgumentColorTransformation);

            _colorTransformation = value;
        }
    }

    /// <summary>
    /// Gets the estimated size in bytes of the memory buffer that should be used as output destination.
    /// </summary>
    /// <value>
    /// The size in bytes of the memory buffer.
    /// </value>
    /// <exception cref="OverflowException">When the required size doesn't fit in an int.</exception>
    public int EstimatedDestinationSize
    {
        get
        {
            ThrowHelper.ThrowInvalidOperationIfFalse(IsFrameInfoConfigured());

            checked
            {
                return (FrameInfo!.Width * FrameInfo.Height * FrameInfo.ComponentCount *
                           Algorithm.BitToByteCount(FrameInfo.BitsPerSample)) + 1024 + Constants.SpiffHeaderSizeInBytes;
            }
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
            ThrowHelper.ThrowInvalidOperationIfFalse(_state == State.Initial);
            _writer.Destination = value;
            _state = State.DestinationSet;
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
    /// Configures the mapping table ID the encoder should reference when encoding a component.
    /// The referenced mapping table can be included in the stream or provided in another JPEG-LS abbreviated format stream.
    /// </summary>
    public void SetMappingTableId(int componentIndex, int mappingTableId)
    {
        ThrowHelper.ThrowIfOutsideRange(Constants.MinimumComponentIndex, Constants.MaximumComponentIndex, componentIndex);
        ThrowHelper.ThrowIfOutsideRange(0, Constants.MaximumMappingTableId, mappingTableId);

        _writer.SetTableId(componentIndex, mappingTableId);
    }

    /// <summary>
    /// Resets the write position of the destination buffer to the beginning.
    /// </summary>
    public void Rewind()
    {
        if (_state == State.Initial)
            return; // Nothing to do, stay in the same state.

        _writer.Rewind();
        _state = State.DestinationSet;
    }

    /// <summary>
    /// Encodes the passed image data into encoded JPEG-LS data.
    /// </summary>
    /// <param name="source">The memory region that is the source input to the encoding process.</param>
    /// <param name="stride">The stride of the image pixel of the source input.</param>
    public void Encode(ReadOnlyMemory<byte> source, int stride = 0)
    {
        if (source.IsEmpty)
            throw new ArgumentException("", nameof(source));
        ThrowHelper.ThrowInvalidOperationIfFalse(IsFrameInfoConfigured() && _state != State.Initial);
        CheckInterleaveModeAgainstComponentCount();

        int maximumSampleValue = Algorithm.CalculateMaximumSampleValue(FrameInfo!.BitsPerSample);
        ThrowHelper.ThrowArgumentExceptionIfFalse(
            _userPresetCodingParameters!.IsValid(maximumSampleValue, NearLossless, out var presetCodingParameters),
            null, ErrorCode.InvalidArgumentPresetCodingParameters);

        if (stride == Constants.AutoCalculateStride)
        {
            stride = CalculateStride();
        }
        else
        {
            CheckStride(stride, source.Length);
        }

        TransitionToTablesAndMiscellaneousState();
        WriteColorTransformSegment();
        WriteStartOfFrameSegment();
        WriteJpegLSPresetParametersSegment(maximumSampleValue, presetCodingParameters);

        if (InterleaveMode == InterleaveMode.None)
        {
            int byteCountComponent = stride * FrameInfo.Height;
            int lastComponent = FrameInfo.ComponentCount - 1;
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
        _scanEncoder = new ScanEncoder(
            new FrameInfo(FrameInfo!.Width, FrameInfo.Height, FrameInfo.BitsPerSample, componentCount),
            codingParameters,
            new CodingParameters
            {
                InterleaveMode = InterleaveMode,
                NearLossless = NearLossless,
                RestartInterval = 0,
                ColorTransformation = ColorTransformation
            });

        int bytesWritten = _scanEncoder.EncodeScan(source, _writer.GetRemainingDestination(), stride);

        // Synchronize the destination encapsulated in the writer (encode_scan works on a local copy)
        _writer.AdvancePosition(bytesWritten);
    }

    /// <summary>
    /// Writes a SPIFF header to the destination memory buffer.
    /// A SPIFF header is optional, but recommended for standalone JPEG-LS files.
    /// It should not be used when embedding a JPEG-LS image in a DICOM file.
    /// </summary>
    /// <param name="spiffHeader">Reference to a SPIFF header that will be written to the destination buffer.</param>
    public void WriteSpiffHeader(SpiffHeader spiffHeader)
    {
        ThrowHelper.ThrowIfOutsideRange(1, int.MaxValue, spiffHeader.Height, ErrorCode.InvalidArgumentHeight);
        ThrowHelper.ThrowIfOutsideRange(1, int.MaxValue, spiffHeader.Width, ErrorCode.InvalidArgumentWidth);
        ThrowHelper.ThrowInvalidOperationIfFalse(_state == State.DestinationSet);

        _writer.WriteStartOfImage();
        _writer.WriteSpiffHeaderSegment(spiffHeader);
        _state = State.SpiffHeader;
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
        ThrowHelper.ThrowInvalidOperationIfFalse(IsFrameInfoConfigured());
        var spiffHeader = new SpiffHeader
        {
            ColorSpace = colorSpace,
            Height = FrameInfo!.Height,
            Width = FrameInfo.Width,
            BitsPerSample = FrameInfo!.BitsPerSample,
            ComponentCount = FrameInfo.ComponentCount,
            ResolutionUnit = resolutionUnit,
            VerticalResolution = verticalResolution,
            HorizontalResolution = horizontalResolution
        };

        WriteSpiffHeader(spiffHeader);
    }

    /// <summary>
    /// Writes a SPIFF directory entry to the destination.
    /// </summary>
    /// <param name="entryTag">The entry tag of the directory entry.</param>
    /// <param name="entryData">The data of the directory entry.</param>
    public void WriteSpiffEntry(SpiffEntryTag entryTag, ReadOnlySpan<byte> entryData)
    {
        WriteSpiffEntry((int)entryTag, entryData);
    }

    /// <summary>
    /// Writes a SPIFF directory entry to the destination.
    /// </summary>
    /// <param name="entryTag">The entry tag of the directory entry.</param>
    /// <param name="entryData">The data of the directory entry.</param>
    public void WriteSpiffEntry(int entryTag, ReadOnlySpan<byte> entryData)
    {
        ThrowHelper.ThrowArgumentExceptionIfFalse(entryTag != Constants.SpiffEndOfDirectoryEntryType, nameof(entryTag));
        ThrowHelper.ThrowArgumentExceptionIfFalse(entryData.Length <= 65528, nameof(entryData), ErrorCode.InvalidArgumentSize);
        ThrowHelper.ThrowInvalidOperationIfFalse(_state == State.SpiffHeader);

        _writer.WriteSpiffDirectoryEntry(entryTag, entryData);
    }

    /// <summary>
    /// Writes a SPIFF end of directory entry to the destination.
    /// The encoder will normally do this automatically. It is made available
    /// for the scenario to create SPIFF headers in front of existing JPEG-LS streams.
    /// </summary>
    /// <remarks>
    /// The end of directory also includes a SOI marker. This marker should be skipped from the JPEG-LS stream.
    /// </remarks>
    public void WriteSpiffEndOfDirectoryEntry()
    {
        ThrowHelper.ThrowInvalidOperationIfFalse(_state == State.SpiffHeader);
        TransitionToTablesAndMiscellaneousState();
    }

    /// <summary>
    /// Writes a comment (COM) segment to the destination.
    /// </summary>
    /// <remarks>
    /// Function should be called before encoding the image data.
    /// </remarks>
    /// <param name="comment">The 'comment' bytes. Application specific, usually human-readable UTF-8 string.</param>
    public void WriteComment(ReadOnlySpan<byte> comment)
    {
        ThrowHelper.ThrowArgumentExceptionIfFalse(comment.Length <= Constants.SegmentMaxDataSize, nameof(comment));
        ThrowHelper.ThrowInvalidOperationIfFalse(_state is >= State.DestinationSet and < State.Completed);

        TransitionToTablesAndMiscellaneousState();
        _writer.WriteCommentSegment(comment);
    }

    /// <summary>
    /// Writes a comment (COM) segment to the destination.
    /// </summary>
    /// <remarks>
    /// Function should be called before encoding the image data.
    /// </remarks>
    /// <param name="comment">Application specific value, usually human-readable UTF-8 string.</param>
    public void WriteComment(string comment)
    {
        WriteComment(ToUtf8(comment).Span);
    }

    /// <summary>
    /// Writes an application data (APPn) segment to the destination.
    /// </summary>
    /// <remarks>
    /// Function should be called before encoding the image data.
    /// </remarks>
    /// <param name="applicationDataId">The ID of the application data segment in the range [0..15].</param>
    /// <param name="applicationData">The 'application data' bytes. Application specific.</param>
    public void WriteApplicationData(int applicationDataId, ReadOnlySpan<byte> applicationData)
    {
        ThrowHelper.ThrowIfOutsideRange(Constants.MinimumApplicationDataId, Constants.MaximumApplicationDataId, applicationDataId);
        ThrowHelper.ThrowArgumentExceptionIfFalse(applicationData.Length <= Constants.SegmentMaxDataSize, nameof(applicationData));
        ThrowHelper.ThrowInvalidOperationIfFalse(_state is >= State.DestinationSet and < State.Completed);

        TransitionToTablesAndMiscellaneousState();
        _writer.WriteApplicationDataSegment(applicationDataId, applicationData);
    }

    /// <summary>
    /// Writes a mapping table segment to the destination.
    /// </summary>
    /// <remarks>
    /// No validation is performed if the table ID is unique and if the table size matches the required size.
    /// </remarks>
    /// <param name="tableId">Table ID. Unique identifier of the mapping table in the range [1..255]</param>
    /// <param name="entrySize">Size in bytes of a single table entry.</param>
    /// <param name="tableData">Buffer that holds the mapping table.</param>
    public void WriteMappingTable(int tableId, int entrySize, ReadOnlySpan<byte> tableData)
    {
        ThrowHelper.ThrowIfOutsideRange(Constants.MinimumMappingTableId, Constants.MaximumMappingTableId, tableId);
        ThrowHelper.ThrowIfOutsideRange(Constants.MinimumMappingEntrySize, Constants.MaximumMappingEntrySize, entrySize);
        ThrowHelper.ThrowArgumentExceptionIfFalse(tableData.Length >= entrySize, nameof(tableData), ErrorCode.InvalidArgumentSize);
        ThrowHelper.ThrowInvalidOperationIfFalse(_state is >= State.DestinationSet and < State.Completed);

        TransitionToTablesAndMiscellaneousState();
        _writer.WriteJpegLSPresetParametersSegment(tableId, entrySize, tableData);
    }

    /// <summary>
    /// Creates a JPEG-LS stream in abbreviated format that only contain mapping tables (See JPEG-LS standard, C.4).
    /// These tables should have been written to the stream first with the method write_mapping_table.
    /// </summary>
    public void CreateAbbreviatedFormat()
    {
        ThrowHelper.ThrowInvalidOperationIfFalse(_state == State.TablesAndMiscellaneous);
        WriteEndOfImage();
    }

    private void TransitionToTablesAndMiscellaneousState()
    {
        switch (_state)
        {
            case State.TablesAndMiscellaneous:
                return;
            case State.SpiffHeader:
                _writer.WriteSpiffEndOfDirectoryEntry();
                break;
            default:
                Debug.Assert(_state == State.DestinationSet);
                _writer.WriteStartOfImage();
                break;
        }

        if (EncodingOptions.HasFlag(EncodingOptions.IncludeVersionNumber))
        {
            string informationVersion = RemoveGitHash(Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion!);

            byte[] utfBytes = Encoding.UTF8.GetBytes(informationVersion);

            var versionNumber = "charls-dotnet "u8.ToArray().Concat(utfBytes).ToArray();
            _writer.WriteCommentSegment(versionNumber);
        }

        _state = State.TablesAndMiscellaneous;
        return;

        static string RemoveGitHash(string version)
        {
            int plusIndex = version.IndexOf('+', StringComparison.InvariantCulture);
            return plusIndex != -1 ? version[..plusIndex] : version;
        }
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
            ThrowHelper.ThrowArgumentException(ErrorCode.InvalidArgumentInterleaveMode);
    }

    private int CalculateStride()
    {
        int stride = FrameInfo!.Width * Algorithm.BitToByteCount(FrameInfo.BitsPerSample);
        if (_interleaveMode == InterleaveMode.None)
            return stride;

        return stride * FrameInfo.ComponentCount;
    }

    private void CheckStride(int stride, int sourceSize)
    {
        int minimumStride = CalculateStride();
        if (stride < minimumStride)
            ThrowHelper.ThrowArgumentException(ErrorCode.InvalidArgumentStride);

        // Simple check to verify user input, and prevent out-of-bound read access.
        // Stride parameter defines the number of bytes on a scan line.
        if (InterleaveMode == InterleaveMode.None)
        {
            int minimumSourceSize =
                (stride * FrameInfo!.ComponentCount * FrameInfo.Height) - (stride - minimumStride);

            if (sourceSize < minimumSourceSize)
                ThrowHelper.ThrowArgumentException(ErrorCode.InvalidArgumentStride);
        }
        else
        {
            int minimumSourceSize = (stride * FrameInfo!.Height) - (stride - minimumStride);
            if (sourceSize < minimumSourceSize)
                ThrowHelper.ThrowArgumentException(ErrorCode.InvalidArgumentStride);
        }
    }

    private bool IsFrameInfoConfigured()
    {
        return FrameInfo != null;
    }

    private static ReadOnlyMemory<byte> ToUtf8(string text)
    {
        if (string.IsNullOrEmpty(text))
            return new ReadOnlyMemory<byte>(); // TODO -> change to readonly span?

        var utf8Encoded = new byte[Encoding.UTF8.GetMaxByteCount(text.Length) + 1];
        int bytesWritten = Encoding.UTF8.GetBytes(text, 0, text.Length, utf8Encoded, 0);
        utf8Encoded[bytesWritten] = 0;

        return new ReadOnlyMemory<byte>(utf8Encoded, 0, bytesWritten + 1);
    }
}
