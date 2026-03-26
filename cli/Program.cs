// SPDX-FileCopyrightText: © 2026 Team CharLS
// SPDX-License-Identifier: BSD-3-Clause

using System.CommandLine;

using CharLS.Managed;
using CharLS.Managed.Support;

Argument<string> fileArgument = new("file")
{
    Description = "The binary PGM (P5) or PPM (P6) file to encode to JPEG-LS"
};

Command encodeCommand = new("encode", "Encode a binary PGM (P5) or PPM (P6) file to a JPEG-LS (.jls) file")
{
    fileArgument
};
encodeCommand.SetAction(parseResult => Encode(parseResult.GetValue(fileArgument)!));

RootCommand rootCommand = new("CharLS managed command line app")
{
    encodeCommand
};

return rootCommand.Parse(args).Invoke();


static int Encode(string filename)
{
    if (!File.Exists(filename))
    {
        Console.Error.WriteLine($"Error: file not found: {filename}");
        return 1;
    }

    try
    {
        PortableAnymapFile anymap = new(filename);

        var interleaveMode = anymap.ComponentCount == 1 ? InterleaveMode.None : InterleaveMode.Sample;
        JpegLSEncoder encoder = new(anymap.Width, anymap.Height, anymap.BitsPerSample, anymap.ComponentCount, interleaveMode);

        encoder.WriteStandardSpiffHeader(anymap.ComponentCount == 1 ? SpiffColorSpace.Grayscale : SpiffColorSpace.Rgb);
        encoder.Encode(anymap.ImageData);

        string outputPath = Path.ChangeExtension(filename, ".jls");
        using FileStream output = new(outputPath, FileMode.Create, FileAccess.Write);
        output.Write(encoder.EncodedData.Span);

        Console.WriteLine($"Encoded {Path.GetFileName(filename)} -> {Path.GetFileName(outputPath)}");
        return 0;
    }
    catch (Exception e) when (e is IOException or UnauthorizedAccessException)   
    {
        Console.Error.WriteLine($"Error: {e.Message}");
        return 1;
    }
}
