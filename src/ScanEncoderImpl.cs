// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CharLS.JpegLS;

internal class ScanEncoderImpl : ScanEncoder
{
    private readonly Traits _traits;
    private readonly sbyte[] _quantizationLut;
    private ProcessEncodedSingleComponent? _processLine;

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
            EncodeLines8BitInterleaveModeNone(source);
        }
        else
        {
            switch (CodingParameters.InterleaveMode)
            {
                case InterleaveMode.None:
                    EncodeLines16BitInterleaveModeNone(source);
                    break;

                case InterleaveMode.Line:
                    //DecodeLinesByteLine(destination);
                    break;

                case InterleaveMode.Sample:
                    //DecodeLinesTripletByte(destination);
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
        int componentCount = CodingParameters.InterleaveMode == InterleaveMode.Line ? FrameInfo.ComponentCount : 1;

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
    private ProcessEncodedSingleComponent CreateProcessLine()
    {
        switch (CodingParameters.InterleaveMode)
        {
            case InterleaveMode.None:
                return new ProcessEncodedSingleComponent();

                //case InterleaveMode.Line:
                //new ProcessDecodedSingleComponentToLine(stride, 3);

                //case InterleaveMode.Sample:
                //    return new ProcessDecodedTripletComponent(stride, 3);
        }

        throw new NotImplementedException();

        //if (parameters().interleave_mode == interleave_mode::none)
        //{
        //    return std::make_unique<process_decoded_single_component>(destination, stride, sizeof(pixel_type));
        //}

        //switch (parameters().transformation)
        //{
        //    case color_transformation::none:
        //        return std::make_unique<process_decoded_transformed<transform_none<sample_type>>>(
        //            destination, stride, frame_info().component_count, parameters().interleave_mode);
        //    case color_transformation::hp1:
        //        ASSERT(color_transformation_possible(frame_info()));
        //        return std::make_unique<process_decoded_transformed<transform_hp1<sample_type>>>(
        //            destination, stride, frame_info().component_count, parameters().interleave_mode);
        //    case color_transformation::hp2:
        //        ASSERT(color_transformation_possible(frame_info()));
        //        return std::make_unique<process_decoded_transformed<transform_hp2<sample_type>>>(
        //            destination, stride, frame_info().component_count, parameters().interleave_mode);
        //    case color_transformation::hp3:
        //        ASSERT(color_transformation_possible(frame_info()));
        //        return std::make_unique<process_decoded_transformed<transform_hp3<sample_type>>>(
        //            destination, stride, frame_info().component_count, parameters().interleave_mode);
        //}

        //unreachable();
    }

    private int OnLineBegin(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
    {
        _processLine!.NewLineRequested(source, destination, pixelCount);
        return pixelCount;
    }

    private int OnLineBegin(ReadOnlySpan<byte> source, Span<ushort> destination, int pixelCount)
    {
        Span<byte> destinationInBytes = MemoryMarshal.Cast<ushort, byte>(destination);
        _processLine!.NewLineRequested(source, destinationInBytes, pixelCount * 2);
        return pixelCount * 2;
    }

}
