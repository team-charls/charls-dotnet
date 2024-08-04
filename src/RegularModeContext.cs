// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Diagnostics;

namespace CharLS.JpegLS;

internal struct RegularModeContext
{
    // Initialize with the default values as defined in ISO 14495-1, A.8, step 1.d.
    internal int A;
    internal int B;
    internal int C;
    internal int N = 1;

    internal RegularModeContext(int range)
    {
        A = Algorithm.InitializationValueForA(range);
    }

    /// <summary>
    /// Computes the Golomb coding parameter using the algorithm as defined in ISO 14495-1, code segment A.10
    /// </summary>
    internal int GetGolombCodingParameter()
    {
        int k = 0;
        for (; N << k < A && k < Constants.MaxKValue; ++k)
        {
            // Purpose of this loop is to calculate 'k', by design no content.
        }

        if (k == Constants.MaxKValue)
            throw Util.CreateInvalidDataException(JpegLSError.InvalidEncodedData);

        return k;
    }

    internal int GetErrorCorrection(int k)
    {
        return k != 0 ? 0 : Algorithm.BitWiseSign(2 * B + N - 1);
    }

    /// <summary>Code segment A.12 – Variables update. ISO 14495-1, page 22</summary>
    internal void update_variables_and_bias(int errorValue, int nearLossless, int resetThreshold)
    {
        Debug.Assert(N != 0);

        A += Math.Abs(errorValue);
        B += errorValue* (2 * nearLossless + 1);

        const int limit = 65536 * 256;
        if (A >= limit || Math.Abs(B) >= limit)
            throw Util.CreateInvalidDataException(JpegLSError.InvalidEncodedData);

        if (N == resetThreshold)
        {
            A >>= 1;
            B >>= 1;
            N >>= 1;
        }

        ++N;
        Debug.Assert(N != 0);

        // This part is from: Code segment A.13 – Update of bias-related variables B[Q] and C[Q]
        const int maxC = 127;  // MAX_C: maximum allowed value of C[0..364]. ISO 14495-1, section 3.3
        const int minC = -128; // MIN_C: Minimum allowed value of C[0..364]. ISO 14495-1, section 3.3
        if (B + N <= 0)
        {
            B += N;
            if (B <= -N)
            {
                B = -N + 1;
            }
            if (C > minC)
            {
                --C;
            }
        }
        else if (B > 0)
        {
            B -= N;
            if (B > 0)
            {
                B = 0;
            }
            if (C < maxC)
            {
                ++C;
            }
        }
    }
}
