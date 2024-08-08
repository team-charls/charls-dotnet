// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.ComponentModel;
using System;

namespace CharLS.JpegLS;

internal class ScanEncoderImpl : ScanEncoder
{
    private readonly Traits _traits;
    private readonly sbyte[] _quantizationLut;

    internal ScanEncoderImpl(FrameInfo frameInfo, JpegLSPresetCodingParameters presetCodingParameters,
        CodingParameters codingParameters, Traits traits) :
        base(frameInfo, presetCodingParameters, codingParameters)
    {
        _traits = traits;

        _quantizationLut = InitializeQuantizationLut(traits, PresetCodingParameters.Threshold1,
            PresetCodingParameters.Threshold2, PresetCodingParameters.Threshold3);

        ////ResetParameters(_traits.Range);
    }

    public override int EncodeScan(ReadOnlyMemory<byte> source, Memory<byte> destination, int stride)
    {
        //process_line_ = create_process_line(source, stride);

        Initialize(destination);
        EncodeLines();
        //end_scan();

        //return get_length();
        return 0;
    }

    // In ILV_SAMPLE mode, multiple components are handled in do_line
    // In ILV_LINE mode, a call to do_line is made for every component
    // In ILV_NONE mode, do_scan is called for each component
    private void EncodeLines()
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

            ////on_line_begin(current_line_, width_, pixel_stride);

            for (int component = 0; component < componentCount; ++component)
            {
                RunIndex = runIndex[component];

                // initialize edge pixels used for prediction
                previousLine[FrameInfo.Width + 1] = previousLine[FrameInfo.Width];
                currentLine[0] = previousLine[1];

                //encode_sample_line();

                runIndex[component] = RunIndex;
                currentLine = currentLine[pixelStride..];
                previousLine = previousLine[pixelStride..];
            }
        }
    }
}
