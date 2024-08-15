// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

namespace CharLS.Managed.Benchmark.Micro;

public class BlockCopy
{
    private byte[] _source = default!;
    private byte[] _destination = default!;

    [Params(65536)]
    public int BufferCount { get; set; }

    [GlobalSetup]
    public void Init()
    {
        _source = new byte[BufferCount];
        _source.AsSpan().Fill(99);
        _destination = new byte[_source.Length];
    }

    [Benchmark]
    public void MemoryCopy()
    {
        // Span CopyTo using Buffer.MemoryCopy
        _source.AsSpan().CopyTo(_destination);
    }

    [Benchmark]
    public void CopyBlockUnaligned()
    {
        Unsafe.CopyBlockUnaligned(ref MemoryMarshal.GetArrayDataReference(_destination),
            ref MemoryMarshal.GetArrayDataReference(_source), (uint)_source.Length);
    }
}
