// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

internal sealed record CodingParameters
{
    public int NearLossless { get; init; }
    public int RestartInterval { get; init; }
    public InterleaveMode InterleaveMode { get; init; }
}
