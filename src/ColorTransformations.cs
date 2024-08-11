// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

internal static class ColorTransformations
{
    internal static bool IsPossible(FrameInfo frameInfo)
    {
        return frameInfo is { ComponentCount: 3, BitsPerSample: 8 or 16 };
    }

    internal static Triplet<byte> TransformHP1(byte red, byte green, byte blue)
    {
        const int range = 1 << (sizeof(byte) * 8);

        return new Triplet<byte>(
            (byte)(red - green + range / 2),
            green,
            (byte)(blue - green + range / 2));
    }

    internal static Triplet<byte> ReverseTransformHP1(byte v1, byte v2, byte v3)
    {
        const int range = 1 << (sizeof(byte) * 8);

        return new Triplet<byte>(
            (byte)(v1 + v2 - range / 2),
            v2,
            (byte)(v3 + v2 - range / 2));
    }

    internal static Triplet<byte> TransformHP2(byte red, byte green, byte blue)
    {
        const int range = 1 << (sizeof(byte) * 8);

        return new Triplet<byte>(
            (byte)(red - green + range / 2),
            green,
            (byte)(blue - ((red + green) >> 1) - range / 2));
    }

    internal static Triplet<byte> ReverseTransformHP2(byte v1, byte v2, byte v3)
    {
        const int range = 1 << (sizeof(byte) * 8);

        var r = (byte)(v1 + v2 - range / 2);
        return new Triplet<byte>(
            r,
            v2,
            (byte)(v3 + ((r + (v2)) >> 1) - range / 2));
    }

    internal static Triplet<byte> TransformHP3(byte red, byte green, byte blue)
    {
        const int range = 1 << (sizeof(byte) * 8);

        var v2 = (byte)(blue - green + range / 2);
        var v3 = (byte)(red - green + range / 2);

        return new Triplet<byte>(
            (byte)(green + ((v2 + v3) >> 2) - range / 4),
            (byte)(blue - green + range / 2),
            (byte)(red - green + range / 2));
    }

    internal static Triplet<byte> ReverseTransformHP3(byte v1, byte v2, byte v3)
    {
        const int range = 1 << (sizeof(byte) * 8);

        var g = (v1 - ((v3 + v2) >> 2) + range / 4);

        return new Triplet<byte>(
         (byte)(v3 + g - range / 2),
         (byte)(g),
         (byte)(v2 + g - range / 2));
    }
}
