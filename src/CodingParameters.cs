// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed;

internal readonly record struct CodingParameters
{
    public int NearLossless { get; init; }
    public int RestartInterval { get; init; }
    public InterleaveMode InterleaveMode { get; init; }
    public ColorTransformation ColorTransformation { get; init; }
}
