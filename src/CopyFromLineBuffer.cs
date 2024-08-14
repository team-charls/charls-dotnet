// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CharLS.Managed;

internal class CopyFromLineBuffer
{
    internal delegate int CopyFromLineBufferFn(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride);

    internal static CopyFromLineBufferFn? GetMethod(int bitsPerSample, int componentCount, InterleaveMode interleaveMode, ColorTransformation colorTransformation)
    {
        if (bitsPerSample <= 8)
        {
            switch (interleaveMode)
            {
                case InterleaveMode.None:
                    return CopySamples;

                case InterleaveMode.Line:
                    switch (componentCount)
                    {
                        case 3:
                            switch (colorTransformation)
                            {
                                case ColorTransformation.None:
                                    return CopyLine8Bit3Components;
                                case ColorTransformation.HP1:
                                    return CopyLine8Bit3ComponentsHP1;
                                case ColorTransformation.HP2:
                                    return CopyLine8Bit3ComponentsHP2;
                                case ColorTransformation.HP3:
                                default:
                                    return CopyLine8Bit3ComponentsHP3;
                            }

                        default:
                            Debug.Assert(componentCount == 4);
                            return CopyToLine8Bit4Components;
                    }

                case InterleaveMode.Sample:
                default:
                    Debug.Assert(interleaveMode == InterleaveMode.Sample);
                    switch (colorTransformation)
                    {
                        case ColorTransformation.None:
                        default:
                            Debug.Assert(colorTransformation == ColorTransformation.None);
                            switch (componentCount)
                            {
                                case 3:
                                    return CopyPixels8BitTriplet;

                                default:
                                    return CopyPixels8BitQuad;
                            }
                        case ColorTransformation.HP1:
                            return CopyPixels8BitHP1;
                        case ColorTransformation.HP2:
                            return CopyPixels8BitHP2;
                        case ColorTransformation.HP3:
                            return CopyPixels8BitHP3;
                    }
            }
        }

        switch (interleaveMode)
        {
            case InterleaveMode.None:
                return CopySamples;

            case InterleaveMode.Line:
                switch (componentCount)
                {
                    case 3:
                        switch (colorTransformation)
                        {
                            case ColorTransformation.None:
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

            case InterleaveMode.Sample:
            default:
                Debug.Assert(interleaveMode == InterleaveMode.Sample);
                switch (colorTransformation)
                {
                    case ColorTransformation.None:
                    default:
                        switch (componentCount)
                        {
                            case 3:
                                return CopyPixels16BitTriplet;

                            default:
                                Debug.Assert(componentCount == 4);
                                return CopyPixels16BitQuad;
                        }
                    case ColorTransformation.HP1:
                        return CopyPixels16BitHP1;
                    case ColorTransformation.HP2:
                        return CopyPixels16BitHP2;
                    case ColorTransformation.HP3:
                        return CopyPixels16BitHP3;
                }
        }
    }

    private static int CopySamples(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        source[..pixelCount].CopyTo(destination);
        return pixelCount;
    }

    private static int CopyLine8Bit3Components(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(destination);

        for (int i = 0; i < pixelCount; ++i)
        {
            destinationTriplet[i] = new Triplet<byte>(
                source[i], source[i + sourceStride], source[i + 2 * sourceStride]);
        }

        return pixelCount * 3;
    }

    private static int CopyLine8Bit3ComponentsHP1(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(destination);

        for (int i = 0; i < pixelCount; ++i)
        {
            destinationTriplet[i] =
                ColorTransformations.ReverseTransformHP1(source[i], source[i + sourceStride],
                    source[i + 2 * sourceStride]);
        }

        return pixelCount * 3;
    }

    private static int CopyLine8Bit3ComponentsHP2(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(destination);

        for (int i = 0; i < pixelCount; ++i)
        {
            destinationTriplet[i] =
                ColorTransformations.ReverseTransformHP2(source[i], source[i + sourceStride],
                    source[i + 2 * sourceStride]);
        }

        return pixelCount * 3;
    }

    private static int CopyLine8Bit3ComponentsHP3(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(destination);

        for (int i = 0; i < pixelCount; ++i)
        {
            destinationTriplet[i] =
                ColorTransformations.ReverseTransformHP3(source[i], source[i + sourceStride],
                    source[i + 2 * sourceStride]);
        }

        return pixelCount * 3;
    }

    private static int CopyToLine8Bit4Components(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        var destinationQuad = MemoryMarshal.Cast<byte, Quad<byte>>(destination);

        for (int i = 0; i < pixelCount; ++i)
        {
            destinationQuad[i] = new Quad<byte>(
                source[i], source[i + sourceStride], source[i + 2 * sourceStride], source[i + 3 * sourceStride]);
        }

        return pixelCount * 4;
    }

    private static int CopyPixels8BitTriplet(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        int bytesCount = pixelCount * 3;
        source[..bytesCount].CopyTo(destination);
        return bytesCount;
    }

    private static int CopyPixels8BitQuad(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        int bytesCount = pixelCount * 4;
        source[..bytesCount].CopyTo(destination);
        return bytesCount;
    }

    private static int CopyPixels8BitHP1(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(destination);

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = ColorTransformations.ReverseTransformHP1(pixel.V1, pixel.V2, pixel.V3);
        }

        return pixelCount * 3;
    }

    private static int CopyPixels8BitHP2(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(destination);

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = ColorTransformations.ReverseTransformHP2(pixel.V1, pixel.V2, pixel.V3);
        }

        return pixelCount * 3;
    }

    private static int CopyPixels8BitHP3(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(destination);

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = ColorTransformations.ReverseTransformHP3(pixel.V1, pixel.V2, pixel.V3);
        }

        return pixelCount * 3;
    }

    private static int CopyLine16Bit3Components(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        var sourceUshort = MemoryMarshal.Cast<byte, ushort>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(destination);

        for (int i = 0; i < pixelCount; ++i)
        {
            destinationTriplet[i] = new Triplet<ushort>(
                sourceUshort[i], sourceUshort[i + sourceStride], sourceUshort[i + 2 * sourceStride]);
        }

        return pixelCount * 3 * 2;
    }

    private static int CopyLine16Bit3ComponentsHP1(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        var sourceUshort = MemoryMarshal.Cast<byte, ushort>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(destination);

        for (int i = 0; i < pixelCount; ++i)
        {
            destinationTriplet[i] =
                ColorTransformations.ReverseTransformHP1(sourceUshort[i], sourceUshort[i + sourceStride],
                    sourceUshort[i + 2 * sourceStride]);
        }

        return pixelCount * 3 * 2;
    }

    private static int CopyLine16Bit3ComponentsHP2(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        var sourceUshort = MemoryMarshal.Cast<byte, ushort>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(destination);

        for (int i = 0; i < pixelCount; ++i)
        {
            destinationTriplet[i] =
                ColorTransformations.ReverseTransformHP2(sourceUshort[i], sourceUshort[i + sourceStride],
                    sourceUshort[i + 2 * sourceStride]);
        }

        return pixelCount * 3 * 2;
    }

    private static int CopyLine16Bit3ComponentsHP3(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        var sourceUshort = MemoryMarshal.Cast<byte, ushort>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(destination);

        for (int i = 0; i < pixelCount; ++i)
        {
            destinationTriplet[i] =
                ColorTransformations.ReverseTransformHP3(sourceUshort[i], sourceUshort[i + sourceStride],
                    sourceUshort[i + 2 * sourceStride]);
        }

        return pixelCount * 3 * 2;
    }

    private static int CopyLine16Bit4Components(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        var sourceUshort = MemoryMarshal.Cast<byte, ushort>(source);
        var destinationQuad = MemoryMarshal.Cast<byte, Quad<ushort>>(destination);

        for (int i = 0; i < pixelCount; ++i)
        {
            destinationQuad[i] = new Quad<ushort>(
                sourceUshort[i], sourceUshort[i + sourceStride], sourceUshort[i + 2 * sourceStride], sourceUshort[i + 3 * sourceStride]);
        }

        return pixelCount * 4 * 2;
    }

    private static int CopyPixels16BitHP1(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(destination);

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = ColorTransformations.ReverseTransformHP1(pixel.V1, pixel.V2, pixel.V3);
        }

        return pixelCount * 3 * 2;
    }

    private static int CopyPixels16BitHP2(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(destination);

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = ColorTransformations.ReverseTransformHP2(pixel.V1, pixel.V2, pixel.V3);
        }
        return pixelCount * 3 * 2;
    }

    private static int CopyPixels16BitHP3(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(destination);

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = ColorTransformations.ReverseTransformHP3(pixel.V1, pixel.V2, pixel.V3);
        }
        return pixelCount * 3 * 2;
    }

    private static int CopyPixels16BitTriplet(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        int bytesCount = pixelCount * 3 * 2;
        source[..bytesCount].CopyTo(destination);
        return bytesCount;
    }

    private static int CopyPixels16BitQuad(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        int bytesCount = pixelCount * 4 * 2;
        source[..bytesCount].CopyTo(destination);
        return bytesCount;
    }

}
