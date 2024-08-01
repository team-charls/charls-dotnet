// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

internal abstract class TraitsBase<TSample, TPixel> : ITraits<TSample, TPixel>
    where TSample : struct
{
    protected TraitsBase(int max, int near, int reset = Constants.DefaultResetValue)
    {
        MaximumSampleValue = max;
        Range = (max + 2 * near) / (2 * near + 1) + 1;
        NEAR = near;
        qbpp = Log2(Range);
        bpp = Log2(max);
        Limit = 2 * (bpp + Math.Max(8, bpp));
        RESET = reset;
    }

    //protected TraitsBase(ITraits<TSample, TPixel> other)
    //{
    //    MAXVAL = other.MAXVAL;
    //    RANGE = other.RANGE;
    //    NEAR = other.NEAR;
    //    qbpp = other.qbpp;
    //    bpp = other.bpp;
    //    Limit = other.Limit;
    //    RESET = other.RESET;
    //}

    protected TraitsBase(int bitsperpixel)
    {
        NEAR = 0;
        bpp = bitsperpixel;
        qbpp = bitsperpixel;
        Range = 1 << bpp;
        MaximumSampleValue = (1 << bpp) - 1;
        Limit = 2 * (bitsperpixel + Math.Max(8, bitsperpixel));
        RESET = Constants.DefaultResetValue;
        QuantizationRange = 1 << bitsperpixel;
    }

    public int MaximumSampleValue { get; set; }

    public int Range { get; }

    public int NEAR { get; }

    public int qbpp { get; }

    public int bpp { get; }

    public int Limit { get; }

    public int RESET { get; }

    public int QuantizationRange { get; }

    public abstract int NearLossless { get; }

    public abstract int ComputeErrVal(int e);

    public abstract TSample ComputeReconstructedSample(int Px, int ErrVal);

    public abstract bool IsNear(int lhs, int rhs);

    public abstract bool IsNear(TPixel lhs, TPixel rhs);

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

