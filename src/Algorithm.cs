// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Diagnostics;

namespace CharLS.JpegLS;

internal static class Algorithm
{
    // Computes the initial value for A. See ISO/IEC 14495-1, A.8, step 1.d and A.2.1
    internal static int InitializationValueForA(int range)
    {
        Debug.Assert(range is >= 4 and <= (ushort.MaxValue + 1));
        return Math.Max(2, (range + 32) / 64);
    }

    /// <summary>
    /// This is the algorithm of ISO/IEC 14495-1, A.5.2, Code Segment A.11 (second else branch)
    /// It will map signed values to unsigned values. It has been optimized to prevent branching.
    /// </summary>
    internal static int MapErrorValue(int errorValue)
    {
        //ASSERT(error_value <= std::numeric_limits<int32_t>::max() / 2);

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
    /// Computes the parameter RANGE. When NEAR = 0, RANGE = MAXVAL + 1. (see ISO/IEC 14495-1, A.2.1)
    /// </summary>
    internal static int ComputeRangeParameter(int maximumSampleValue, int nearLossless)
    {
        return (maximumSampleValue + 2 * nearLossless) / (2 * nearLossless + 1) + 1;
    }

    internal static int GetPredictedValue(int ra, int rb, int rc)
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
        if (di < threshold3)
            return 3;

        return 4;
    }

    internal static int ComputeContextId(int q1, int q2, int q3)
    {
        return (q1 * 9 + q2) * 9 + q3;
    }

    internal static int CalculateMaximumSampleValue(int bitsPerSample)
    {
        //ASSERT(bits_per_sample > 0 && bits_per_sample <= 16);
        return (int)((1U << bitsPerSample) - 1);
    }

}
