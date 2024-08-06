// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;

namespace CharLS.JpegLS;

internal class ScanDecoderImpl<TSample, TPixel> : ScanDecoder
    where TSample : struct, IBinaryInteger<TSample>
    where TPixel : struct
{
    private int _restartInterval;
    private readonly TraitsBase<TSample, TPixel> _traits;
    private readonly sbyte[] _quantizationLut;
    private ProcessDecodedSingleComponent? _processLineDecoded;

    internal ScanDecoderImpl(FrameInfo frameInfo, JpegLSPresetCodingParameters presetCodingParameters,
        CodingParameters codingParameters, TraitsBase<TSample, TPixel> traits) :
        base(frameInfo, presetCodingParameters, codingParameters)
    {
        _traits = traits;

        _quantizationLut = InitializeQuantizationLut<TSample, TPixel>(traits, PresetCodingParameters.Threshold1,
            PresetCodingParameters.Threshold2, PresetCodingParameters.Threshold3);

        ResetParameters(_traits.Range);
    }

    public override unsafe int DecodeScan(ReadOnlySpan<byte> source, Span<byte> destination, int stride)
    {
        _processLineDecoded = CreateProcessLine(stride);

        //const auto* scan_begin{ to_address(source.begin())};

        fixed (byte* scan_begin = source)
        {
            Initialize(scan_begin, source.Length);

            // Process images without a restart interval, as 1 large restart interval.
            if (CodingParameters.RestartInterval == 0)
            {
                _restartInterval = FrameInfo.Height;
            }

            DecodeLines(destination);
            EndScan();

            return (int)(get_cur_byte_pos() - scan_begin);
        }
    }

    // Factory function for ProcessLine objects to copy/transform un encoded pixels to/from our scan line buffers.
    private static ProcessDecodedSingleComponent CreateProcessLine(int stride)
    {
        return new ProcessDecodedSingleComponent(stride, 1);

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

    // In ILV_SAMPLE mode, multiple components are handled in do_line
    // In ILV_LINE mode, a call to do_line is made for every component
    // In ILV_NONE mode, do_scan is called for each component
    private void DecodeLines(Span<byte> destination)
    {
        int pixelStride = FrameInfo.Width + 2;
        int componentCount = CodingParameters.InterleaveMode == JpegLSInterleaveMode.Line ? FrameInfo.ComponentCount : 1;
        int restartIntervalCounter = 0;

        Span<int> runIndex = stackalloc int[componentCount];
        Span<TPixel> lineBuffer = new TPixel[componentCount * pixelStride * 2];

        for (int line = 0; ;)
        {
            int linesInInterval = Math.Min(FrameInfo.Height - line, _restartInterval);

            for (int mcu = 0; mcu < linesInInterval; ++mcu, ++line)
            {
                Span<TPixel> previousLine = lineBuffer;
                Span<TPixel> currentLine = lineBuffer[(componentCount * pixelStride)..];
                if ((line & 1) == 1)
                {
                    Span<TPixel> temp = previousLine;
                    previousLine = currentLine;
                    currentLine = temp;
                }

                for (int component = 0; ;)
                {
                    RunIndex = runIndex[component];

                    // initialize edge pixels used for prediction
                    previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
                    currentLine[0] = previousLine[1];

                    if (typeof(TPixel) == typeof(TSample))
                    {
                        DecodeSampleLine(previousLine, currentLine);
                    }
                    else if (typeof(TPixel) == typeof(Triplet<TSample>))
                    {
                        DecodeTripletLine(MemoryMarshal.Cast<TPixel, Triplet<TSample>>(previousLine),
                            MemoryMarshal.Cast<TPixel, Triplet<TSample>>(currentLine));
                    }

                    runIndex[component] = RunIndex;

                    ++component;
                    if (component == componentCount)
                        break;

                    previousLine = previousLine[pixelStride..];
                    currentLine = currentLine[pixelStride..];
                }

                int bytesWritten = on_line_end(currentLine[1..], destination, FrameInfo.Width, pixelStride);
                destination = destination[bytesWritten..];
            }

            if (line == FrameInfo.Height)
                break;

            // At this point in the byte stream a restart marker should be present: process it.
            ReadRestartMarker(restartIntervalCounter);
            restartIntervalCounter = (restartIntervalCounter + 1) % Constants.JpegRestartMarkerRange;

            // After a restart marker it is required to reset the decoder.
            Reset();
            lineBuffer.Clear();
            runIndex.Clear();
            ResetParameters(_traits.Range);
        }
    }

    private int QuantizeGradient(int di)
    {
        Debug.Assert(QuantizeGradientOrg(di, _traits.NearLossless) == _quantizationLut[_quantizationLut.Length / 2 + di]);
        return _quantizationLut[_quantizationLut.Length / 2 + di];
    }

    private void DecodeSampleLine(Span<TPixel> previousLine, Span<TPixel> currentLine)
    {
        int index = 1;
        int rb = ToInt32(previousLine[0]);
        int rd = ToInt32(previousLine[index]);

        while (index <= FrameInfo.Width)
        {
            int ra = ToInt32(currentLine[index - 1]);
            int rc = rb;
            rb = rd;
            rd = ToInt32(previousLine[index + 1]);

            int qs = Algorithm.ComputeContextId(QuantizeGradient(rd - rb),
                QuantizeGradient(rb - rc), QuantizeGradient(rc - ra));
            if (qs != 0)
            {
                currentLine[index] =
                    (TPixel)
                    Convert.ChangeType(DecodeRegular(qs, Algorithm.GetPredictedValue(ra, rb, rc)),
                    typeof(TPixel), CultureInfo.InvariantCulture);
                ++index;
            }
            else
            {
                index += DecodeRunMode(index, previousLine, currentLine);
                rb = ToInt32(previousLine[index - 1]);
                rd = ToInt32(previousLine[index]);
            }
        }
    }

    private void DecodeTripletLine(Span<Triplet<TSample>> previousLine, Span<Triplet<TSample>> currentLine)
    {
        int index = 1;
        while (index <= FrameInfo.Width)
        {
            var ra = currentLine[index - 1];
            var rc = previousLine[index - 1];
            var rb = previousLine[index];
            var rd = previousLine[index + 1];

            int qs1 =
                Algorithm.ComputeContextId(QuantizeGradient(int.CreateTruncating(rd.V1 - rb.V1)),
                    QuantizeGradient(int.CreateTruncating(rb.V1 - rc.V1)),
                        QuantizeGradient(int.CreateTruncating(rc.V1 - ra.V1)));
            int qs2 =
                Algorithm.ComputeContextId(QuantizeGradient(int.CreateTruncating(rd.V2 - rb.V2)),
                    QuantizeGradient(int.CreateTruncating(rb.V2 - rc.V2)),
                    QuantizeGradient(int.CreateTruncating(rc.V2 - ra.V2)));

            int qs3 =
                Algorithm.ComputeContextId(QuantizeGradient(int.CreateTruncating(rd.V3 - rb.V3)),
                    QuantizeGradient(int.CreateTruncating(rb.V3 - rc.V3)),
                    QuantizeGradient(int.CreateTruncating(rc.V3 - ra.V3)));
            if (qs1 == 0 && qs2 == 0 && qs3 == 0)
            {
                index += DecodeRunMode(index, MemoryMarshal.Cast<Triplet<TSample>, TPixel>(previousLine),
                    MemoryMarshal.Cast<Triplet<TSample>, TPixel>(currentLine));
            }
            else
            {
                Triplet<TSample> rx;
                rx.V1 = DecodeRegular(qs1, Algorithm.GetPredictedValue(ToInt32(ra.V1), ToInt32(rb.V1), ToInt32(rc.V1)));
                rx.V2 = DecodeRegular(qs2, Algorithm.GetPredictedValue(ToInt32(ra.V2), ToInt32(rb.V2), ToInt32(rc.V2)));
                rx.V3 = DecodeRegular(qs3, Algorithm.GetPredictedValue(ToInt32(ra.V3), ToInt32(rb.V3), ToInt32(rc.V3)));
                currentLine[index] = rx;
                ++index;
            }
        }
    }

    private TSample DecodeRegular(int qs, int predicted)
    {
        int sign = Algorithm.BitWiseSign(qs);
        ref var context = ref RegularModeContext[Algorithm.ApplySign(qs, sign)];
        int k = context.GetGolombCodingParameter();
        int predictedValue = _traits.CorrectPrediction(predicted + Algorithm.ApplySign(context.C, sign));

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
            errorValue = Algorithm.UnmapErrorValue(DecodeValue(k, _traits.Limit, _traits.qbpp));
            if (Math.Abs(errorValue) > 65535)
                throw Util.CreateInvalidDataException(JpegLSError.InvalidEncodedData);
        }

        if (k == 0)
        {
            errorValue = errorValue ^ context.GetErrorCorrection(_traits.NearLossless);
        }

        context.update_variables_and_bias(errorValue, _traits.NearLossless, _traits.RESET);
        errorValue = Algorithm.ApplySign(errorValue, sign);
        return _traits.ComputeReconstructedSample(predictedValue, errorValue);
    }

    private int DecodeRunMode(int startIndex, Span<TPixel> previousLine, Span<TPixel> currentLine)
    {
        var ra = currentLine[startIndex - 1];

        int runLength = DecodeRunPixels(ra, currentLine[startIndex..], FrameInfo.Width - (startIndex - 1));
        int endIndex = startIndex + runLength;

        if (endIndex - 1 == FrameInfo.Width)
            return endIndex - startIndex;

        // run interruption
        var rb = ToInt32(previousLine[endIndex]);
        currentLine[endIndex] = (TPixel)
            Convert.ChangeType(
            DecodeRunInterruptionPixel(ToInt32(ra), rb),
            typeof(TPixel), CultureInfo.InvariantCulture);

        DecrementRunIndex();
        return endIndex - startIndex + 1;
    }

    private int DecodeRunPixels(TPixel ra, Span<TPixel> startPos, int pixelCount)
    {
        int index = 0;
        while (ReadBit())
        {
            int count = Math.Min(1 << J[RunIndex], pixelCount - index);
            index += count;
            ////ASSERT(index <= pixel_count);

            if (count == (1 << J[RunIndex]))
            {
                IncrementRunIndex();
            }

            if (index == pixelCount)
                break;
        }

        if (index != pixelCount)
        {
            // incomplete run.
            index += (J[RunIndex] > 0) ? ReadValue(J[RunIndex]) : 0;
        }

        if (index > pixelCount)
            throw Util.CreateInvalidDataException(JpegLSError.InvalidEncodedData);

        for (int i = 0; i < index; ++i)
        {
            startPos[i] = ra;
        }

        return index;
    }

    private TSample DecodeRunInterruptionPixel(int ra, int rb)
    {
        if (Math.Abs(ra - rb) <= _traits.NearLossless)
        {
            int errorValue = DecodeRunInterruptionError(ref RunModeContexts[1]);
            return _traits.ComputeReconstructedSample(ra, errorValue);
        }
        else
        {
            int errorValue = DecodeRunInterruptionError(ref RunModeContexts[0]);
            return _traits.ComputeReconstructedSample(rb, errorValue * Algorithm.Sign(rb - ra));
        }
    }

    private int DecodeRunInterruptionError(ref RunModeContext context)
    {
        int k = context.GetGolombCode();
        int eMappedErrorValue = DecodeValue(k, _traits.Limit - J[RunIndex] - 1, _traits.qbpp);
        int errorValue = context.ComputeErrorValue(eMappedErrorValue + context.RunInterruptionType, k);
        context.UpdateVariables(errorValue, eMappedErrorValue, (byte)PresetCodingParameters.ResetValue);
        return errorValue;
    }

    private static int ToInt32(TPixel value)
    {
        // TODO assert that TPixel is byte or ushort

        return value switch
        {
            byte b => b,
            short s => s,
            _ => default
        };
    }

    private static int ToInt32(TSample value)
    {
        // TODO assert that TPixel is byte or ushort

        return value switch
        {
            byte b => b,
            short s => s,
            _ => default
        };
    }

    private int on_line_end(Span<TPixel> source, Span<byte> destination, int pixelCount, int pixelStride)
    {
        Span<byte> sourceInBytes = MemoryMarshal.Cast<TPixel, byte>(source);

        return _processLineDecoded!.LineDecoded(sourceInBytes, destination, pixelCount, pixelStride);
    }
}
