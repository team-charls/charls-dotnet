// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using static CharLS.Managed.Algorithm;

namespace CharLS.Managed;

internal struct RegularModeContext
{
    // Initialize with the default values as defined in ISO 14495-1, A.8, step 1.d.
    private int _a;
    private int _b;
    private int _n = 1;

    internal RegularModeContext(int range)
    {
        _a = InitializationValueForA(range);
    }

    internal int C { get; private set; }

    /// <summary>
    /// Computes the Golomb coding parameter using the algorithm as defined in ISO 14495-1, code segment A.10.
    /// </summary>
    /// <remarks>
    /// An additional check is done for bad input values to prevent an endless loop.
    /// </remarks>
    internal readonly int ComputeGolombCodingParameterChecked()
    {
        const int maxKValue = 16; // This is an implementation limit as there are only 16 lookup tables (theoretical limit is 32)

        int k = ComputeGolombCodingParameter();
        if (k >= maxKValue)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidData);

        return k;
    }

    /// <summary>
    /// Computes the Golomb coding parameter using the algorithm as defined in ISO 14495-1, code segment A.10.
    /// </summary>
    internal readonly int ComputeGolombCodingParameter()
    {
        int k = 0;
        for (; _n << k < _a; ++k)
        {
            // Purpose of this loop is to calculate 'k', by design no content.
        }

        return k;
    }

    internal readonly int GetErrorCorrection(int k)
    {
        return k != 0 ? 0 : BitWiseSign((2 * _b) + _n - 1);
    }

    /// <summary>Code segment A.12 – Variables update. ISO 14495-1, page 22.</summary>
    internal void UpdateVariablesAndBias(int errorValue, int nearLossless, int resetThreshold)
    {
        Debug.Assert(_n != 0);
        Debug.Assert(resetThreshold >= 3);

        _a += AbsUnchecked(errorValue);
        _b += errorValue * ((2 * nearLossless) + 1);

        const int limit = 65536 * 256;
        if (_a >= limit || OutsideRange(_b, limit))
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidData);

        if (_n == resetThreshold)
        {
            _a >>= 1;
            _b >>= 1; // Note: C# performs arithmetic right shift on signed integers, handling _b < 0 case is not needed.
            _n >>= 1;
        }

        ++_n;
        Debug.Assert(_n != 0);

        // This part is from: Code segment A.13 – Update of bias-related variables B[Q] and C[Q]
        const int maxC = 127;  // MAX_C: maximum allowed value of C[0..364]. ISO 14495-1, section 3.3
        const int minC = -128; // MIN_C: Minimum allowed value of C[0..364]. ISO 14495-1, section 3.3
        if (_b + _n <= 0)
        {
            _b += _n;
            if (_b <= -_n)
            {
                _b = -_n + 1;
            }

            if (C > minC)
            {
                --C;
            }
        }
        else if (_b > 0)
        {
            _b -= _n;
            if (_b > 0)
            {
                _b = 0;
            }

            if (C < maxC)
            {
                ++C;
            }
        }
    }
}
