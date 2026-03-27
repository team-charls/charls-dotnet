// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using BenchmarkDotNet.Attributes;

namespace CharLS.Managed.Benchmark;

public class EncodeMonochrome
{
    private byte[]? _source;
    private byte[]? _destination;
    private PortableAnymapFile? _referenceFile;

    [Params("benchmark_8bit.pgm", "benchmark_16bit.pgm")]
    public string Filename { get; set; } = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _referenceFile = new PortableAnymapFile(Path.Combine(AppContext.BaseDirectory, Filename));

        _source = _referenceFile.ImageData;

        JpegLSEncoder encoder = new(
            _referenceFile.Width, _referenceFile.Height, _referenceFile.BitsPerSample, _referenceFile.ComponentCount);
        _destination = new byte[encoder.EstimatedDestinationSize];
    }

    [Benchmark]
    public void EncodeLossless()
    {
        JpegLSEncoder encoder = new(
            _referenceFile!.Width, _referenceFile.Height, _referenceFile.BitsPerSample, _referenceFile.ComponentCount, InterleaveMode.None, false)
        {
            Destination = _destination
        };

        encoder.Encode(_source);
    }

    [Benchmark]
    public void EncodeLossy()
    {
        JpegLSEncoder encoder = new(
            _referenceFile!.Width, _referenceFile.Height, _referenceFile.BitsPerSample, _referenceFile.ComponentCount, InterleaveMode.None, false)
        {
            Destination = _destination,
            NearLossless = 3
        };

        encoder.Encode(_source);
    }
}
