// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace CharLS.Managed.Benchmark;

public class JpegLSDecoders
{
    private readonly byte[] _source;
    private readonly byte[] _destination;

    public JpegLSDecoders()
    {
        _source = File.ReadAllBytes("d:/benchmark-test-image.jls");

        var decoder = new JpegLSDecoder(_source);
        _destination = new byte[decoder.GetDestinationSize()];
    }

    [Benchmark]
    public byte[] Decode()
    {
        var decoder = new JpegLSDecoder(_source);

        decoder.Decode(_destination);

        return _destination;
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        _ = BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(args);
    }
}
