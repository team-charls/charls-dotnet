// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed;

internal sealed class LosslessTraits16(int maximumSampleValue, int nearLossless, int resetThreshold)
    : LosslessTraits(maximumSampleValue, nearLossless, resetThreshold)
{
    internal override int ComputeErrorValue(int errorValue)
    {
        return (short)errorValue;
    }

    internal override int ComputeReconstructedSample(int predictedValue, int errorValue)
    {
        return predictedValue + errorValue;
    }

    internal override int ModuloRange(int errorValue)
    {
        return (short)errorValue;
    }
}
