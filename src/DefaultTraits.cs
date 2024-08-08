// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause


namespace CharLS.JpegLS;

internal class DefaultTraits : Traits
{
    internal DefaultTraits(int maximumSampleValue, int nearLossless, int resetThreshold = Constants.DefaultResetThreshold)
        : base(maximumSampleValue, nearLossless, resetThreshold)
    {
    }

    public override int ComputeErrVal(int e)
    {
        throw new NotImplementedException();
    }

    public override int ComputeReconstructedSample(int predictedValue, int errorValue)
    {
        return FixReconstructedValue(predictedValue + Dequantize(errorValue));
    }

    public override int CorrectPrediction(int predicted)
    {
        if ((predicted & MaximumSampleValue) == predicted)
            return predicted;

        return (~(predicted >> (Constants.Int32BitCount - 1))) & MaximumSampleValue;
    }

    public override bool IsNear(int lhs, int rhs)
    {
        throw new NotImplementedException();
    }

    //public override bool IsNear(TPixel lhs, TPixel rhs)
    //{
    //    throw new NotImplementedException();
    //}

    public override int ModuloRange(int errorValue)
    {
        throw new NotImplementedException();
    }

    private int Dequantize(int errorValue)
    {
        return errorValue * (2 * NearLossless + 1);
    }

    private int FixReconstructedValue(int value)
    {
        if (value < -NearLossless)
        {
            value = value + Range * (2 * NearLossless + 1);
        }
        else if (value > MaximumSampleValue + NearLossless)
        {
            value = value - Range * (2 * NearLossless + 1);
        }

        return CorrectPrediction(value);
    }
}
