// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using static CharLS.Managed.Algorithm;

namespace CharLS.Managed;

internal class Traits
{
    internal Traits(int maximumSampleValue, int nearLossless)
    {
        MaximumSampleValue = maximumSampleValue;
        Range = ComputeRangeParameter(maximumSampleValue, nearLossless);
        NearLossless = nearLossless;
        BitsPerSample = Log2Ceiling(maximumSampleValue);
    }

    /// <summary>
    /// ISO 14495-1 MAX symbol: maximum possible image sample value over all components of a scan.
    /// </summary>
    internal int MaximumSampleValue { get; set; }

    /// <summary>
    /// ISO 14495-1 bpp (Bits per pixel per component) symbol: number of bits needed to represent MAXVAL (not less than 2).
    /// </summary>
    internal int BitsPerSample { get; }

    /// <summary>
    /// ISO 14495-1 RANGE symbol: range of prediction error representation.
    /// </summary>
    internal int Range { get; }

    /// <summary>
    /// ISO 14495-1 NEAR symbol: difference bound for near-lossless coding, 0 means lossless.
    /// </summary>
    internal int NearLossless { get; }

    internal int CorrectPrediction(int predicted)
    {
        if ((predicted & MaximumSampleValue) == predicted)
            return predicted;

        return ~(predicted >> (Constants.Int32BitCount - 1)) & MaximumSampleValue;
    }

    internal virtual int ComputeErrorValue(int errorValue)
    {
        return ModuloRange(Quantize(errorValue, NearLossless));
    }

    internal virtual int ComputeReconstructedSample(int predictedValue, int errorValue)
    {
        return FixReconstructedValue(predictedValue + Dequantize(errorValue, NearLossless));
    }

    /// <summary>
    /// Returns the value of errorValue modulo RANGE. ITU.T.87, A.4.5 (code segment A.9).
    /// </summary>
    internal virtual int ModuloRange(int errorValue)
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

    internal virtual bool IsNear(int lhs, int rhs)
    {
        return Math.Abs(lhs - rhs) <= NearLossless;
    }

    internal virtual bool IsNear(Triplet<byte> lhs, Triplet<byte> rhs)
    {
        return Math.Abs(lhs.V1 - rhs.V1) <= NearLossless && Math.Abs(lhs.V2 - rhs.V2) <= NearLossless &&
               Math.Abs(lhs.V3 - rhs.V3) <= NearLossless;
    }

    internal virtual bool IsNear(Triplet<ushort> lhs, Triplet<ushort> rhs)
    {
        return Math.Abs(lhs.V1 - rhs.V1) <= NearLossless && Math.Abs(lhs.V2 - rhs.V2) <= NearLossless &&
               Math.Abs(lhs.V3 - rhs.V3) <= NearLossless;
    }

    internal virtual bool IsNear(Quad<byte> lhs, Quad<byte> rhs)
    {
        return Math.Abs(lhs.V1 - rhs.V1) <= NearLossless && Math.Abs(lhs.V2 - rhs.V2) <= NearLossless &&
               Math.Abs(lhs.V3 - rhs.V3) <= NearLossless && Math.Abs(lhs.V4 - rhs.V4) <= NearLossless;
    }

    internal virtual bool IsNear(Quad<ushort> lhs, Quad<ushort> rhs)
    {
        return Math.Abs(lhs.V1 - rhs.V1) <= NearLossless && Math.Abs(lhs.V2 - rhs.V2) <= NearLossless &&
               Math.Abs(lhs.V3 - rhs.V3) <= NearLossless && Math.Abs(lhs.V4 - rhs.V4) <= NearLossless;
    }

    internal static Traits Create(int bitsPerSample, int nearLossless)
    {
        int maximumSampleValue = CalculateMaximumSampleValue(bitsPerSample);

        if (nearLossless != 0)
            return new Traits(maximumSampleValue, nearLossless);

        return bitsPerSample switch
        {
            8 => new LosslessTraits8(),
            16 => new LosslessTraits16(),
            _ => new LosslessTraits(maximumSampleValue)
        };
    }

    private int FixReconstructedValue(int value)
    {
        if (value < -NearLossless)
        {
            value += Range * ((2 * NearLossless) + 1);
        }
        else if (value > MaximumSampleValue + NearLossless)
        {
            value -= Range * ((2 * NearLossless) + 1);
        }

        return CorrectPrediction(value);
    }

    private static int Quantize(int errorValue, int nearLossless)
    {
        if (errorValue > 0)
            return (errorValue + nearLossless) / ((2 * nearLossless) + 1);

        return -(nearLossless - errorValue) / ((2 * nearLossless) + 1);
    }

    private static int Dequantize(int errorValue, int nearLossless)
    {
        return errorValue * ((2 * nearLossless) + 1);
    }
}
