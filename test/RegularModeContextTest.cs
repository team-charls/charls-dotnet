// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Numerics;

namespace CharLS.Managed.Test;

public class RegularModeContextTest
{
    [Fact]
    public void ComputeGolombCodingParameterAlgorithm()
    {
        Assert.Equal(ComputeGolombCodingParameterLocal(2, 5), ComputeGolombCodingParameterLocalLeadingZeroCount(2,5));
        Assert.Equal(ComputeGolombCodingParameterLocal(3, 5), ComputeGolombCodingParameterLocalLeadingZeroCount(3, 5));
        Assert.Equal(ComputeGolombCodingParameterLocal(16, 6), ComputeGolombCodingParameterLocalLeadingZeroCount(16, 6));
    }

    private static int ComputeGolombCodingParameterLocalLeadingZeroCount(int n, int a)
    {
        int nZeroCount = BitOperations.LeadingZeroCount((uint)n);
        int aZeroCount = BitOperations.LeadingZeroCount((uint)a);
        int k = nZeroCount - aZeroCount;
        if (k < 0)
            return 0;

        if ((n << k) < a)
        {
            ++k;
        }

        const int maxKValue = 16;
        if (k >= maxKValue)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidData);

        return k;
    }

    private static int ComputeGolombCodingParameterLocal(int n, int a)
    {
        int k = 0;
        for (; (n << k) < a; ++k)
        {
            // Purpose of this loop is to calculate 'k', by design no content.
        }

        const int maxKValue = 16;
        if (k >= maxKValue)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidData);
        return k;
    }
}
