// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed;

internal static class Validation
{
    internal static bool IsBitsPerSampleValid(int bitsPerSample)
    {
        const int minimumBitsPerSample = 2;
        const int maximumBitsPerSample = 16;

        return bitsPerSample is >= minimumBitsPerSample and <= maximumBitsPerSample;
    }
}
