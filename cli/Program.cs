// SPDX-FileCopyrightText: © 2026 Team CharLS
// SPDX-License-Identifier: BSD-3-Clause

using System.CommandLine;

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

RootCommand rootCommand = new("CharLS managed command line app")
{
    encodeCommand,
    decodeCommand
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
