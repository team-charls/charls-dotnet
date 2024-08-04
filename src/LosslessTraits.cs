// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

internal abstract class LosslessTraitsImplT<TSample, TPixel> : TraitsBase<TSample, TPixel>
    where TSample : struct
{
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

    public override bool IsNear(TPixel lhs, TPixel rhs)
    {
        return false;
        ////return IsNear(Convert.ToInt32(lhs), Convert.ToInt32(rhs));
    }

    public override int ModuloRange(int errorValue)
    {
        return (errorValue << (Constants.Int32BitCount - bpp)) >> (Constants.Int32BitCount - bpp);
    }

    public override TSample ComputeReconstructedSample(int predictedValue, int errorValue)
    {
        return default;
        ////return (TSample)Convert.ChangeType(MAXVAL & (Px + ErrVal), typeof(TSample));
    }

    public override int CorrectPrediction(int predicted)
    {
        if ((predicted & MaximumSampleValue) == predicted)
            return predicted;

        return ~(predicted >> (Constants.Int32BitCount - 1)) & MaximumSampleValue;
    }

    public override int NearLossless => 0;
}


internal class LosslessTraits8 : LosslessTraitsImplT<byte, byte>
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

    public override byte ComputeReconstructedSample(int predictedValue, int errorValue)
    {
        return (byte)(predictedValue + errorValue);
    }
}
