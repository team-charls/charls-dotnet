// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Numerics;
using System.Runtime.CompilerServices;

namespace CharLS.Managed;

internal static class Algorithm
{
    /// <summary>
    /// Abs, but without the check for overflow.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int AbsUnchecked(int value)
    {
        Debug.Assert(value != int.MinValue);
        return value < 0 ? -value : value;
    }

    /// <summary>
    /// Checks if a value is outside the range [-range, range].
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool OutsideRange(int value, int range)
    {
        return value < -range || value > range;
    }

    internal static int Log2Ceiling(int value)
    {
        Debug.Assert(value >= 0);
        Debug.Assert((uint)value <= uint.MaxValue >> 2); // otherwise 1 << x becomes negative.

        int log2Floor = BitOperations.Log2((uint)value);

        // If value is not a power of two, add 1 to get the ceiling
        return BitOperations.IsPow2(value) ? log2Floor : log2Floor + 1;
    }

    /// <summary>
    /// Computes how many bytes are needed to hold the number of bits.
    /// </summary>
    internal static int BitToByteCount(int bitCount)
    {
        return (bitCount + 7) / 8;
    }

    internal static int CalculateMaximumSampleValue(int bitsPerSample)
    {
        Debug.Assert(bitsPerSample is > 0 and <= 16);
        return (1 << bitsPerSample) - 1;
    }

    internal static int ComputeMaximumNearLossless(int maximumSampleValue)
    {
        return Math.Min(Constants.MaximumNearLossless, maximumSampleValue / 2); // As defined by ISO/IEC 14495-1, C.2.3
    }

    // Computes the initial value for A. See ISO/IEC 14495-1, A.8, step 1.d and A.2.1
    internal static int InitializationValueForA(int range)
    {
        Debug.Assert(range is >= 4 and <= ushort.MaxValue + 1);
        return Math.Max(2, (range + 32) / 64);
    }

    /// <summary>
    /// This is the algorithm of ISO/IEC 14495-1, A.5.2, Code Segment A.11 (second else branch)
    /// It will map signed values to unsigned values. It has been optimized to prevent branching.
    /// </summary>
    internal static int MapErrorValue(int errorValue)
    {
        Debug.Assert(errorValue <= int.MaxValue / 2);

        int mappedError = (errorValue >> (Constants.Int32BitCount - 2)) ^ (2 * errorValue);
        return mappedError;
    }

    /// <summary>
    /// This is the optimized inverse algorithm of ISO/IEC 14495-1, A.5.2, Code Segment A.11 (second else branch)
    /// It will map unsigned values back to signed values.
    /// </summary>
    internal static int UnmapErrorValue(int mappedError)
    {
        int sign = (int)((uint)mappedError << (Constants.Int32BitCount - 1)) >> (Constants.Int32BitCount - 1);
        return sign ^ (mappedError >> 1);
    }

    internal static int Sign(int n)
    {
        return (n >> (Constants.Int32BitCount - 1)) | 1;
    }

    internal static int BitWiseSign(int i)
    {
        return i >> (Constants.Int32BitCount - 1);
    }

    internal static int ApplySign(int i, int sign)
    {
        return (sign ^ i) - sign;
    }

    /// <summary>
    /// Computes the parameter RANGE. When NEAR = 0, RANGE = MAXVAL + 1. (see ISO/IEC 14495-1, A.2.1).
    /// </summary>
    internal static int ComputeRangeParameter(int maximumSampleValue, int nearLossless)
    {
        return ((maximumSampleValue + (2 * nearLossless)) / ((2 * nearLossless) + 1)) + 1;
    }

    /// <summary>
    /// Computes the parameter LIMIT. (see ISO/IEC 14495-1, A.2.1).
    /// </summary>
    internal static int ComputeLimitParameter(int bitsPerSample)
    {
        return 2 * (bitsPerSample + Math.Max(8, bitsPerSample));
    }

    internal static int ComputePredictedValue(int ra, int rb, int rc)
    {
        // sign trick reduces the number of if statements (branches)
        int sign = BitWiseSign(rb - ra);

        // is Ra between Rc and Rb?
        if ((sign ^ (rc - ra)) < 0)
        {
            return rb;
        }

        if ((sign ^ (rb - rc)) < 0)
        {
            return ra;
        }

        // default case, valid if Rc element of [Ra,Rb]
        return ra + rb - rc;
    }

    // See JPEG-LS standard ISO/IEC 14495-1, A.3.3, golomb_code Segment A.4
    internal static sbyte QuantizeGradientOrg(int di, int threshold1, int threshold2, int threshold3, int nearLossless = 0)
    {
        if (di <= -threshold3)
            return -4;
        if (di <= -threshold2)
            return -3;
        if (di <= -threshold1)
            return -2;
        if (di < -nearLossless)
            return -1;
        if (di <= nearLossless)
            return 0;
        if (di < threshold1)
            return 1;
        if (di < threshold2)
            return 2;

        return di < threshold3 ? (sbyte)3 : (sbyte)4;
    }

    internal static int ComputeContextId(int q1, int q2, int q3)
    {
        return (((q1 * 9) + q2) * 9) + q3;
    }
}
