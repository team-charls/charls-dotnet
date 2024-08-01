// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System;
using System.Diagnostics;
using System.Globalization;

namespace CharLS.JpegLS;

internal class ScanDecoderImpl<TSample, TPixel> : ScanDecoder
    where TSample : struct
    where TPixel : struct
{
    private int _restartInterval;
    private readonly TraitsBase<TSample, TPixel> _traits;
    private sbyte[] _quantization;
    private IProcessLineDecoded? _processLineDecoded;

    internal ScanDecoderImpl(FrameInfo frameInfo, JpegLSPresetCodingParameters presetCodingParameters,
        CodingParameters codingParameters, TraitsBase<TSample, TPixel> traits) :
        base(frameInfo, presetCodingParameters, codingParameters)
    {
        _traits = traits;

        _quantization = InitializeQuantizationLut<TSample, TPixel>(traits, PresetCodingParameters.Threshold1,
            PresetCodingParameters.Threshold2, PresetCodingParameters.Threshold3);

        ResetParameters(_traits.Range);
    }

    public override uint DecodeScan(ReadOnlySpan<byte> source, Span<byte> destination, int stride)
    {
        _processLineDecoded = CreateProcessLine(stride);

        //const auto* scan_begin{ to_address(source.begin())};

        Initialize(source);

        // Process images without a restart interval, as 1 large restart interval.
        if (CodingParameters.RestartInterval == 0)
        {
            _restartInterval = FrameInfo.Height;
        }

        DecodeLines(source);
        EndScan(source);

        //return get_cur_byte_pos() - scan_begin;
        return 0;
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
    private void DecodeLines(ReadOnlySpan<byte> source)
    {
        int pixelStride = FrameInfo.Width + 4;
        int componentCount = CodingParameters.InterleaveMode == JpegLSInterleaveMode.Line ? FrameInfo.ComponentCount : 1;
        int restartIntervalCounter = 0;

        Span<TPixel> lineBuffer = new TPixel[componentCount * pixelStride * 2];
        Span<int> runIndex = stackalloc int[componentCount];

        for (int line = 0;;)
        {
            int linesInInterval = Math.Min(FrameInfo.Height - line, CodingParameters.RestartInterval);

            for (int mcu = 0; mcu < linesInInterval; ++mcu, ++line)
            {
                Span<TPixel> previousLine = lineBuffer[1..];
                Span<TPixel> currentLine = lineBuffer[(1 + componentCount * pixelStride)..];
                if ((line & 1) == 1)
                {
                    Span<TPixel> temp = previousLine;
                    previousLine = currentLine;
                    currentLine = temp;
                }

                for (int component = 0; component < componentCount; ++component)
                {
                    RunIndex = runIndex[component];

                    // initialize edge pixels used for prediction
                    previousLine[FrameInfo.Width] = previousLine[FrameInfo.Width - 1];
                    currentLine[-1] = previousLine[0];

                    DecodeSampleLine(source, previousLine, currentLine);
                    //if constexpr(std::is_same_v<pixel_type, sample_type>)
                    //{
                    //    decode_sample_line();
                    //}
                    //else if constexpr(std::is_same_v<pixel_type, triplet<sample_type>>)
                    //{
                    //    decode_triplet_line();
                    //}
                    //else
                    //{
                    //    static_assert(std::is_same_v<pixel_type, quad<sample_type>>);
                    //    decode_quad_line();
                    //}

                    runIndex[component] = RunIndex;
                    //previousLine += pixel_stride;
                    //current_line_ += pixel_stride;
                }

                //on_line_end(current_line_ - (component_count * pixel_stride), frame_info().width, pixelStride);
            }

            if (line == FrameInfo.Height)
                break;

            // At this point in the byte stream a restart marker should be present: process it.
            ReadRestartMarker(source, restartIntervalCounter);
            restartIntervalCounter = (restartIntervalCounter + 1) % Constants.JpegRestartMarkerRange;

            // After a restart marker it is required to reset the decoder.
            Reset(source);
            lineBuffer.Clear();
            runIndex.Clear();
            ResetParameters(_traits.Range);
        }
    }

    private int QuantizeGradient(int di)
    {
        //ASSERT(quantize_gradient_org(di, traits_.near_lossless) == *(quantization_ + di));
        return _quantization[di];
    }

    private void DecodeSampleLine(ReadOnlySpan<byte> source, Span<TPixel> previousLine, Span<TPixel> currentLine)
    {
        int index = 0;
        int rb = ToInt32(previousLine[index - 1]);
        int rd = ToInt32(previousLine[index]);

        while (index < FrameInfo.Width)
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
                    Convert.ChangeType(DecodeRegular(source, qs, Algorithm.GetPredictedValue(ra, rb, rc)),
                    typeof(TPixel), CultureInfo.InvariantCulture);
                ++index;
            }
            else
            {
                //index += decode_run_mode(index);
                //rb = previous_line_[index - 1];
                //rd = previous_line_[index];
            }
        }
    }

    private TSample DecodeRegular(ReadOnlySpan<byte> source, int qs, int predicted)
    {
        int sign = Algorithm.BitWiseSign(qs);
        var context = RegularModeContext[Algorithm.ApplySign(qs, sign)];
        int k = context.GetGolombCodingParameter();
        int predictedValue = _traits.CorrectPrediction(predicted + Algorithm.ApplySign(context.C, sign));

        int errorValue;
        var code = ColombCodeTable[k].Get(PeekByte(source));
        if (code.Length != 0)
        {
            SkipBits(code.Length);
            errorValue = code.Value;
            //ASSERT(std::abs(error_value) < 65535);
        }
        else
        {
            errorValue = Algorithm.UnmapErrorValue(DecodeValue(source, k, _traits.Limit, _traits.qbpp));
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

    private int decode_run_mode(ReadOnlySpan<byte> source, int start_index, Span<TPixel> previousLine, Span<TPixel> currentLine)
    {
        var ra = currentLine[start_index - 1];

        int run_length = decode_run_pixels(source, ra, currentLine.Slice(start_index), FrameInfo.Width - start_index);
        int end_index = start_index + run_length;

        if (end_index == FrameInfo.Width)
            return end_index - start_index;

        // run interruption
        var rb = ToInt32(previousLine[end_index]);
        currentLine[end_index] = (TPixel)
            Convert.ChangeType(
            decode_run_interruption_pixel(ToInt32(ra), rb),
            typeof(TPixel), CultureInfo.InvariantCulture);

        DecrementRunIndex();
        return end_index - start_index + 1;
    }

    private int decode_run_pixels(ReadOnlySpan<byte> source, TPixel ra, Span<TPixel> startPos, int pixelCount)
    {
        int index = 0;
        while (ReadBit(source))
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
            index += (J[RunIndex] > 0) ? ReadValue(source, J[RunIndex]) : 0;
        }

        if (index > pixelCount)
            throw Util.CreateInvalidDataException(JpegLSError.InvalidEncodedData);

        for (int i = 0; i < index; ++i)
        {
            startPos[i] = ra;
        }

        return index;
    }

    TSample decode_run_interruption_pixel(int ra, int rb)
    {
        if (Math.Abs(ra - rb) <= _traits.NearLossless)
        {
            int error_value = decode_run_interruption_error(RunModeContexts[1]);
            return _traits.ComputeReconstructedSample(ra, error_value);
        }
        else
        {
            int error_value = decode_run_interruption_error(RunModeContexts[0]);
            return _traits.ComputeReconstructedSample(rb, error_value * Algorithm.Sign(rb - ra));
        }
    }

    private int decode_run_interruption_error(RunModeContext context)
    {
        //int k = context.get_golomb_code();
        //int e_mapped_error_value =
        //    decode_value(k, traits_.limit - J[run_index_] - 1, traits_.quantized_bits_per_pixel);
        //int error_value = context.compute_error_value(e_mapped_error_value + context.run_interruption_type(), k);
        //context.update_variables(error_value, e_mapped_error_value, reset_value_);
        //return error_value;
        return default;
    }

    private int ToInt32(TPixel value)
    {
        return value switch
        {
            byte b => b,
            short s => s,
            _ => default
        };

        //Debug.Fail("Unreachable");
    }

}
