// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;

using static CharLS.Managed.Algorithm;

namespace CharLS.Managed;

internal struct ScanDecoder
{
    private const int CacheBitCount = sizeof(ulong) * 8;
    private const int MaxReadableCacheBits = CacheBitCount - 8;

    private ScanCodec _scanCodec;

    private ReadOnlyMemory<byte> _source;
    private int _position;
    private int _endPosition;
    private int _positionFF;
    private int _validBits;
    private ulong _readCache; // TODO: change for 32-bit build
    private int _restartIntervalCounter;

    private int _restartInterval;
    private readonly Traits _traits;
    private readonly sbyte[] _quantizationLut;
    private readonly CopyFromLineBuffer.Method _copyFromLineBuffer;

    private readonly FrameInfo FrameInfo => _scanCodec.FrameInfo;

    private readonly CodingParameters CodingParameters => _scanCodec.CodingParameters;

    private int RunIndex
    {
        readonly get => _scanCodec.RunIndex;

        set => _scanCodec.RunIndex = value;
    }

    private static readonly GolombCodeTable[] ColombCodeTable =
    [
        GolombCodeTable.Create(0), GolombCodeTable.Create(1), GolombCodeTable.Create(2), GolombCodeTable.Create(3),
        GolombCodeTable.Create(4), GolombCodeTable.Create(5), GolombCodeTable.Create(6), GolombCodeTable.Create(7),
        GolombCodeTable.Create(8), GolombCodeTable.Create(9), GolombCodeTable.Create(10), GolombCodeTable.Create(11),
        GolombCodeTable.Create(12), GolombCodeTable.Create(13), GolombCodeTable.Create(14), GolombCodeTable.Create(15)
    ];

    internal ScanDecoder(FrameInfo frameInfo, JpegLSPresetCodingParameters presetCodingParameters, CodingParameters codingParameters)
    {
        _scanCodec = new ScanCodec(frameInfo, presetCodingParameters, codingParameters);

        _copyFromLineBuffer = CopyFromLineBuffer.GetMethod(FrameInfo.BitsPerSample, FrameInfo.ComponentCount,
            CodingParameters.InterleaveMode, CodingParameters.ColorTransformation);

        _traits = Traits.Create(frameInfo, codingParameters.NearLossless, presetCodingParameters.ResetValue);

        _quantizationLut = _scanCodec.InitializeQuantizationLut(_traits, presetCodingParameters.Threshold1,
            presetCodingParameters.Threshold2, presetCodingParameters.Threshold3);

        _scanCodec.InitializeParameters(_traits.Range);
    }

    internal int DecodeScan(ReadOnlyMemory<byte> source, Span<byte> destination, int stride)
    {
        Initialize(source);

        // Process images without a restart interval, as 1 large restart interval.
        _restartInterval = CodingParameters.RestartInterval == 0 ? FrameInfo.Height : CodingParameters.RestartInterval;

        if (FrameInfo.BitsPerSample <= 8)
        {
            switch (CodingParameters.InterleaveMode)
            {
                case InterleaveMode.None:
                    DecodeLines8BitInterleaveModeNone(destination);
                    break;

                case InterleaveMode.Line:
                    DecodeLines8BitInterleaveModeLine(destination);
                    break;

                case InterleaveMode.Sample:
                    switch (FrameInfo.ComponentCount)
                    {
                        case 3:
                            DecodeLines8Bit3ComponentsInterleaveModeSample(destination);
                            break;
                        case 4:
                            DecodeLines8Bit4ComponentsInterleaveModeSample(destination);
                            break;
                    }
                    break;
            }
        }
        else
        {
            switch (CodingParameters.InterleaveMode)
            {
                case InterleaveMode.None:
                    DecodeLines16BitInterleaveModeNone(destination);
                    break;

                case InterleaveMode.Line:
                    DecodeLines16BitInterleaveModeLine(destination);
                    break;

                case InterleaveMode.Sample:
                    switch (FrameInfo.ComponentCount)
                    {
                        case 3:
                            DecodeLines16Bit3ComponentsInterleaveModeSample(destination);
                            break;
                        case 4:
                            DecodeLines16Bit4ComponentsInterleaveModeSample(destination);
                            break;
                    }
                    break;
            }
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
        for (;;)
        {
            int lastBitsCount = source[_position - 1] == Constants.JpegMarkerStartByte ? 7 : 8;

            if (validBits < lastBitsCount)
                return _position;

            validBits -= lastBitsCount;
            --_position;
        }
    }

    internal int DecodeValue(int k, int limit, int quantizedBitsPerPixel)
    {
        int highBits = ReadHighBits();

        if (highBits >= limit - (quantizedBitsPerPixel + 1))
            return ReadValue(quantizedBitsPerPixel) + 1;

        if (k == 0)
            return highBits;

        return (highBits << k) + ReadValue(k);
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

    internal byte PeekByte()
    {
        if (_validBits < 8)
        {
            FillReadCache();
        }

        return (byte)(_readCache >> MaxReadableCacheBits);
    }

    internal bool ReadBit()
    {
        if (_validBits <= 0)
        {
            FillReadCache();
        }

        bool set = (_readCache & (1UL << (CacheBitCount - 1))) != 0;
        SkipBits(1);
        return set;
    }

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

    internal int ReadHighBits()
    {
        int count = Peek0Bits();
        if (count >= 0)
        {
            SkipBits(count + 1);
            return count;
        }

        SkipBits(15);

        for (int highBitsCount = 15; ; ++highBitsCount)
        {
            if (ReadBit())
                return highBitsCount;
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
        } while (value == Constants.JpegMarkerStartByte);

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
        // ASSERT(valid_bits_ <= max_readable_cache_bits);

        if (FillReadCacheOptimistic())
            return;

        var source = _source.Span;
        do
        {
            if (_position >= _endPosition)
            {
                if (_validBits == 0)
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

        } while (_validBits < MaxReadableCacheBits);

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

    // In ILV_SAMPLE mode, multiple components are handled in do_line
    // In ILV_LINE mode, a call to do_line is made for every component
    // In ILV_NONE mode, do_scan is called for each component
    private void DecodeLines8BitInterleaveModeNone(Span<byte> destination)
    {
        int pixelStride = FrameInfo.Width + 2;

        Span<byte> lineBuffer = new byte[pixelStride * 2];

        for (int line = 0; ;)
        {
            int linesInInterval = Math.Min(FrameInfo.Height - line, _restartInterval);

            for (int mcu = 0; mcu < linesInInterval; ++mcu, ++line)
            {
                var previousLine = lineBuffer;
                var currentLine = lineBuffer[pixelStride..];
                if ((line & 1) == 1)
                {
                    var temp = previousLine;
                    previousLine = currentLine;
                    currentLine = temp;
                }

                // initialize edge pixels used for prediction
                previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
                currentLine[0] = previousLine[1];

                DecodeSampleLine(previousLine, currentLine);

                int bytesWritten = CopyLineBufferToDestination(currentLine[1..], destination, FrameInfo.Width, pixelStride);
                destination = destination[bytesWritten..];
            }

            if (line == FrameInfo.Height)
                break;

            ProcessRestartMarker();

            // After a restart marker it is required to reset the decoder.
            lineBuffer.Clear();
            _scanCodec.InitializeParameters(_traits.Range);
        }
    }

    private void DecodeLines8BitInterleaveModeLine(Span<byte> destination)
    {
        int pixelStride = FrameInfo.Width + 2;
        int componentCount = FrameInfo.ComponentCount;

        Span<int> runIndex = stackalloc int[componentCount];
        Span<byte> lineBuffer = new byte[componentCount * pixelStride * 2];

        for (int line = 0; ;)
        {
            int linesInInterval = Math.Min(FrameInfo.Height - line, _restartInterval);

            for (int mcu = 0; mcu < linesInInterval; ++mcu, ++line)
            {
                var previousLine = lineBuffer;
                var currentLine = lineBuffer[(componentCount * pixelStride)..];
                bool oddLine = (line & 1) == 1;
                if (oddLine)
                {
                    var temp = previousLine;
                    previousLine = currentLine;
                    currentLine = temp;
                }

                for (int component = 0; component < componentCount; ++component)
                {
                    RunIndex = runIndex[component];

                    // initialize edge pixels used for prediction
                    previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
                    currentLine[0] = previousLine[1];

                    DecodeSampleLine(previousLine, currentLine);

                    runIndex[component] = RunIndex;
                    currentLine = currentLine[pixelStride..];
                    previousLine = previousLine[pixelStride..];
                }

                int startPosition = (oddLine ? 0 : (pixelStride * componentCount)) + 1;
                int bytesWritten = CopyLineBufferToDestinationInterleaveLine(lineBuffer[startPosition..], destination, FrameInfo.Width, pixelStride);
                destination = destination[bytesWritten..];
            }

            if (line == FrameInfo.Height)
                break;

            ProcessRestartMarker();

            // After a restart marker it is required to reset the decoder.
            lineBuffer.Clear();
            runIndex.Clear();
            _scanCodec.InitializeParameters(_traits.Range);
        }
    }

    private void DecodeLines16BitInterleaveModeLine(Span<byte> destination)
    {
        int pixelStride = FrameInfo.Width + 2;
        int componentCount = FrameInfo.ComponentCount;

        Span<int> runIndex = stackalloc int[componentCount];
        Span<ushort> lineBuffer = new ushort[componentCount * pixelStride * 2];

        for (int line = 0; ;)
        {
            int linesInInterval = Math.Min(FrameInfo.Height - line, _restartInterval);

            for (int mcu = 0; mcu < linesInInterval; ++mcu, ++line)
            {
                var previousLine = lineBuffer;
                var currentLine = lineBuffer[(componentCount * pixelStride)..];
                bool oddLine = (line & 1) == 1;
                if (oddLine)
                {
                    var temp = previousLine;
                    previousLine = currentLine;
                    currentLine = temp;
                }

                for (int component = 0; component < componentCount; ++component)
                {
                    RunIndex = runIndex[component];

                    // initialize edge pixels used for prediction
                    previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
                    currentLine[0] = previousLine[1];

                    DecodeSampleLine(previousLine, currentLine);

                    runIndex[component] = RunIndex;
                    currentLine = currentLine[pixelStride..];
                    previousLine = previousLine[pixelStride..];
                }

                int startPosition = (oddLine ? 0 : (pixelStride * componentCount)) + 1;
                int bytesWritten = CopyLineBufferToDestinationInterleaveLine(lineBuffer[startPosition..], destination, FrameInfo.Width, pixelStride);
                destination = destination[bytesWritten..];
            }

            if (line == FrameInfo.Height)
                break;

            ProcessRestartMarker();

            // After a restart marker it is required to reset the decoder.
            lineBuffer.Clear();
            runIndex.Clear();
            _scanCodec.InitializeParameters(_traits.Range);
        }
    }

    private void DecodeLines8Bit3ComponentsInterleaveModeSample(Span<byte> destination)
    {
        int pixelStride = FrameInfo.Width + 2;

        Span<Triplet<byte>> lineBuffer = new Triplet<byte>[pixelStride * 2];

        for (int line = 0; ;)
        {
            int linesInInterval = Math.Min(FrameInfo.Height - line, _restartInterval);

            for (int mcu = 0; mcu < linesInInterval; ++mcu, ++line)
            {
                var previousLine = lineBuffer;
                var currentLine = lineBuffer[pixelStride..];
                if ((line & 1) == 1)
                {
                    var temp = previousLine;
                    previousLine = currentLine;
                    currentLine = temp;
                }

                // initialize edge pixels used for prediction
                previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
                currentLine[0] = previousLine[1];

                DecodeTripletLine(previousLine, currentLine);

                int bytesWritten = CopyLineBufferToDestination(currentLine[1..], destination, FrameInfo.Width, pixelStride);
                destination = destination[bytesWritten..];
            }

            if (line == FrameInfo.Height)
                break;

            ProcessRestartMarker();

            // After a restart marker it is required to reset the decoder.
            lineBuffer.Clear();
            _scanCodec.InitializeParameters(_traits.Range);
        }
    }

    private void DecodeLines16Bit3ComponentsInterleaveModeSample(Span<byte> destination)
    {
        int pixelStride = FrameInfo.Width + 2;

        Span<Triplet<ushort>> lineBuffer = new Triplet<ushort>[pixelStride * 2];

        for (int line = 0; ;)
        {
            int linesInInterval = Math.Min(FrameInfo.Height - line, _restartInterval);

            for (int mcu = 0; mcu < linesInInterval; ++mcu, ++line)
            {
                var previousLine = lineBuffer;
                var currentLine = lineBuffer[pixelStride..];
                if ((line & 1) == 1)
                {
                    var temp = previousLine;
                    previousLine = currentLine;
                    currentLine = temp;
                }

                // initialize edge pixels used for prediction
                previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
                currentLine[0] = previousLine[1];

                DecodeTripletLine(previousLine, currentLine);

                int bytesWritten = CopyLineBufferToDestination(currentLine[1..], destination, FrameInfo.Width, pixelStride);
                destination = destination[bytesWritten..];
            }

            if (line == FrameInfo.Height)
                break;

            ProcessRestartMarker();

            // After a restart marker it is required to reset the decoder.
            lineBuffer.Clear();
            _scanCodec.InitializeParameters(_traits.Range);
        }
    }

    private void DecodeLines8Bit4ComponentsInterleaveModeSample(Span<byte> destination)
    {
        int pixelStride = FrameInfo.Width + 2;

        Span<Quad<byte>> lineBuffer = new Quad<byte>[pixelStride * 2];

        for (int line = 0; ;)
        {
            int linesInInterval = Math.Min(FrameInfo.Height - line, _restartInterval);

            for (int mcu = 0; mcu < linesInInterval; ++mcu, ++line)
            {
                var previousLine = lineBuffer;
                var currentLine = lineBuffer[pixelStride..];
                if ((line & 1) == 1)
                {
                    var temp = previousLine;
                    previousLine = currentLine;
                    currentLine = temp;
                }

                // initialize edge pixels used for prediction
                previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
                currentLine[0] = previousLine[1];

                DecodeQuadLine(previousLine, currentLine);

                int bytesWritten = CopyLineBufferToDestination(currentLine[1..], destination, FrameInfo.Width, pixelStride);
                destination = destination[bytesWritten..];
            }

            if (line == FrameInfo.Height)
                break;

            ProcessRestartMarker();

            // After a restart marker it is required to reset the decoder.
            lineBuffer.Clear();
            _scanCodec.InitializeParameters(_traits.Range);
        }
    }

    private void DecodeLines16Bit4ComponentsInterleaveModeSample(Span<byte> destination)
    {
        int pixelStride = FrameInfo.Width + 2;

        Span<Quad<ushort>> lineBuffer = new Quad<ushort>[pixelStride * 2];

        for (int line = 0; ;)
        {
            int linesInInterval = Math.Min(FrameInfo.Height - line, _restartInterval);

            for (int mcu = 0; mcu < linesInInterval; ++mcu, ++line)
            {
                var previousLine = lineBuffer;
                var currentLine = lineBuffer[pixelStride..];
                if ((line & 1) == 1)
                {
                    var temp = previousLine;
                    previousLine = currentLine;
                    currentLine = temp;
                }

                // initialize edge pixels used for prediction
                previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
                currentLine[0] = previousLine[1];

                DecodeQuadLine(previousLine, currentLine);

                int bytesWritten = CopyLineBufferToDestination(currentLine[1..], destination, FrameInfo.Width, pixelStride);
                destination = destination[bytesWritten..];
            }

            if (line == FrameInfo.Height)
                break;

            ProcessRestartMarker();

            // After a restart marker it is required to reset the decoder.
            lineBuffer.Clear();
            _scanCodec.InitializeParameters(_traits.Range);
        }
    }

    // In ILV_SAMPLE mode, multiple components are handled in do_line
    // In ILV_LINE mode, a call to do_line is made for every component
    // In ILV_NONE mode, do_scan is called for each component
    private void DecodeLines16BitInterleaveModeNone(Span<byte> destination)
    {
        int pixelStride = FrameInfo.Width + 2;

        Span<ushort> lineBuffer = new ushort[pixelStride * 2];

        for (int line = 0; ;)
        {
            int linesInInterval = Math.Min(FrameInfo.Height - line, _restartInterval);

            for (int mcu = 0; mcu < linesInInterval; ++mcu, ++line)
            {
                var previousLine = lineBuffer;
                var currentLine = lineBuffer[pixelStride..];
                if ((line & 1) == 1)
                {
                    var temp = previousLine;
                    previousLine = currentLine;
                    currentLine = temp;
                }

                // initialize edge pixels used for prediction
                previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
                currentLine[0] = previousLine[1];

                DecodeSampleLine(previousLine, currentLine);

                int bytesWritten = CopyLineBufferToDestination(currentLine[1..], destination, FrameInfo.Width, pixelStride);
                destination = destination[bytesWritten..];
            }

            if (line == FrameInfo.Height)
                break;

            ProcessRestartMarker();

            // After a restart marker it is required to reset the decoder.
            lineBuffer.Clear();
            _scanCodec.InitializeParameters(_traits.Range);
        }
    }

    private readonly int QuantizeGradient(int di)
    {
        Debug.Assert(_scanCodec.QuantizeGradientOrg(di, _traits.NearLossless) == _quantizationLut[(_quantizationLut.Length / 2) + di]);
        return _quantizationLut[(_quantizationLut.Length / 2) + di];
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

            int qs = ComputeContextId(QuantizeGradient(rd - rb),
                QuantizeGradient(rb - rc), QuantizeGradient(rc - ra));
            if (qs != 0)
            {
                currentLine[index] = (byte)DecodeRegular(qs, ComputePredictedValue(ra, rb, rc));
                ++index;
            }
            else
            {
                index += DecodeRunMode(index, previousLine, currentLine);
                rb = previousLine[index - 1];
                rd = previousLine[index];
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

            int qs = ComputeContextId(QuantizeGradient(rd - rb),
                QuantizeGradient(rb - rc), QuantizeGradient(rc - ra));
            if (qs != 0)
            {
                currentLine[index] = (ushort)DecodeRegular(qs, ComputePredictedValue(ra, rb, rc));
                ++index;
            }
            else
            {
                index += DecodeRunMode(index, previousLine, currentLine);
                rb = previousLine[index - 1];
                rd = previousLine[index];
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

            int qs1 = ComputeContextId(QuantizeGradient(rd.V1 - rb.V1), QuantizeGradient(rb.V1 - rc.V1),
                        QuantizeGradient(rc.V1 - ra.V1));
            int qs2 = ComputeContextId(QuantizeGradient(rd.V2 - rb.V2), QuantizeGradient(rb.V2 - rc.V2),
                        QuantizeGradient(rc.V2 - ra.V2));
            int qs3 = ComputeContextId(QuantizeGradient(rd.V3 - rb.V3), QuantizeGradient(rb.V3 - rc.V3),
                        QuantizeGradient(rc.V3 - ra.V3));
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

            int qs1 = ComputeContextId(QuantizeGradient(rd.V1 - rb.V1), QuantizeGradient(rb.V1 - rc.V1),
                        QuantizeGradient(rc.V1 - ra.V1));
            int qs2 = ComputeContextId(QuantizeGradient(rd.V2 - rb.V2), QuantizeGradient(rb.V2 - rc.V2),
                        QuantizeGradient(rc.V2 - ra.V2));
            int qs3 = ComputeContextId(QuantizeGradient(rd.V3 - rb.V3), QuantizeGradient(rb.V3 - rc.V3),
                        QuantizeGradient(rc.V3 - ra.V3));
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

            int qs1 = ComputeContextId(QuantizeGradient(rd.V1 - rb.V1), QuantizeGradient(rb.V1 - rc.V1),
                        QuantizeGradient(rc.V1 - ra.V1));
            int qs2 = ComputeContextId(QuantizeGradient(rd.V2 - rb.V2), QuantizeGradient(rb.V2 - rc.V2),
                        QuantizeGradient(rc.V2 - ra.V2));
            int qs3 = ComputeContextId(QuantizeGradient(rd.V3 - rb.V3), QuantizeGradient(rb.V3 - rc.V3),
                        QuantizeGradient(rc.V3 - ra.V3));
            int qs4 = ComputeContextId(QuantizeGradient(rd.V4 - rb.V4), QuantizeGradient(rb.V4 - rc.V4),
                        QuantizeGradient(rc.V4 - ra.V4));

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

            int qs1 = ComputeContextId(QuantizeGradient(rd.V1 - rb.V1), QuantizeGradient(rb.V1 - rc.V1),
                        QuantizeGradient(rc.V1 - ra.V1));
            int qs2 = ComputeContextId(QuantizeGradient(rd.V2 - rb.V2), QuantizeGradient(rb.V2 - rc.V2),
                        QuantizeGradient(rc.V2 - ra.V2));
            int qs3 = ComputeContextId(QuantizeGradient(rd.V3 - rb.V3), QuantizeGradient(rb.V3 - rc.V3),
                        QuantizeGradient(rc.V3 - ra.V3));
            int qs4 = ComputeContextId(QuantizeGradient(rd.V4 - rb.V4), QuantizeGradient(rb.V4 - rc.V4),
                        QuantizeGradient(rc.V4 - ra.V4));

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
        int k = context.ComputeGolombCodingParameter();
        int predictedValue = _traits.CorrectPrediction(predicted + ApplySign(context.C, sign));

        int errorValue;
        var code = ColombCodeTable[k].Get(PeekByte());
        if (code.Length != 0)
        {
            SkipBits(code.Length);
            errorValue = code.Value;
            //ASSERT(std::abs(error_value) < 65535);
        }
        else
        {
            errorValue = UnmapErrorValue(DecodeValue(k, _traits.Limit, _traits.QuantizedBitsPerSample));
            if (Math.Abs(errorValue) > 65535)
                ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidData);
        }

        if (k == 0)
        {
            errorValue ^= context.GetErrorCorrection(_traits.NearLossless);
        }

        context.UpdateVariablesAndBias(errorValue, _traits.NearLossless, _traits.ResetThreshold);
        errorValue = ApplySign(errorValue, sign);
        return _traits.ComputeReconstructedSample(predictedValue, errorValue);
    }

    private int DecodeRunMode(int startIndex, Span<byte> previousLine, Span<byte> currentLine)
    {
        var ra = currentLine[startIndex - 1];

        int runLength = DecodeRunPixels(ra, currentLine[startIndex..], FrameInfo.Width - (startIndex - 1));
        int endIndex = startIndex + runLength;

        if (endIndex - 1 == FrameInfo.Width)
            return endIndex - startIndex;

        // run interruption
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

        // run interruption
        var rb = previousLine[endIndex];
        currentLine[endIndex] = (ushort)DecodeRunInterruptionPixel(ra, rb);
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

        // run interruption
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

        // run interruption
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

        // run interruption
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

        // run interruption
        var rb = previousLine[endIndex];
        currentLine[endIndex] = DecodeRunInterruptionPixel(ra, rb);
        _scanCodec.DecrementRunIndex();
        return endIndex - startIndex + 1;
    }

    private int DecodeRunPixels(byte ra, Span<byte> startPos, int pixelCount)
    {
        int index = 0;
        while (ReadBit())
        {
            int count = Math.Min(1 << ScanCodec.J[RunIndex], pixelCount - index);
            index += count;
            ////ASSERT(index <= pixel_count);

            if (count == (1 << ScanCodec.J[RunIndex]))
            {
                _scanCodec.IncrementRunIndex();
            }

            if (index == pixelCount)
                break;
        }

        if (index != pixelCount)
        {
            // incomplete run.
            index += (ScanCodec.J[RunIndex] > 0) ? ReadValue(ScanCodec.J[RunIndex]) : 0;
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
        while (ReadBit())
        {
            int count = Math.Min(1 << ScanCodec.J[RunIndex], pixelCount - index);
            index += count;
            ////ASSERT(index <= pixel_count);

            if (count == (1 << ScanCodec.J[RunIndex]))
            {
                _scanCodec.IncrementRunIndex();
            }

            if (index == pixelCount)
                break;
        }

        if (index != pixelCount)
        {
            // incomplete run.
            index += (ScanCodec.J[RunIndex] > 0) ? ReadValue(ScanCodec.J[RunIndex]) : 0;
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
        while (ReadBit())
        {
            int count = Math.Min(1 << ScanCodec.J[RunIndex], pixelCount - index);
            index += count;
            ////ASSERT(index <= pixel_count);

            if (count == (1 << ScanCodec.J[RunIndex]))
            {
                _scanCodec.IncrementRunIndex();
            }

            if (index == pixelCount)
                break;
        }

        if (index != pixelCount)
        {
            // incomplete run.
            index += (ScanCodec.J[RunIndex] > 0) ? ReadValue(ScanCodec.J[RunIndex]) : 0;
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
        while (ReadBit())
        {
            int count = Math.Min(1 << ScanCodec.J[RunIndex], pixelCount - index);
            index += count;
            ////ASSERT(index <= pixel_count);

            if (count == (1 << ScanCodec.J[RunIndex]))
            {
                _scanCodec.IncrementRunIndex();
            }

            if (index == pixelCount)
                break;
        }

        if (index != pixelCount)
        {
            // incomplete run.
            index += (ScanCodec.J[RunIndex] > 0) ? ReadValue(ScanCodec.J[RunIndex]) : 0;
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
        while (ReadBit())
        {
            int count = Math.Min(1 << ScanCodec.J[RunIndex], pixelCount - index);
            index += count;
            ////ASSERT(index <= pixel_count);

            if (count == (1 << ScanCodec.J[RunIndex]))
            {
                _scanCodec.IncrementRunIndex();
            }

            if (index == pixelCount)
                break;
        }

        if (index != pixelCount)
        {
            // incomplete run.
            index += (ScanCodec.J[RunIndex] > 0) ? ReadValue(ScanCodec.J[RunIndex]) : 0;
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
        while (ReadBit())
        {
            int count = Math.Min(1 << ScanCodec.J[RunIndex], pixelCount - index);
            index += count;
            ////ASSERT(index <= pixel_count);

            if (count == (1 << ScanCodec.J[RunIndex]))
            {
                _scanCodec.IncrementRunIndex();
            }

            if (index == pixelCount)
                break;
        }

        if (index != pixelCount)
        {
            // incomplete run.
            index += (ScanCodec.J[RunIndex] > 0) ? ReadValue(ScanCodec.J[RunIndex]) : 0;
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
        if (Math.Abs(ra - rb) <= _traits.NearLossless)
        {
            int errorValue = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[1]);
            return _traits.ComputeReconstructedSample(ra, errorValue);
        }
        else
        {
            int errorValue = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);
            return _traits.ComputeReconstructedSample(rb, errorValue * Sign(rb - ra));
        }
    }

    private Triplet<byte> DecodeRunInterruptionPixel(Triplet<byte> ra, Triplet<byte> rb)
    {
        int errorValue1 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);
        int errorValue2 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);
        int errorValue3 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);

        return new Triplet<byte>(
            (byte)_traits.ComputeReconstructedSample(rb.V1, errorValue1 * Sign(rb.V1 - ra.V1)),
            (byte)_traits.ComputeReconstructedSample(rb.V2, errorValue2 * Sign(rb.V2 - ra.V2)),
            (byte)_traits.ComputeReconstructedSample(rb.V3, errorValue3 * Sign(rb.V3 - ra.V3)));
    }

    private Triplet<ushort> DecodeRunInterruptionPixel(Triplet<ushort> ra, Triplet<ushort> rb)
    {
        int errorValue1 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);
        int errorValue2 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);
        int errorValue3 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);

        return new Triplet<ushort>(
            (ushort)_traits.ComputeReconstructedSample(rb.V1, errorValue1 * Sign(rb.V1 - ra.V1)),
            (ushort)_traits.ComputeReconstructedSample(rb.V2, errorValue2 * Sign(rb.V2 - ra.V2)),
            (ushort)_traits.ComputeReconstructedSample(rb.V3, errorValue3 * Sign(rb.V3 - ra.V3)));
    }

    private Quad<byte> DecodeRunInterruptionPixel(Quad<byte> ra, Quad<byte> rb)
    {
        int errorValue1 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);
        int errorValue2 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);
        int errorValue3 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);
        int errorValue4 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);

        return new Quad<byte>(
            (byte)_traits.ComputeReconstructedSample(rb.V1, errorValue1 * Sign(rb.V1 - ra.V1)),
            (byte)_traits.ComputeReconstructedSample(rb.V2, errorValue2 * Sign(rb.V2 - ra.V2)),
            (byte)_traits.ComputeReconstructedSample(rb.V3, errorValue3 * Sign(rb.V3 - ra.V3)),
            (byte)_traits.ComputeReconstructedSample(rb.V4, errorValue4 * Sign(rb.V4 - ra.V4)));
    }

    private Quad<ushort> DecodeRunInterruptionPixel(Quad<ushort> ra, Quad<ushort> rb)
    {
        int errorValue1 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);
        int errorValue2 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);
        int errorValue3 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);
        int errorValue4 = DecodeRunInterruptionError(ref _scanCodec.RunModeContexts[0]);

        return new Quad<ushort>(
            (ushort)_traits.ComputeReconstructedSample(rb.V1, errorValue1 * Sign(rb.V1 - ra.V1)),
            (ushort)_traits.ComputeReconstructedSample(rb.V2, errorValue2 * Sign(rb.V2 - ra.V2)),
            (ushort)_traits.ComputeReconstructedSample(rb.V3, errorValue3 * Sign(rb.V3 - ra.V3)),
            (ushort)_traits.ComputeReconstructedSample(rb.V4, errorValue4 * Sign(rb.V4 - ra.V4)));
    }

    private int DecodeRunInterruptionError(ref RunModeContext context)
    {
        int k = context.GetGolombCode();
        int eMappedErrorValue = DecodeValue(k, _traits.Limit - ScanCodec.J[RunIndex] - 1, _traits.QuantizedBitsPerSample);
        int errorValue = context.ComputeErrorValue(eMappedErrorValue + context.RunInterruptionType, k);
        context.UpdateVariables(errorValue, eMappedErrorValue, (byte)_scanCodec.PresetCodingParameters.ResetValue);
        return errorValue;
    }

    private readonly int CopyLineBufferToDestination(Span<byte> source, Span<byte> destination, int pixelCount, int pixelStride)
    {
        return _copyFromLineBuffer(source, destination, pixelCount, pixelStride);
    }

    private readonly int CopyLineBufferToDestinationInterleaveLine(Span<byte> source, Span<byte> destination, int pixelCount, int pixelStride)
    {
        return _copyFromLineBuffer(source, destination, pixelCount, pixelStride);
    }

    private readonly int CopyLineBufferToDestination(Span<ushort> source, Span<byte> destination, int pixelCount, int pixelStride)
    {
        Span<byte> sourceInBytes = MemoryMarshal.Cast<ushort, byte>(source);
        return _copyFromLineBuffer(sourceInBytes, destination, pixelCount * 2, pixelStride);
    }

    private readonly int CopyLineBufferToDestinationInterleaveLine(Span<ushort> source, Span<byte> destination, int pixelCount, int pixelStride)
    {
        Span<byte> sourceInBytes = MemoryMarshal.Cast<ushort, byte>(source);
        return _copyFromLineBuffer(sourceInBytes, destination, pixelCount, pixelStride);
    }

    private readonly int CopyLineBufferToDestination(Span<Triplet<byte>> source, Span<byte> destination, int pixelCount, int pixelStride)
    {
        Span<byte> sourceInBytes = MemoryMarshal.Cast<Triplet<byte>, byte>(source);
        return _copyFromLineBuffer(sourceInBytes, destination, pixelCount, pixelStride);
    }

    private readonly int CopyLineBufferToDestination(Span<Triplet<ushort>> source, Span<byte> destination, int pixelCount, int pixelStride)
    {
        Span<byte> sourceInBytes = MemoryMarshal.Cast<Triplet<ushort>, byte>(source);
        return _copyFromLineBuffer(sourceInBytes, destination, pixelCount, pixelStride);
    }

    private readonly int CopyLineBufferToDestination(Span<Quad<byte>> source, Span<byte> destination, int pixelCount, int pixelStride)
    {
        Span<byte> sourceInBytes = MemoryMarshal.Cast<Quad<byte>, byte>(source);
        return _copyFromLineBuffer(sourceInBytes, destination, pixelCount, pixelStride);
    }

    private readonly int CopyLineBufferToDestination(Span<Quad<ushort>> source, Span<byte> destination, int pixelCount, int pixelStride)
    {
        Span<byte> sourceInBytes = MemoryMarshal.Cast<Quad<ushort>, byte>(source);
        return _copyFromLineBuffer(sourceInBytes, destination, pixelCount, pixelStride);
    }
}
