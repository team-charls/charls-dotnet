// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed;

internal readonly struct GolombCode
{
    internal GolombCode(int value, int length)
    {
        Value = value;
        Length = length;
    }

    internal int Value { get; }

    internal int Length { get; }
}
