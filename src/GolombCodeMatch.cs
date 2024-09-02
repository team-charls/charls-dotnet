// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed;

internal readonly struct GolombCodeMatch
{
    internal GolombCodeMatch(int errorValue, int bitCount)
    {
        ErrorValue = errorValue;
        BitCount = bitCount;
    }

    internal int ErrorValue { get; }

    internal int BitCount { get; }
}
