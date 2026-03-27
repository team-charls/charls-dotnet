// SPDX-FileCopyrightText: © 2026 Team CharLS
// SPDX-License-Identifier: BSD-3-Clause

using System.CommandLine;
using System.Diagnostics;

using CharLS.Managed;
using CharLS.Managed.Support;

Argument<string> encodeInputFilenameArgument = new("input")
{
    Description = "The binary PGM (P5) or PPM (P6) file to encode to JPEG-LS (required)",
    Arity = ArgumentArity.ExactlyOne
};

Argument<string?> encodeOutputFilenameArgument = new("output")
{
    Description = "The output JPEG-LS file path. If not specified, the output file is created with the same name as the input file and a .jls extension",
    Arity = ArgumentArity.ZeroOrOne
};

Command encodeCommand = new("encode", "Encode a binary PGM (P5) or PPM (P6) file to a JPEG-LS (.jls) file")
{
    encodeInputFilenameArgument,
    encodeOutputFilenameArgument
};
encodeCommand.SetAction(parseResult => Encode(parseResult.GetValue(encodeInputFilenameArgument)!, parseResult.GetValue(encodeOutputFilenameArgument)));

Argument<string> decodeInputFilenameArgument = new("input")
{
    Description = "The JPEG-LS (.jls) file to decode to a binary PGM (P5) or PPM (P6) file (required)",
    Arity = ArgumentArity.ExactlyOne
};

Argument<string?> decodeOutputFilenameArgument = new("output")
{
    Description = "The output Netpbm file path. If not specified, the output file is created with the same name as the input file and a .pgm or .ppm extension",
    Arity = ArgumentArity.ZeroOrOne
};

Command decodeCommand = new("decode", "Decode a JPEG-LS (.jls) file to a binary PGM (P5) or PPM (P6) file")
{
    decodeInputFilenameArgument,
    decodeOutputFilenameArgument
};
decodeCommand.SetAction(parseResult => Decode(parseResult.GetValue(decodeInputFilenameArgument)!, parseResult.GetValue(decodeOutputFilenameArgument)));

Argument<string> benchmarkInputFilenameArgument = new("input")
{
    Description = "The JPEG-LS (.jls) file to benchmark decode performance (required)",
    Arity = ArgumentArity.ExactlyOne
};

var benchmarkIterationsOption = new Option<int>("--iterations")
{
    Description = "Number of times to decode for benchmarking (default: 10)",
    Arity = ArgumentArity.ZeroOrOne
};
var benchmarkIterationsShortOption = new Option<int>("-n")
{
    Arity = ArgumentArity.ZeroOrOne
};

Command benchmarkCommand = new("benchmark", "Benchmark JPEG-LS decode performance")
{
    benchmarkInputFilenameArgument,
    benchmarkIterationsOption,
    benchmarkIterationsShortOption
};
benchmarkCommand.SetAction(parseResult => Benchmark(
    parseResult.GetValue(benchmarkInputFilenameArgument)!,
    parseResult.GetValue(benchmarkIterationsOption) != 0 ? parseResult.GetValue(benchmarkIterationsOption) : (parseResult.GetValue(benchmarkIterationsShortOption) != 0 ? parseResult.GetValue(benchmarkIterationsShortOption) : 10)));

RootCommand rootCommand = new("CharLS managed command line app")
{
    encodeCommand,
    decodeCommand,
    benchmarkCommand
};

return rootCommand.Parse(args).Invoke();


static int Encode(string inputFilename, string? outputFilename)
{
    if (!File.Exists(inputFilename))
    {
        Console.Error.WriteLine($"Error: file not found: {inputFilename}");
        return 1;
    }

    try
    {
        PortableAnymapFile anymap = new(inputFilename);

        var interleaveMode = anymap.ComponentCount == 1 ? InterleaveMode.None : InterleaveMode.Sample;
        JpegLSEncoder encoder = new(anymap.Width, anymap.Height, anymap.BitsPerSample, anymap.ComponentCount, interleaveMode);

        encoder.WriteStandardSpiffHeader(anymap.ComponentCount == 1 ? SpiffColorSpace.Grayscale : SpiffColorSpace.Rgb);
        encoder.Encode(anymap.ImageData);

        string outputPath = outputFilename ?? Path.ChangeExtension(inputFilename, ".jls");
        using FileStream output = new(outputPath, FileMode.Create, FileAccess.Write);
        output.Write(encoder.EncodedData.Span);

        Console.WriteLine($"Encoded {Path.GetFileName(inputFilename)} -> {Path.GetFileName(outputPath)}");
        return 0;
    }
    catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException)
    {
        Console.Error.WriteLine($"Error: {e.Message}");
        return 1;
    }
}


static int Decode(string inputFilename, string? outputFilename)
{
    if (!File.Exists(inputFilename))
    {
        Console.Error.WriteLine($"Error: file not found: {inputFilename}");
        return 1;
    }

    try
    {
        byte[] encodedData = File.ReadAllBytes(inputFilename);
        JpegLSDecoder decoder = new(encodedData);

        FrameInfo frameInfo = decoder.FrameInfo;
        byte[] decodedData = decoder.Decode();

        string outputPath = outputFilename ?? Path.ChangeExtension(inputFilename, frameInfo.ComponentCount == 1 ? ".pgm" : ".ppm");
        PortableAnymapFile.Write(outputPath, frameInfo.Width, frameInfo.Height, frameInfo.BitsPerSample, frameInfo.ComponentCount, decodedData);

        Console.WriteLine($"Decoded {Path.GetFileName(inputFilename)} -> {Path.GetFileName(outputPath)}");
        return 0;
    }
    catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException)
    {
        Console.Error.WriteLine($"Error: {e.Message}");
        return 1;
    }
}


static int Benchmark(string inputFilename, int iterations)
{
    if (!File.Exists(inputFilename))
    {
        Console.Error.WriteLine($"Error: file not found: {inputFilename}");
        return 1;
    }

    try
    {
        byte[] encodedData = File.ReadAllBytes(inputFilename);
        JpegLSDecoder decoder = new(encodedData);

        // Warm-up
        int size = decoder.GetDestinationSize();
        byte[] destination = new byte[size];
        decoder.Decode(destination);

        var stopwatch = new Stopwatch();
        long totalTicks = 0;
        for (int i = 0; i < iterations; ++i)
        {
            stopwatch.Restart();
            decoder = new JpegLSDecoder(encodedData); // ensure fresh state
            decoder.Decode(destination);
            stopwatch.Stop();
            totalTicks += stopwatch.ElapsedTicks;
        }

        double avgMs = totalTicks * 1000.0 / iterations / Stopwatch.Frequency;
        Console.WriteLine($"Decoded {Path.GetFileName(inputFilename)} {iterations} times. Average: {avgMs:F3} ms");

        return 0;
    }
    catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException)
    {
        Console.Error.WriteLine($"Error: {e.Message}");
        return 1;
    }
}
