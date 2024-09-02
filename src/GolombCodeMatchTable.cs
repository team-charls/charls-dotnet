// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using static CharLS.Managed.Algorithm;

namespace CharLS.Managed;

internal sealed class GolombCodeMatchTable
{
    private const int ByteBitCount = 8;
    private readonly GolombCodeMatch[] _matches = new GolombCodeMatch[1 << ByteBitCount];

    internal GolombCodeMatchTable(int k)
    {
        for (short errorValue = 0; ; ++errorValue)
        {
            // Q is not used when k != 0
            int mappedErrorValue = MapErrorValue(errorValue);
            (int codeLength, int tableValue) = CreateEncodedValue(k, mappedErrorValue);
            if (codeLength > ByteBitCount)
                break;

            var code = new GolombCodeMatch(errorValue, codeLength);
            AddEntry(tableValue, code);
        }

        for (short errorValue = -1; ; --errorValue)
        {
            // Q is not used when k != 0
            int mappedErrorValue = MapErrorValue(errorValue);
            (int codeLength, int tableValue) = CreateEncodedValue(k, mappedErrorValue);
            if (codeLength > ByteBitCount)
                break;

            var code = new GolombCodeMatch(errorValue, codeLength);
            AddEntry(tableValue, code);
        }
    }

    internal GolombCodeMatch Get(int value)
    {
        return _matches[value];
    }

    private void AddEntry(int value, GolombCodeMatch codeMatch)
    {
        int length = codeMatch.BitCount;
        Debug.Assert(length <= ByteBitCount);

        for (int i = 0; i < 1U << (ByteBitCount - length); ++i)
        {
            Debug.Assert(_matches[(value << (ByteBitCount - length)) + i].BitCount == 0);
            _matches[(value << (ByteBitCount - length)) + i] = codeMatch;
        }
    }

    private static (int CodeLenght, int TableValue) CreateEncodedValue(int k, int mappedError)
    {
        int highBits = mappedError >> k;
        return (highBits + k + 1, (1 << k) | (mappedError & ((1 << k) - 1)));
    }
}
