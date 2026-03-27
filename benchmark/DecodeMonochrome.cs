// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using BenchmarkDotNet.Attributes;

namespace CharLS.Managed.Benchmark;

public class DecodeMonochrome
{
    private byte[] _sourceLossless = [];
    private byte[] _sourceLossy = [];
    private byte[] _destination = [];

    [Params("benchmark_8bit.pgm", "benchmark_16bit.pgm")]
    public string Filename { get; set; } = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        PortableAnymapFile referenceFile = new(Path.Combine(AppContext.BaseDirectory, Filename));

        // Create lossless JPEG-LS encoded data from the reference file.
        JpegLSEncoder encoder = new(
            referenceFile.Width, referenceFile.Height, referenceFile.BitsPerSample, referenceFile.ComponentCount, InterleaveMode.None, false);
        encoder.Destination = new byte[encoder.EstimatedDestinationSize];
        encoder.Encode(referenceFile.ImageData);
        _sourceLossless = encoder.EncodedData.ToArray();

        // Create lossy JPEG-LS encoded data from the reference file.
        encoder = new(
            referenceFile.Width, referenceFile.Height, referenceFile.BitsPerSample, referenceFile.ComponentCount, InterleaveMode.None, false);
        encoder.Destination = new byte[encoder.EstimatedDestinationSize];
        encoder.NearLossless = 3;
        encoder.Encode(referenceFile.ImageData);
        _sourceLossy = encoder.EncodedData.ToArray();

        JpegLSDecoder decoder = new(_sourceLossless);
        _destination = new byte[decoder.GetDestinationSize()];
    }

    [Benchmark]
    public void DecodeLossless()
    {
        JpegLSDecoder decoder = new(_sourceLossless);

        decoder.Decode(_destination);
    }

    [Benchmark]
    public void DecodeLossy()
    {
        JpegLSDecoder decoder = new(_sourceLossy);

        decoder.Decode(_destination);
    }
}
