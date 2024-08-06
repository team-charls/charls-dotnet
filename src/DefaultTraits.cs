// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause


namespace CharLS.JpegLS;

internal class DefaultTraits<TSample, TPixel> : TraitsBase<TSample, TPixel>
    where TSample : struct
{
    private readonly int _maximumSampleValue;
    private readonly int _nearLossless;
    private readonly int _range;


    internal DefaultTraits(int maximumSampleValue, int bitsPerPixel, int nearLossless)
        : base(bitsPerPixel)
    {
        _maximumSampleValue = maximumSampleValue;
        _nearLossless = nearLossless;
        _range = Algorithm.ComputeRangeParameter(_maximumSampleValue, _nearLossless);
    }

    public override int NearLossless => _nearLossless;

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

    public override bool IsNear(TPixel lhs, TPixel rhs)
    {
        throw new NotImplementedException();
    }

    public override int ModuloRange(int errorValue)
    {
        throw new NotImplementedException();
    }

    private int Dequantize(int errorValue)
    {
        return errorValue * (2 * _nearLossless + 1);
    }

    private int FixReconstructedValue(int value)
    {
        if (value < -_nearLossless)
        {
            value = value + _range * (2 * _nearLossless + 1);
        }
        else if (value > _maximumSampleValue + _nearLossless)
        {
            value = value - _range * (2 * _nearLossless + 1);
        }

        return CorrectPrediction(value);
    }
}
