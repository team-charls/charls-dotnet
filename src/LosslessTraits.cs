// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Numerics;

namespace CharLS.JpegLS;

internal class LosslessTraitsImplT : Traits
{
    internal LosslessTraitsImplT(int maximumSampleValue, int nearLossless, int resetThreshold = Constants.DefaultResetValue)
        : base(maximumSampleValue, nearLossless, resetThreshold)
    {
    }

    protected LosslessTraitsImplT(int bitsperpixel)
        : base(bitsperpixel)
    {
    }

    public override int ComputeErrVal(int d)
    {
        return ModuloRange(d);
    }

    public override bool IsNear(int lhs, int rhs)
    {
        return lhs == rhs;
    }

    //public override bool IsNear(TPixel lhs, TPixel rhs)
    //{
    //    return false;
    //    ////return IsNear(Convert.ToInt32(lhs), Convert.ToInt32(rhs));
    //}

    public override int ModuloRange(int errorValue)
    {
        return (errorValue << (Constants.Int32BitCount - bpp)) >> (Constants.Int32BitCount - bpp);
    }

    public override int ComputeReconstructedSample(int predictedValue, int errorValue)
    {
        return MaximumSampleValue & (predictedValue + errorValue);
    }

    public override int CorrectPrediction(int predicted)
    {
        if ((predicted & MaximumSampleValue) == predicted)
            return predicted;

        return ~(predicted >> (Constants.Int32BitCount - 1)) & MaximumSampleValue;
    }

    public override int NearLossless => 0;
}


internal class LosslessTraits8 : LosslessTraitsImplT
{
    public LosslessTraits8()
        : base(8)
    {
    }

    public sbyte ModRange(int errorValue)
    {
        return (sbyte)errorValue;
    }

    public /*override*/ int ComputeErrorValue(int errorValue)
    {
        return (sbyte)errorValue;
    }

    public override int ComputeReconstructedSample(int predictedValue, int errorValue)
    {
        return predictedValue + errorValue;
    }
}


internal class LosslessTraits16 : LosslessTraitsImplT
{
    public LosslessTraits16(int bitsPerSample)
        : base(bitsPerSample)
    {
    }

    public /*override*/ int ComputeErrorValue(int errorValue)
    {
        return (ushort)errorValue;
    }

    public override int ComputeReconstructedSample(int predictedValue, int errorValue)
    {
        return predictedValue + errorValue;
    }
}


internal class LosslessTraitsTriplet<TSample> : LosslessTraitsImplT
    where TSample : struct, IBinaryInteger<TSample>
{
    public LosslessTraitsTriplet(int maximumSampleValue, int near, int reset = Constants.DefaultResetValue)
        : base(maximumSampleValue, near, reset)
    {
    }

    //public override bool IsNear(Triplet<TSample> lhs, Triplet<TSample> rhs)
    //{
    //    return lhs.Equals(rhs);
    //}

    public override int ComputeReconstructedSample(int predictedValue, int errorValue)
    {
        return predictedValue + errorValue;
    }
}

