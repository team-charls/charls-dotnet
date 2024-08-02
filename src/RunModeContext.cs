// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Diagnostics;

namespace CharLS.JpegLS;

internal struct RunModeContext
{
    // Initialize with the default values as defined in ISO 14495-1, A.8, step 1.d and 1.f.
    internal int RunInterruptionType;
    internal int A;
    internal byte N = 1;
    internal byte Nn;

    internal RunModeContext(int runInterruptionType, int range)
    {
        RunInterruptionType = runInterruptionType;
        A = Algorithm.InitializationValueForA(range);
    }

    internal int GetGolombCode()
    {
        int temp = A + (N >> 1) * RunInterruptionType;
        int nTest = N;
        int k = 0;
        for (; nTest < temp; ++k)
        {
            nTest <<= 1;
            Debug.Assert(k <= 32);
        }
        return k;
    }

    internal int ComputeErrorValue(int temp, int k)
    {
        bool map = (temp & 1) == 1;
        int errorValueAbs = (temp + Convert.ToInt32(map)) / 2;

        if ((k != 0 || (2 * Nn >= N)) == map)
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
            ++Nn;
        }

        A += (eMappedErrorValue + 1 - RunInterruptionType) >> 1;

        if (N == resetThreshold)
        {
            A >>= 1;
            N = (byte)(N >> 1);
            Nn = (byte)(Nn >> 1);
        }

        ++N;
    }

    /// <summary>Code segment A.21 – Computation of map for Errval mapping.</summary>
    private bool ComputeMap(int errorValue, int k)
    {
        if (k == 0 && errorValue > 0 && 2 * Nn < N)
            return true;

        if (errorValue < 0 && 2 * Nn >= N)
            return true;

        if (errorValue < 0 && k != 0)
            return true;

        return false;
    }

    internal bool ComputeMapNegativeE(int k)
    {
        return k != 0 || 2 * Nn >= N;
    }
}
