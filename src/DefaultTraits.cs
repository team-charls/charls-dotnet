// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause


using System.Diagnostics;

namespace CharLS.JpegLS;

internal class DefaultTraits : Traits
{
    internal DefaultTraits(int maximumSampleValue, int nearLossless, int resetThreshold)
        : base(maximumSampleValue, nearLossless, resetThreshold)
    {
    }

    internal override int ComputeErrorValue(int e)
    {
        return ModuloRange(Quantize(e));
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
        return Math.Abs(lhs - rhs) <= NearLossless;
    }

    //public override bool IsNear(TPixel lhs, TPixel rhs)
    //{
    //    throw new NotImplementedException();
    //}

    public override int ModuloRange(int errorValue)
    {
        Debug.Assert(Math.Abs(errorValue) <= Range);

        if (errorValue < 0)
        {
            errorValue += Range;
        }

        if (errorValue >= (Range + 1) / 2)
        {
            errorValue -= Range;
        }

        Debug.Assert(-Range / 2 <= errorValue && errorValue <= ((Range + 1) / 2) - 1);
        return errorValue;
    }

    private int Quantize(int errorValue)
    {
        if (errorValue > 0)
            return (errorValue + NearLossless) / (2 * NearLossless + 1);

        return -(NearLossless - errorValue) / (2 * NearLossless + 1);
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
