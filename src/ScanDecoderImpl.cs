// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Diagnostics;
using System.Numerics;

namespace CharLS.JpegLS;

internal class ScanDecoderImpl<TSample, TPixel> : ScanDecoder
    where TSample : struct, IBinaryInteger<TSample>
    where TPixel : struct
{
    private int _restartInterval;
    private readonly TraitsBase<TSample, TPixel> _traits;
    private readonly sbyte[] _quantizationLut;
    private IProcessLineDecoded? _processLineDecoded;

    internal ScanDecoderImpl(FrameInfo frameInfo, JpegLSPresetCodingParameters presetCodingParameters,
        CodingParameters codingParameters, TraitsBase<TSample, TPixel> traits) :
        base(frameInfo, presetCodingParameters, codingParameters)
    {
        _traits = traits;

        _quantizationLut = InitializeQuantizationLut<TSample, TPixel>(traits, PresetCodingParameters.Threshold1,
            PresetCodingParameters.Threshold2, PresetCodingParameters.Threshold3);

        ResetParameters(_traits.Range);
    }

    public override int DecodeScan(ReadOnlyMemory<byte> source, Span<byte> destination, int stride)
    {
        _processLineDecoded = CreateProcessLine(stride);

        //const auto* scan_begin{ to_address(source.begin())};

        Initialize(source);

        // Process images without a restart interval, as 1 large restart interval.
        if (CodingParameters.RestartInterval == 0)
        {
            _restartInterval = FrameInfo.Height;
        }

        switch (CodingParameters.InterleaveMode)
        {
            case JpegLSInterleaveMode.None:
                DecodeLinesByteNone(destination);
                break;

            case JpegLSInterleaveMode.Sample:
                DecodeLinesTripletByte(destination);
                break;
        }

        EndScan();

        return get_cur_byte_pos();
    }

    // Factory function for ProcessLine objects to copy/transform un encoded pixels to/from our scan line buffers.
    private IProcessLineDecoded CreateProcessLine(int stride)
    {
        switch (CodingParameters.InterleaveMode)
        {
            case JpegLSInterleaveMode.None:
                return new ProcessDecodedSingleComponent(stride, 1);

            case JpegLSInterleaveMode.Sample:
                return new ProcessDecodedTripletComponent(stride, 3);
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

    // In ILV_SAMPLE mode, multiple components are handled in do_line
    // In ILV_LINE mode, a call to do_line is made for every component
    // In ILV_NONE mode, do_scan is called for each component
    private void DecodeLinesByteNone(Span<byte> destination)
    {
        int pixelStride = FrameInfo.Width + 2;
        int componentCount = CodingParameters.InterleaveMode == JpegLSInterleaveMode.Line ? FrameInfo.ComponentCount : 1;
        int restartIntervalCounter = 0;

        Span<int> runIndex = stackalloc int[componentCount];
        Span<byte> lineBuffer = new byte[componentCount * pixelStride * 2];

        for (int line = 0; ;)
        {
            int linesInInterval = Math.Min(FrameInfo.Height - line, _restartInterval);

            for (int mcu = 0; mcu < linesInInterval; ++mcu, ++line)
            {
                var previousLine = lineBuffer;
                var currentLine = lineBuffer[(componentCount * pixelStride)..];
                if ((line & 1) == 1)
                {
                    var temp = previousLine;
                    previousLine = currentLine;
                    currentLine = temp;
                }

                for (int component = 0; ;)
                {
                    RunIndex = runIndex[component];

                    // initialize edge pixels used for prediction
                    previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
                    currentLine[0] = previousLine[1];

                    DecodeSampleLine(previousLine, currentLine);

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

    private void DecodeLinesTripletByte(Span<byte> destination)
    {
        int pixelStride = FrameInfo.Width + 2;
        int componentCount = CodingParameters.InterleaveMode == JpegLSInterleaveMode.Line ? FrameInfo.ComponentCount : 1;
        int restartIntervalCounter = 0;

        Span<int> runIndex = stackalloc int[componentCount];
        Span<Triplet<byte>> lineBuffer = new Triplet<byte>[componentCount * pixelStride * 2];

        for (int line = 0; ;)
        {
            int linesInInterval = Math.Min(FrameInfo.Height - line, _restartInterval);

            for (int mcu = 0; mcu < linesInInterval; ++mcu, ++line)
            {
                var previousLine = lineBuffer;
                var currentLine = lineBuffer[(componentCount * pixelStride)..];
                if ((line & 1) == 1)
                {
                    var temp = previousLine;
                    previousLine = currentLine;
                    currentLine = temp;
                }

                for (int component = 0; ;)
                {
                    RunIndex = runIndex[component];

                    // initialize edge pixels used for prediction
                    previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
                    currentLine[0] = previousLine[1];

                    DecodeTripletLine(previousLine, currentLine);

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

            int qs = Algorithm.ComputeContextId(QuantizeGradient(rd - rb),
                QuantizeGradient(rb - rc), QuantizeGradient(rc - ra));
            if (qs != 0)
            {
                currentLine[index] = DecodeRegular(qs, Algorithm.GetPredictedValue(ra, rb, rc));
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

            int qs1 =
                Algorithm.ComputeContextId(QuantizeGradient(rd.V1 - rb.V1),
                    QuantizeGradient(rb.V1 - rc.V1),
                        QuantizeGradient(rc.V1 - ra.V1));
            int qs2 =
                Algorithm.ComputeContextId(QuantizeGradient(rd.V2 - rb.V2),
                    QuantizeGradient(rb.V2 - rc.V2),
                    QuantizeGradient(rc.V2 - ra.V2));

            int qs3 =
                Algorithm.ComputeContextId(QuantizeGradient(rd.V3 - rb.V3),
                    QuantizeGradient(rb.V3 - rc.V3),
                    QuantizeGradient(rc.V3 - ra.V3));
            if (qs1 == 0 && qs2 == 0 && qs3 == 0)
            {
                index += DecodeRunMode(index, previousLine, currentLine);
            }
            else
            {
                Triplet<byte> rx;
                rx.V1 = DecodeRegular(qs1, Algorithm.GetPredictedValue(ra.V1, rb.V1, rc.V1));
                rx.V2 = DecodeRegular(qs2, Algorithm.GetPredictedValue(ra.V2, rb.V2, rc.V2));
                rx.V3 = DecodeRegular(qs3, Algorithm.GetPredictedValue(ra.V3, rb.V3, rc.V3));
                currentLine[index] = rx;
                ++index;
            }
        }
    }

    private byte DecodeRegular(int qs, int predicted)
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
        return (byte)_traits.ComputeReconstructedSample(predictedValue, errorValue);
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
        DecrementRunIndex();
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
        DecrementRunIndex();
        return endIndex - startIndex + 1;
    }

    private int DecodeRunPixels(byte ra, Span<byte> startPos, int pixelCount)
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

    private int DecodeRunPixels(Triplet<byte> ra, Span<Triplet<byte>> startPos, int pixelCount)
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


    private int DecodeRunInterruptionPixel(int ra, int rb)
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

    private Triplet<byte> DecodeRunInterruptionPixel(Triplet<byte> ra, Triplet<byte> rb)
    {
        int errorValue1 = DecodeRunInterruptionError(ref RunModeContexts[0]);
        int errorValue2 = DecodeRunInterruptionError(ref RunModeContexts[0]);
        int errorValue3 = DecodeRunInterruptionError(ref RunModeContexts[0]);

        return new Triplet<byte>(
            (byte)_traits.ComputeReconstructedSample(rb.V1, errorValue1 * Algorithm.Sign(rb.V1 - ra.V1)),
            (byte)_traits.ComputeReconstructedSample(rb.V2, errorValue2 * Algorithm.Sign(rb.V2 - ra.V2)),
            (byte)_traits.ComputeReconstructedSample(rb.V3, errorValue3 * Algorithm.Sign(rb.V3 - ra.V3)));
    }

    private int DecodeRunInterruptionError(ref RunModeContext context)
    {
        int k = context.GetGolombCode();
        int eMappedErrorValue = DecodeValue(k, _traits.Limit - J[RunIndex] - 1, _traits.qbpp);
        int errorValue = context.ComputeErrorValue(eMappedErrorValue + context.RunInterruptionType, k);
        context.UpdateVariables(errorValue, eMappedErrorValue, (byte)PresetCodingParameters.ResetValue);
        return errorValue;
    }

    private int on_line_end(Span<byte> source, Span<byte> destination, int pixelCount, int pixelStride)
    {
        //Span<byte> sourceInBytes = MemoryMarshal.Cast<TPixel, byte>(source);

        return _processLineDecoded!.LineDecoded(source, destination, pixelCount, pixelStride);
    }

    private int on_line_end(Span<Triplet<byte>> source, Span<byte> destination, int pixelCount, int pixelStride)
    {
        //Span<byte> sourceInBytes = MemoryMarshal.Cast<TPixel, byte>(source);

        return _processLineDecoded!.LineDecoded(source, destination, pixelCount, pixelStride);
    }

}
