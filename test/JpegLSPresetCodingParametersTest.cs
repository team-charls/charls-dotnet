// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed.Test;


internal struct Thresholds
{
    internal int MaxValue;
    internal int T1;
    internal int T2;
    internal int T3;
    internal int Reset;
};


internal sealed class JpegLSPresetCodingParametersTest
{
    // Threshold function of JPEG-LS reference implementation.
    internal static Thresholds ComputeDefaultsUsingReferenceImplementation(int maxValue, ushort near)
    {
        Thresholds result = new Thresholds { MaxValue = maxValue, Reset = 64 };

        if (result.MaxValue >= 128)
        {
            int factor = result.MaxValue;
            if (factor > 4095)
                factor = 4095;
            factor = (factor + 128) >> 8;
            result.T1 = factor * (3 - 2) + 2 + 3 * near;
            if (result.T1 > result.MaxValue || result.T1 < near + 1)
                result.T1 = near + 1;
            result.T2 = factor * (7 - 3) + 3 + 5 * near;
            if (result.T2 > result.MaxValue || result.T2 < result.T1)
                result.T2 = result.T1;
            result.T3 = factor * (21 - 4) + 4 + 7 * near;
            if (result.T3 > result.MaxValue || result.T3 < result.T2)
                result.T3 = result.T2;
        }
        else
        {
            int factor = 256 / (result.MaxValue + 1);
            result.T1 = 3 / factor + 3 * near;
            if (result.T1 < 2)
                result.T1 = 2;
            if (result.T1 > result.MaxValue || result.T1 < near + 1)
                result.T1 = near + 1;
            result.T2 = 7 / factor + 5 * near;
            if (result.T2 < 3)
                result.T2 = 3;
            if (result.T2 > result.MaxValue || result.T2 < result.T1)
                result.T2 = result.T1;
            result.T3 = 21 / factor + 7 * near;
            if (result.T3 < 4)
                result.T3 = 4;
            if (result.T3 > result.MaxValue || result.T3 < result.T2)
                result.T3 = result.T2;
        }

        return result;
    }
}
