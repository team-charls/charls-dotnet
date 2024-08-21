// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using static CharLS.Managed.Algorithm;

namespace CharLS.Managed;

internal sealed class GolombCodeTable
{
    private const int ByteBitCount = 8;
    private readonly GolombCode[] _types = new GolombCode[1 << ByteBitCount];

    internal void AddEntry(int value, GolombCode code)
    {
        int length = code.Length;
        Debug.Assert(length <= ByteBitCount);

        for (int i = 0; i < 1U << (ByteBitCount - length); ++i)
        {
            Debug.Assert(_types[((value) << (ByteBitCount - length)) + i].Length == 0);
            _types[(value << (ByteBitCount - length)) + i] = code;
        }
    }

    internal GolombCode Get(int value)
    {
        return _types[value];
    }

    internal static GolombCodeTable Create(int k)
    {
        GolombCodeTable table = new();

        for (short errorValue = 0;; ++errorValue)
        {
            // Q is not used when k != 0
            int mappedErrorValue = MapErrorValue(errorValue);
            (int codeLength, int tableValue) = CreateEncodedValue(k, mappedErrorValue);
            if (codeLength > ByteBitCount)
                break;

            var code = new GolombCode(errorValue, codeLength);
            table.AddEntry(tableValue, code);
        }

        for (short errorValue = -1; ; --errorValue)
        {
            // Q is not used when k != 0
            int mappedErrorValue = MapErrorValue(errorValue);
            (int codeLength, int tableValue) = CreateEncodedValue(k, mappedErrorValue);
            if (codeLength > ByteBitCount)
                break;

            var code = new GolombCode(errorValue, codeLength);
            table.AddEntry(tableValue, code);
        }

        return table;
    }

    private static ValueTuple<int, int> CreateEncodedValue(int k, int mappedError)
    {
        int highBits = mappedError >> k;
        return new ValueTuple<int, int>(highBits + k + 1, (1 << k) | (mappedError & ((1 << k) - 1)));
    }
}
