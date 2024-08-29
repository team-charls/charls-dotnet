// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using BenchmarkDotNet.Attributes;

namespace CharLS.Managed.Benchmark;

public class EncodeMonochrome
{
    private byte[]? _source;
    private byte[]? _destination;
    private PortableAnymapFile? _referenceFile;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _referenceFile = new PortableAnymapFile("d:/benchmark-test-image.pgm");

        _source = _referenceFile.ImageData;

        var encoder = new JpegLSEncoder(
            _referenceFile.Width, _referenceFile.Height, _referenceFile.BitsPerSample, _referenceFile.ComponentCount);
        _destination = new byte[encoder.EstimatedDestinationSize];
    }

    [Benchmark]
    public void EncodeLossless()
    {
        var encoder = new JpegLSEncoder(
            _referenceFile!.Width, _referenceFile.Height, _referenceFile.BitsPerSample, _referenceFile.ComponentCount, InterleaveMode.None, false)
        {
            Destination = _destination
        };

        encoder.Encode(_source);
    }

    [Benchmark]
    public void EncodeLossy()
    {
        var encoder = new JpegLSEncoder(
            _referenceFile!.Width, _referenceFile.Height, _referenceFile.BitsPerSample, _referenceFile.ComponentCount, InterleaveMode.None, false)
        {
            Destination = _destination,
            NearLossless = 3
        };

        encoder.Encode(_source);
    }
}
