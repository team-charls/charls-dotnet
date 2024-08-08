// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

internal abstract class Traits
{
    protected Traits(int maximumSampleValue, int nearLossless, int resetThreshold = Constants.DefaultResetThreshold)
    {
        MaximumSampleValue = maximumSampleValue;
        Range = (maximumSampleValue + 2 * nearLossless) / (2 * nearLossless + 1) + 1;
        NearLossless = nearLossless;
        QuantizedBitsPerSample = Algorithm.Log2Ceiling(Range);
        BitsPerSample = Algorithm.Log2Ceiling(maximumSampleValue);
        Limit = 2 * (BitsPerSample + Math.Max(8, BitsPerSample));
        ResetThreshold = resetThreshold;
        QuantizationRange = 1 << BitsPerSample;
    }

    protected Traits(int bitsPerSample)
    {
        BitsPerSample = bitsPerSample;
        QuantizedBitsPerSample = bitsPerSample;
        Range = 1 << BitsPerSample;
        MaximumSampleValue = (1 << BitsPerSample) - 1;
        Limit = 2 * (bitsPerSample + Math.Max(8, bitsPerSample));
        ResetThreshold = Constants.DefaultResetThreshold;
        QuantizationRange = 1 << bitsPerSample;
    }

    /// <summary>
    /// ISO 14495-1 MAX symbol: maximum possible image sample value over all components of a scan.
    /// </summary>
    internal int MaximumSampleValue { get; set; }

    /// <summary>
    /// ISO 14495-1 bpp symbol: number of bits needed to represent MAXVAL (not less than 2).
    /// </summary>
    internal int BitsPerSample { get; }

    /// <summary>
    /// ISO 14495-1 RANGE symbol: range of prediction error representation.
    /// </summary>
    internal int Range { get; }

    /// <summary>
    /// ISO 14495-1 qbpp symbol: number of bits needed to represent a mapped error value.
    /// </summary>
    internal int QuantizedBitsPerSample { get; }

    /// <summary>
    /// ISO 14495-1 LIMIT symbol: the value of glimit for a sample encoded in regular mode.
    /// </summary>
    internal int Limit { get; }

    /// <summary>
    /// ISO 14495-1 RESET symbol: threshold value at which A, B, and N are halved.
    /// </summary>
    internal int ResetThreshold { get; }

    internal int QuantizationRange { get; }

    /// <summary>
    /// ISO 14495-1 NEAR symbol: difference bound for near-lossless coding, 0 means lossless
    /// </summary>
    public virtual int NearLossless { get; }

    public abstract int ComputeErrVal(int e);

    public abstract int ComputeReconstructedSample(int predictedValue, int errorValue);

    public abstract bool IsNear(int lhs, int rhs);

    //public abstract bool IsNear(TPixel lhs, TPixel rhs);

    public abstract int CorrectPrediction(int predicted);

    /// <summary>
    /// Returns the value of errorValue modulo RANGE. ITU.T.87, A.4.5 (code segment A.9)
    /// </summary>
    public abstract int ModuloRange(int errorValue);
}
