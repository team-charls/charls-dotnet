// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Numerics;
using BenchmarkDotNet.Attributes;

namespace CharLS.Managed.Benchmark.Micro;

public class GolombCodingParameter
{
    private int[]? _nValues;
    private int[]? _aValues;

    [GlobalSetup]
    public void Init()
    {
        // Use as input values that will return k as [0, 1, 2, 3, 4, 5, 6]
        // Remark: this input is even distributed, real JPEG-LS images have a different distribution.
        _nValues = [3, 39, 56, 35, 53, 60, 52, 50, 35];
        _aValues = [107, 56, 124, 33, 249, 56, 478, 1259, 41];
    }

    [Benchmark]
    public int ComputeOriginalChecked()
    {
        int total = 0;

        for (int i = 0; i != _nValues!.Length; ++i)
        {
            total += ComputeGolombCodingParameterChecked(_nValues[i], _aValues![i]);
        }

        return total;
    }

    [Benchmark]
    public int ComputeOriginalCheckAfter()
    {
        int total = 0;

        for (int i = 0; i != _nValues!.Length; ++i)
        {
            total += ComputeGolombCodingParameterCheckAfter(_nValues[i], _aValues![i]);
        }

        return total;
    }

    [Benchmark]
    public int ComputeGolombCodingParameterLeadingZeroCount()
    {
        int total = 0;

        for (int i = 0; i != _nValues!.Length; ++i)
        {
            total += ComputeGolombCodingParameterLeadingZeroCount(_nValues[i], _aValues![i]);
        }

        return total;
    }

    private static int ComputeGolombCodingParameterChecked(int n, int a)
    {
        const int maxKValue = 16; // This is an implementation limit (theoretical limit is 32)

        int k = 0;
        for (; n << k < a && k < maxKValue; ++k)
        {
            // Purpose of this loop is to calculate 'k', by design no content.
        }

        if (k == maxKValue)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidData);

        return k;
    }

    private static int ComputeGolombCodingParameterCheckAfter(int n, int a)
    {
        const int maxKValue = 16;
        int k = ComputeGolombCodingParameter(n, a);
        if (k >= maxKValue)
            ThrowHelper.ThrowInvalidDataException(ErrorCode.InvalidData);

        return k;
    }

    private static int ComputeGolombCodingParameter(int n, int a)
    {
        int k = 0;
        for (; n << k < a; ++k)
        {
            // Purpose of this loop is to calculate 'k', by design no content.
        }

        return k;
    }

    private static int ComputeGolombCodingParameterLeadingZeroCount(int n, int a)
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
}
