// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed;

internal static class Validation
{
    internal static bool IsBitsPerSampleValid(int bitsPerSample)
    {
        return bitsPerSample is >= Constants.MinimumBitsPerSample and <= Constants.MaximumBitsPerSample;
    }

    internal static bool IsValid(this InterleaveMode interleaveMode)
    {
        // More efficient than Enum.IsDefined as it doesn't use reflection.
        return interleaveMode is >= InterleaveMode.None and <= InterleaveMode.Sample;
    }

    internal static bool IsValid(this ColorTransformation colorTransformation)
    {
        // More efficient than Enum.IsDefined as it doesn't use reflection.
        return colorTransformation is >= ColorTransformation.None and <= ColorTransformation.HP3;
    }

    internal static bool IsValid(this EncodingOptions encodingOptions)
    {
        // More efficient than Enum.IsDefined as it doesn't use reflection.
        return encodingOptions is >= EncodingOptions.None and <=
            (EncodingOptions.EvenDestinationSize | EncodingOptions.IncludeVersionNumber | EncodingOptions.IncludePCParametersJai);
    }
}
