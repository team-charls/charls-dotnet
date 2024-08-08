// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

internal class LosslessTraitsImpl : Traits
{
    internal LosslessTraitsImpl(int maximumSampleValue, int nearLossless, int resetThreshold)
        : base(maximumSampleValue, nearLossless, resetThreshold)
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
        return (errorValue << (Constants.Int32BitCount - BitsPerSample)) >> (Constants.Int32BitCount - BitsPerSample);
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


internal class LosslessTraits8 : LosslessTraitsImpl
{
    public LosslessTraits8(int maximumSampleValue, int nearLossless, int resetThreshold)
        : base(maximumSampleValue, nearLossless, resetThreshold)
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


internal class LosslessTraits16 : LosslessTraitsImpl
{
    public LosslessTraits16(int maximumSampleValue, int nearLossless, int resetThreshold)
        : base(maximumSampleValue, nearLossless, resetThreshold)
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


internal class LosslessTraitsTriplet : LosslessTraitsImpl
{
    public LosslessTraitsTriplet(int maximumSampleValue, int nearLossless, int resetThreshold)
        : base(maximumSampleValue, nearLossless, resetThreshold)
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

