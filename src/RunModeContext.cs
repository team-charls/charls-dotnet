// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

internal struct RunModeContext
{
    internal int RunInterruptionType;
    internal int A;
    internal byte N;
    internal byte Nn;

    internal RunModeContext(int runInterruptionType, int range)
    {
        RunInterruptionType = runInterruptionType;
        A = Algorithm.InitializationValueForA(range);
    }

    internal bool ComputeMapNegativeE(int k)
    {
        return k != 0 || 2 * Nn >= N;
    }
}
