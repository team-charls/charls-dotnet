// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed;

internal struct Pair<T>
    where T : struct
{
    internal T V1;
    internal T V2;

    internal Pair(T v1, T v2)
    {
        V1 = v1;
        V2 = v2;
    }
}
