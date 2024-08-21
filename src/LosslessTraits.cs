// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed;

internal class LosslessTraits : Traits
{
    internal LosslessTraits(int maximumSampleValue, int nearLossless, int resetThreshold)
        : base(maximumSampleValue, nearLossless, resetThreshold)
    {
    }

    internal override int ComputeErrorValue(int errorValue)
    {
        return ModuloRange(errorValue);
    }

    internal override int ComputeReconstructedSample(int predictedValue, int errorValue)
    {
        return MaximumSampleValue & (predictedValue + errorValue);
    }

    internal override int ModuloRange(int errorValue)
    {
        return (errorValue << (Constants.Int32BitCount - BitsPerSample)) >> (Constants.Int32BitCount - BitsPerSample);
    }

    internal sealed override bool IsNear(int lhs, int rhs)
    {
        return lhs == rhs;
    }

    internal sealed override bool IsNear(Triplet<byte> lhs, Triplet<byte> rhs)
    {
        return lhs.V1 == rhs.V1 && lhs.V2 == rhs.V2 && lhs.V3 == rhs.V3;
    }

    internal sealed override bool IsNear(Triplet<ushort> lhs, Triplet<ushort> rhs)
    {
        return lhs.V1 == rhs.V1 && lhs.V2 == rhs.V2 && lhs.V3 == rhs.V3;
    }

    internal sealed override bool IsNear(Quad<byte> lhs, Quad<byte> rhs)
    {
        return lhs.V1 == rhs.V1 && lhs.V2 == rhs.V2 && lhs.V3 == rhs.V3 && lhs.V4 == rhs.V4;
    }

    internal sealed override bool IsNear(Quad<ushort> lhs, Quad<ushort> rhs)
    {
        return lhs.V1 == rhs.V1 && lhs.V2 == rhs.V2 && lhs.V3 == rhs.V3 && lhs.V4 == rhs.V4;
    }
}
