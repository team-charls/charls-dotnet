// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed;

internal static class Util
{
    internal static int CalculateMaximumSampleValue(int bitsPerSample)
    {
        return 1 << bitsPerSample - 1;
    }

    internal static int ComputeMaximumNearLossless(int maximumSampleValue)
    {
        return Math.Min(Constants.MaximumNearLossless, maximumSampleValue / 2); // As defined by ISO/IEC 14495-1, C.2.3
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

    internal static int CheckedMul(int a, int b)
    {
        return a * b;
    }
}
