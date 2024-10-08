// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Runtime.InteropServices;

namespace CharLS.Managed;

internal class CopyFromLineBuffer
{
    internal delegate void Method(Span<byte> source, Span<byte> destination, int pixelCount);

    internal static Method GetCopyMethod(
        int bitsPerSample,
        InterleaveMode interleaveMode,
        int componentCount,
        ColorTransformation colorTransformation)
    {
        return bitsPerSample <= 8
            ? GetMethod8Bit(interleaveMode, componentCount, colorTransformation)
            : GetMethod16Bit(interleaveMode, componentCount, colorTransformation);
    }

    private static Method GetMethod8Bit(InterleaveMode interleaveMode, int componentCount, ColorTransformation colorTransformation)
    {
        switch (interleaveMode)
        {
            case InterleaveMode.None:
                return NotUsed;

            case InterleaveMode.Line:
                switch (componentCount)
                {
                    case 2:
                        return CopyLine8Bit2Components;

                    case 3:
                        switch (colorTransformation)
                        {
                            default:
                                Debug.Assert(colorTransformation == ColorTransformation.None);
                                return CopyLine8Bit3Components;
                            case ColorTransformation.HP1:
                                return CopyLine8Bit3ComponentsHP1;
                            case ColorTransformation.HP2:
                                return CopyLine8Bit3ComponentsHP2;
                            case ColorTransformation.HP3:
                                return CopyLine8Bit3ComponentsHP3;
                        }

                    default:
                        Debug.Assert(componentCount == 4);
                        return CopyToLine8Bit4Components;
                }

            default:
                Debug.Assert(interleaveMode == InterleaveMode.Sample);
                switch (componentCount)
                {
                    case 2:
                        return CopyPixels8Bit2Components;

                    case 3:
                        switch (colorTransformation)
                        {
                            default:
                                Debug.Assert(colorTransformation == ColorTransformation.None);
                                return CopyPixels8Bit3Components;
                            case ColorTransformation.HP1:
                                return CopyPixels8BitHP1;
                            case ColorTransformation.HP2:
                                return CopyPixels8BitHP2;
                            case ColorTransformation.HP3:
                                return CopyPixels8BitHP3;
                        }

                    default:
                        Debug.Assert(componentCount == 4);
                        return CopyPixels8Bit4Components;
                }
        }
    }

    private static Method GetMethod16Bit(InterleaveMode interleaveMode, int componentCount, ColorTransformation colorTransformation)
    {
        switch (interleaveMode)
        {
            case InterleaveMode.None:
                return NotUsed;

            case InterleaveMode.Line:
                switch (componentCount)
                {
                    case 2:
                        return CopyLine16Bit2Components;

                    case 3:
                        switch (colorTransformation)
                        {
                            default:
                                Debug.Assert(colorTransformation == ColorTransformation.None);
                                return CopyLine16Bit3Components;
                            case ColorTransformation.HP1:
                                return CopyLine16Bit3ComponentsHP1;
                            case ColorTransformation.HP2:
                                return CopyLine16Bit3ComponentsHP2;
                            case ColorTransformation.HP3:
                                return CopyLine16Bit3ComponentsHP3;
                        }

                    default:
                        Debug.Assert(componentCount == 4);
                        return CopyLine16Bit4Components;
                }

            default:
                Debug.Assert(interleaveMode == InterleaveMode.Sample);
                switch (componentCount)
                {
                    case 2:
                        return CopyPixels16Bit2Components;

                    case 3:
                        switch (colorTransformation)
                        {
                            default:
                                Debug.Assert(colorTransformation == ColorTransformation.None);
                                return CopyPixels16Bit3Components;
                            case ColorTransformation.HP1:
                                return CopyPixels16BitHP1;
                            case ColorTransformation.HP2:
                                return CopyPixels16BitHP2;
                            case ColorTransformation.HP3:
                                return CopyPixels16BitHP3;
                        }

                    default:
                        Debug.Assert(componentCount == 4);
                        return CopyPixels16Bit4Components;
                }
        }
    }

    private static void NotUsed(Span<byte> source, Span<byte> destination, int pixelCount)
    {
    }

    private static void CopyLine8Bit2Components(Span<byte> source, Span<byte> destination, int pixelCount)
    {
        var destinationTriplet = MemoryMarshal.Cast<byte, Pair<byte>>(destination);
        int pixelStride = PixelCountToPixelStride(pixelCount);

        for (int i = 0; i < pixelCount; ++i)
        {
            destinationTriplet[i] = new Pair<byte>(source[i], source[i + pixelStride]);
        }
    }

    private static void CopyLine8Bit3Components(Span<byte> source, Span<byte> destination, int pixelCount)
    {
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(destination);
        int pixelStride = PixelCountToPixelStride(pixelCount);

        for (int i = 0; i < pixelCount; ++i)
        {
            destinationTriplet[i] = new Triplet<byte>(
                source[i], source[i + pixelStride], source[i + (2 * pixelStride)]);
        }
    }

    private static void CopyLine8Bit3ComponentsHP1(Span<byte> source, Span<byte> destination, int pixelCount)
    {
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(destination);
        int pixelStride = PixelCountToPixelStride(pixelCount);

        for (int i = 0; i < pixelCount; ++i)
        {
            destinationTriplet[i] = ColorTransformations.ReverseTransformHP1(
                    source[i], source[i + pixelStride], source[i + (2 * pixelStride)]);
        }
    }

    private static void CopyLine8Bit3ComponentsHP2(Span<byte> source, Span<byte> destination, int pixelCount)
    {
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(destination);
        int pixelStride = PixelCountToPixelStride(pixelCount);

        for (int i = 0; i < pixelCount; ++i)
        {
            destinationTriplet[i] = ColorTransformations.ReverseTransformHP2(
                    source[i], source[i + pixelStride], source[i + (2 * pixelStride)]);
        }
    }

    private static void CopyLine8Bit3ComponentsHP3(Span<byte> source, Span<byte> destination, int pixelCount)
    {
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(destination);
        int pixelStride = PixelCountToPixelStride(pixelCount);

        for (int i = 0; i < pixelCount; ++i)
        {
            destinationTriplet[i] = ColorTransformations.ReverseTransformHP3(
                source[i], source[i + pixelStride], source[i + (2 * pixelStride)]);
        }
    }

    private static void CopyToLine8Bit4Components(Span<byte> source, Span<byte> destination, int pixelCount)
    {
        var destinationQuad = MemoryMarshal.Cast<byte, Quad<byte>>(destination);
        int pixelStride = PixelCountToPixelStride(pixelCount);

        for (int i = 0; i < pixelCount; ++i)
        {
            destinationQuad[i] = new Quad<byte>(
                source[i], source[i + pixelStride], source[i + (2 * pixelStride)], source[i + (3 * pixelStride)]);
        }
    }

    private static void CopyPixels8Bit2Components(Span<byte> source, Span<byte> destination, int pixelCount)
    {
        int bytesCount = pixelCount * 2;
        source[..bytesCount].CopyTo(destination);
    }

    private static void CopyPixels8Bit3Components(Span<byte> source, Span<byte> destination, int pixelCount)
    {
        int bytesCount = pixelCount * 3;
        source[..bytesCount].CopyTo(destination);
    }

    private static void CopyPixels8Bit4Components(Span<byte> source, Span<byte> destination, int pixelCount)
    {
        int bytesCount = pixelCount * 4;
        source[..bytesCount].CopyTo(destination);
    }

    private static void CopyPixels8BitHP1(Span<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(destination);

        for (int i = 0; i != pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = ColorTransformations.ReverseTransformHP1(pixel.V1, pixel.V2, pixel.V3);
        }
    }

    private static void CopyPixels8BitHP2(Span<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(destination);

        for (int i = 0; i != pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = ColorTransformations.ReverseTransformHP2(pixel.V1, pixel.V2, pixel.V3);
        }
    }

    private static void CopyPixels8BitHP3(Span<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(destination);

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = ColorTransformations.ReverseTransformHP3(pixel.V1, pixel.V2, pixel.V3);
        }
    }

    private static void CopyLine16Bit2Components(Span<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceUshort = MemoryMarshal.Cast<byte, ushort>(source);
        var destinationPair = MemoryMarshal.Cast<byte, Pair<ushort>>(destination);
        int pixelStride = PixelCountToPixelStride(pixelCount);

        for (int i = 0; i < pixelCount; ++i)
        {
            destinationPair[i] = new Pair<ushort>(sourceUshort[i], sourceUshort[i + pixelStride]);
        }
    }

    private static void CopyLine16Bit3Components(Span<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceUshort = MemoryMarshal.Cast<byte, ushort>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(destination);
        int pixelStride = PixelCountToPixelStride(pixelCount);

        for (int i = 0; i < pixelCount; ++i)
        {
            destinationTriplet[i] = new Triplet<ushort>(
                sourceUshort[i], sourceUshort[i + pixelStride], sourceUshort[i + (2 * pixelStride)]);
        }
    }

    private static void CopyLine16Bit3ComponentsHP1(Span<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceUshort = MemoryMarshal.Cast<byte, ushort>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(destination);
        int pixelStride = PixelCountToPixelStride(pixelCount);

        for (int i = 0; i < pixelCount; ++i)
        {
            destinationTriplet[i] =
                ColorTransformations.ReverseTransformHP1(
                    sourceUshort[i], sourceUshort[i + pixelStride], sourceUshort[i + (2 * pixelStride)]);
        }
    }

    private static void CopyLine16Bit3ComponentsHP2(Span<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceUshort = MemoryMarshal.Cast<byte, ushort>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(destination);
        int pixelStride = PixelCountToPixelStride(pixelCount);

        for (int i = 0; i < pixelCount; ++i)
        {
            destinationTriplet[i] =
                ColorTransformations.ReverseTransformHP2(
                    sourceUshort[i], sourceUshort[i + pixelStride], sourceUshort[i + (2 * pixelStride)]);
        }
    }

    private static void CopyLine16Bit3ComponentsHP3(Span<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceUshort = MemoryMarshal.Cast<byte, ushort>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(destination);
        int pixelStride = PixelCountToPixelStride(pixelCount);

        for (int i = 0; i < pixelCount; ++i)
        {
            destinationTriplet[i] =
                ColorTransformations.ReverseTransformHP3(
                    sourceUshort[i], sourceUshort[i + pixelStride], sourceUshort[i + (2 * pixelStride)]);
        }
    }

    private static void CopyLine16Bit4Components(Span<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceUshort = MemoryMarshal.Cast<byte, ushort>(source);
        var destinationQuad = MemoryMarshal.Cast<byte, Quad<ushort>>(destination);
        int pixelStride = PixelCountToPixelStride(pixelCount);

        for (int i = 0; i < pixelCount; ++i)
        {
            destinationQuad[i] = new Quad<ushort>(
                sourceUshort[i], sourceUshort[i + pixelStride], sourceUshort[i + (2 * pixelStride)], sourceUshort[i + (3 * pixelStride)]);
        }
    }

    private static void CopyPixels16BitHP1(Span<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(destination);

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = ColorTransformations.ReverseTransformHP1(pixel.V1, pixel.V2, pixel.V3);
        }
    }

    private static void CopyPixels16BitHP2(Span<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(destination);

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = ColorTransformations.ReverseTransformHP2(pixel.V1, pixel.V2, pixel.V3);
        }
    }

    private static void CopyPixels16BitHP3(Span<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(destination);

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = ColorTransformations.ReverseTransformHP3(pixel.V1, pixel.V2, pixel.V3);
        }
    }

    private static void CopyPixels16Bit2Components(Span<byte> source, Span<byte> destination, int pixelCount)
    {
        int bytesCount = pixelCount * 2 * 2;
        source[..bytesCount].CopyTo(destination);
    }

    private static void CopyPixels16Bit3Components(Span<byte> source, Span<byte> destination, int pixelCount)
    {
        int bytesCount = pixelCount * 3 * 2;
        source[..bytesCount].CopyTo(destination);
    }

    private static void CopyPixels16Bit4Components(Span<byte> source, Span<byte> destination, int pixelCount)
    {
        int bytesCount = pixelCount * 4 * 2;
        source[..bytesCount].CopyTo(destination);
    }

    private static int PixelCountToPixelStride(int pixelCount)
    {
        // The line buffer is allocated with 2 extra pixels for the edges.
        return pixelCount + 2;
    }
}
