// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CharLS.JpegLS;

internal class ScanEncoderImpl : ScanEncoder
{
    private readonly Traits _traits;
    private readonly sbyte[] _quantizationLut;
    private IProcessLineEncoded? _processLine;

    internal ScanEncoderImpl(FrameInfo frameInfo, JpegLSPresetCodingParameters presetCodingParameters,
        CodingParameters codingParameters, Traits traits) :
        base(frameInfo, presetCodingParameters, codingParameters)
    {
        _traits = traits;

        _quantizationLut = InitializeQuantizationLut(traits, PresetCodingParameters.Threshold1,
            PresetCodingParameters.Threshold2, PresetCodingParameters.Threshold3);

        InitializeParameters(_traits.Range);
    }

    public override int EncodeScan(ReadOnlyMemory<byte> source, Memory<byte> destination, int stride)
    {
        _processLine = CreateProcessLine();

        Initialize(destination);

        if (FrameInfo.BitsPerSample <= 8)
        {
            switch (CodingParameters.InterleaveMode)
            {
                case InterleaveMode.None:
                    EncodeLines8BitInterleaveModeNone(source);
                    break;

                case InterleaveMode.Line:
                    EncodeLines8BitInterleaveModeLine(source);
                    break;

                case InterleaveMode.Sample:
                    switch (FrameInfo.ComponentCount)
                    {
                        case 3:
                            EncodeLines8Bit3ComponentsInterleaveModeSample(source);
                            break;
                        case 4:
                            EncodeLines8Bit4ComponentsInterleaveModeSample(source);
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
                    EncodeLines16BitInterleaveModeNone(source);
                    break;

                case InterleaveMode.Line:
                    EncodeLines16BitInterleaveModeLine(source);
                    break;

                case InterleaveMode.Sample:
                    switch (FrameInfo.ComponentCount)
                    {
                        case 3:
                            EncodeLines16Bit3ComponentsInterleaveModeSample(source);
                            break;
                        case 4:
                            EncodeLines16Bit4ComponentsInterleaveModeSample(source);
                            break;
                    }
                    break;
            }
        }

        EndScan();

        return GetLength();
    }

    // In ILV_SAMPLE mode, multiple components are handled in do_line
    // In ILV_LINE mode, a call to do_line is made for every component
    // In ILV_NONE mode, do_scan is called for each component
    private void EncodeLines8BitInterleaveModeNone(ReadOnlyMemory<byte> source)
    {
        int pixelStride = FrameInfo.Width + 2;
        Span<byte> lineBuffer = new byte[pixelStride * 2];

        for (int line = 0; line < FrameInfo.Height; ++line)
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

            int bytesRead = OnLineBegin(source.Span, currentLine[1..], FrameInfo.Width);
            source = source[bytesRead..];

            // initialize edge pixels used for prediction
            previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
            currentLine[0] = previousLine[1];

            EncodeSampleLine(previousLine, currentLine);
        }
    }

    private void EncodeLines8BitInterleaveModeLine(ReadOnlyMemory<byte> source)
    {
        int pixelStride = FrameInfo.Width + 2;
        int componentCount = FrameInfo.ComponentCount;

        Span<int> runIndex = stackalloc int[componentCount];
        Span<byte> lineBuffer = new byte[componentCount * pixelStride * 2]; // TODO: can use smaller buffer?

        for (int line = 0; line < FrameInfo.Height; ++line)
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

            int bytesRead = OnLineBeginInterleaveModeLine(source.Span, currentLine[1..], FrameInfo.Width);
            source = source[bytesRead..];

            for (int component = 0; component < componentCount; ++component)
            {
                RunIndex = runIndex[component];

                // initialize edge pixels used for prediction
                previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
                currentLine[0] = previousLine[1];

                EncodeSampleLine(previousLine, currentLine);

                runIndex[component] = RunIndex;
                currentLine = currentLine[pixelStride..];
                previousLine = previousLine[pixelStride..];
            }
        }
    }

    private void EncodeLines16BitInterleaveModeLine(ReadOnlyMemory<byte> source)
    {
        int pixelStride = FrameInfo.Width + 2;
        int componentCount = FrameInfo.ComponentCount;

        Span<int> runIndex = stackalloc int[componentCount];
        Span<ushort> lineBuffer = new ushort[componentCount * pixelStride * 2]; // TODO: can use smaller buffer?

        for (int line = 0; line < FrameInfo.Height; ++line)
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

            int bytesRead = OnLineBeginInterleaveModeLine(source.Span, currentLine[1..], FrameInfo.Width, componentCount);
            source = source[bytesRead..];

            for (int component = 0; component < componentCount; ++component)
            {
                RunIndex = runIndex[component];

                // initialize edge pixels used for prediction
                previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
                currentLine[0] = previousLine[1];

                EncodeSampleLine(previousLine, currentLine);

                runIndex[component] = RunIndex;
                currentLine = currentLine[pixelStride..];
                previousLine = previousLine[pixelStride..];
            }
        }
    }

    private void EncodeLines8Bit3ComponentsInterleaveModeSample(ReadOnlyMemory<byte> source)
    {
        int pixelStride = FrameInfo.Width + 2;

        Span<Triplet<byte>> lineBuffer = new Triplet<byte>[pixelStride * 2];

        for (int line = 0; line < FrameInfo.Height; ++line)
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

            int bytesRead = OnLineBeginInterleaveModeSample(source.Span, currentLine[1..], FrameInfo.Width);
            source = source[bytesRead..];

            // initialize edge pixels used for prediction
            previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
            currentLine[0] = previousLine[1];

            EncodeSampleLine(previousLine, currentLine);
        }
    }

    private void EncodeLines16Bit3ComponentsInterleaveModeSample(ReadOnlyMemory<byte> source)
    {
        int pixelStride = FrameInfo.Width + 2;

        Span<Triplet<ushort>> lineBuffer = new Triplet<ushort>[pixelStride * 2];

        for (int line = 0; line < FrameInfo.Height; ++line)
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

            int bytesRead = OnLineBeginInterleaveModeSample(source.Span, currentLine[1..], FrameInfo.Width);
            source = source[bytesRead..];

            // initialize edge pixels used for prediction
            previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
            currentLine[0] = previousLine[1];

            EncodeSampleLine(previousLine, currentLine);
        }
    }

    private void EncodeLines8Bit4ComponentsInterleaveModeSample(ReadOnlyMemory<byte> source)
    {
        int pixelStride = FrameInfo.Width + 2;

        Span<Quad<byte>> lineBuffer = new Quad<byte>[pixelStride * 2];

        for (int line = 0; line < FrameInfo.Height; ++line)
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

            int bytesRead = OnLineBeginInterleaveModeSample(source.Span, currentLine[1..], FrameInfo.Width);
            source = source[bytesRead..];

            // initialize edge pixels used for prediction
            previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
            currentLine[0] = previousLine[1];

            EncodeSampleLine(previousLine, currentLine);
        }
    }

    private void EncodeLines16Bit4ComponentsInterleaveModeSample(ReadOnlyMemory<byte> source)
    {
        int pixelStride = FrameInfo.Width + 2;

        Span<Quad<ushort>> lineBuffer = new Quad<ushort>[pixelStride * 2];

        for (int line = 0; line < FrameInfo.Height; ++line)
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

            int bytesRead = OnLineBeginInterleaveModeSample(source.Span, currentLine[1..], FrameInfo.Width);
            source = source[bytesRead..];

            // initialize edge pixels used for prediction
            previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
            currentLine[0] = previousLine[1];

            EncodeSampleLine(previousLine, currentLine);
        }
    }

    private void EncodeLines16BitInterleaveModeNone(ReadOnlyMemory<byte> source)
    {
        int pixelStride = FrameInfo.Width + 2;
        int componentCount = CodingParameters.InterleaveMode == InterleaveMode.Line ? FrameInfo.ComponentCount : 1;

        Span<int> runIndex = stackalloc int[componentCount];
        Span<ushort> lineBuffer = new ushort[componentCount * pixelStride * 2]; // TODO: can use smaller buffer?

        for (int line = 0; line < FrameInfo.Height; ++line)
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

            int bytesRead = OnLineBegin(source.Span, currentLine[1..], FrameInfo.Width);
            source = source[bytesRead..];

            for (int component = 0; component < componentCount; ++component)
            {
                RunIndex = runIndex[component];

                // initialize edge pixels used for prediction
                previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
                currentLine[0] = previousLine[1];

                EncodeSampleLine(previousLine, currentLine);

                runIndex[component] = RunIndex;
                currentLine = currentLine[pixelStride..];
                previousLine = previousLine[pixelStride..];
            }
        }
    }

    private void EncodeSampleLine(Span<byte> previousLine, Span<byte> currentLine)
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

            int qs = Algorithm.ComputeContextId(QuantizeGradient(rd - rb), QuantizeGradient(rb - rc), QuantizeGradient(rc - ra));
            if (qs != 0)
            {
                currentLine[index] = (byte)encode_regular(qs, currentLine[index], Algorithm.ComputePredictedValue(ra, rb, rc));
                ++index;
            }
            else
            {
                index += EncodeRunMode(index, previousLine, currentLine);
                rb = previousLine[index - 1];
                rd = previousLine[index];
            }
        }
    }

    private void EncodeSampleLine(Span<ushort> previousLine, Span<ushort> currentLine)
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

            int qs = Algorithm.ComputeContextId(QuantizeGradient(rd - rb), QuantizeGradient(rb - rc), QuantizeGradient(rc - ra));
            if (qs != 0)
            {
                currentLine[index] = (ushort)encode_regular(qs, currentLine[index], Algorithm.ComputePredictedValue(ra, rb, rc));
                ++index;
            }
            else
            {
                index += EncodeRunMode(index, previousLine, currentLine);
                rb = previousLine[index - 1];
                rd = previousLine[index];
            }
        }
    }

    private void EncodeSampleLine(Span<Triplet<byte>> previousLine, Span<Triplet<byte>> currentLine)
    {
        int index = 1;
        while (index <= FrameInfo.Width)
        {
            var ra = currentLine[index - 1];
            var rc = previousLine[index - 1];
            var rb = previousLine[index];
            var rd = previousLine[index + 1];

            int qs1 = Algorithm.ComputeContextId(QuantizeGradient(rd.V1 - rb.V1), QuantizeGradient(rb.V1 - rc.V1),
                    QuantizeGradient(rc.V1 - ra.V1));
            int qs2 = Algorithm.ComputeContextId(QuantizeGradient(rd.V2 - rb.V2), QuantizeGradient(rb.V2 - rc.V2),
                    QuantizeGradient(rc.V2 - ra.V2));
            int qs3 = Algorithm.ComputeContextId(QuantizeGradient(rd.V3 - rb.V3), QuantizeGradient(rb.V3 - rc.V3),
                    QuantizeGradient(rc.V3 - ra.V3));
            if (qs1 == 0 && qs2 == 0 && qs3 == 0)
            {
                index += EncodeRunMode(index, previousLine, currentLine);
            }
            else
            {
                Triplet<byte> rx;
                rx.V1 = (byte)encode_regular(qs1, currentLine[index].V1, Algorithm.ComputePredictedValue(ra.V1, rb.V1, rc.V1));
                rx.V2 = (byte)encode_regular(qs2, currentLine[index].V2, Algorithm.ComputePredictedValue(ra.V2, rb.V2, rc.V2));
                rx.V3 = (byte)encode_regular(qs3, currentLine[index].V3, Algorithm.ComputePredictedValue(ra.V3, rb.V3, rc.V3));
                currentLine[index] = rx;
                ++index;
            }
        }
    }

    private void EncodeSampleLine(Span<Triplet<ushort>> previousLine, Span<Triplet<ushort>> currentLine)
    {
        int index = 1;
        while (index <= FrameInfo.Width)
        {
            var ra = currentLine[index - 1];
            var rc = previousLine[index - 1];
            var rb = previousLine[index];
            var rd = previousLine[index + 1];

            int qs1 = Algorithm.ComputeContextId(QuantizeGradient(rd.V1 - rb.V1), QuantizeGradient(rb.V1 - rc.V1),
                QuantizeGradient(rc.V1 - ra.V1));
            int qs2 = Algorithm.ComputeContextId(QuantizeGradient(rd.V2 - rb.V2), QuantizeGradient(rb.V2 - rc.V2),
                QuantizeGradient(rc.V2 - ra.V2));
            int qs3 = Algorithm.ComputeContextId(QuantizeGradient(rd.V3 - rb.V3), QuantizeGradient(rb.V3 - rc.V3),
                QuantizeGradient(rc.V3 - ra.V3));
            if (qs1 == 0 && qs2 == 0 && qs3 == 0)
            {
                index += EncodeRunMode(index, previousLine, currentLine);
            }
            else
            {
                Triplet<ushort> rx;
                rx.V1 = (ushort)encode_regular(qs1, currentLine[index].V1, Algorithm.ComputePredictedValue(ra.V1, rb.V1, rc.V1));
                rx.V2 = (ushort)encode_regular(qs2, currentLine[index].V2, Algorithm.ComputePredictedValue(ra.V2, rb.V2, rc.V2));
                rx.V3 = (ushort)encode_regular(qs3, currentLine[index].V3, Algorithm.ComputePredictedValue(ra.V3, rb.V3, rc.V3));
                currentLine[index] = rx;
                ++index;
            }
        }
    }

    private void EncodeSampleLine(Span<Quad<byte>> previousLine, Span<Quad<byte>> currentLine)
    {
        int index = 1;
        while (index <= FrameInfo.Width)
        {
            var ra = currentLine[index - 1];
            var rc = previousLine[index - 1];
            var rb = previousLine[index];
            var rd = previousLine[index + 1];

            int qs1 = Algorithm.ComputeContextId(QuantizeGradient(rd.V1 - rb.V1), QuantizeGradient(rb.V1 - rc.V1),
                QuantizeGradient(rc.V1 - ra.V1));
            int qs2 = Algorithm.ComputeContextId(QuantizeGradient(rd.V2 - rb.V2), QuantizeGradient(rb.V2 - rc.V2),
                QuantizeGradient(rc.V2 - ra.V2));
            int qs3 = Algorithm.ComputeContextId(QuantizeGradient(rd.V3 - rb.V3), QuantizeGradient(rb.V3 - rc.V3),
                QuantizeGradient(rc.V3 - ra.V3));
            int qs4 = Algorithm.ComputeContextId(QuantizeGradient(rd.V4 - rb.V4), QuantizeGradient(rb.V4 - rc.V4),
                QuantizeGradient(rc.V4 - ra.V4));
            if (qs1 == 0 && qs2 == 0 && qs3 == 0 && qs4 == 0)
            {
                index += EncodeRunMode(index, previousLine, currentLine);
            }
            else
            {
                Quad<byte> rx;
                rx.V1 = (byte)encode_regular(qs1, currentLine[index].V1, Algorithm.ComputePredictedValue(ra.V1, rb.V1, rc.V1));
                rx.V2 = (byte)encode_regular(qs2, currentLine[index].V2, Algorithm.ComputePredictedValue(ra.V2, rb.V2, rc.V2));
                rx.V3 = (byte)encode_regular(qs3, currentLine[index].V3, Algorithm.ComputePredictedValue(ra.V3, rb.V3, rc.V3));
                rx.V4 = (byte)encode_regular(qs3, currentLine[index].V4, Algorithm.ComputePredictedValue(ra.V4, rb.V4, rc.V4));
                currentLine[index] = rx;
                ++index;
            }
        }
    }

    private void EncodeSampleLine(Span<Quad<ushort>> previousLine, Span<Quad<ushort>> currentLine)
    {
        int index = 1;
        while (index <= FrameInfo.Width)
        {
            var ra = currentLine[index - 1];
            var rc = previousLine[index - 1];
            var rb = previousLine[index];
            var rd = previousLine[index + 1];

            int qs1 = Algorithm.ComputeContextId(QuantizeGradient(rd.V1 - rb.V1), QuantizeGradient(rb.V1 - rc.V1),
                QuantizeGradient(rc.V1 - ra.V1));
            int qs2 = Algorithm.ComputeContextId(QuantizeGradient(rd.V2 - rb.V2), QuantizeGradient(rb.V2 - rc.V2),
                QuantizeGradient(rc.V2 - ra.V2));
            int qs3 = Algorithm.ComputeContextId(QuantizeGradient(rd.V3 - rb.V3), QuantizeGradient(rb.V3 - rc.V3),
                QuantizeGradient(rc.V3 - ra.V3));
            int qs4 = Algorithm.ComputeContextId(QuantizeGradient(rd.V4 - rb.V4), QuantizeGradient(rb.V4 - rc.V4),
                QuantizeGradient(rc.V4 - ra.V4));
            if (qs1 == 0 && qs2 == 0 && qs3 == 0 && qs4 == 0)
            {
                index += EncodeRunMode(index, previousLine, currentLine);
            }
            else
            {
                Quad<ushort> rx;
                rx.V1 = (ushort)encode_regular(qs1, currentLine[index].V1, Algorithm.ComputePredictedValue(ra.V1, rb.V1, rc.V1));
                rx.V2 = (ushort)encode_regular(qs2, currentLine[index].V2, Algorithm.ComputePredictedValue(ra.V2, rb.V2, rc.V2));
                rx.V3 = (ushort)encode_regular(qs3, currentLine[index].V3, Algorithm.ComputePredictedValue(ra.V3, rb.V3, rc.V3));
                rx.V4 = (ushort)encode_regular(qs3, currentLine[index].V4, Algorithm.ComputePredictedValue(ra.V4, rb.V4, rc.V4));
                currentLine[index] = rx;
                ++index;
            }
        }
    }

    private int encode_regular(int qs, int x, int predicted)
    {
        int sign = Algorithm.BitWiseSign(qs);
        ref var context = ref RegularModeContext[Algorithm.ApplySign(qs, sign)];
        int k = context.ComputeGolombCodingParameter();
        int predictedValue = _traits.CorrectPrediction(predicted + Algorithm.ApplySign(context.C, sign));
        int errorValue = _traits.ComputeErrorValue(Algorithm.ApplySign(x - predictedValue, sign));

        encode_mapped_value(k, Algorithm.MapErrorValue(context.GetErrorCorrection(k | _traits.NearLossless) ^ errorValue), _traits.Limit);
        context.UpdateVariablesAndBias(errorValue, _traits.NearLossless, _traits.ResetThreshold);

        Debug.Assert(_traits.IsNear(_traits.ComputeReconstructedSample(predictedValue, Algorithm.ApplySign(errorValue, sign)), x));
        return _traits.ComputeReconstructedSample(predictedValue, Algorithm.ApplySign(errorValue, sign));
    }

    private int EncodeRunMode(int startIndex, Span<byte> previousLine, Span<byte> currentLine)
    {
        int countTypeRemain = FrameInfo.Width - (startIndex - 1);
        var typePrevX = previousLine[startIndex..];
        var typeCurX = currentLine[startIndex..];
        var ra = currentLine[startIndex - 1];

        int runLength = 0;
        while (_traits.IsNear(typeCurX[runLength], ra))
        {
            typeCurX[runLength] = ra;
            ++runLength;

            if (runLength == countTypeRemain)
                break;
        }

        EncodeRunPixels(runLength, runLength == countTypeRemain);

        if (runLength == countTypeRemain)
            return runLength;

        typeCurX[runLength] = (byte)encode_run_interruption_pixel(typeCurX[runLength], ra, typePrevX[runLength]);
        DecrementRunIndex();
        return runLength + 1;
    }

    private int EncodeRunMode(int startIndex, Span<ushort> previousLine, Span<ushort> currentLine)
    {
        int countTypeRemain = FrameInfo.Width - (startIndex - 1);
        var typePrevX = previousLine[startIndex..];
        var typeCurX = currentLine[startIndex..];
        var ra = currentLine[startIndex - 1];

        int runLength = 0;
        while (_traits.IsNear(typeCurX[runLength], ra))
        {
            typeCurX[runLength] = ra;
            ++runLength;

            if (runLength == countTypeRemain)
                break;
        }

        EncodeRunPixels(runLength, runLength == countTypeRemain);

        if (runLength == countTypeRemain)
            return runLength;

        typeCurX[runLength] = (ushort)encode_run_interruption_pixel(typeCurX[runLength], ra, typePrevX[runLength]);
        DecrementRunIndex();
        return runLength + 1;
    }

    private int EncodeRunMode(int startIndex, Span<Triplet<byte>> previousLine, Span<Triplet<byte>> currentLine)
    {
        int countTypeRemain = FrameInfo.Width - (startIndex - 1);
        var typePrevX = previousLine[startIndex..];
        var typeCurX = currentLine[startIndex..];
        var ra = currentLine[startIndex - 1];

        int runLength = 0;
        while (_traits.IsNear(typeCurX[runLength], ra))
        {
            typeCurX[runLength] = ra;
            ++runLength;

            if (runLength == countTypeRemain)
                break;
        }

        EncodeRunPixels(runLength, runLength == countTypeRemain);

        if (runLength == countTypeRemain)
            return runLength;

        typeCurX[runLength] = encode_run_interruption_pixel(typeCurX[runLength], ra, typePrevX[runLength]);
        DecrementRunIndex();
        return runLength + 1;
    }

    private int EncodeRunMode(int startIndex, Span<Triplet<ushort>> previousLine, Span<Triplet<ushort>> currentLine)
    {
        int countTypeRemain = FrameInfo.Width - (startIndex - 1);
        var typePrevX = previousLine[startIndex..];
        var typeCurX = currentLine[startIndex..];
        var ra = currentLine[startIndex - 1];

        int runLength = 0;
        while (_traits.IsNear(typeCurX[runLength], ra))
        {
            typeCurX[runLength] = ra;
            ++runLength;

            if (runLength == countTypeRemain)
                break;
        }

        EncodeRunPixels(runLength, runLength == countTypeRemain);

        if (runLength == countTypeRemain)
            return runLength;

        typeCurX[runLength] = encode_run_interruption_pixel(typeCurX[runLength], ra, typePrevX[runLength]);
        DecrementRunIndex();
        return runLength + 1;
    }

    private int EncodeRunMode(int startIndex, Span<Quad<byte>> previousLine, Span<Quad<byte>> currentLine)
    {
        int countTypeRemain = FrameInfo.Width - (startIndex - 1);
        var typePrevX = previousLine[startIndex..];
        var typeCurX = currentLine[startIndex..];
        var ra = currentLine[startIndex - 1];

        int runLength = 0;
        while (_traits.IsNear(typeCurX[runLength], ra))
        {
            typeCurX[runLength] = ra;
            ++runLength;

            if (runLength == countTypeRemain)
                break;
        }

        EncodeRunPixels(runLength, runLength == countTypeRemain);

        if (runLength == countTypeRemain)
            return runLength;

        typeCurX[runLength] = encode_run_interruption_pixel(typeCurX[runLength], ra, typePrevX[runLength]);
        DecrementRunIndex();
        return runLength + 1;
    }

    private int EncodeRunMode(int startIndex, Span<Quad<ushort>> previousLine, Span<Quad<ushort>> currentLine)
    {
        int countTypeRemain = FrameInfo.Width - (startIndex - 1);
        var typePrevX = previousLine[startIndex..];
        var typeCurX = currentLine[startIndex..];
        var ra = currentLine[startIndex - 1];

        int runLength = 0;
        while (_traits.IsNear(typeCurX[runLength], ra))
        {
            typeCurX[runLength] = ra;
            ++runLength;

            if (runLength == countTypeRemain)
                break;
        }

        EncodeRunPixels(runLength, runLength == countTypeRemain);

        if (runLength == countTypeRemain)
            return runLength;

        typeCurX[runLength] = encode_run_interruption_pixel(typeCurX[runLength], ra, typePrevX[runLength]);
        DecrementRunIndex();
        return runLength + 1;
    }

    private int encode_run_interruption_pixel(int x, int ra, int rb)
    {
        if (Math.Abs(ra - rb) <= _traits.NearLossless)
        {
            int errorValue = _traits.ComputeErrorValue(x - ra);
            encode_run_interruption_error(ref RunModeContexts[1], errorValue);
            return _traits.ComputeReconstructedSample(ra, errorValue);
        }
        else
        {
            int errorValue = _traits.ComputeErrorValue((x - rb) * Algorithm.Sign(rb - ra));
            encode_run_interruption_error(ref RunModeContexts[0], errorValue);
            return _traits.ComputeReconstructedSample(rb, errorValue * Algorithm.Sign(rb - ra));
        }
    }

    private Triplet<byte> encode_run_interruption_pixel(Triplet<byte> x, Triplet<byte> ra, Triplet<byte> rb)
    {
        int errorValue1 = _traits.ComputeErrorValue(Algorithm.Sign(rb.V1 - ra.V1) * (x.V1 - rb.V1));
        encode_run_interruption_error(ref RunModeContexts[0], errorValue1);

        int errorValue2 = _traits.ComputeErrorValue(Algorithm.Sign(rb.V2 - ra.V2) * (x.V2 - rb.V2));
        encode_run_interruption_error(ref RunModeContexts[0], errorValue2);

        int errorValue3 = _traits.ComputeErrorValue(Algorithm.Sign(rb.V3 - ra.V3) * (x.V3 - rb.V3));
        encode_run_interruption_error(ref RunModeContexts[0], errorValue3);

        return new Triplet<byte>(
            (byte)_traits.ComputeReconstructedSample(rb.V1, errorValue1 * Algorithm.Sign(rb.V1 - ra.V1)),
            (byte)_traits.ComputeReconstructedSample(rb.V2, errorValue2 * Algorithm.Sign(rb.V2 - ra.V2)),
            (byte)_traits.ComputeReconstructedSample(rb.V3, errorValue3 * Algorithm.Sign(rb.V3 - ra.V3)));
    }

    private Triplet<ushort> encode_run_interruption_pixel(Triplet<ushort> x, Triplet<ushort> ra, Triplet<ushort> rb)
    {
        int errorValue1 = _traits.ComputeErrorValue(Algorithm.Sign(rb.V1 - ra.V1) * (x.V1 - rb.V1));
        encode_run_interruption_error(ref RunModeContexts[0], errorValue1);

        int errorValue2 = _traits.ComputeErrorValue(Algorithm.Sign(rb.V2 - ra.V2) * (x.V2 - rb.V2));
        encode_run_interruption_error(ref RunModeContexts[0], errorValue2);

        int errorValue3 = _traits.ComputeErrorValue(Algorithm.Sign(rb.V3 - ra.V3) * (x.V3 - rb.V3));
        encode_run_interruption_error(ref RunModeContexts[0], errorValue3);

        return new Triplet<ushort>(
            (ushort)_traits.ComputeReconstructedSample(rb.V1, errorValue1 * Algorithm.Sign(rb.V1 - ra.V1)),
            (ushort)_traits.ComputeReconstructedSample(rb.V2, errorValue2 * Algorithm.Sign(rb.V2 - ra.V2)),
            (ushort)_traits.ComputeReconstructedSample(rb.V3, errorValue3 * Algorithm.Sign(rb.V3 - ra.V3)));
    }

    private Quad<byte> encode_run_interruption_pixel(Quad<byte> x, Quad<byte> ra, Quad<byte> rb)
    {
        int errorValue1 = _traits.ComputeErrorValue(Algorithm.Sign(rb.V1 - ra.V1) * (x.V1 - rb.V1));
        encode_run_interruption_error(ref RunModeContexts[0], errorValue1);

        int errorValue2 = _traits.ComputeErrorValue(Algorithm.Sign(rb.V2 - ra.V2) * (x.V2 - rb.V2));
        encode_run_interruption_error(ref RunModeContexts[0], errorValue2);

        int errorValue3 = _traits.ComputeErrorValue(Algorithm.Sign(rb.V3 - ra.V3) * (x.V3 - rb.V3));
        encode_run_interruption_error(ref RunModeContexts[0], errorValue3);

        int errorValue4 = _traits.ComputeErrorValue(Algorithm.Sign(rb.V4 - ra.V4) * (x.V4 - rb.V4));
        encode_run_interruption_error(ref RunModeContexts[0], errorValue4);

        return new Quad<byte>(
            (byte)_traits.ComputeReconstructedSample(rb.V1, errorValue1 * Algorithm.Sign(rb.V1 - ra.V1)),
            (byte)_traits.ComputeReconstructedSample(rb.V2, errorValue2 * Algorithm.Sign(rb.V2 - ra.V2)),
            (byte)_traits.ComputeReconstructedSample(rb.V3, errorValue3 * Algorithm.Sign(rb.V3 - ra.V3)),
            (byte)_traits.ComputeReconstructedSample(rb.V4, errorValue4 * Algorithm.Sign(rb.V4 - ra.V4)));
    }

    private Quad<ushort> encode_run_interruption_pixel(Quad<ushort> x, Quad<ushort> ra, Quad<ushort> rb)
    {
        int errorValue1 = _traits.ComputeErrorValue(Algorithm.Sign(rb.V1 - ra.V1) * (x.V1 - rb.V1));
        encode_run_interruption_error(ref RunModeContexts[0], errorValue1);

        int errorValue2 = _traits.ComputeErrorValue(Algorithm.Sign(rb.V2 - ra.V2) * (x.V2 - rb.V2));
        encode_run_interruption_error(ref RunModeContexts[0], errorValue2);

        int errorValue3 = _traits.ComputeErrorValue(Algorithm.Sign(rb.V3 - ra.V3) * (x.V3 - rb.V3));
        encode_run_interruption_error(ref RunModeContexts[0], errorValue3);

        int errorValue4 = _traits.ComputeErrorValue(Algorithm.Sign(rb.V4 - ra.V4) * (x.V4 - rb.V4));
        encode_run_interruption_error(ref RunModeContexts[0], errorValue4);

        return new Quad<ushort>(
            (ushort)_traits.ComputeReconstructedSample(rb.V1, errorValue1 * Algorithm.Sign(rb.V1 - ra.V1)),
            (ushort)_traits.ComputeReconstructedSample(rb.V2, errorValue2 * Algorithm.Sign(rb.V2 - ra.V2)),
            (ushort)_traits.ComputeReconstructedSample(rb.V3, errorValue3 * Algorithm.Sign(rb.V3 - ra.V3)),
            (ushort)_traits.ComputeReconstructedSample(rb.V4, errorValue4 * Algorithm.Sign(rb.V4 - ra.V4)));
    }

    void encode_run_interruption_error(ref RunModeContext context, int errorValue)
    {
        int k = context.GetGolombCode();
        int map = context.ComputeMap(errorValue, k) ? 1 : 0;
        int eMappedErrorValue = 2 * Math.Abs(errorValue) - context.RunInterruptionType - map;
        Debug.Assert(errorValue == context.ComputeErrorValue(eMappedErrorValue + context.RunInterruptionType, k));
        encode_mapped_value(k, eMappedErrorValue, _traits.Limit - J[RunIndex] - 1);
        context.UpdateVariables(errorValue, eMappedErrorValue, (byte)PresetCodingParameters.ResetValue);
    }

    private void encode_mapped_value(int k, int mappedError, int limit)
    {
        int highBits = mappedError >> k;
        if (highBits < limit - _traits.QuantizedBitsPerSample - 1)
        {
            if (highBits + 1 > 31)
            {
                append_to_bit_stream(0, highBits / 2);
                highBits = highBits - highBits / 2;
            }
            append_to_bit_stream(1, highBits + 1);
            append_to_bit_stream((uint)(mappedError & ((1 << k) - 1)), k);
            return;
        }

        if (limit - _traits.QuantizedBitsPerSample > 31)
        {
            append_to_bit_stream(0, 31);
            append_to_bit_stream(1, limit - _traits.QuantizedBitsPerSample - 31);
        }
        else
        {
            append_to_bit_stream(1, limit - _traits.QuantizedBitsPerSample);
        }
        append_to_bit_stream((uint)((mappedError - 1) & ((1 << _traits.QuantizedBitsPerSample) - 1)), _traits.QuantizedBitsPerSample);
    }

    private int QuantizeGradient(int di)
    {
        Debug.Assert(QuantizeGradientOrg(di, _traits.NearLossless) == _quantizationLut[_quantizationLut.Length / 2 + di]);
        return _quantizationLut[_quantizationLut.Length / 2 + di];
    }

    // Factory function for ProcessLine objects to copy/transform un encoded pixels to/from our scan line buffers.
    private IProcessLineEncoded CreateProcessLine()
    {
        if (FrameInfo.BitsPerSample <= 8)
        {
            switch (CodingParameters.InterleaveMode)
            {
                case InterleaveMode.None:
                    return new ProcessEncodedSingleComponent();

                case InterleaveMode.Line:
                    switch (FrameInfo.ComponentCount)
                    {
                        case 3:
                            switch (CodingParameters.ColorTransformation)
                            {
                                case ColorTransformation.None:
                                    return new ProcessEncodedSingleComponentToLine8Bit3Components();
                                case ColorTransformation.HP1:
                                    return new ProcessEncodedSingleComponentToLine8Bit3ComponentsHP1();
                                case ColorTransformation.HP2:
                                    return new ProcessEncodedSingleComponentToLine8Bit3ComponentsHP2();
                                case ColorTransformation.HP3:
                                    return new ProcessEncodedSingleComponentToLine8Bit3ComponentsHP3();
                            }
                            break;
                        case 4:
                            return new ProcessEncodedSingleComponentToLine8Bit4Components();
                    }
                    break;

                case InterleaveMode.Sample:
                    switch (CodingParameters.ColorTransformation)
                    {
                        case ColorTransformation.None:
                            return new ProcessEncodedSingleComponent();
                        case ColorTransformation.HP1:
                            return new ProcessEncodedSingleComponent8BitHP1();
                        case ColorTransformation.HP2:
                            return new ProcessEncodedSingleComponent8BitHP2();
                        case ColorTransformation.HP3:
                            return new ProcessEncodedSingleComponent8BitHP3();
                    }
                    break;
            }
        }
        else
        {
            switch (CodingParameters.InterleaveMode)
            {
                case InterleaveMode.None:
                    return new ProcessEncodedSingleComponent();

                case InterleaveMode.Line:
                    switch (FrameInfo.ComponentCount)
                    {
                        case 3:
                            return new ProcessEncodedSingleComponentToLine16Bit3Components();
                        case 4:
                            return new ProcessEncodedSingleComponentToLine16Bit4Components();
                    }

                    break;

                case InterleaveMode.Sample:
                    switch (CodingParameters.ColorTransformation)
                    {
                        case ColorTransformation.None:
                            return new ProcessEncodedSingleComponent();
                        case ColorTransformation.HP1:
                            return new ProcessEncodedSingleComponent16BitHP1();
                        case ColorTransformation.HP2:
                            return new ProcessEncodedSingleComponent16BitHP2();
                        case ColorTransformation.HP3:
                            return new ProcessEncodedSingleComponent16BitHP3();
                    }
                    break;
            }
        }

        throw new NotImplementedException();
    }

    private int OnLineBegin(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
    {
        _processLine!.NewLineRequested(source, destination, pixelCount);
        return pixelCount;
    }

    private int OnLineBeginInterleaveModeLine(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
    {
        _processLine!.NewLineRequested(source, destination, pixelCount);
        return pixelCount * 3;
    }

    private int OnLineBeginInterleaveModeLine(ReadOnlySpan<byte> source, Span<ushort> destination, int pixelCount, int componentCount)
    {
        Span<byte> destinationInBytes = MemoryMarshal.Cast<ushort, byte>(destination);
        _processLine!.NewLineRequested(source, destinationInBytes, pixelCount);
        return pixelCount * componentCount;
    }

    private int OnLineBeginInterleaveModeSample(ReadOnlySpan<byte> source, Span<Triplet<byte>> destination, int pixelCount)
    {
        var destinationByte = MemoryMarshal.Cast<Triplet<byte>, byte>(destination);
        _processLine!.NewLineRequested(source, destinationByte, pixelCount * 3);
        return pixelCount * 3;
    }

    private int OnLineBeginInterleaveModeSample(ReadOnlySpan<byte> source, Span<Triplet<ushort>> destination, int pixelCount)
    {
        var destinationByte = MemoryMarshal.Cast<Triplet<ushort>, byte>(destination);
        _processLine!.NewLineRequested(source, destinationByte, pixelCount * 3 * 2);
        return pixelCount * 3 * 2;
    }

    private int OnLineBeginInterleaveModeSample(ReadOnlySpan<byte> source, Span<Quad<byte>> destination, int pixelCount)
    {
        var destinationByte = MemoryMarshal.Cast<Quad<byte>, byte>(destination);
        _processLine!.NewLineRequested(source, destinationByte, pixelCount * 4);
        return pixelCount * 4;
    }

    private int OnLineBeginInterleaveModeSample(ReadOnlySpan<byte> source, Span<Quad<ushort>> destination, int pixelCount)
    {
        var destinationByte = MemoryMarshal.Cast<Quad<ushort>, byte>(destination);
        _processLine!.NewLineRequested(source, destinationByte, pixelCount * 4 * 2);
        return pixelCount * 4 * 2;
    }

    private int OnLineBegin(ReadOnlySpan<byte> source, Span<ushort> destination, int pixelCount)
    {
        Span<byte> destinationInBytes = MemoryMarshal.Cast<ushort, byte>(destination);
        _processLine!.NewLineRequested(source, destinationInBytes, pixelCount * 2);
        return pixelCount * 2;
    }

}
