// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

internal abstract class Traits
{
    protected Traits(int maximumSampleValue, int nearLossless, int resetThreshold = Constants.DefaultResetValue)
    {
        MaximumSampleValue = maximumSampleValue;
        Range = (maximumSampleValue + 2 * nearLossless) / (2 * nearLossless + 1) + 1;
        NearLossless = nearLossless;
        qbpp = Log2(Range);
        bpp = Log2(maximumSampleValue);
        Limit = 2 * (bpp + Math.Max(8, bpp));
        ResetThreshold = resetThreshold;
        QuantizationRange = 1 << bpp;
    }

    protected Traits(int bitsperpixel)
    {
        bpp = bitsperpixel;
        qbpp = bitsperpixel;
        Range = 1 << bpp;
        MaximumSampleValue = (1 << bpp) - 1;
        Limit = 2 * (bitsperpixel + Math.Max(8, bitsperpixel));
        ResetThreshold = Constants.DefaultResetValue;
        QuantizationRange = 1 << bitsperpixel;
    }

    public int MaximumSampleValue { get; set; }

    public int Range { get; }

    public int qbpp { get; }

    public int bpp { get; }

    public int Limit { get; }

    public int ResetThreshold { get; }

    public int QuantizationRange { get; }

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

    private static int Log2(int n)
    {
        var x = 0;
        while (n > 1 << x)
        {
            ++x;
        }

        return x;
    }
}

