// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Diagnostics;

namespace CharLS.Managed;

internal struct RunModeContext
{
    // Initialize with the default values as defined in ISO 14495-1, A.8, step 1.d and 1.f.
    private int _a;
    private byte _n = 1;
    private byte _nn;

    internal RunModeContext(int runInterruptionType, int range)
    {
        RunInterruptionType = runInterruptionType;
        _a = Algorithm.InitializationValueForA(range);
    }

    internal int RunInterruptionType { get; }

    internal readonly int GetGolombCode()
    {
        int temp = _a + ((_n >> 1) * RunInterruptionType);
        int nTest = _n;
        int k = 0;
        for (; nTest < temp; ++k)
        {
            nTest <<= 1;
            Debug.Assert(k <= 32);
        }
        return k;
    }

    internal readonly int ComputeErrorValue(int temp, int k)
    {
        bool map = (temp & 1) == 1;
        int errorValueAbs = (temp + Convert.ToInt32(map)) / 2;

        if ((k != 0 || (2 * _nn >= _n)) == map)
        {
            Debug.Assert(map == ComputeMap(-errorValueAbs, k));
            return -errorValueAbs;
        }

        Debug.Assert(map == ComputeMap(errorValueAbs, k));
        return errorValueAbs;
    }

    /// <summary>Code segment A.23 – Update of variables for run interruption sample.</summary>
    internal void UpdateVariables(int errorValue, int eMappedErrorValue, byte resetThreshold)
    {
        if (errorValue< 0)
        {
            ++_nn;
        }

        _a += (eMappedErrorValue + 1 - RunInterruptionType) >> 1;

        if (_n == resetThreshold)
        {
            _a >>= 1;
            _n = (byte)(_n >> 1);
            _nn = (byte)(_nn >> 1);
        }

        ++_n;
    }

    /// <summary>Code segment A.21 – Computation of map for error value mapping.</summary>
    internal readonly bool ComputeMap(int errorValue, int k)
    {
        if (k == 0 && errorValue > 0 && 2 * _nn < _n)
            return true;

        if (errorValue < 0 && 2 * _nn >= _n)
            return true;

        if (errorValue < 0 && k != 0)
            return true;

        return false;
    }

    internal readonly bool ComputeMapNegativeE(int k)
    {
        return k != 0 || 2 * _nn >= _n;
    }
}
