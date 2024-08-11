// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

internal abstract class Traits
{
    protected Traits(int maximumSampleValue, int nearLossless, int resetThreshold = Constants.DefaultResetThreshold)
    {
        MaximumSampleValue = maximumSampleValue;
        Range = Algorithm.ComputeRangeParameter(maximumSampleValue, nearLossless);
        NearLossless = nearLossless;
        QuantizedBitsPerSample = Algorithm.Log2Ceiling(Range);
        BitsPerSample = Algorithm.Log2Ceiling(maximumSampleValue);
        Limit = 2 * (BitsPerSample + Math.Max(8, BitsPerSample));
        ResetThreshold = resetThreshold;
        QuantizationRange = 1 << BitsPerSample;
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

    internal abstract int ComputeErrorValue(int e);

    public abstract int ComputeReconstructedSample(int predictedValue, int errorValue);

    public abstract bool IsNear(int lhs, int rhs);

    internal bool IsNear(Triplet<byte> lhs, Triplet<byte> rhs)
    {
        return Math.Abs(lhs.V1 - rhs.V1) <= NearLossless && Math.Abs(lhs.V2 - rhs.V2) <= NearLossless &&
               Math.Abs(lhs.V3 - rhs.V3) <= NearLossless;
    }

    internal bool IsNear(Quad<byte> lhs, Quad<byte> rhs)
    {
        return Math.Abs(lhs.V1 - rhs.V1) <= NearLossless && Math.Abs(lhs.V2 - rhs.V2) <= NearLossless &&
               Math.Abs(lhs.V3 - rhs.V3) <= NearLossless && Math.Abs(lhs.V4 - rhs.V4) <= NearLossless;
    }

    internal bool IsNear(Quad<ushort> lhs, Quad<ushort> rhs)
    {
        return Math.Abs(lhs.V1 - rhs.V1) <= NearLossless && Math.Abs(lhs.V2 - rhs.V2) <= NearLossless &&
               Math.Abs(lhs.V3 - rhs.V3) <= NearLossless && Math.Abs(lhs.V4 - rhs.V4) <= NearLossless;
    }

    //public abstract bool IsNear(TPixel lhs, TPixel rhs);

    public abstract int CorrectPrediction(int predicted);

    /// <summary>
    /// Returns the value of errorValue modulo RANGE. ITU.T.87, A.4.5 (code segment A.9)
    /// </summary>
    public abstract int ModuloRange(int errorValue);
}
