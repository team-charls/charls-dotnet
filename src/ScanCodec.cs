// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

/// <summary>
/// Base class for ScanEncoder and ScanDecoder
/// Contains the variables and methods that are identical for the encoding/decoding process and can be shared.
/// </summary>
internal class ScanCodec
{
    protected int RunIndex;
    protected RunModeContext[] RunModeContexts = new RunModeContext[2];
    protected RegularModeContext[] RegularModeContext = new RegularModeContext[365];

    protected ScanCodec(FrameInfo frameInfo, JpegLSPresetCodingParameters presetCodingParameters, CodingParameters codingParameters)
    {
        FrameInfo = frameInfo;
        PresetCodingParameters = presetCodingParameters;
        CodingParameters = codingParameters;
    }

    protected FrameInfo FrameInfo { get; private set; }

    protected JpegLSPresetCodingParameters PresetCodingParameters { get; }

    protected CodingParameters CodingParameters { get; }

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
