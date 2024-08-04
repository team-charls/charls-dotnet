// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System;

namespace CharLS.JpegLS;

internal interface ITraits<out TSample, in TPixel>
    where TSample : struct
{
    int Range { get; }

    bool IsNear(int lhs, int rhs);

    bool IsNear(TPixel lhs, TPixel rhs);

    int QuantizationRange { get; }

    int NearLossless { get; }
}
