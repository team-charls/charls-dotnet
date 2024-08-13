// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CharLS.Managed;

internal class CopyToLineBuffer
{
    internal delegate void CopyToLineBufferFn(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount);

    internal static CopyToLineBufferFn GetMethod(int bitsPerSample, int componentCount, InterleaveMode interleaveMode, ColorTransformation colorTransformation)
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
                            return CopySamples;
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
                        return CopySamples;
                    case ColorTransformation.HP1:
                        return CopyPixels16BitHP1;
                    case ColorTransformation.HP2:
                        return CopyPixels16BitHP2;
                    case ColorTransformation.HP3:
                        return CopyPixels16BitHP3;
                }
        }
    }

    private static void CopySamples(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
    {
        source[..pixelCount].CopyTo(destination);
    }

    private static void CopyLine8Bit3Components(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(source);
        int pixelStride = pixelCount + 2;

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];

            destination[i] = pixel.V1;
            destination[i + pixelStride] = pixel.V2;
            destination[i + 2 * pixelStride] = pixel.V3;
        }
    }

    private static void CopyLine8Bit3ComponentsHP1(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(source);
        int pixelStride = pixelCount + 2;

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            pixel = ColorTransformations.TransformHP1(pixel.V1, pixel.V2, pixel.V3);

            destination[i] = pixel.V1;
            destination[i + pixelStride] = pixel.V2;
            destination[i + 2 * pixelStride] = pixel.V3;
        }
    }

    private static void CopyLine8Bit3ComponentsHP2(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(source);
        int pixelStride = pixelCount + 2;

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            pixel = ColorTransformations.TransformHP2(pixel.V1, pixel.V2, pixel.V3);

            destination[i] = pixel.V1;
            destination[i + pixelStride] = pixel.V2;
            destination[i + 2 * pixelStride] = pixel.V3;
        }
    }

    private static void CopyLine8Bit3ComponentsHP3(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(source);
        int pixelStride = pixelCount + 2;

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            pixel = ColorTransformations.TransformHP3(pixel.V1, pixel.V2, pixel.V3);

            destination[i] = pixel.V1;
            destination[i + pixelStride] = pixel.V2;
            destination[i + 2 * pixelStride] = pixel.V3;
        }
    }

    private static void CopyToLine8Bit4Components(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Quad<byte>>(source);
        int pixelStride = pixelCount + 2;

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            ////const triplet<SampleType> color_transformed{ transform(color.v1 & mask, color.v2 & mask, color.v3 & mask)};

            destination[i] = pixel.V1;
            destination[i + pixelStride] = pixel.V2;
            destination[i + 2 * pixelStride] = pixel.V3;
            destination[i + 3 * pixelStride] = pixel.V4;
        }
    }

    private static void CopyPixels8BitHP1(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(destination);
        pixelCount = pixelCount / 3;

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = ColorTransformations.TransformHP1(pixel.V1, pixel.V2, pixel.V3);
        }
    }

    private static void CopyPixels8BitHP2(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(destination);
        pixelCount = pixelCount / 3;

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = ColorTransformations.TransformHP2(pixel.V1, pixel.V2, pixel.V3);
        }
    }

    private static void CopyPixels8BitHP3(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(destination);
        pixelCount = pixelCount / 3;

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = ColorTransformations.TransformHP3(pixel.V1, pixel.V2, pixel.V3);
        }
    }

    private static void CopyLine16Bit3Components(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(source);
        var destinationUshort = MemoryMarshal.Cast<byte, ushort>(destination);
        int pixelStride = pixelCount + 2;

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];

            destinationUshort[i] = pixel.V1;
            destinationUshort[i + pixelStride] = pixel.V2;
            destinationUshort[i + 2 * pixelStride] = pixel.V3;
        }
    }

    private static void CopyLine16Bit3ComponentsHP1(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(source);
        var destinationUshort = MemoryMarshal.Cast<byte, ushort>(destination);
        int pixelStride = pixelCount + 2;

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            pixel = ColorTransformations.TransformHP1(pixel.V1, pixel.V2, pixel.V3);

            destinationUshort[i] = pixel.V1;
            destinationUshort[i + pixelStride] = pixel.V2;
            destinationUshort[i + 2 * pixelStride] = pixel.V3;
        }
    }

    private static void CopyLine16Bit3ComponentsHP2(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(source);
        var destinationUshort = MemoryMarshal.Cast<byte, ushort>(destination);
        int pixelStride = pixelCount + 2;

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            pixel = ColorTransformations.TransformHP2(pixel.V1, pixel.V2, pixel.V3);

            destinationUshort[i] = pixel.V1;
            destinationUshort[i + pixelStride] = pixel.V2;
            destinationUshort[i + 2 * pixelStride] = pixel.V3;
        }
    }

    private static void CopyLine16Bit3ComponentsHP3(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(source);
        var destinationUshort = MemoryMarshal.Cast<byte, ushort>(destination);
        int pixelStride = pixelCount + 2;

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            pixel = ColorTransformations.TransformHP3(pixel.V1, pixel.V2, pixel.V3);

            destinationUshort[i] = pixel.V1;
            destinationUshort[i + pixelStride] = pixel.V2;
            destinationUshort[i + 2 * pixelStride] = pixel.V3;
        }
    }

    private static void CopyLine16Bit4Components(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Quad<ushort>>(source);
        var destinationUshort = MemoryMarshal.Cast<byte, ushort>(destination);
        int pixelStride = pixelCount + 2;

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            ////const triplet<SampleType> color_transformed{ transform(color.v1 & mask, color.v2 & mask, color.v3 & mask)};

            destinationUshort[i] = pixel.V1;
            destinationUshort[i + pixelStride] = pixel.V2;
            destinationUshort[i + 2 * pixelStride] = pixel.V3;
            destinationUshort[i + 3 * pixelStride] = pixel.V4;
        }
    }

    private static void CopyPixels16BitHP1(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(destination);
        pixelCount = pixelCount / (3 * 2);

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = ColorTransformations.TransformHP1(pixel.V1, pixel.V2, pixel.V3);
        }
    }

    private static void CopyPixels16BitHP2(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(destination);
        pixelCount = pixelCount / (3 * 2);

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = ColorTransformations.TransformHP2(pixel.V1, pixel.V2, pixel.V3);
        }
    }

    private static void CopyPixels16BitHP3(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(destination);
        pixelCount = pixelCount / (3 * 2);

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = ColorTransformations.TransformHP3(pixel.V1, pixel.V2, pixel.V3);
        }
    }
}
