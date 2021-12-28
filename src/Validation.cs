// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS;

internal static class Validation
{
    internal static bool IsBitsPerSampleValid(int bitsPerSample)
    {
        const int minimum_bits_per_sample = 2;
        const int maximum_bits_per_sample = 16;

        return bitsPerSample is >= minimum_bits_per_sample and <= maximum_bits_per_sample;
    }
}
