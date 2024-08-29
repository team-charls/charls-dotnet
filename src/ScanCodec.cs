// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Runtime.CompilerServices;

using static CharLS.Managed.Algorithm;

namespace CharLS.Managed;

/// <summary>
/// Base class for ScanEncoder and ScanDecoder
/// Contains the variables and methods that are identical for the encoding/decoding process and can be shared.
/// </summary>
internal struct ScanCodec
{
    internal Traits Traits;
    internal sbyte[] QuantizationLut;

    internal int RunIndex;

    /// <summary>
    /// ISO 14495-1 RESET symbol: threshold value at which A, B, and N are halved.
    /// </summary>
    internal int ResetThreshold;

    /// <summary>
    /// ISO 14495-1 NEAR symbol: difference bound for near-lossless coding, 0 means lossless.
    /// </summary>
    internal int NearLossless;

    /// <summary>
    /// ISO 14495-1 LIMIT symbol: the value of glimit for a sample encoded in regular mode.
    /// </summary>
    internal int Limit;

    /// <summary>
    /// ISO 14495-1 qbpp symbol: number of bits needed to represent a mapped error value.
    /// </summary>
    internal int QuantizedBitsPerSample;

    internal RunModeContextArray RunModeContexts;

    internal RegularModeContextArray RegularModeContext;

    // Used to determine how large runs should be encoded at a time.
    // Defined by the JPEG-LS standard, A.2.1., Initialization step 3.
    internal static readonly int[] J =
        [0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 9, 10, 11, 12, 13, 14, 15];

    private static readonly Lazy<sbyte[]> QuantizationLutLossless8 = new(CreateQuantizeLutLossless(8));
    private static readonly Lazy<sbyte[]> QuantizationLutLossless10 = new(CreateQuantizeLutLossless(10));
    private static readonly Lazy<sbyte[]> QuantizationLutLossless12 = new(CreateQuantizeLutLossless(12));
    private static readonly Lazy<sbyte[]> QuantizationLutLossless16 = new(CreateQuantizeLutLossless(16));

    internal ScanCodec(Traits traits, FrameInfo frameInfo, JpegLSPresetCodingParameters presetCodingParameters, CodingParameters codingParameters)
    {
        Traits = traits;
        QuantizationLut = GetQuantizationLut(traits, presetCodingParameters.Threshold1, presetCodingParameters.Threshold2, presetCodingParameters.Threshold3);

        FrameInfo = frameInfo;
        PresetCodingParameters = presetCodingParameters;
        CodingParameters = codingParameters;

        // Copy often used preset coding parameters to local fields for faster access.
        ResetThreshold = presetCodingParameters.ResetValue;
        NearLossless = CodingParameters.NearLossless;
        Limit = ComputeLimitParameter(traits.BitsPerSample);
        QuantizedBitsPerSample = Log2Ceiling(traits.Range);
    }

    internal FrameInfo FrameInfo { get; private set; }

    internal JpegLSPresetCodingParameters PresetCodingParameters { get; }

    internal CodingParameters CodingParameters { get; }

    internal void InitializeParameters(int range)
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

    internal readonly sbyte QuantizeGradient(int di, int nearLossless)
    {
        return QuantizeGradientOrg(
            di, PresetCodingParameters.Threshold1, PresetCodingParameters.Threshold2, PresetCodingParameters.Threshold3, nearLossless);
    }

    internal void IncrementRunIndex()
    {
        RunIndex = Math.Min(31, RunIndex + 1);
    }

    internal void DecrementRunIndex()
    {
        RunIndex = Math.Max(0, RunIndex - 1);
    }

    internal static sbyte[] GetQuantizationLut(Traits traits, int threshold1, int threshold2, int threshold3)
    {
        if (IsCachedQuantizationLutAvailable(traits, threshold1, threshold2, threshold3))
        {
            switch (traits.BitsPerSample)
            {
                case 8:
                    return QuantizationLutLossless8.Value;

                case 10:
                    return QuantizationLutLossless10.Value;

                case 12:
                    return QuantizationLutLossless12.Value;

                case 16:
                    return QuantizationLutLossless16.Value;
            }
        }

        // Initialize the quantization lookup table dynamic.
        int quantizationRange = 1 << traits.BitsPerSample;
        var quantizationLut = new sbyte[quantizationRange * 2];
        for (int i = 0; i < quantizationLut.Length; ++i)
        {
            quantizationLut[i] = QuantizeGradientOrg(
                -quantizationRange + i, threshold1, threshold2, threshold3, traits.NearLossless);
        }

        return quantizationLut;
    }

    private static bool IsCachedQuantizationLutAvailable(Traits traits, int threshold1, int threshold2, int threshold3)
    {
        if (traits.NearLossless != 0 || traits.MaximumSampleValue != (1 << traits.BitsPerSample) - 1)
            return false;

        var codingParameters = JpegLSPresetCodingParameters.ComputeDefault(traits.MaximumSampleValue, traits.NearLossless);
        return codingParameters.Threshold1 == threshold1 && codingParameters.Threshold2 == threshold2 && codingParameters.Threshold3 == threshold3;
    }

    private static sbyte[] CreateQuantizeLutLossless(int bitCount)
    {
        var codingParameters = JpegLSPresetCodingParameters.ComputeDefault(CalculateMaximumSampleValue(bitCount), 0);
        int range = codingParameters.MaximumSampleValue + 1;

        sbyte[] quantizationLut = new sbyte[range * 2];
        for (int i = 0; i != quantizationLut.Length; ++i)
        {
            quantizationLut[i] = QuantizeGradientOrg(-range + i, codingParameters.Threshold1, codingParameters.Threshold2, codingParameters.Threshold3);
        }

        return quantizationLut;
    }

    [InlineArray(2)]
    internal struct RunModeContextArray
    {
        private RunModeContext _;
    }

    [InlineArray(365)]
    internal struct RegularModeContextArray
    {
        private RegularModeContext _;
    }
}
