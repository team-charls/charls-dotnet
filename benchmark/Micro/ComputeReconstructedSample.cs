// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using BenchmarkDotNet.Attributes;

namespace CharLS.Managed.Benchmark.Micro;

public class ComputeReconstructedSample
{
    private Traits _traits = null!;
    private Traits _traitsLossy = null!;
    private Traits _losslessTraits = null!;
    private Traits _losslessTraits8 = null!;
    private Traits _losslessTraits16 = null!;
    private int _predictedValue;
    private int _errorValue;

    [GlobalSetup]
    public void Init()
    {
        _traits = new Traits(256, 0);
        _traitsLossy = new Traits(256, 3);
        _losslessTraits = new LosslessTraits(255);
        _losslessTraits8 = new LosslessTraits8();
        _losslessTraits16 = new LosslessTraits16();
        _predictedValue = 256;
        _errorValue = 240;
    }

    [Benchmark]
    public int ComputeReconstructedSampleTraits()
    {
        return _traits.ComputeReconstructedSample(_predictedValue, _errorValue);
    }

    [Benchmark]
    public int ComputeReconstructedSampleTraitsLossy()
    {
        return _traitsLossy.ComputeReconstructedSample(_predictedValue, _errorValue);
    }

    [Benchmark]
    public int ComputeReconstructedSampleTraitsWithLosslessCheck()
    {
        if (_traits.NearLossless == 0)
        {
            return _traits.MaximumSampleValue & (_predictedValue + _errorValue);
        }

        return _traits.ComputeReconstructedSample(_predictedValue, _errorValue);
    }

    [Benchmark]
    public int ComputeReconstructedSampleTraitsWithLosslessCheckForLossy()
    {
        if (_traitsLossy.NearLossless == 0)
        {
            return _traitsLossy.MaximumSampleValue & (_predictedValue + _errorValue);
        }

        return _traitsLossy.ComputeReconstructedSample(_predictedValue, _errorValue);
    }

    [Benchmark]
    public int ComputeReconstructedSampleLosslessTraits()
    {
        return _losslessTraits.ComputeReconstructedSample(_predictedValue, _errorValue);
    }

    [Benchmark]
    public int ComputeReconstructedSampleLosslessTraits8()
    {
        return _losslessTraits8.ComputeReconstructedSample(_predictedValue, _errorValue);
    }

    [Benchmark]
    public int ComputeReconstructedSampleLosslessTraits16()
    {
        return _losslessTraits16.ComputeReconstructedSample(_predictedValue, _errorValue);
    }
}
