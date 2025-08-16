// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace CharLS.Managed.Benchmark.Micro;

public class Abs
{
    private int[] _source = null!;
    private int[] _destination = null!;

    [Params(65536)]
    public int BufferCount { get; set; }

    [GlobalSetup]
    public void Init()
    {
        _source = new int[BufferCount];
        _destination = new int[_source.Length];
        for (int i = 0; i != BufferCount; i += 2)
        {
            _source[i] = -4000;
            _source[i + 1] = 400340;
        }
    }

    [Benchmark]
    public void AbsDefault()
    {
        for (int i = 0; i != _source.Length; i++)
        {
            _destination[i] = Math.Abs(_source[i]);
        }
    }

    [Benchmark]
    public void AbsNoOverflowCheck()
    {
        for (int i = 0; i != _source.Length; i++)
        {
            _destination[i] = VersionAbsNoOverflow(_source[i]);
        }
    }

    [Benchmark]
    public void AbsNoOverflowCheckNoCompare()
    {
        for (int i = 0; i != _source.Length; i++)
        {
            _destination[i] = VersionAbsNoOverflowNoCompare(_source[i]);
        }
    }

    [Benchmark]
    public void InRangeWithAbs()
    {
        for (int i = 0; i != _source.Length; i++)
        {
            if (VersionAbsNoOverflow(_source[i]) > 5000)
            {
                _destination[i] = _source[i];
            }
        }
    }

    [Benchmark]
    public void InRangeWithNoAbs()
    {
        for (int i = 0; i != _source.Length; i++)
        {
            if (_source[i] is < 5000 or > 5000)
            {
                _destination[i] = _source[i];
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int VersionAbsNoOverflow(int x)
    {
        return x < 0 ? -x : x;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int VersionAbsNoOverflowNoCompare(int x)
    {
        return (x ^ (x >> 31)) - (x >> 31);
    }
}
