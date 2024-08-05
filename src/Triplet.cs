// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

internal struct Triplet<TSample>
    where TSample : struct
{
    public TSample V1;
    public TSample V2;
    public TSample V3;
}
