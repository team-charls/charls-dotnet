// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed;

internal struct Quad<T>
    where T : struct
{
    internal T V1;
    internal T V2;
    internal T V3;
    internal T V4;

    internal Quad(T v1, T v2, T v3, T v4)
    {
        V1 = v1;
        V2 = v2;
        V3 = v3;
        V4 = v4;
    }
}
