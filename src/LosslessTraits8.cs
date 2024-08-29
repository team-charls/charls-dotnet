// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed;

internal sealed class LosslessTraits8(int maximumSampleValue, int nearLossless)
    : LosslessTraits(maximumSampleValue, nearLossless)
{
    internal override int ComputeErrorValue(int errorValue)
    {
        return (sbyte)errorValue;
    }

    internal override int ComputeReconstructedSample(int predictedValue, int errorValue)
    {
        return (byte)(predictedValue + errorValue);
    }

    internal override int ModuloRange(int errorValue)
    {
        return (sbyte)errorValue;
    }
}
