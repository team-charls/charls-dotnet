// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Buffers.Binary;
using System.Runtime.InteropServices;

using static CharLS.Managed.Algorithm;

namespace CharLS.Managed;

internal struct ScanDecoder
{
    private const int CacheBitCount = sizeof(ulong) * 8;
    private const int MaxReadableCacheBits = CacheBitCount - 8;

    private static readonly GolombCodeMatchTable[] GolombCodeTable =
    [
        new(0), new(1), new(2), new(3),
        new(4), new(5), new(6), new(7),
        new(8), new(9), new(10), new(11),
        new(12), new(13), new(14), new(15)
    ];

    private readonly CopyFromLineBuffer.Method _copyFromLineBuffer;
    private ScanCodec _scanCodec;

    private ReadOnlyMemory<byte> _source;
    private int _position;
    private int _endPosition;
    private int _positionFF;
    private int _validBits;
    private ulong _readCache;
    private int _restartIntervalCounter;
    private int _restartInterval;

    internal ScanDecoder(FrameInfo frameInfo, JpegLSPresetCodingParameters presetCodingParameters, CodingParameters codingParameters)
    {
        var traits = Traits.Create(frameInfo.BitsPerSample, codingParameters.NearLossless);
        _scanCodec = new ScanCodec(traits, frameInfo, presetCodingParameters, codingParameters);

        _copyFromLineBuffer = CopyFromLineBuffer.GetCopyMethod(
            FrameInfo.BitsPerSample, CodingParameters.InterleaveMode, FrameInfo.ComponentCount, CodingParameters.ColorTransformation);

        _scanCodec.InitializeParameters(traits.Range);
    }

    private readonly FrameInfo FrameInfo => _scanCodec.FrameInfo;

    private readonly CodingParameters CodingParameters => _scanCodec.CodingParameters;

    private readonly Traits Traits => _scanCodec.Traits;

    private readonly int PixelStride => FrameInfo.Width + 2;

    internal int DecodeScan(ReadOnlyMemory<byte> source, Span<byte> destination, int stride)
    {
        Initialize(source);

        // Process images without a restart interval, as 1 large restart interval.
        _restartInterval = CodingParameters.RestartInterval == 0 ? FrameInfo.Height : CodingParameters.RestartInterval;

        if (FrameInfo.BitsPerSample <= 8)
        {
            DecodeLines8Bit(destination, stride);
        }
        else
        {
            DecodeLines16Bit(destination, stride);
        }

        EndScan();

        return GetActualPosition();
    }

    internal void Initialize(ReadOnlyMemory<byte> source)
    {
        _source = source;
        _position = 0;
        _endPosition = source.Length;

        FindJpegMarkerStartByte();
        FillReadCache();
    }

    internal void SkipBits(int bitCount)
    {
        Debug.Assert(bitCount > 0);

        _validBits -= bitCount; // Note: valid_bits_ may become negative to indicate that extra bits are needed.
        _readCache <<= bitCount;
    }

    internal void EndScan()
    {
        if (_position >= _endPosition)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.NeedMoreData);

        var source = _source.Span;
        if (source[_position] != Constants.JpegMarkerStartByte)
        {
            _ = ReadBit();

            if (_position >= _endPosition)
                ThrowHelper.ThrowInvalidDataException(ErrorCode.NeedMoreData);

            if (source[_position] != Constants.JpegMarkerStartByte)
                ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidData);
        }

        if (_readCache != 0)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidData);
    }

    internal int GetActualPosition()
    {
        int validBits = _validBits;

        var source = _source.Span;
        for (; ; )
        {
            int lastBitsCount = source[_position - 1] == Constants.JpegMarkerStartByte ? 7 : 8;

            if (validBits < lastBitsCount)
                return _position;

            validBits -= lastBitsCount;
            --_position;
        }
    }

    /// <summary>
    /// Step F.1, 9: decode the mapped error value MErrval from the limited golomb code stored in the bitstream.
    /// </summary>
    internal int DecodeMappedErrorValue(int k, int limit, int quantizedBitsPerPixel)
    {
        int unaryCode = ReadUnaryCode();

        if (unaryCode < limit - quantizedBitsPerPixel - 1)
        {
            // Option a: mapped error value is stored as golomb code.
            return k == 0 ? unaryCode : (unaryCode << k) + ReadValue(k);
        }

        // Option b: unary code was escape code as mapped error value was too large,
        // read mapped error value - 1 from bitstream.
        return ReadValue(quantizedBitsPerPixel) + 1;
    }

    internal int ReadValue(int bitCount)
    {
        Debug.Assert(bitCount is > 0 and < 32);

        if (_validBits < bitCount)
        {
            FillReadCache();
            if (_validBits < bitCount)
                ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidData);
        }

        Debug.Assert(bitCount <= _validBits);
        int result = (int)(_readCache >> (CacheBitCount - bitCount));
        SkipBits(bitCount);
        return result;
    }

    /// <summary>
    /// Reads a byte from the bitstream without removing it.
    /// This byte is used to check if there is a pre-computed Golomb code available.
    /// </summary>
    internal int PeekByte()
    {
        if (_validBits < 8)
        {
            FillReadCache();
        }

        return (int)(_readCache >> MaxReadableCacheBits);
    }

    internal nuint ReadBit()
    {
        if (_validBits <= 0)
        {
            FillReadCache();
        }

        nuint bit = (nuint)(_readCache >> (CacheBitCount - 1));
        SkipBits(1);
        return bit;
    }

    /// <summary>
    /// Peek how many leading zero bits are present.
    /// </summary>
    /// <returns>
    /// The number of leading zero bits or -1 when there are more than 16 leading zero bits.
    /// </returns>
    internal int Peek0Bits()
    {
        if (_validBits < 16)
        {
            FillReadCache();
        }

        var valueTest = _readCache;
        for (int count = 0; count < 16; ++count)
        {
            if ((valueTest & (1UL << (CacheBitCount - 1))) != 0)
                return count;

            valueTest <<= 1;
        }

        return -1;
    }

    /// <summary>
    /// Read zero bits until the first high bit.
    /// </summary>
    internal int ReadUnaryCode()
    {
        int count = Peek0Bits();
        if (count >= 0)
        {
            SkipBits(count + 1);
            return count;
        }

        SkipBits(15);
        for (int zeroBitCount = 15; ; ++zeroBitCount)
        {
            if (ReadBit() == 1)
                return zeroBitCount;
        }
    }

    internal byte ReadByte()
    {
        if (_position == _endPosition)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.NeedMoreData);

        byte value = _source.Span[_position];
        ++_position;
        return value;
    }

    internal void ProcessRestartMarker()
    {
        ReadRestartMarker(Constants.JpegRestartMarkerBase + _restartIntervalCounter);
        _restartIntervalCounter = (_restartIntervalCounter + 1) % Constants.JpegRestartMarkerRange;

        ReInitializeReadCache();
    }

    private void ReadRestartMarker(int expectedRestartMarkerId)
    {
        byte value = ReadByte();

        if (value != Constants.JpegMarkerStartByte)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.RestartMarkerNotFound);

        // Read all preceding 0xFF fill bytes until a non 0xFF byte has been found. (see T.81, B.1.1.2)
        do
        {
            value = ReadByte();
        }
        while (value == Constants.JpegMarkerStartByte);

        if (value != expectedRestartMarkerId)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.RestartMarkerNotFound);
    }

    private void ReInitializeReadCache()
    {
        _validBits = 0;
        _readCache = 0;

        FindJpegMarkerStartByte();
        FillReadCache();
    }

    private void FindJpegMarkerStartByte()
    {
        int positionFF = _source[_position..].Span.IndexOf(Constants.JpegMarkerStartByte);
        _positionFF = positionFF == -1 ? _endPosition : _position + positionFF;
    }

    private void FillReadCache()
    {
        Debug.Assert(_validBits <= MaxReadableCacheBits);

        if (FillReadCacheOptimistic())
            return;

        var source = _source.Span;
        do
        {
            if (_position >= _endPosition)
            {
                if (_validBits <= 0)
                {
                    // Decoding process expects at least some bits to be added to the cache.
                    ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidData);
                }

                return;
            }

            ulong newByteValue = source[_position];

            // JPEG-LS bit stream rule: if FF is followed by a 1 bit then it is a marker
            if (newByteValue == Constants.JpegMarkerStartByte &&
                (_position == _endPosition - 1 || (source[_position + 1] & 0x80) != 0))
            {
                if (_validBits <= 0)
                {
                    // Decoding process expects at least some bits to be added to the cache.
                    ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidData);
                }

                // End of buffer or marker detected. Typical found markers are EOI, SOS (next scan) or RSTm.
                return;
            }

            _readCache |= newByteValue << (MaxReadableCacheBits - _validBits);
            _validBits += 8;
            ++_position;

            if (newByteValue == Constants.JpegMarkerStartByte)
            {
                // The next bit after an 0xFF needs to be ignored, compensate for the next read (see ISO/IEC 14495-1,A.1)
                --_validBits;
            }
        }
        while (_validBits < MaxReadableCacheBits);

        FindJpegMarkerStartByte();
    }

    private bool FillReadCacheOptimistic()
    {
        if (_position >= _positionFF - (sizeof(ulong) - 1))
            return false;

        // Easy & fast: there is no 0xFF byte in sight, read without bit stuffing
        _readCache |= BinaryPrimitives.ReadUInt64BigEndian(_source.Span[_position..]) >> _validBits;
        int bytesConsumed = (CacheBitCount - _validBits) / 8;
        _position += bytesConsumed;
        _validBits += bytesConsumed * 8;
        Debug.Assert(_validBits >= MaxReadableCacheBits);
        return true;
    }

    private void DecodeLines8Bit(Span<byte> destination, int stride)
    {
        switch (CodingParameters.InterleaveMode)
        {
            case InterleaveMode.None:
                DecodeLines8BitInterleaveModeNone(destination, stride);
                break;

            case InterleaveMode.Line:
                DecodeLines8BitInterleaveModeLine(destination, stride);
                break;

            case InterleaveMode.Sample:
                switch (FrameInfo.ComponentCount)
                {
                    case 2:
                        DecodeLines8Bit2ComponentsInterleaveModeSample(destination, stride);
                        break;
                    case 3:
                        DecodeLines8Bit3ComponentsInterleaveModeSample(destination, stride);
                        break;
                    case 4:
                        DecodeLines8Bit4ComponentsInterleaveModeSample(destination, stride);
                        break;
                }

                break;
        }
    }

    private void DecodeLines16Bit(Span<byte> destination, int stride)
    {
        switch (CodingParameters.InterleaveMode)
        {
            case InterleaveMode.None:
                DecodeLines16BitInterleaveModeNone(destination, stride);
                break;

            case InterleaveMode.Line:
                DecodeLines16BitInterleaveModeLine(destination, stride);
                break;

            case InterleaveMode.Sample:
                switch (FrameInfo.ComponentCount)
                {
                    case 2:
                        DecodeLines16Bit2ComponentsInterleaveModeSample(destination, stride);
                        break;

                    case 3:
                        DecodeLines16Bit3ComponentsInterleaveModeSample(destination, stride);
                        break;
                    case 4:
                        DecodeLines16Bit4ComponentsInterleaveModeSample(destination, stride);
                        break;
                }

                break;
        }
    }

    private void DecodeLines8BitInterleaveModeNone(Span<byte> destination, int stride)
    {
        int pixelStride = PixelStride;
        using var rentedArray = ArrayPoolHelper.Rent<byte>(pixelStride * 2);
        Span<byte> lineBuffer = rentedArray.Value;

        for (int line = 0; ;)
        {
            int linesInInterval = Math.Min(FrameInfo.Height - line, _restartInterval);

            for (int mcu = 0; ;)
            {
                var previousLine = lineBuffer;
                var currentLine = lineBuffer[pixelStride..];
                if ((mcu & 1) == 1)
                {
                    var temp = previousLine;
                    previousLine = currentLine;
                    currentLine = temp;
                }

                // Initialize edge pixels used for prediction
                previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
                currentLine[0] = previousLine[1];

                DecodeSampleLine(previousLine, currentLine);

                CopyLineBufferToDestinationInterleaveNone(currentLine[1..], destination, FrameInfo.Width);

                ++mcu;
                if (mcu == linesInInterval)
                {
                    line += mcu;
                    break;
                }

                destination = destination[stride..];
            }

            if (line == FrameInfo.Height)
                break;

            destination = destination[stride..];
            ProcessRestartMarker();

            // After a restart marker it is required to reset the decoder.
            lineBuffer.Clear();
            _scanCodec.InitializeParameters(Traits.Range);
        }
    }

    private void DecodeLines16BitInterleaveModeNone(Span<byte> destination, int stride)
    {
        int pixelStride = PixelStride;
        using var rentedArray = ArrayPoolHelper.Rent<ushort>(pixelStride * 2);
        Span<ushort> lineBuffer = rentedArray.Value;

        for (int line = 0; ;)
        {
            int linesInInterval = Math.Min(FrameInfo.Height - line, _restartInterval);

            for (int mcu = 0; ;)
            {
                var previousLine = lineBuffer;
                var currentLine = lineBuffer[pixelStride..];
                if ((mcu & 1) == 1)
                {
                    var temp = previousLine;
                    previousLine = currentLine;
                    currentLine = temp;
                }

                // Initialize edge pixels used for prediction
                previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
                currentLine[0] = previousLine[1];

                DecodeSampleLine(previousLine, currentLine);

                CopyLineBufferToDestinationInterleaveNone(currentLine[1..], destination, FrameInfo.Width);

                ++mcu;
                if (mcu == linesInInterval)
                {
                    line += mcu;
                    break;
                }

                destination = destination[stride..];
            }

            if (line == FrameInfo.Height)
                break;

            destination = destination[stride..];
            ProcessRestartMarker();

            // After a restart marker it is required to reset the decoder.
            lineBuffer.Clear();
            _scanCodec.InitializeParameters(Traits.Range);
        }
    }

    private void DecodeLines8BitInterleaveModeLine(Span<byte> destination, int stride)
    {
        int pixelStride = FrameInfo.Width + 2;
        int componentCount = FrameInfo.ComponentCount;
        using var rentedArray = ArrayPoolHelper.Rent<byte>(componentCount * pixelStride * 2);
        Span<byte> lineBuffer = rentedArray.Value;
        Span<int> runIndex = stackalloc int[componentCount];

        for (int line = 0; ;)
        {
            int linesInInterval = Math.Min(FrameInfo.Height - line, _restartInterval);

            for (int mcu = 0; ;)
            {
                var previousLine = lineBuffer;
                var currentLine = lineBuffer[(componentCount * pixelStride)..];
                bool oddLine = (mcu & 1) == 1;
                if (oddLine)
                {
                    var temp = previousLine;
                    previousLine = currentLine;
                    currentLine = temp;
                }

                for (int component = 0; component < componentCount; ++component)
                {
                    _scanCodec.RunIndex = runIndex[component];

                    // Initialize edge pixels used for prediction
                    previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
                    currentLine[0] = previousLine[1];

                    DecodeSampleLine(previousLine, currentLine);

                    runIndex[component] = _scanCodec.RunIndex;
                    currentLine = currentLine[pixelStride..];
                    previousLine = previousLine[pixelStride..];
                }

                int startPosition = (oddLine ? 0 : (pixelStride * componentCount)) + 1;
                CopyLineBufferToDestinationInterleaveLine(lineBuffer[startPosition..], destination, FrameInfo.Width);

                ++mcu;
                if (mcu == linesInInterval)
                {
                    line += mcu;
                    break;
                }

                destination = destination[stride..];
            }

            if (line == FrameInfo.Height)
                break;

            destination = destination[stride..];
            ProcessRestartMarker();

            // After a restart marker it is required to reset the decoder.
            lineBuffer.Clear();
            runIndex.Clear();
            _scanCodec.InitializeParameters(Traits.Range);
        }
    }

    private void DecodeLines16BitInterleaveModeLine(Span<byte> destination, int stride)
    {
        int pixelStride = FrameInfo.Width + 2;
        int componentCount = FrameInfo.ComponentCount;
        using var rentedArray = ArrayPoolHelper.Rent<ushort>(componentCount * pixelStride * 2);
        Span<ushort> lineBuffer = rentedArray.Value;
        Span<int> runIndex = stackalloc int[componentCount];

        for (int line = 0; ;)
        {
            int linesInInterval = Math.Min(FrameInfo.Height - line, _restartInterval);

            for (int mcu = 0; ;)
            {
                var previousLine = lineBuffer;
                var currentLine = lineBuffer[(componentCount * pixelStride)..];
                bool oddLine = (mcu & 1) == 1;
                if (oddLine)
                {
                    var temp = previousLine;
                    previousLine = currentLine;
                    currentLine = temp;
                }

                for (int component = 0; component < componentCount; ++component)
                {
                    _scanCodec.RunIndex = runIndex[component];

                    // Initialize edge pixels used for prediction
                    previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
                    currentLine[0] = previousLine[1];

                    DecodeSampleLine(previousLine, currentLine);

                    runIndex[component] = _scanCodec.RunIndex;
                    currentLine = currentLine[pixelStride..];
                    previousLine = previousLine[pixelStride..];
                }

                int startPosition = (oddLine ? 0 : (pixelStride * componentCount)) + 1;
                CopyLineBufferToDestinationInterleaveLine(lineBuffer[startPosition..], destination, FrameInfo.Width);

                ++mcu;
                if (mcu == linesInInterval)
                {
                    line += mcu;
                    break;
                }

                destination = destination[stride..];
            }

            if (line == FrameInfo.Height)
                break;

            destination = destination[stride..];
            ProcessRestartMarker();

            // After a restart marker it is required to reset the decoder.
            lineBuffer.Clear();
            runIndex.Clear();
            _scanCodec.InitializeParameters(Traits.Range);
        }
    }

    private void DecodeLines8Bit2ComponentsInterleaveModeSample(Span<byte> destination, int stride)
    {
        int pixelStride = PixelStride;
        using var rentedArray = ArrayPoolHelper.Rent<Pair<byte>>(pixelStride * 2);
        Span<Pair<byte>> lineBuffer = rentedArray.Value;

        for (int line = 0; ;)
        {
            int linesInInterval = Math.Min(FrameInfo.Height - line, _restartInterval);

            for (int mcu = 0; ;)
            {
                var previousLine = lineBuffer;
                var currentLine = lineBuffer[pixelStride..];
                if ((mcu & 1) == 1)
                {
                    var temp = previousLine;
                    previousLine = currentLine;
                    currentLine = temp;
                }

                // Initialize edge pixels used for prediction
                previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
                currentLine[0] = previousLine[1];

                DecodePairLine(previousLine, currentLine);

                CopyLineBufferToDestination(currentLine[1..], destination, FrameInfo.Width);

                ++mcu;
                if (mcu == linesInInterval)
                {
                    line += mcu;
                    break;
                }

                destination = destination[stride..];
            }

            if (line == FrameInfo.Height)
                break;

            destination = destination[stride..];
            ProcessRestartMarker();

            // After a restart marker it is required to reset the decoder.
            lineBuffer.Clear();
            _scanCodec.InitializeParameters(Traits.Range);
        }
    }

    private void DecodeLines8Bit3ComponentsInterleaveModeSample(Span<byte> destination, int stride)
    {
        int pixelStride = PixelStride;
        using var rentedArray = ArrayPoolHelper.Rent<Triplet<byte>>(pixelStride * 2);
        Span<Triplet<byte>> lineBuffer = rentedArray.Value;

        for (int line = 0; ;)
        {
            int linesInInterval = Math.Min(FrameInfo.Height - line, _restartInterval);

            for (int mcu = 0; ;)
            {
                var previousLine = lineBuffer;
                var currentLine = lineBuffer[pixelStride..];
                if ((mcu & 1) == 1)
                {
                    var temp = previousLine;
                    previousLine = currentLine;
                    currentLine = temp;
                }

                // Initialize edge pixels used for prediction
                previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
                currentLine[0] = previousLine[1];

                DecodeTripletLine(previousLine, currentLine);

                CopyLineBufferToDestination(currentLine[1..], destination, FrameInfo.Width);

                ++mcu;
                if (mcu == linesInInterval)
                {
                    line += mcu;
                    break;
                }

                destination = destination[stride..];
            }

            if (line == FrameInfo.Height)
                break;

            destination = destination[stride..];
            ProcessRestartMarker();

            // After a restart marker it is required to reset the decoder.
            lineBuffer.Clear();
            _scanCodec.InitializeParameters(Traits.Range);
        }
    }

    private void DecodeLines16Bit2ComponentsInterleaveModeSample(Span<byte> destination, int stride)
    {
        int pixelStride = PixelStride;
        using var rentedArray = ArrayPoolHelper.Rent<Pair<ushort>>(pixelStride * 2);
        Span<Pair<ushort>> lineBuffer = rentedArray.Value;

        for (int line = 0; ;)
        {
            int linesInInterval = Math.Min(FrameInfo.Height - line, _restartInterval);

            for (int mcu = 0; ;)
            {
                var previousLine = lineBuffer;
                var currentLine = lineBuffer[pixelStride..];
                if ((mcu & 1) == 1)
                {
                    var temp = previousLine;
                    previousLine = currentLine;
                    currentLine = temp;
                }

                // Initialize edge pixels used for prediction
                previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
                currentLine[0] = previousLine[1];

                DecodePairLine(previousLine, currentLine);

                CopyLineBufferToDestination(currentLine[1..], destination, FrameInfo.Width);

                ++mcu;
                if (mcu == linesInInterval)
                {
                    line += mcu;
                    break;
                }

                destination = destination[stride..];
            }

            if (line == FrameInfo.Height)
                break;

            destination = destination[stride..];
            ProcessRestartMarker();

            // After a restart marker it is required to reset the decoder.
            lineBuffer.Clear();
            _scanCodec.InitializeParameters(Traits.Range);
        }
    }

    private void DecodeLines16Bit3ComponentsInterleaveModeSample(Span<byte> destination, int stride)
    {
        int pixelStride = PixelStride;
        using var rentedArray = ArrayPoolHelper.Rent<Triplet<ushort>>(pixelStride * 2);
        Span<Triplet<ushort>> lineBuffer = rentedArray.Value;

        for (int line = 0; ;)
        {
            int linesInInterval = Math.Min(FrameInfo.Height - line, _restartInterval);

            for (int mcu = 0; ;)
            {
                var previousLine = lineBuffer;
                var currentLine = lineBuffer[pixelStride..];
                if ((mcu & 1) == 1)
                {
                    var temp = previousLine;
                    previousLine = currentLine;
                    currentLine = temp;
                }

                // Initialize edge pixels used for prediction
                previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
                currentLine[0] = previousLine[1];

                DecodeTripletLine(previousLine, currentLine);

                CopyLineBufferToDestination(currentLine[1..], destination, FrameInfo.Width);

                ++mcu;
                if (mcu == linesInInterval)
                {
                    line += mcu;
                    break;
                }

                destination = destination[stride..];
            }

            if (line == FrameInfo.Height)
                break;

            destination = destination[stride..];
            ProcessRestartMarker();

            // After a restart marker it is required to reset the decoder.
            lineBuffer.Clear();
            _scanCodec.InitializeParameters(Traits.Range);
        }
    }

    private void DecodeLines8Bit4ComponentsInterleaveModeSample(Span<byte> destination, int stride)
    {
        int pixelStride = PixelStride;
        using var rentedArray = ArrayPoolHelper.Rent<Quad<byte>>(pixelStride * 2);
        Span<Quad<byte>> lineBuffer = rentedArray.Value;

        for (int line = 0; ;)
        {
            int linesInInterval = Math.Min(FrameInfo.Height - line, _restartInterval);

            for (int mcu = 0; ;)
            {
                var previousLine = lineBuffer;
                var currentLine = lineBuffer[pixelStride..];
                if ((mcu & 1) == 1)
                {
                    var temp = previousLine;
                    previousLine = currentLine;
                    currentLine = temp;
                }

                // Initialize edge pixels used for prediction
                previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
                currentLine[0] = previousLine[1];

                DecodeQuadLine(previousLine, currentLine);

                CopyLineBufferToDestination(currentLine[1..], destination, FrameInfo.Width);

                ++mcu;
                if (mcu == linesInInterval)
                {
                    line += mcu;
                    break;
                }

                destination = destination[stride..];
            }

            if (line == FrameInfo.Height)
                break;

            destination = destination[stride..];
            ProcessRestartMarker();

            // After a restart marker it is required to reset the decoder.
            lineBuffer.Clear();
            _scanCodec.InitializeParameters(Traits.Range);
        }
    }

    private void DecodeLines16Bit4ComponentsInterleaveModeSample(Span<byte> destination, int stride)
    {
        int pixelStride = FrameInfo.Width + 2;
        using var rentedArray = ArrayPoolHelper.Rent<Quad<ushort>>(pixelStride * 2);
        Span<Quad<ushort>> lineBuffer = rentedArray.Value;

        for (int line = 0; ;)
        {
            int linesInInterval = Math.Min(FrameInfo.Height - line, _restartInterval);

            for (int mcu = 0; ;)
            {
                var previousLine = lineBuffer;
                var currentLine = lineBuffer[pixelStride..];
                if ((mcu & 1) == 1)
                {
                    var temp = previousLine;
                    previousLine = currentLine;
                    currentLine = temp;
                }

                // Initialize edge pixels used for prediction
                previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
                currentLine[0] = previousLine[1];

                DecodeQuadLine(previousLine, currentLine);

                CopyLineBufferToDestination(currentLine[1..], destination, FrameInfo.Width);

                ++mcu;
                if (mcu == linesInInterval)
                {
                    line += mcu;
                    break;
                }

                destination = destination[stride..];
            }

            if (line == FrameInfo.Height)
                break;

            destination = destination[stride..];
            ProcessRestartMarker();

            // After a restart marker it is required to reset the decoder.
            lineBuffer.Clear();
            _scanCodec.InitializeParameters(Traits.Range);
        }
    }

    private readonly int QuantizeGradient(int di)
    {
        Debug.Assert(_scanCodec.QuantizeGradient(di, _scanCodec.NearLossless) == _scanCodec.QuantizationLut[(_scanCodec.QuantizationLut.Length / 2) + di]);
        return _scanCodec.QuantizationLut[(_scanCodec.QuantizationLut.Length / 2) + di];
    }

    private void DecodeSampleLine(Span<byte> previousLine, Span<byte> currentLine)
    {
        int index = 1;
        int rb = previousLine[0];
        int rd = previousLine[index];

        while (index <= FrameInfo.Width)
        {
            int ra = currentLine[index - 1];
            int rc = rb;
            rb = rd;
            rd = previousLine[index + 1];

            int qs = ComputeContextId(
                QuantizeGradient(rd - rb), QuantizeGradient(rb - rc), QuantizeGradient(rc - ra));
            if (qs == 0)
            {
                index += DecodeRunMode(index, previousLine, currentLine);
                rb = previousLine[index - 1];
                rd = previousLine[index];
            }
            else
            {
                currentLine[index] = (byte)DecodeRegular(qs, ComputePredictedValue(ra, rb, rc));
                ++index;
            }
        }
    }

    private void DecodeSampleLine(Span<ushort> previousLine, Span<ushort> currentLine)
    {
        int index = 1;
        int rb = previousLine[0];
        int rd = previousLine[index];

        while (index <= FrameInfo.Width)
        {
            int ra = currentLine[index - 1];
            int rc = rb;
            rb = rd;
            rd = previousLine[index + 1];

            int qs = ComputeContextId(
                QuantizeGradient(rd - rb), QuantizeGradient(rb - rc), QuantizeGradient(rc - ra));
            if (qs == 0)
            {
                index += DecodeRunMode(index, previousLine, currentLine);
                rb = previousLine[index - 1];
                rd = previousLine[index];
            }
            else
            {
                currentLine[index] = (ushort)DecodeRegular(qs, ComputePredictedValue(ra, rb, rc));
                ++index;
            }
        }
    }

    private void DecodePairLine(Span<Pair<byte>> previousLine, Span<Pair<byte>> currentLine)
    {
        int index = 1;
        while (index <= FrameInfo.Width)
        {
            var ra = currentLine[index - 1];
            var rc = previousLine[index - 1];
            var rb = previousLine[index];
            var rd = previousLine[index + 1];

            int qs1 = ComputeContextId(
                QuantizeGradient(rd.V1 - rb.V1), QuantizeGradient(rb.V1 - rc.V1), QuantizeGradient(rc.V1 - ra.V1));
            int qs2 = ComputeContextId(
                QuantizeGradient(rd.V2 - rb.V2), QuantizeGradient(rb.V2 - rc.V2), QuantizeGradient(rc.V2 - ra.V2));
            if (qs1 == 0 && qs2 == 0)
            {
                index += DecodeRunMode(index, previousLine, currentLine);
            }
            else
            {
                currentLine[index] = new Pair<byte>(
                    (byte)DecodeRegular(qs1, ComputePredictedValue(ra.V1, rb.V1, rc.V1)),
                    (byte)DecodeRegular(qs2, ComputePredictedValue(ra.V2, rb.V2, rc.V2)));
                ++index;
            }
        }
    }

    private void DecodePairLine(Span<Pair<ushort>> previousLine, Span<Pair<ushort>> currentLine)
    {
        int index = 1;
        while (index <= FrameInfo.Width)
        {
            var ra = currentLine[index - 1];
            var rc = previousLine[index - 1];
            var rb = previousLine[index];
            var rd = previousLine[index + 1];

            int qs1 = ComputeContextId(
                QuantizeGradient(rd.V1 - rb.V1), QuantizeGradient(rb.V1 - rc.V1), QuantizeGradient(rc.V1 - ra.V1));
            int qs2 = ComputeContextId(
                QuantizeGradient(rd.V2 - rb.V2), QuantizeGradient(rb.V2 - rc.V2), QuantizeGradient(rc.V2 - ra.V2));
            if (qs1 == 0 && qs2 == 0)
            {
                index += DecodeRunMode(index, previousLine, currentLine);
            }
            else
            {
                currentLine[index] = new Pair<ushort>(
                    (ushort)DecodeRegular(qs1, ComputePredictedValue(ra.V1, rb.V1, rc.V1)),
                    (ushort)DecodeRegular(qs2, ComputePredictedValue(ra.V2, rb.V2, rc.V2)));
                ++index;
            }
        }
    }

    private void DecodeTripletLine(Span<Triplet<byte>> previousLine, Span<Triplet<byte>> currentLine)
    {
        int index = 1;
        while (index <= FrameInfo.Width)
        {
            var ra = currentLine[index - 1];
            var rc = previousLine[index - 1];
            var rb = previousLine[index];
            var rd = previousLine[index + 1];

            int qs1 = ComputeContextId(
                QuantizeGradient(rd.V1 - rb.V1), QuantizeGradient(rb.V1 - rc.V1), QuantizeGradient(rc.V1 - ra.V1));
            int qs2 = ComputeContextId(
                QuantizeGradient(rd.V2 - rb.V2), QuantizeGradient(rb.V2 - rc.V2), QuantizeGradient(rc.V2 - ra.V2));
            int qs3 = ComputeContextId(
                QuantizeGradient(rd.V3 - rb.V3), QuantizeGradient(rb.V3 - rc.V3), QuantizeGradient(rc.V3 - ra.V3));
            if (qs1 == 0 && qs2 == 0 && qs3 == 0)
            {
                index += DecodeRunMode(index, previousLine, currentLine);
            }
            else
            {
                currentLine[index] = new Triplet<byte>(
                    (byte)DecodeRegular(qs1, ComputePredictedValue(ra.V1, rb.V1, rc.V1)),
                    (byte)DecodeRegular(qs2, ComputePredictedValue(ra.V2, rb.V2, rc.V2)),
                    (byte)DecodeRegular(qs3, ComputePredictedValue(ra.V3, rb.V3, rc.V3)));
                ++index;
            }
        }
    }

    private void DecodeTripletLine(Span<Triplet<ushort>> previousLine, Span<Triplet<ushort>> currentLine)
    {
        int index = 1;
        while (index <= FrameInfo.Width)
        {
            var ra = currentLine[index - 1];
            var rc = previousLine[index - 1];
            var rb = previousLine[index];
            var rd = previousLine[index + 1];

            int qs1 = ComputeContextId(
                QuantizeGradient(rd.V1 - rb.V1), QuantizeGradient(rb.V1 - rc.V1), QuantizeGradient(rc.V1 - ra.V1));
            int qs2 = ComputeContextId(
                QuantizeGradient(rd.V2 - rb.V2), QuantizeGradient(rb.V2 - rc.V2), QuantizeGradient(rc.V2 - ra.V2));
            int qs3 = ComputeContextId(
                QuantizeGradient(rd.V3 - rb.V3), QuantizeGradient(rb.V3 - rc.V3), QuantizeGradient(rc.V3 - ra.V3));
            if (qs1 == 0 && qs2 == 0 && qs3 == 0)
            {
                index += DecodeRunMode(index, previousLine, currentLine);
            }
            else
            {
                currentLine[index] = new Triplet<ushort>(
                    (ushort)DecodeRegular(qs1, ComputePredictedValue(ra.V1, rb.V1, rc.V1)),
                    (ushort)DecodeRegular(qs2, ComputePredictedValue(ra.V2, rb.V2, rc.V2)),
                    (ushort)DecodeRegular(qs3, ComputePredictedValue(ra.V3, rb.V3, rc.V3)));
                ++index;
            }
        }
    }

    private void DecodeQuadLine(Span<Quad<byte>> previousLine, Span<Quad<byte>> currentLine)
    {
        int index = 1;
        while (index <= FrameInfo.Width)
        {
            var ra = currentLine[index - 1];
            var rc = previousLine[index - 1];
            var rb = previousLine[index];
            var rd = previousLine[index + 1];

            int qs1 = ComputeContextId(
                QuantizeGradient(rd.V1 - rb.V1), QuantizeGradient(rb.V1 - rc.V1), QuantizeGradient(rc.V1 - ra.V1));
            int qs2 = ComputeContextId(
                QuantizeGradient(rd.V2 - rb.V2), QuantizeGradient(rb.V2 - rc.V2), QuantizeGradient(rc.V2 - ra.V2));
            int qs3 = ComputeContextId(
                QuantizeGradient(rd.V3 - rb.V3), QuantizeGradient(rb.V3 - rc.V3), QuantizeGradient(rc.V3 - ra.V3));
            int qs4 = ComputeContextId(
                QuantizeGradient(rd.V4 - rb.V4), QuantizeGradient(rb.V4 - rc.V4), QuantizeGradient(rc.V4 - ra.V4));

            if (qs1 == 0 && qs2 == 0 && qs3 == 0 && qs4 == 0)
            {
                index += DecodeRunMode(index, previousLine, currentLine);
            }
            else
            {
                currentLine[index] = new Quad<byte>(
                    (byte)DecodeRegular(qs1, ComputePredictedValue(ra.V1, rb.V1, rc.V1)),
                    (byte)DecodeRegular(qs2, ComputePredictedValue(ra.V2, rb.V2, rc.V2)),
                    (byte)DecodeRegular(qs3, ComputePredictedValue(ra.V3, rb.V3, rc.V3)),
                    (byte)DecodeRegular(qs3, ComputePredictedValue(ra.V4, rb.V4, rc.V4)));
                ++index;
            }
        }
    }

    private void DecodeQuadLine(Span<Quad<ushort>> previousLine, Span<Quad<ushort>> currentLine)
    {
        int index = 1;
        while (index <= FrameInfo.Width)
        {
            var ra = currentLine[index - 1];
            var rc = previousLine[index - 1];
            var rb = previousLine[index];
            var rd = previousLine[index + 1];

            int qs1 = ComputeContextId(
                QuantizeGradient(rd.V1 - rb.V1), QuantizeGradient(rb.V1 - rc.V1), QuantizeGradient(rc.V1 - ra.V1));
            int qs2 = ComputeContextId(
                QuantizeGradient(rd.V2 - rb.V2), QuantizeGradient(rb.V2 - rc.V2), QuantizeGradient(rc.V2 - ra.V2));
            int qs3 = ComputeContextId(
                QuantizeGradient(rd.V3 - rb.V3), QuantizeGradient(rb.V3 - rc.V3), QuantizeGradient(rc.V3 - ra.V3));
            int qs4 = ComputeContextId(
                QuantizeGradient(rd.V4 - rb.V4), QuantizeGradient(rb.V4 - rc.V4), QuantizeGradient(rc.V4 - ra.V4));

            if (qs1 == 0 && qs2 == 0 && qs3 == 0 && qs4 == 0)
            {
                index += DecodeRunMode(index, previousLine, currentLine);
            }
            else
            {
                currentLine[index] = new Quad<ushort>(
                    (ushort)DecodeRegular(qs1, ComputePredictedValue(ra.V1, rb.V1, rc.V1)),
                    (ushort)DecodeRegular(qs2, ComputePredictedValue(ra.V2, rb.V2, rc.V2)),
                    (ushort)DecodeRegular(qs3, ComputePredictedValue(ra.V3, rb.V3, rc.V3)),
                    (ushort)DecodeRegular(qs3, ComputePredictedValue(ra.V4, rb.V4, rc.V4)));
                ++index;
            }
        }
    }

    private int DecodeRegular(int qs, int predicted)
    {
        int sign = BitWiseSign(qs);
        ref var context = ref _scanCodec.RegularModeContext[ApplySign(qs, sign)];
        int correctedPrediction = Traits.CorrectPrediction(predicted + ApplySign(context.C, sign));
        int k = context.ComputeGolombCodingParameterChecked();

        int errorValue;
        var golombCodeMatch = GolombCodeTable[k].Get(PeekByte());
        if (golombCodeMatch.BitCount != 0)
        {
            // There is a pre-computed match.
            SkipBits(golombCodeMatch.BitCount);
            errorValue = golombCodeMatch.ErrorValue;
            Debug.Assert(Math.Abs(errorValue) <= ushort.MaxValue);
        }
        else
        {
            errorValue = UnmapErrorValue(DecodeMappedErrorValue(k, _scanCodec.Limit, _scanCodec.QuantizedBitsPerSample));
            if (OutsideRange(errorValue, ushort.MaxValue))
                ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidData);
        }

        if (k == 0)
        {
            errorValue ^= context.GetErrorCorrection(_scanCodec.NearLossless);
        }

        context.UpdateVariablesAndBias(errorValue, _scanCodec.NearLossless, _scanCodec.ResetThreshold);
        errorValue = ApplySign(errorValue, sign);
        return Traits.ComputeReconstructedSample(correctedPrediction, errorValue);
    }

    private int DecodeRunMode(int startIndex, Span<byte> previousLine, Span<byte> currentLine)
    {
        var ra = currentLine[startIndex - 1];

        int runLength = DecodeRunPixels(ra, currentLine[startIndex..], FrameInfo.Width - (startIndex - 1));
        int endIndex = startIndex + runLength;

        if (endIndex - 1 == FrameInfo.Width)
            return endIndex - startIndex;

        // Run interruption
        var rb = previousLine[endIndex];
        currentLine[endIndex] = (byte)DecodeRunInterruptionPixel(ra, rb);
        _scanCodec.DecrementRunIndex();
        return endIndex - startIndex + 1;
    }

    private int DecodeRunMode(int startIndex, Span<ushort> previousLine, Span<ushort> currentLine)
    {
        var ra = currentLine[startIndex - 1];

        int runLength = DecodeRunPixels(ra, currentLine[startIndex..], FrameInfo.Width - (startIndex - 1));
        int endIndex = startIndex + runLength;

        if (endIndex - 1 == FrameInfo.Width)
            return endIndex - startIndex;

        // Run interruption
        var rb = previousLine[endIndex];
        currentLine[endIndex] = (ushort)DecodeRunInterruptionPixel(ra, rb);
        _scanCodec.DecrementRunIndex();
        return endIndex - startIndex + 1;
    }

    private int DecodeRunMode(int startIndex, Span<Pair<byte>> previousLine, Span<Pair<byte>> currentLine)
    {
        var ra = currentLine[startIndex - 1];

        int runLength = DecodeRunPixels(ra, currentLine[startIndex..], FrameInfo.Width - (startIndex - 1));
        int endIndex = startIndex + runLength;

        if (endIndex - 1 == FrameInfo.Width)
            return endIndex - startIndex;

        // Run interruption
        var rb = previousLine[endIndex];
        currentLine[endIndex] = DecodeRunInterruptionPixel(ra, rb);
        _scanCodec.DecrementRunIndex();
        return endIndex - startIndex + 1;
    }

    private int DecodeRunMode(int startIndex, Span<Pair<ushort>> previousLine, Span<Pair<ushort>> currentLine)
    {
        var ra = currentLine[startIndex - 1];

        int runLength = DecodeRunPixels(ra, currentLine[startIndex..], FrameInfo.Width - (startIndex - 1));
        int endIndex = startIndex + runLength;

        if (endIndex - 1 == FrameInfo.Width)
            return endIndex - startIndex;

        // Run interruption
        var rb = previousLine[endIndex];
        currentLine[endIndex] = DecodeRunInterruptionPixel(ra, rb);
        _scanCodec.DecrementRunIndex();
        return endIndex - startIndex + 1;
    }

    private int DecodeRunMode(int startIndex, Span<Triplet<byte>> previousLine, Span<Triplet<byte>> currentLine)
    {
        var ra = currentLine[startIndex - 1];

        int runLength = DecodeRunPixels(ra, currentLine[startIndex..], FrameInfo.Width - (startIndex - 1));
        int endIndex = startIndex + runLength;

        if (endIndex - 1 == FrameInfo.Width)
            return endIndex - startIndex;

        // Run interruption
        var rb = previousLine[endIndex];
        currentLine[endIndex] = DecodeRunInterruptionPixel(ra, rb);
        _scanCodec.DecrementRunIndex();
        return endIndex - startIndex + 1;
    }

    private int DecodeRunMode(int startIndex, Span<Triplet<ushort>> previousLine, Span<Triplet<ushort>> currentLine)
    {
        var ra = currentLine[startIndex - 1];

        int runLength = DecodeRunPixels(ra, currentLine[startIndex..], FrameInfo.Width - (startIndex - 1));
        int endIndex = startIndex + runLength;

        if (endIndex - 1 == FrameInfo.Width)
            return endIndex - startIndex;

        // Run interruption
        var rb = previousLine[endIndex];
        currentLine[endIndex] = DecodeRunInterruptionPixel(ra, rb);
        _scanCodec.DecrementRunIndex();
        return endIndex - startIndex + 1;
    }

    private int DecodeRunMode(int startIndex, Span<Quad<byte>> previousLine, Span<Quad<byte>> currentLine)
    {
        var ra = currentLine[startIndex - 1];

        int runLength = DecodeRunPixels(ra, currentLine[startIndex..], FrameInfo.Width - (startIndex - 1));
        int endIndex = startIndex + runLength;

        if (endIndex - 1 == FrameInfo.Width)
            return endIndex - startIndex;

        // Run interruption
        var rb = previousLine[endIndex];
        currentLine[endIndex] = DecodeRunInterruptionPixel(ra, rb);
        _scanCodec.DecrementRunIndex();
        return endIndex - startIndex + 1;
    }

    private int DecodeRunMode(int startIndex, Span<Quad<ushort>> previousLine, Span<Quad<ushort>> currentLine)
    {
        var ra = currentLine[startIndex - 1];

        int runLength = DecodeRunPixels(ra, currentLine[startIndex..], FrameInfo.Width - (startIndex - 1));
        int endIndex = startIndex + runLength;

        if (endIndex - 1 == FrameInfo.Width)
            return endIndex - startIndex;

        // Run interruption
        var rb = previousLine[endIndex];
        currentLine[endIndex] = DecodeRunInterruptionPixel(ra, rb);
        _scanCodec.DecrementRunIndex();
        return endIndex - startIndex + 1;
    }

    private int DecodeRunPixels(byte ra, Span<byte> startPos, int pixelCount)
    {
        int index = 0;
        while (ReadBit() == 1)
        {
            int count = Math.Min(1 << ScanCodec.J[_scanCodec.RunIndex], pixelCount - index);
            index += count;
            Debug.Assert(index <= pixelCount);

            if (count == (1 << ScanCodec.J[_scanCodec.RunIndex]))
            {
                _scanCodec.IncrementRunIndex();
            }

            if (index == pixelCount)
                break;
        }

        if (index != pixelCount)
        {
            // Incomplete run.
            index += (ScanCodec.J[_scanCodec.RunIndex] > 0) ? ReadValue(ScanCodec.J[_scanCodec.RunIndex]) : 0;
        }

        if (index > pixelCount)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidData);

        for (int i = 0; i < index; ++i)
        {
            startPos[i] = ra;
        }

        return index;
    }

    private int DecodeRunPixels(ushort ra, Span<ushort> startPos, int pixelCount)
    {
        int index = 0;
        while (ReadBit() == 1)
        {
            int count = Math.Min(1 << ScanCodec.J[_scanCodec.RunIndex], pixelCount - index);
            index += count;
            Debug.Assert(index <= pixelCount);

            if (count == (1 << ScanCodec.J[_scanCodec.RunIndex]))
            {
                _scanCodec.IncrementRunIndex();
            }

            if (index == pixelCount)
                break;
        }

        if (index != pixelCount)
        {
            // Incomplete run.
            index += (ScanCodec.J[_scanCodec.RunIndex] > 0) ? ReadValue(ScanCodec.J[_scanCodec.RunIndex]) : 0;
        }

        if (index > pixelCount)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidData);

        for (int i = 0; i < index; ++i)
        {
            startPos[i] = ra;
        }

        return index;
    }

    private int DecodeRunPixels(Pair<byte> ra, Span<Pair<byte>> startPos, int pixelCount)
    {
        int index = 0;
        while (ReadBit() == 1)
        {
            int count = Math.Min(1 << ScanCodec.J[_scanCodec.RunIndex], pixelCount - index);
            index += count;
            Debug.Assert(index <= pixelCount);

            if (count == (1 << ScanCodec.J[_scanCodec.RunIndex]))
            {
                _scanCodec.IncrementRunIndex();
            }

            if (index == pixelCount)
                break;
        }

        if (index != pixelCount)
        {
            // Incomplete run.
            index += (ScanCodec.J[_scanCodec.RunIndex] > 0) ? ReadValue(ScanCodec.J[_scanCodec.RunIndex]) : 0;
        }

        if (index > pixelCount)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidData);

        for (int i = 0; i < index; ++i)
        {
            startPos[i] = ra;
        }

        return index;
    }

    private int DecodeRunPixels(Pair<ushort> ra, Span<Pair<ushort>> startPos, int pixelCount)
    {
        int index = 0;
        while (ReadBit() == 1)
        {
            int count = Math.Min(1 << ScanCodec.J[_scanCodec.RunIndex], pixelCount - index);
            index += count;
            Debug.Assert(index <= pixelCount);

            if (count == (1 << ScanCodec.J[_scanCodec.RunIndex]))
            {
                _scanCodec.IncrementRunIndex();
            }

            if (index == pixelCount)
                break;
        }

        if (index != pixelCount)
        {
            // Incomplete run.
            index += (ScanCodec.J[_scanCodec.RunIndex] > 0) ? ReadValue(ScanCodec.J[_scanCodec.RunIndex]) : 0;
        }

        if (index > pixelCount)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidData);

        for (int i = 0; i < index; ++i)
        {
            startPos[i] = ra;
        }

        return index;
    }

    private int DecodeRunPixels(Triplet<byte> ra, Span<Triplet<byte>> startPos, int pixelCount)
    {
        int index = 0;
        while (ReadBit() == 1)
        {
            int count = Math.Min(1 << ScanCodec.J[_scanCodec.RunIndex], pixelCount - index);
            index += count;
            Debug.Assert(index <= pixelCount);

            if (count == (1 << ScanCodec.J[_scanCodec.RunIndex]))
            {
                _scanCodec.IncrementRunIndex();
            }

            if (index == pixelCount)
                break;
        }

        if (index != pixelCount)
        {
            // Incomplete run.
            index += (ScanCodec.J[_scanCodec.RunIndex] > 0) ? ReadValue(ScanCodec.J[_scanCodec.RunIndex]) : 0;
        }

        if (index > pixelCount)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidData);

        for (int i = 0; i < index; ++i)
        {
            startPos[i] = ra;
        }

        return index;
    }

    private int DecodeRunPixels(Triplet<ushort> ra, Span<Triplet<ushort>> startPos, int pixelCount)
    {
        int index = 0;
        while (ReadBit() == 1)
        {
            int count = Math.Min(1 << ScanCodec.J[_scanCodec.RunIndex], pixelCount - index);
            index += count;
            Debug.Assert(index <= pixelCount);

            if (count == (1 << ScanCodec.J[_scanCodec.RunIndex]))
            {
                _scanCodec.IncrementRunIndex();
            }

            if (index == pixelCount)
                break;
        }

        if (index != pixelCount)
        {
            // Incomplete run.
            index += (ScanCodec.J[_scanCodec.RunIndex] > 0) ? ReadValue(ScanCodec.J[_scanCodec.RunIndex]) : 0;
        }

        if (index > pixelCount)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidData);

        for (int i = 0; i < index; ++i)
        {
            startPos[i] = ra;
        }

        return index;
    }

    private int DecodeRunPixels(Quad<byte> ra, Span<Quad<byte>> startPos, int pixelCount)
    {
        int index = 0;
        while (ReadBit() == 1)
        {
            int count = Math.Min(1 << ScanCodec.J[_scanCodec.RunIndex], pixelCount - index);
            index += count;
            Debug.Assert(index <= pixelCount);

            if (count == (1 << ScanCodec.J[_scanCodec.RunIndex]))
            {
                _scanCodec.IncrementRunIndex();
            }

            if (index == pixelCount)
                break;
        }

        if (index != pixelCount)
        {
            // Incomplete run.
            index += (ScanCodec.J[_scanCodec.RunIndex] > 0) ? ReadValue(ScanCodec.J[_scanCodec.RunIndex]) : 0;
        }

        if (index > pixelCount)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidData);

        for (int i = 0; i < index; ++i)
        {
            startPos[i] = ra;
        }

        return index;
    }

    private int DecodeRunPixels(Quad<ushort> ra, Span<Quad<ushort>> startPos, int pixelCount)
    {
        int index = 0;
        while (ReadBit() == 1)
        {
            int count = Math.Min(1 << ScanCodec.J[_scanCodec.RunIndex], pixelCount - index);
            index += count;
            Debug.Assert(index <= pixelCount);

            if (count == (1 << ScanCodec.J[_scanCodec.RunIndex]))
            {
                _scanCodec.IncrementRunIndex();
            }

            if (index == pixelCount)
                break;
        }

        if (index != pixelCount)
        {
            // Incomplete run.
            index += (ScanCodec.J[_scanCodec.RunIndex] > 0) ? ReadValue(ScanCodec.J[_scanCodec.RunIndex]) : 0;
        }

        if (index > pixelCount)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidData);

        for (int i = 0; i < index; ++i)
        {
            startPos[i] = ra;
        }

        return index;
    }

    private int DecodeRunInterruptionPixel(int ra, int rb)
    {
        bool same = _scanCodec.NearLossless == 0 ? ra == rb : AbsUnchecked(ra - rb) <= _scanCodec.NearLossless;
        if (same)
        {
            int errorValue = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[1]);
            return Traits.ComputeReconstructedSample(ra, errorValue);
        }
        else
        {
            int errorValue = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);
            return Traits.ComputeReconstructedSample(rb, errorValue * Sign(rb - ra));
        }
    }

    private Pair<byte> DecodeRunInterruptionPixel(Pair<byte> ra, Pair<byte> rb)
    {
        int errorValue1 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);
        int errorValue2 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);

        return new Pair<byte>(
            (byte)Traits.ComputeReconstructedSample(rb.V1, errorValue1 * Sign(rb.V1 - ra.V1)),
            (byte)Traits.ComputeReconstructedSample(rb.V2, errorValue2 * Sign(rb.V2 - ra.V2)));
    }

    private Pair<ushort> DecodeRunInterruptionPixel(Pair<ushort> ra, Pair<ushort> rb)
    {
        int errorValue1 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);
        int errorValue2 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);

        return new Pair<ushort>(
            (ushort)Traits.ComputeReconstructedSample(rb.V1, errorValue1 * Sign(rb.V1 - ra.V1)),
            (ushort)Traits.ComputeReconstructedSample(rb.V2, errorValue2 * Sign(rb.V2 - ra.V2)));
    }

    private Triplet<byte> DecodeRunInterruptionPixel(Triplet<byte> ra, Triplet<byte> rb)
    {
        int errorValue1 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);
        int errorValue2 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);
        int errorValue3 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);

        return new Triplet<byte>(
            (byte)Traits.ComputeReconstructedSample(rb.V1, errorValue1 * Sign(rb.V1 - ra.V1)),
            (byte)Traits.ComputeReconstructedSample(rb.V2, errorValue2 * Sign(rb.V2 - ra.V2)),
            (byte)Traits.ComputeReconstructedSample(rb.V3, errorValue3 * Sign(rb.V3 - ra.V3)));
    }

    private Triplet<ushort> DecodeRunInterruptionPixel(Triplet<ushort> ra, Triplet<ushort> rb)
    {
        int errorValue1 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);
        int errorValue2 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);
        int errorValue3 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);

        return new Triplet<ushort>(
            (ushort)Traits.ComputeReconstructedSample(rb.V1, errorValue1 * Sign(rb.V1 - ra.V1)),
            (ushort)Traits.ComputeReconstructedSample(rb.V2, errorValue2 * Sign(rb.V2 - ra.V2)),
            (ushort)Traits.ComputeReconstructedSample(rb.V3, errorValue3 * Sign(rb.V3 - ra.V3)));
    }

    private Quad<byte> DecodeRunInterruptionPixel(Quad<byte> ra, Quad<byte> rb)
    {
        int errorValue1 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);
        int errorValue2 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);
        int errorValue3 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);
        int errorValue4 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);

        return new Quad<byte>(
            (byte)Traits.ComputeReconstructedSample(rb.V1, errorValue1 * Sign(rb.V1 - ra.V1)),
            (byte)Traits.ComputeReconstructedSample(rb.V2, errorValue2 * Sign(rb.V2 - ra.V2)),
            (byte)Traits.ComputeReconstructedSample(rb.V3, errorValue3 * Sign(rb.V3 - ra.V3)),
            (byte)Traits.ComputeReconstructedSample(rb.V4, errorValue4 * Sign(rb.V4 - ra.V4)));
    }

    private Quad<ushort> DecodeRunInterruptionPixel(Quad<ushort> ra, Quad<ushort> rb)
    {
        int errorValue1 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);
        int errorValue2 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);
        int errorValue3 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);
        int errorValue4 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);

        return new Quad<ushort>(
            (ushort)Traits.ComputeReconstructedSample(rb.V1, errorValue1 * Sign(rb.V1 - ra.V1)),
            (ushort)Traits.ComputeReconstructedSample(rb.V2, errorValue2 * Sign(rb.V2 - ra.V2)),
            (ushort)Traits.ComputeReconstructedSample(rb.V3, errorValue3 * Sign(rb.V3 - ra.V3)),
            (ushort)Traits.ComputeReconstructedSample(rb.V4, errorValue4 * Sign(rb.V4 - ra.V4)));
    }

    private int DecodeRunInterruptionError(ref RunModeContext context)
    {
        int k = context.ComputeGolombCodingParameterChecked();
        int eMappedErrorValue = DecodeMappedErrorValue(k, _scanCodec.Limit - ScanCodec.J[_scanCodec.RunIndex] - 1, _scanCodec.QuantizedBitsPerSample);
        int errorValue = context.ComputeErrorValue(eMappedErrorValue + context.RunInterruptionType, k);
        context.UpdateVariables(errorValue, eMappedErrorValue, (byte)_scanCodec.PresetCodingParameters.ResetValue);
        return errorValue;
    }

    private static void CopyLineBufferToDestinationInterleaveNone(Span<byte> source, Span<byte> destination, int pixelCount)
    {
        source[..pixelCount].CopyTo(destination);
    }

    private static void CopyLineBufferToDestinationInterleaveNone(Span<ushort> source, Span<byte> destination, int pixelCount)
    {
        Span<byte> sourceInBytes = MemoryMarshal.Cast<ushort, byte>(source);
        sourceInBytes[..(pixelCount * 2)].CopyTo(destination);
    }

    private readonly void CopyLineBufferToDestination(Span<Pair<byte>> source, Span<byte> destination, int pixelCount)
    {
        Span<byte> sourceInBytes = MemoryMarshal.Cast<Pair<byte>, byte>(source);
        _copyFromLineBuffer(sourceInBytes, destination, pixelCount);
    }

    private readonly void CopyLineBufferToDestination(Span<Pair<ushort>> source, Span<byte> destination, int pixelCount)
    {
        Span<byte> sourceInBytes = MemoryMarshal.Cast<Pair<ushort>, byte>(source);
        _copyFromLineBuffer(sourceInBytes, destination, pixelCount);
    }

    private readonly void CopyLineBufferToDestination(Span<Triplet<byte>> source, Span<byte> destination, int pixelCount)
    {
        Span<byte> sourceInBytes = MemoryMarshal.Cast<Triplet<byte>, byte>(source);
        _copyFromLineBuffer(sourceInBytes, destination, pixelCount);
    }

    private readonly void CopyLineBufferToDestination(Span<Triplet<ushort>> source, Span<byte> destination, int pixelCount)
    {
        Span<byte> sourceInBytes = MemoryMarshal.Cast<Triplet<ushort>, byte>(source);
        _copyFromLineBuffer(sourceInBytes, destination, pixelCount);
    }

    private readonly void CopyLineBufferToDestination(Span<Quad<byte>> source, Span<byte> destination, int pixelCount)
    {
        Span<byte> sourceInBytes = MemoryMarshal.Cast<Quad<byte>, byte>(source);
        _copyFromLineBuffer(sourceInBytes, destination, pixelCount);
    }

    private readonly void CopyLineBufferToDestination(Span<Quad<ushort>> source, Span<byte> destination, int pixelCount)
    {
        Span<byte> sourceInBytes = MemoryMarshal.Cast<Quad<ushort>, byte>(source);
        _copyFromLineBuffer(sourceInBytes, destination, pixelCount);
    }

    private readonly void CopyLineBufferToDestinationInterleaveLine(Span<byte> source, Span<byte> destination, int pixelCount)
    {
        _copyFromLineBuffer(source, destination, pixelCount);
    }

    private readonly void CopyLineBufferToDestinationInterleaveLine(Span<ushort> source, Span<byte> destination, int pixelCount)
    {
        Span<byte> sourceInBytes = MemoryMarshal.Cast<ushort, byte>(source);
        _copyFromLineBuffer(sourceInBytes, destination, pixelCount);
    }
}
