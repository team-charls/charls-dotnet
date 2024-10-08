// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Runtime.InteropServices;

using static CharLS.Managed.Algorithm;

namespace CharLS.Managed;

internal struct ScanEncoder
{
    private readonly int _mask;
    private ScanCodec _scanCodec;
    private Memory<byte> _destination;
    private uint _bitBuffer;
    private int _freeBitCount = 4 * 8;
    private int _position;
    private int _compressedLength;
    private bool _isFFWritten;
    private int _bytesWritten;
    private CopyToLineBuffer.Method? _copyToLineBuffer;

    internal ScanEncoder(FrameInfo frameInfo, JpegLSPresetCodingParameters presetCodingParameters, CodingParameters codingParameters)
    {
        var traits = Traits.Create(frameInfo.BitsPerSample, codingParameters.NearLossless);
        _scanCodec = new ScanCodec(traits, frameInfo, presetCodingParameters, codingParameters);

        _mask = (1 << frameInfo.BitsPerSample) - 1;

        _scanCodec.InitializeParameters(traits.Range);
    }

    private readonly int PixelStride => _scanCodec.FrameInfo.Width + 2;

    private readonly FrameInfo FrameInfo => _scanCodec.FrameInfo;

    private readonly CodingParameters CodingParameters => _scanCodec.CodingParameters;

    private readonly Traits Traits => _scanCodec.Traits;

    private int RunIndex
    {
        readonly get => _scanCodec.RunIndex;
        set => _scanCodec.RunIndex = value;
    }

    internal int EncodeScan(ReadOnlySpan<byte> source, Memory<byte> destination, int stride)
    {
        _copyToLineBuffer = CopyToLineBuffer.GetCopyMethod(
            FrameInfo.BitsPerSample, FrameInfo.ComponentCount, CodingParameters.InterleaveMode, CodingParameters.ColorTransformation);

        InitializeDestination(destination);

        if (FrameInfo.BitsPerSample <= 8)
        {
            EncodeLines8Bit(source, stride);
        }
        else
        {
            EncodeLines16Bit(source, stride);
        }

        EndScan();

        return GetLength();
    }

    internal void InitializeDestination(Memory<byte> destination)
    {
        _destination = destination;
        _compressedLength = destination.Length;
    }

    internal void AppendToBitStream(uint bits, int bitCount)
    {
        Debug.Assert(bitCount is >= 0 and < 32);
        Debug.Assert((bits | ((1U << bitCount) - 1U)) == ((1U << bitCount) - 1U)); // Not used bits must be set to zero.

        _freeBitCount -= bitCount;
        if (_freeBitCount >= 0)
        {
            _bitBuffer |= bits << _freeBitCount;
        }
        else
        {
            // Add as many bits in the remaining space as possible and flush.
            _bitBuffer |= bits >> -_freeBitCount;
            Flush();

            // A second flush may be required if extra marker detect bits were needed and not all bits could be written.
            if (_freeBitCount < 0)
            {
                _bitBuffer |= bits >> -_freeBitCount;
                Flush();
            }

            Debug.Assert(_freeBitCount >= 0);
            _bitBuffer |= bits << _freeBitCount;
        }
    }

    internal void Flush()
    {
        if (_compressedLength < 4)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.DestinationTooSmall);

        for (int i = 0; i < 4; ++i)
        {
            if (_freeBitCount >= 32)
            {
                _freeBitCount = 32;
                break;
            }

            if (_isFFWritten)
            {
                // JPEG-LS requirement (T.87, A.1) to detect markers: after a xFF value a single 0 bit needs to be inserted.
                _destination.Span[_position] = (byte)(_bitBuffer >> 25);
                _bitBuffer <<= 7;
                _freeBitCount += 7;
            }
            else
            {
                _destination.Span[_position] = (byte)(_bitBuffer >> 24);
                _bitBuffer <<= 8;
                _freeBitCount += 8;
            }

            _isFFWritten = _destination.Span[_position] == Constants.JpegMarkerStartByte;
            ++_position;
            --_compressedLength;
            ++_bytesWritten;
        }
    }

    private void EncodeRunPixels(int runLength, bool endOfLine)
    {
        while (runLength >= 1 << ScanCodec.J[RunIndex])
        {
            AppendOnesToBitStream(1);
            runLength -= 1 << ScanCodec.J[RunIndex];
            _scanCodec.IncrementRunIndex();
        }

        if (endOfLine)
        {
            if (runLength != 0)
            {
                AppendOnesToBitStream(1);
            }
        }
        else
        {
            AppendToBitStream((uint)runLength, ScanCodec.J[RunIndex] + 1); // leading 0 + actual remaining length
        }
    }

    private void AppendOnesToBitStream(int bitCount)
    {
        AppendToBitStream((1U << bitCount) - 1U, bitCount);
    }

    private void EndScan()
    {
        Flush();

        // if a 0xff was written, Flush() will force one unset bit anyway
        if (_isFFWritten)
        {
            AppendToBitStream(0, (_freeBitCount - 1) % 8);
        }

        Flush();
        Debug.Assert(_freeBitCount == 32);
    }

    private readonly int GetLength()
    {
        return _bytesWritten - ((_freeBitCount - 32) / 8);
    }

    private void EncodeLines8Bit(ReadOnlySpan<byte> source, int stride)
    {
        switch (CodingParameters.InterleaveMode)
        {
            case InterleaveMode.None:
                EncodeLines8BitInterleaveModeNone(source, stride);
                break;

            case InterleaveMode.Line:
                EncodeLines8BitInterleaveModeLine(source, stride);
                break;

            case InterleaveMode.Sample:
                switch (FrameInfo.ComponentCount)
                {
                    case 2:
                        EncodeLines8Bit2ComponentsInterleaveModeSample(source, stride);
                        break;
                    case 3:
                        EncodeLines8Bit3ComponentsInterleaveModeSample(source, stride);
                        break;
                    case 4:
                        EncodeLines8Bit4ComponentsInterleaveModeSample(source, stride);
                        break;
                }

                break;
        }
    }

    private void EncodeLines16Bit(ReadOnlySpan<byte> source, int stride)
    {
        switch (CodingParameters.InterleaveMode)
        {
            case InterleaveMode.None:
                EncodeLines16BitInterleaveModeNone(source, stride);
                break;

            case InterleaveMode.Line:
                EncodeLines16BitInterleaveModeLine(source, stride);
                break;

            case InterleaveMode.Sample:
                switch (FrameInfo.ComponentCount)
                {
                    case 2:
                        EncodeLines16Bit2ComponentsInterleaveModeSample(source, stride);
                        break;
                    case 3:
                        EncodeLines16Bit3ComponentsInterleaveModeSample(source, stride);
                        break;
                    case 4:
                        EncodeLines16Bit4ComponentsInterleaveModeSample(source, stride);
                        break;
                }

                break;
        }
    }

    private void EncodeLines8BitInterleaveModeNone(ReadOnlySpan<byte> source, int stride)
    {
        int pixelStride = PixelStride;
        using var rentedArray = ArrayPoolHelper.Rent<byte>(pixelStride * 2);
        Span<byte> lineBuffer = rentedArray.Value;

        for (int line = 0; ;)
        {
            var previousLine = lineBuffer;
            var currentLine = lineBuffer[pixelStride..];
            bool oddLine = (line & 1) == 1;
            if (oddLine)
            {
                var temp = previousLine;
                previousLine = currentLine;
                currentLine = temp;
            }

            CopySourceToLineBufferInterleaveModeNone(source, currentLine[1..], FrameInfo.Width);

            // Initialize edge pixels used for prediction
            previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
            currentLine[0] = previousLine[1];

            EncodeLine(previousLine, currentLine);

            ++line;
            if (line == FrameInfo.Height)
                break;

            source = source[stride..];
        }
    }

    private void EncodeLines16BitInterleaveModeNone(ReadOnlySpan<byte> source, int stride)
    {
        int pixelStride = PixelStride;
        using var rentedArray = ArrayPoolHelper.Rent<ushort>(pixelStride * 2);
        Span<ushort> lineBuffer = rentedArray.Value;

        for (int line = 0; ;)
        {
            var previousLine = lineBuffer;
            var currentLine = lineBuffer[pixelStride..];
            bool oddLine = (line & 1) == 1;
            if (oddLine)
            {
                var temp = previousLine;
                previousLine = currentLine;
                currentLine = temp;
            }

            CopySourceToLineBufferInterleaveModeNone(source, currentLine[1..], FrameInfo.Width);

            // Initialize edge pixels used for prediction
            previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
            currentLine[0] = previousLine[1];

            EncodeLine(previousLine, currentLine);

            ++line;
            if (line == FrameInfo.Height)
                break;

            source = source[stride..];
        }
    }

    private void EncodeLines8BitInterleaveModeLine(ReadOnlySpan<byte> source, int stride)
    {
        int pixelStride = PixelStride;
        int componentCount = FrameInfo.ComponentCount;
        using var rentedArray = ArrayPoolHelper.Rent<byte>(componentCount * pixelStride * 2);
        Span<byte> lineBuffer = rentedArray.Value;
        Span<int> runIndex = stackalloc int[componentCount];

        for (int line = 0; ;)
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

            CopySourceToLineBufferInterleaveModeLine(source, currentLine[1..], FrameInfo.Width);

            for (int component = 0; component < componentCount; ++component)
            {
                RunIndex = runIndex[component];

                // Initialize edge pixels used for prediction
                previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
                currentLine[0] = previousLine[1];

                EncodeLine(previousLine, currentLine);

                runIndex[component] = RunIndex;
                currentLine = currentLine[pixelStride..];
                previousLine = previousLine[pixelStride..];
            }

            ++line;
            if (line == FrameInfo.Height)
                break;

            source = source[stride..];
        }
    }

    private void EncodeLines16BitInterleaveModeLine(ReadOnlySpan<byte> source, int stride)
    {
        int pixelStride = PixelStride;
        int componentCount = FrameInfo.ComponentCount;
        using var rentedArray = ArrayPoolHelper.Rent<ushort>(componentCount * pixelStride * 2);
        Span<ushort> lineBuffer = rentedArray.Value;
        Span<int> runIndex = stackalloc int[componentCount];

        for (int line = 0; ;)
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

            CopySourceToLineBufferInterleaveModeLine(source, currentLine[1..], FrameInfo.Width);

            for (int component = 0; component < componentCount; ++component)
            {
                _scanCodec.RunIndex = runIndex[component];

                // Initialize edge pixels used for prediction
                previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
                currentLine[0] = previousLine[1];

                EncodeLine(previousLine, currentLine);

                runIndex[component] = RunIndex;
                currentLine = currentLine[pixelStride..];
                previousLine = previousLine[pixelStride..];
            }

            ++line;
            if (line == FrameInfo.Height)
                break;

            source = source[stride..];
        }
    }

    private void EncodeLines8Bit2ComponentsInterleaveModeSample(ReadOnlySpan<byte> source, int stride)
    {
        int pixelStride = PixelStride;
        using var rentedArray = ArrayPoolHelper.Rent<Pair<byte>>(pixelStride * 2);
        Span<Pair<byte>> lineBuffer = rentedArray.Value;

        for (int line = 0; ;)
        {
            var previousLine = lineBuffer;
            var currentLine = lineBuffer[pixelStride..];
            bool oddLine = (line & 1) == 1;
            if (oddLine)
            {
                var temp = previousLine;
                previousLine = currentLine;
                currentLine = temp;
            }

            CopySourceToLineBufferInterleaveModeSample(source, currentLine[1..], FrameInfo.Width);

            // Initialize edge pixels used for prediction
            previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
            currentLine[0] = previousLine[1];

            EncodeLine(previousLine, currentLine);

            ++line;
            if (line == FrameInfo.Height)
                break;

            source = source[stride..];
        }
    }

    private void EncodeLines8Bit3ComponentsInterleaveModeSample(ReadOnlySpan<byte> source, int stride)
    {
        int pixelStride = PixelStride;
        using var rentedArray = ArrayPoolHelper.Rent<Triplet<byte>>(pixelStride * 2);
        Span<Triplet<byte>> lineBuffer = rentedArray.Value;

        for (int line = 0; ;)
        {
            var previousLine = lineBuffer;
            var currentLine = lineBuffer[pixelStride..];
            bool oddLine = (line & 1) == 1;
            if (oddLine)
            {
                var temp = previousLine;
                previousLine = currentLine;
                currentLine = temp;
            }

            CopySourceToLineBufferInterleaveModeSample(source, currentLine[1..], FrameInfo.Width);

            // Initialize edge pixels used for prediction
            previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
            currentLine[0] = previousLine[1];

            EncodeLine(previousLine, currentLine);

            ++line;
            if (line == FrameInfo.Height)
                break;

            source = source[stride..];
        }
    }

    private void EncodeLines16Bit2ComponentsInterleaveModeSample(ReadOnlySpan<byte> source, int stride)
    {
        int pixelStride = PixelStride;
        using var rentedArray = ArrayPoolHelper.Rent<Pair<ushort>>(pixelStride * 2);
        Span<Pair<ushort>> lineBuffer = rentedArray.Value;

        for (int line = 0; ;)
        {
            var previousLine = lineBuffer;
            var currentLine = lineBuffer[pixelStride..];
            bool oddLine = (line & 1) == 1;
            if (oddLine)
            {
                var temp = previousLine;
                previousLine = currentLine;
                currentLine = temp;
            }

            CopySourceToLineBufferInterleaveModeSample(source, currentLine[1..], FrameInfo.Width);

            // Initialize edge pixels used for prediction
            previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
            currentLine[0] = previousLine[1];

            EncodeLine(previousLine, currentLine);

            ++line;
            if (line == FrameInfo.Height)
                break;

            source = source[stride..];
        }
    }

    private void EncodeLines16Bit3ComponentsInterleaveModeSample(ReadOnlySpan<byte> source, int stride)
    {
        int pixelStride = PixelStride;
        using var rentedArray = ArrayPoolHelper.Rent<Triplet<ushort>>(pixelStride * 2);
        Span<Triplet<ushort>> lineBuffer = rentedArray.Value;

        for (int line = 0; ;)
        {
            var previousLine = lineBuffer;
            var currentLine = lineBuffer[pixelStride..];
            bool oddLine = (line & 1) == 1;
            if (oddLine)
            {
                var temp = previousLine;
                previousLine = currentLine;
                currentLine = temp;
            }

            CopySourceToLineBufferInterleaveModeSample(source, currentLine[1..], FrameInfo.Width);

            // Initialize edge pixels used for prediction
            previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
            currentLine[0] = previousLine[1];

            EncodeLine(previousLine, currentLine);

            ++line;
            if (line == FrameInfo.Height)
                break;

            source = source[stride..];
        }
    }

    private void EncodeLines8Bit4ComponentsInterleaveModeSample(ReadOnlySpan<byte> source, int stride)
    {
        int pixelStride = PixelStride;
        using var rentedArray = ArrayPoolHelper.Rent<Quad<byte>>(pixelStride * 2);
        Span<Quad<byte>> lineBuffer = rentedArray.Value;

        for (int line = 0; ;)
        {
            var previousLine = lineBuffer;
            var currentLine = lineBuffer[pixelStride..];
            bool oddLine = (line & 1) == 1;
            if (oddLine)
            {
                var temp = previousLine;
                previousLine = currentLine;
                currentLine = temp;
            }

            CopySourceToLineBufferInterleaveModeSample(source, currentLine[1..], FrameInfo.Width);

            // Initialize edge pixels used for prediction
            previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
            currentLine[0] = previousLine[1];

            EncodeLine(previousLine, currentLine);

            ++line;
            if (line == FrameInfo.Height)
                break;

            source = source[stride..];
        }
    }

    private void EncodeLines16Bit4ComponentsInterleaveModeSample(ReadOnlySpan<byte> source, int stride)
    {
        int pixelStride = PixelStride;
        using var rentedArray = ArrayPoolHelper.Rent<Quad<ushort>>(pixelStride * 2);

        Span<Quad<ushort>> lineBuffer = rentedArray.Value;

        for (int line = 0; ;)
        {
            var previousLine = lineBuffer;
            var currentLine = lineBuffer[pixelStride..];
            bool oddLine = (line & 1) == 1;
            if (oddLine)
            {
                var temp = previousLine;
                previousLine = currentLine;
                currentLine = temp;
            }

            CopySourceToLineBufferInterleaveModeSample(source, currentLine[1..], FrameInfo.Width);

            // Initialize edge pixels used for prediction
            previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
            currentLine[0] = previousLine[1];

            EncodeLine(previousLine, currentLine);

            ++line;
            if (line == FrameInfo.Height)
                break;

            source = source[stride..];
        }
    }

    private void EncodeLine(Span<byte> previousLine, Span<byte> currentLine)
    {
        int index = 1;
        int rb = previousLine[index - 1];
        int rd = previousLine[index];

        while (index <= FrameInfo.Width)
        {
            int ra = currentLine[index - 1];
            int rc = rb;
            rb = rd;
            rd = previousLine[index + 1];

            int qs = ComputeContextId(QuantizeGradient(rd - rb), QuantizeGradient(rb - rc), QuantizeGradient(rc - ra));
            if (qs == 0)
            {
                index += EncodeRunMode(index, previousLine, currentLine);
                rb = previousLine[index - 1];
                rd = previousLine[index];
            }
            else
            {
                currentLine[index] = (byte)EncodeRegular(qs, currentLine[index], ComputePredictedValue(ra, rb, rc));
                ++index;
            }
        }
    }

    private void EncodeLine(Span<ushort> previousLine, Span<ushort> currentLine)
    {
        int index = 1;
        int rb = previousLine[index - 1];
        int rd = previousLine[index];

        while (index <= FrameInfo.Width)
        {
            int ra = currentLine[index - 1];
            int rc = rb;
            rb = rd;
            rd = previousLine[index + 1];

            int qs = ComputeContextId(QuantizeGradient(rd - rb), QuantizeGradient(rb - rc), QuantizeGradient(rc - ra));
            if (qs == 0)
            {
                index += EncodeRunMode(index, previousLine, currentLine);
                rb = previousLine[index - 1];
                rd = previousLine[index];
            }
            else
            {
                currentLine[index] = (ushort)EncodeRegular(qs, currentLine[index], ComputePredictedValue(ra, rb, rc));
                ++index;
            }
        }
    }

    private void EncodeLine(Span<Pair<byte>> previousLine, Span<Pair<byte>> currentLine)
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
                index += EncodeRunMode(index, previousLine, currentLine);
            }
            else
            {
                currentLine[index] = new Pair<byte>(
                    (byte)EncodeRegular(qs1, currentLine[index].V1, ComputePredictedValue(ra.V1, rb.V1, rc.V1)),
                    (byte)EncodeRegular(qs2, currentLine[index].V2, ComputePredictedValue(ra.V2, rb.V2, rc.V2)));
                ++index;
            }
        }
    }

    private void EncodeLine(Span<Pair<ushort>> previousLine, Span<Pair<ushort>> currentLine)
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
                index += EncodeRunMode(index, previousLine, currentLine);
            }
            else
            {
                currentLine[index] = new Pair<ushort>(
                    (ushort)EncodeRegular(qs1, currentLine[index].V1, ComputePredictedValue(ra.V1, rb.V1, rc.V1)),
                    (ushort)EncodeRegular(qs2, currentLine[index].V2, ComputePredictedValue(ra.V2, rb.V2, rc.V2)));
                ++index;
            }
        }
    }

    private void EncodeLine(Span<Triplet<byte>> previousLine, Span<Triplet<byte>> currentLine)
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
                index += EncodeRunMode(index, previousLine, currentLine);
            }
            else
            {
                currentLine[index] = new Triplet<byte>(
                    (byte)EncodeRegular(qs1, currentLine[index].V1, ComputePredictedValue(ra.V1, rb.V1, rc.V1)),
                    (byte)EncodeRegular(qs2, currentLine[index].V2, ComputePredictedValue(ra.V2, rb.V2, rc.V2)),
                    (byte)EncodeRegular(qs3, currentLine[index].V3, ComputePredictedValue(ra.V3, rb.V3, rc.V3)));
                ++index;
            }
        }
    }

    private void EncodeLine(Span<Triplet<ushort>> previousLine, Span<Triplet<ushort>> currentLine)
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
                index += EncodeRunMode(index, previousLine, currentLine);
            }
            else
            {
                Triplet<ushort> rx;
                rx.V1 = (ushort)EncodeRegular(qs1, currentLine[index].V1, ComputePredictedValue(ra.V1, rb.V1, rc.V1));
                rx.V2 = (ushort)EncodeRegular(qs2, currentLine[index].V2, ComputePredictedValue(ra.V2, rb.V2, rc.V2));
                rx.V3 = (ushort)EncodeRegular(qs3, currentLine[index].V3, ComputePredictedValue(ra.V3, rb.V3, rc.V3));
                currentLine[index] = rx;
                ++index;
            }
        }
    }

    private void EncodeLine(Span<Quad<byte>> previousLine, Span<Quad<byte>> currentLine)
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
                index += EncodeRunMode(index, previousLine, currentLine);
            }
            else
            {
                currentLine[index] = new Quad<byte>(
                    (byte)EncodeRegular(qs1, currentLine[index].V1, ComputePredictedValue(ra.V1, rb.V1, rc.V1)),
                    (byte)EncodeRegular(qs2, currentLine[index].V2, ComputePredictedValue(ra.V2, rb.V2, rc.V2)),
                    (byte)EncodeRegular(qs3, currentLine[index].V3, ComputePredictedValue(ra.V3, rb.V3, rc.V3)),
                    (byte)EncodeRegular(qs3, currentLine[index].V4, ComputePredictedValue(ra.V4, rb.V4, rc.V4)));
                ++index;
            }
        }
    }

    private void EncodeLine(Span<Quad<ushort>> previousLine, Span<Quad<ushort>> currentLine)
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
                index += EncodeRunMode(index, previousLine, currentLine);
            }
            else
            {
                currentLine[index] = new Quad<ushort>(
                    (ushort)EncodeRegular(qs1, currentLine[index].V1, ComputePredictedValue(ra.V1, rb.V1, rc.V1)),
                    (ushort)EncodeRegular(qs2, currentLine[index].V2, ComputePredictedValue(ra.V2, rb.V2, rc.V2)),
                    (ushort)EncodeRegular(qs3, currentLine[index].V3, ComputePredictedValue(ra.V3, rb.V3, rc.V3)),
                    (ushort)EncodeRegular(qs3, currentLine[index].V4, ComputePredictedValue(ra.V4, rb.V4, rc.V4)));
                ++index;
            }
        }
    }

    private int EncodeRegular(int qs, int x, int predicted)
    {
        int sign = BitWiseSign(qs);
        ref var context = ref _scanCodec.RegularModeContext[ApplySign(qs, sign)];
        int k = context.ComputeGolombCodingParameter();
        int predictedValue = Traits.CorrectPrediction(predicted + ApplySign(context.C, sign));
        int errorValue = Traits.ComputeErrorValue(ApplySign(x - predictedValue, sign));

        EncodeMappedValue(k, MapErrorValue(context.GetErrorCorrection(k | _scanCodec.NearLossless) ^ errorValue), _scanCodec.Limit);
        context.UpdateVariablesAndBias(errorValue, _scanCodec.NearLossless, _scanCodec.ResetThreshold);

        Debug.Assert(Traits.IsNear(Traits.ComputeReconstructedSample(predictedValue, ApplySign(errorValue, sign)), x));
        return Traits.ComputeReconstructedSample(predictedValue, ApplySign(errorValue, sign));
    }

    private int EncodeRunMode(int startIndex, Span<byte> previousLine, Span<byte> currentLine)
    {
        int countTypeRemain = FrameInfo.Width - (startIndex - 1);
        var typePrevX = previousLine[startIndex..];
        var typeCurX = currentLine[startIndex..];
        var ra = currentLine[startIndex - 1];

        int runLength = 0;
        while (Traits.IsNear(typeCurX[runLength], ra))
        {
            typeCurX[runLength] = ra;
            ++runLength;

            if (runLength == countTypeRemain)
                break;
        }

        EncodeRunPixels(runLength, runLength == countTypeRemain);

        if (runLength == countTypeRemain)
            return runLength;

        typeCurX[runLength] = (byte)EncodeRunInterruptionPixel(typeCurX[runLength], ra, typePrevX[runLength]);
        _scanCodec.DecrementRunIndex();
        return runLength + 1;
    }

    private int EncodeRunMode(int startIndex, Span<ushort> previousLine, Span<ushort> currentLine)
    {
        int countTypeRemain = FrameInfo.Width - (startIndex - 1);
        var typePrevX = previousLine[startIndex..];
        var typeCurX = currentLine[startIndex..];
        var ra = currentLine[startIndex - 1];

        int runLength = 0;
        while (Traits.IsNear(typeCurX[runLength], ra))
        {
            typeCurX[runLength] = ra;
            ++runLength;

            if (runLength == countTypeRemain)
                break;
        }

        EncodeRunPixels(runLength, runLength == countTypeRemain);

        if (runLength == countTypeRemain)
            return runLength;

        typeCurX[runLength] = (ushort)EncodeRunInterruptionPixel(typeCurX[runLength], ra, typePrevX[runLength]);
        _scanCodec.DecrementRunIndex();
        return runLength + 1;
    }

    private int EncodeRunMode(int startIndex, Span<Pair<byte>> previousLine, Span<Pair<byte>> currentLine)
    {
        int countTypeRemain = FrameInfo.Width - (startIndex - 1);
        var typePrevX = previousLine[startIndex..];
        var typeCurX = currentLine[startIndex..];
        var ra = currentLine[startIndex - 1];

        int runLength = 0;
        while (Traits.IsNear(typeCurX[runLength], ra))
        {
            typeCurX[runLength] = ra;
            ++runLength;

            if (runLength == countTypeRemain)
                break;
        }

        EncodeRunPixels(runLength, runLength == countTypeRemain);

        if (runLength == countTypeRemain)
            return runLength;

        typeCurX[runLength] = EncodeRunInterruptionPixel(typeCurX[runLength], ra, typePrevX[runLength]);
        _scanCodec.DecrementRunIndex();
        return runLength + 1;
    }

    private int EncodeRunMode(int startIndex, Span<Pair<ushort>> previousLine, Span<Pair<ushort>> currentLine)
    {
        int countTypeRemain = FrameInfo.Width - (startIndex - 1);
        var typePrevX = previousLine[startIndex..];
        var typeCurX = currentLine[startIndex..];
        var ra = currentLine[startIndex - 1];

        int runLength = 0;
        while (Traits.IsNear(typeCurX[runLength], ra))
        {
            typeCurX[runLength] = ra;
            ++runLength;

            if (runLength == countTypeRemain)
                break;
        }

        EncodeRunPixels(runLength, runLength == countTypeRemain);

        if (runLength == countTypeRemain)
            return runLength;

        typeCurX[runLength] = EncodeRunInterruptionPixel(typeCurX[runLength], ra, typePrevX[runLength]);
        _scanCodec.DecrementRunIndex();
        return runLength + 1;
    }

    private int EncodeRunMode(int startIndex, Span<Triplet<byte>> previousLine, Span<Triplet<byte>> currentLine)
    {
        int countTypeRemain = FrameInfo.Width - (startIndex - 1);
        var typePrevX = previousLine[startIndex..];
        var typeCurX = currentLine[startIndex..];
        var ra = currentLine[startIndex - 1];

        int runLength = 0;
        while (Traits.IsNear(typeCurX[runLength], ra))
        {
            typeCurX[runLength] = ra;
            ++runLength;

            if (runLength == countTypeRemain)
                break;
        }

        EncodeRunPixels(runLength, runLength == countTypeRemain);

        if (runLength == countTypeRemain)
            return runLength;

        typeCurX[runLength] = EncodeRunInterruptionPixel(typeCurX[runLength], ra, typePrevX[runLength]);
        _scanCodec.DecrementRunIndex();
        return runLength + 1;
    }

    private int EncodeRunMode(int startIndex, Span<Triplet<ushort>> previousLine, Span<Triplet<ushort>> currentLine)
    {
        int countTypeRemain = FrameInfo.Width - (startIndex - 1);
        var typePrevX = previousLine[startIndex..];
        var typeCurX = currentLine[startIndex..];
        var ra = currentLine[startIndex - 1];

        int runLength = 0;
        while (Traits.IsNear(typeCurX[runLength], ra))
        {
            typeCurX[runLength] = ra;
            ++runLength;

            if (runLength == countTypeRemain)
                break;
        }

        EncodeRunPixels(runLength, runLength == countTypeRemain);

        if (runLength == countTypeRemain)
            return runLength;

        typeCurX[runLength] = EncodeRunInterruptionPixel(typeCurX[runLength], ra, typePrevX[runLength]);
        _scanCodec.DecrementRunIndex();
        return runLength + 1;
    }

    private int EncodeRunMode(int startIndex, Span<Quad<byte>> previousLine, Span<Quad<byte>> currentLine)
    {
        int countTypeRemain = FrameInfo.Width - (startIndex - 1);
        var typePrevX = previousLine[startIndex..];
        var typeCurX = currentLine[startIndex..];
        var ra = currentLine[startIndex - 1];

        int runLength = 0;
        while (Traits.IsNear(typeCurX[runLength], ra))
        {
            typeCurX[runLength] = ra;
            ++runLength;

            if (runLength == countTypeRemain)
                break;
        }

        EncodeRunPixels(runLength, runLength == countTypeRemain);

        if (runLength == countTypeRemain)
            return runLength;

        typeCurX[runLength] = EncodeRunInterruptionPixel(typeCurX[runLength], ra, typePrevX[runLength]);
        _scanCodec.DecrementRunIndex();
        return runLength + 1;
    }

    private int EncodeRunMode(int startIndex, Span<Quad<ushort>> previousLine, Span<Quad<ushort>> currentLine)
    {
        int countTypeRemain = FrameInfo.Width - (startIndex - 1);
        var typePrevX = previousLine[startIndex..];
        var typeCurX = currentLine[startIndex..];
        var ra = currentLine[startIndex - 1];

        int runLength = 0;
        while (Traits.IsNear(typeCurX[runLength], ra))
        {
            typeCurX[runLength] = ra;
            ++runLength;

            if (runLength == countTypeRemain)
                break;
        }

        EncodeRunPixels(runLength, runLength == countTypeRemain);

        if (runLength == countTypeRemain)
            return runLength;

        typeCurX[runLength] = EncodeRunInterruptionPixel(typeCurX[runLength], ra, typePrevX[runLength]);
        _scanCodec.DecrementRunIndex();
        return runLength + 1;
    }

    private int EncodeRunInterruptionPixel(int x, int ra, int rb)
    {
        bool same = _scanCodec.NearLossless == 0 ? ra == rb : AbsUnchecked(ra - rb) <= _scanCodec.NearLossless;
        if (same)
        {
            int errorValue = Traits.ComputeErrorValue(x - ra);
            EncodeRunInterruptionError(ref _scanCodec.RunModeContexts[1], errorValue);
            return Traits.ComputeReconstructedSample(ra, errorValue);
        }
        else
        {
            int errorValue = Traits.ComputeErrorValue((x - rb) * Sign(rb - ra));
            EncodeRunInterruptionError(ref _scanCodec.RunModeContexts[0], errorValue);
            return Traits.ComputeReconstructedSample(rb, errorValue * Sign(rb - ra));
        }
    }

    private Pair<byte> EncodeRunInterruptionPixel(Pair<byte> x, Pair<byte> ra, Pair<byte> rb)
    {
        int errorValue1 = Traits.ComputeErrorValue(Sign(rb.V1 - ra.V1) * (x.V1 - rb.V1));
        EncodeRunInterruptionError(ref _scanCodec.RunModeContexts[0], errorValue1);

        int errorValue2 = Traits.ComputeErrorValue(Sign(rb.V2 - ra.V2) * (x.V2 - rb.V2));
        EncodeRunInterruptionError(ref _scanCodec.RunModeContexts[0], errorValue2);

        return new Pair<byte>(
            (byte)Traits.ComputeReconstructedSample(rb.V1, errorValue1 * Sign(rb.V1 - ra.V1)),
            (byte)Traits.ComputeReconstructedSample(rb.V2, errorValue2 * Sign(rb.V2 - ra.V2)));
    }

    private Pair<ushort> EncodeRunInterruptionPixel(Pair<ushort> x, Pair<ushort> ra, Pair<ushort> rb)
    {
        int errorValue1 = Traits.ComputeErrorValue(Sign(rb.V1 - ra.V1) * (x.V1 - rb.V1));
        EncodeRunInterruptionError(ref _scanCodec.RunModeContexts[0], errorValue1);

        int errorValue2 = Traits.ComputeErrorValue(Sign(rb.V2 - ra.V2) * (x.V2 - rb.V2));
        EncodeRunInterruptionError(ref _scanCodec.RunModeContexts[0], errorValue2);

        return new Pair<ushort>(
            (ushort)Traits.ComputeReconstructedSample(rb.V1, errorValue1 * Sign(rb.V1 - ra.V1)),
            (ushort)Traits.ComputeReconstructedSample(rb.V2, errorValue2 * Sign(rb.V2 - ra.V2)));
    }

    private Triplet<byte> EncodeRunInterruptionPixel(Triplet<byte> x, Triplet<byte> ra, Triplet<byte> rb)
    {
        int errorValue1 = Traits.ComputeErrorValue(Sign(rb.V1 - ra.V1) * (x.V1 - rb.V1));
        EncodeRunInterruptionError(ref _scanCodec.RunModeContexts[0], errorValue1);

        int errorValue2 = Traits.ComputeErrorValue(Sign(rb.V2 - ra.V2) * (x.V2 - rb.V2));
        EncodeRunInterruptionError(ref _scanCodec.RunModeContexts[0], errorValue2);

        int errorValue3 = Traits.ComputeErrorValue(Sign(rb.V3 - ra.V3) * (x.V3 - rb.V3));
        EncodeRunInterruptionError(ref _scanCodec.RunModeContexts[0], errorValue3);

        return new Triplet<byte>(
            (byte)Traits.ComputeReconstructedSample(rb.V1, errorValue1 * Sign(rb.V1 - ra.V1)),
            (byte)Traits.ComputeReconstructedSample(rb.V2, errorValue2 * Sign(rb.V2 - ra.V2)),
            (byte)Traits.ComputeReconstructedSample(rb.V3, errorValue3 * Sign(rb.V3 - ra.V3)));
    }

    private Triplet<ushort> EncodeRunInterruptionPixel(Triplet<ushort> x, Triplet<ushort> ra, Triplet<ushort> rb)
    {
        int errorValue1 = Traits.ComputeErrorValue(Sign(rb.V1 - ra.V1) * (x.V1 - rb.V1));
        EncodeRunInterruptionError(ref _scanCodec.RunModeContexts[0], errorValue1);

        int errorValue2 = Traits.ComputeErrorValue(Sign(rb.V2 - ra.V2) * (x.V2 - rb.V2));
        EncodeRunInterruptionError(ref _scanCodec.RunModeContexts[0], errorValue2);

        int errorValue3 = Traits.ComputeErrorValue(Sign(rb.V3 - ra.V3) * (x.V3 - rb.V3));
        EncodeRunInterruptionError(ref _scanCodec.RunModeContexts[0], errorValue3);

        return new Triplet<ushort>(
            (ushort)Traits.ComputeReconstructedSample(rb.V1, errorValue1 * Sign(rb.V1 - ra.V1)),
            (ushort)Traits.ComputeReconstructedSample(rb.V2, errorValue2 * Sign(rb.V2 - ra.V2)),
            (ushort)Traits.ComputeReconstructedSample(rb.V3, errorValue3 * Sign(rb.V3 - ra.V3)));
    }

    private Quad<byte> EncodeRunInterruptionPixel(Quad<byte> x, Quad<byte> ra, Quad<byte> rb)
    {
        int errorValue1 = Traits.ComputeErrorValue(Sign(rb.V1 - ra.V1) * (x.V1 - rb.V1));
        EncodeRunInterruptionError(ref _scanCodec.RunModeContexts[0], errorValue1);

        int errorValue2 = Traits.ComputeErrorValue(Sign(rb.V2 - ra.V2) * (x.V2 - rb.V2));
        EncodeRunInterruptionError(ref _scanCodec.RunModeContexts[0], errorValue2);

        int errorValue3 = Traits.ComputeErrorValue(Sign(rb.V3 - ra.V3) * (x.V3 - rb.V3));
        EncodeRunInterruptionError(ref _scanCodec.RunModeContexts[0], errorValue3);

        int errorValue4 = Traits.ComputeErrorValue(Sign(rb.V4 - ra.V4) * (x.V4 - rb.V4));
        EncodeRunInterruptionError(ref _scanCodec.RunModeContexts[0], errorValue4);

        return new Quad<byte>(
            (byte)Traits.ComputeReconstructedSample(rb.V1, errorValue1 * Sign(rb.V1 - ra.V1)),
            (byte)Traits.ComputeReconstructedSample(rb.V2, errorValue2 * Sign(rb.V2 - ra.V2)),
            (byte)Traits.ComputeReconstructedSample(rb.V3, errorValue3 * Sign(rb.V3 - ra.V3)),
            (byte)Traits.ComputeReconstructedSample(rb.V4, errorValue4 * Sign(rb.V4 - ra.V4)));
    }

    private Quad<ushort> EncodeRunInterruptionPixel(Quad<ushort> x, Quad<ushort> ra, Quad<ushort> rb)
    {
        int errorValue1 = Traits.ComputeErrorValue(Sign(rb.V1 - ra.V1) * (x.V1 - rb.V1));
        EncodeRunInterruptionError(ref _scanCodec.RunModeContexts[0], errorValue1);

        int errorValue2 = Traits.ComputeErrorValue(Sign(rb.V2 - ra.V2) * (x.V2 - rb.V2));
        EncodeRunInterruptionError(ref _scanCodec.RunModeContexts[0], errorValue2);

        int errorValue3 = Traits.ComputeErrorValue(Sign(rb.V3 - ra.V3) * (x.V3 - rb.V3));
        EncodeRunInterruptionError(ref _scanCodec.RunModeContexts[0], errorValue3);

        int errorValue4 = Traits.ComputeErrorValue(Sign(rb.V4 - ra.V4) * (x.V4 - rb.V4));
        EncodeRunInterruptionError(ref _scanCodec.RunModeContexts[0], errorValue4);

        return new Quad<ushort>(
            (ushort)Traits.ComputeReconstructedSample(rb.V1, errorValue1 * Sign(rb.V1 - ra.V1)),
            (ushort)Traits.ComputeReconstructedSample(rb.V2, errorValue2 * Sign(rb.V2 - ra.V2)),
            (ushort)Traits.ComputeReconstructedSample(rb.V3, errorValue3 * Sign(rb.V3 - ra.V3)),
            (ushort)Traits.ComputeReconstructedSample(rb.V4, errorValue4 * Sign(rb.V4 - ra.V4)));
    }

    private void EncodeRunInterruptionError(ref RunModeContext context, int errorValue)
    {
        int k = context.ComputeGolombCodingParameter();
        int map = context.ComputeMap(errorValue, k) ? 1 : 0;
        int eMappedErrorValue = (2 * AbsUnchecked(errorValue)) - context.RunInterruptionType - map;
        Debug.Assert(errorValue == context.ComputeErrorValue(eMappedErrorValue + context.RunInterruptionType, k));
        EncodeMappedValue(k, eMappedErrorValue, _scanCodec.Limit - ScanCodec.J[RunIndex] - 1);
        context.UpdateVariables(errorValue, eMappedErrorValue, (byte)_scanCodec.PresetCodingParameters.ResetValue);
    }

    private void EncodeMappedValue(int k, int mappedError, int limit)
    {
        int highBits = mappedError >> k;
        if (highBits < limit - _scanCodec.QuantizedBitsPerSample - 1)
        {
            if (highBits + 1 > 31)
            {
                AppendToBitStream(0, highBits / 2);
                highBits -= highBits / 2;
            }

            AppendToBitStream(1, highBits + 1);
            AppendToBitStream((uint)(mappedError & ((1 << k) - 1)), k);
            return;
        }

        if (limit - _scanCodec.QuantizedBitsPerSample > 31)
        {
            AppendToBitStream(0, 31);
            AppendToBitStream(1, limit - _scanCodec.QuantizedBitsPerSample - 31);
        }
        else
        {
            AppendToBitStream(1, limit - _scanCodec.QuantizedBitsPerSample);
        }

        AppendToBitStream((uint)((mappedError - 1) & ((1 << _scanCodec.QuantizedBitsPerSample) - 1)), _scanCodec.QuantizedBitsPerSample);
    }

    private readonly int QuantizeGradient(int di)
    {
        Debug.Assert(_scanCodec.QuantizeGradient(di, _scanCodec.NearLossless) == _scanCodec.QuantizationLut[(_scanCodec.QuantizationLut.Length / 2) + di]);
        return _scanCodec.QuantizationLut[(_scanCodec.QuantizationLut.Length / 2) + di];
    }

    private readonly void CopySourceToLineBufferInterleaveModeNone(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
    {
        _copyToLineBuffer!(source, destination, pixelCount, _mask);
    }

    private readonly void CopySourceToLineBufferInterleaveModeNone(ReadOnlySpan<byte> source, Span<ushort> destination, int pixelCount)
    {
        var destinationInBytes = MemoryMarshal.Cast<ushort, byte>(destination);
        _copyToLineBuffer!(source, destinationInBytes, pixelCount, _mask);
    }

    private readonly void CopySourceToLineBufferInterleaveModeLine(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
    {
        _copyToLineBuffer!(source, destination, pixelCount, _mask);
    }

    private readonly void CopySourceToLineBufferInterleaveModeLine(ReadOnlySpan<byte> source, Span<ushort> destination, int pixelCount)
    {
        var destinationInBytes = MemoryMarshal.Cast<ushort, byte>(destination);
        _copyToLineBuffer!(source, destinationInBytes, pixelCount, _mask);
    }

    private readonly void CopySourceToLineBufferInterleaveModeSample(ReadOnlySpan<byte> source, Span<Pair<byte>> destination, int pixelCount)
    {
        var destinationInBytes = MemoryMarshal.Cast<Pair<byte>, byte>(destination);
        _copyToLineBuffer!(source, destinationInBytes, pixelCount, _mask);
    }

    private readonly void CopySourceToLineBufferInterleaveModeSample(ReadOnlySpan<byte> source, Span<Pair<ushort>> destination, int pixelCount)
    {
        var destinationInBytes = MemoryMarshal.Cast<Pair<ushort>, byte>(destination);
        _copyToLineBuffer!(source, destinationInBytes, pixelCount, _mask);
    }

    private readonly void CopySourceToLineBufferInterleaveModeSample(ReadOnlySpan<byte> source, Span<Triplet<byte>> destination, int pixelCount)
    {
        var destinationInBytes = MemoryMarshal.Cast<Triplet<byte>, byte>(destination);
        _copyToLineBuffer!(source, destinationInBytes, pixelCount, _mask);
    }

    private readonly void CopySourceToLineBufferInterleaveModeSample(ReadOnlySpan<byte> source, Span<Triplet<ushort>> destination, int pixelCount)
    {
        var destinationInBytes = MemoryMarshal.Cast<Triplet<ushort>, byte>(destination);
        _copyToLineBuffer!(source, destinationInBytes, pixelCount, _mask);
    }

    private readonly void CopySourceToLineBufferInterleaveModeSample(ReadOnlySpan<byte> source, Span<Quad<byte>> destination, int pixelCount)
    {
        var destinationInBytes = MemoryMarshal.Cast<Quad<byte>, byte>(destination);
        _copyToLineBuffer!(source, destinationInBytes, pixelCount, _mask);
    }

    private readonly void CopySourceToLineBufferInterleaveModeSample(ReadOnlySpan<byte> source, Span<Quad<ushort>> destination, int pixelCount)
    {
        var destinationInBytes = MemoryMarshal.Cast<Quad<ushort>, byte>(destination);
        _copyToLineBuffer!(source, destinationInBytes, pixelCount, _mask);
    }
}
