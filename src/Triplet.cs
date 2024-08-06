// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

internal struct Triplet<TSample>
    where TSample : struct
{
    public TSample V1;
    public TSample V2;
    public TSample V3;

    internal Triplet(TSample v1, TSample v2, TSample v3)
    {
        V1 = v1;
        V2 = v2;
        V3 = v3;
    }
}
