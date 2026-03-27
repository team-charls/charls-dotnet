// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using BenchmarkDotNet.Running;

namespace CharLS.Managed.Benchmark;

public static class Program
{
    public static void Main(string[] args)
    {
        _ = BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(args);
    }
}
