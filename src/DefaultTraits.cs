// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause


namespace CharLS.JpegLS;

internal class DefaultTraits : Traits
{
    private readonly int _maximumSampleValue;
    private readonly int _range;

    internal DefaultTraits(int maximumSampleValue, int bitsPerPixel, int nearLossless)
        : base(bitsPerPixel)
    {
        _maximumSampleValue = maximumSampleValue;
        _range = Algorithm.ComputeRangeParameter(_maximumSampleValue, nearLossless);
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
        if ((predicted & _maximumSampleValue) == predicted)
            return predicted;

        return (~(predicted >> (Constants.Int32BitCount - 1))) & _maximumSampleValue;
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
            value = value + _range * (2 * NearLossless + 1);
        }
        else if (value > _maximumSampleValue + NearLossless)
        {
            value = value - _range * (2 * NearLossless + 1);
        }

        return CorrectPrediction(value);
    }
}
