// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed;

internal struct GolombCode
{
    private int _value;
    private int _length;

    internal GolombCode(int value, int length)
    {
        _value = value;
        _length = length;
    }

    internal readonly int Value => _value;
    internal readonly int Length => _length;
}
