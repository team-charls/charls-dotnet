// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Runtime.CompilerServices;

namespace CharLS.Managed;

/// <summary>
/// Base class for ScanEncoder and ScanDecoder
/// Contains the variables and methods that are identical for the encoding/decoding process and can be shared.
/// </summary>
internal class ScanCodec
{
    protected int RunIndex;

    protected RunModeContextArray RunModeContexts;

    protected RegularModeContextArray RegularModeContext;

    // Used to determine how large runs should be encoded at a time.
    // Defined by the JPEG-LS standard, A.2.1., Initialization step 3.
    protected static readonly int[] J =
        [0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 9, 10, 11, 12, 13, 14, 15];

    [InlineArray(2)]
    protected struct RunModeContextArray
    {
        private RunModeContext _element;
    }

    [InlineArray(365)]
    protected struct RegularModeContextArray
    {
        private RegularModeContext _element;
    }

    protected ScanCodec(FrameInfo frameInfo, JpegLSPresetCodingParameters presetCodingParameters, CodingParameters codingParameters)
    {
        FrameInfo = frameInfo;
        PresetCodingParameters = presetCodingParameters;
        CodingParameters = codingParameters;
    }

    protected FrameInfo FrameInfo { get; private set; }

    protected JpegLSPresetCodingParameters PresetCodingParameters { get; }

    protected CodingParameters CodingParameters { get; }

    protected void InitializeParameters(int range)
    {
        var regularModeContext = new RegularModeContext(range);
        for (int i = 0; i < 365; i++)
        {
            RegularModeContext[i] = regularModeContext;
        }

        RunModeContexts[0] = new RunModeContext(0, range);
        RunModeContexts[1] = new RunModeContext(1, range);
        RunIndex = 0;
    }

    protected sbyte QuantizeGradientOrg(int di, int nearLossless)
    {
        return Algorithm.QuantizeGradientOrg(di,
            PresetCodingParameters.Threshold1, PresetCodingParameters.Threshold2, PresetCodingParameters.Threshold3,
            nearLossless);
    }

    protected bool IsInterleaved()
    {
        //ASSERT((parameters().interleave_mode == interleave_mode::none && frame_info().component_count == 1) ||
        //       parameters().interleave_mode != interleave_mode::none);

        return CodingParameters.InterleaveMode != InterleaveMode.None;
    }

    protected void IncrementRunIndex()
    {
        RunIndex = Math.Min(31, RunIndex + 1);
    }

    protected void DecrementRunIndex()
    {
        RunIndex = Math.Max(0, RunIndex - 1);
    }

    protected sbyte[] InitializeQuantizationLut(Traits traits, int threshold1, int threshold2, int threshold3)
    {
        //// For lossless mode with default parameters, we have precomputed the lookup table for bit counts 8, 10, 12 and 16.
        //if (precomputed_quantization_lut_available(traits, threshold1, threshold2, threshold3))
        //{
        //    if constexpr(Traits::fixed_bits_per_pixel)
        //{
        //    if constexpr(Traits::bits_per_pixel == 8)
        //            return &quantization_lut_lossless_8[quantization_lut_lossless_8.size() / 2];
        //    else
        //    {
        //        if constexpr(Traits::bits_per_pixel == 12)
        //                return &quantization_lut_lossless_12[quantization_lut_lossless_12.size() / 2];
        //        else
        //        {
        //            static_assert(Traits::bits_per_pixel == 16);
        //            return &quantization_lut_lossless_16[quantization_lut_lossless_16.size() / 2];
        //        }
        //    }
        //}
        //    else
        //    {
        //        switch (traits.bits_per_pixel)
        //        {
        //        case 8:
        //            return &quantization_lut_lossless_8[quantization_lut_lossless_8.size() / 2];
        //        case 10:
        //            return &quantization_lut_lossless_10[quantization_lut_lossless_10.size() / 2];
        //        case 12:
        //            return &quantization_lut_lossless_12[quantization_lut_lossless_12.size() / 2];
        //        case 16:
        //            return &quantization_lut_lossless_16[quantization_lut_lossless_16.size() / 2];
        //        default:
        //            break;
        //        }
        //    }
        //}

        // Initialize the quantization lookup table dynamic.
        var quantizationLut = new sbyte[traits.QuantizationRange * 2];
        for (int i = 0; i < quantizationLut.Length; ++i)
        {
            quantizationLut[i] = Algorithm.QuantizeGradientOrg(-traits.QuantizationRange + i, threshold1, threshold2,
                threshold3, traits.NearLossless);
        }

        return quantizationLut;
    }

}
