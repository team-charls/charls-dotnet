// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Runtime.InteropServices;

namespace CharLS.Managed;

internal class CopyToLineBuffer
{
    internal delegate void Method(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask);

    internal static Method GetMethod(int bitsPerSample, int componentCount, InterleaveMode interleaveMode, ColorTransformation colorTransformation)
    {
        return bitsPerSample <= 8 ?
            GetMethod8Bit(bitsPerSample, componentCount, interleaveMode, colorTransformation) :
            GetMethod16Bit(bitsPerSample, componentCount, interleaveMode, colorTransformation);
    }

    private static Method GetMethod8Bit(int bitsPerSample, int componentCount, InterleaveMode interleaveMode, ColorTransformation colorTransformation)
    {
        switch (interleaveMode)
        {
            case InterleaveMode.None:
                return GetMethodCopySamples8Bit(bitsPerSample);

            case InterleaveMode.Line:
                switch (componentCount)
                {
                    case 3:
                        switch (colorTransformation)
                        {
                            default:
                                Debug.Assert(colorTransformation == ColorTransformation.None);
                                return CopyLine8Bit3Components;
                            case ColorTransformation.HP1:
                                Debug.Assert(bitsPerSample == 8);
                                return CopyLine8Bit3ComponentsHP1;
                            case ColorTransformation.HP2:
                                Debug.Assert(bitsPerSample == 8);
                                return CopyLine8Bit3ComponentsHP2;
                            case ColorTransformation.HP3:
                                Debug.Assert(bitsPerSample == 8);
                                return CopyLine8Bit3ComponentsHP3;
                        }

                    default:
                        Debug.Assert(componentCount == 4);
                        return CopyToLine8Bit4Components;
                }

            default:
                Debug.Assert(interleaveMode == InterleaveMode.Sample);
                switch (colorTransformation)
                {
                    default:
                        Debug.Assert(colorTransformation == ColorTransformation.None);
                        return GetMethodCopySamples8Bit(bitsPerSample);
                    case ColorTransformation.HP1:
                        Debug.Assert(bitsPerSample == 8);
                        return CopyPixels8BitHP1;
                    case ColorTransformation.HP2:
                        Debug.Assert(bitsPerSample == 8);
                        return CopyPixels8BitHP2;
                    case ColorTransformation.HP3:
                        Debug.Assert(bitsPerSample == 8);
                        return CopyPixels8BitHP3;
                }
        }
    }

    private static Method GetMethod16Bit(
        int bitsPerSample,
        int componentCount,
        InterleaveMode interleaveMode,
        ColorTransformation colorTransformation)
    {
        switch (interleaveMode)
        {
            case InterleaveMode.None:
                return GetMethodCopySamples16Bit(bitsPerSample);

            case InterleaveMode.Line:
                switch (componentCount)
                {
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
                switch (colorTransformation)
                {
                    default:
                        Debug.Assert(colorTransformation == ColorTransformation.None);
                        return GetMethodCopySamples16Bit(bitsPerSample);
                    case ColorTransformation.HP1:
                        return CopyPixels16BitHP1;
                    case ColorTransformation.HP2:
                        return CopyPixels16BitHP2;
                    case ColorTransformation.HP3:
                        return CopyPixels16BitHP3;
                }
        }
    }

    private static Method GetMethodCopySamples8Bit(int bitsPerSample)
    {
        bool maskNeeded = bitsPerSample != 8;
        return maskNeeded ? CopySamplesMasked8Bit : CopySamples8Bit;
    }

    private static Method GetMethodCopySamples16Bit(int bitsPerSample)
    {
        bool maskNeeded = bitsPerSample != 16;
        return maskNeeded ? CopySamplesMasked16Bit : CopySamples16Bit;
    }

    private static void CopySamples8Bit(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        source[..pixelCount].CopyTo(destination);
    }

    private static void CopySamples16Bit(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        source[..(pixelCount * 2)].CopyTo(destination);
    }

    private static void CopySamplesMasked8Bit(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        for (int i = 0; i < pixelCount; ++i)
        {
            destination[i] = (byte)(source[i] & mask);
        }
    }

    private static void CopySamplesMasked16Bit(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        var sourceUint16 = MemoryMarshal.Cast<byte, ushort>(source);
        var destinationUint16 = MemoryMarshal.Cast<byte, ushort>(destination);

        for (int i = 0; i < pixelCount; ++i)
        {
            destinationUint16[i] = (ushort)(sourceUint16[i] & mask);
        }
    }

    private static void CopyLine8Bit3Components(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(source);
        int pixelStride = pixelCount + 2;

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];

            destination[i] = (byte)(pixel.V1 & mask);
            destination[i + pixelStride] = (byte)(pixel.V2 & mask);
            destination[i + (2 * pixelStride)] = (byte)(pixel.V3 & mask);
        }
    }

    private static void CopyLine8Bit3ComponentsHP1(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(source);
        int pixelStride = pixelCount + 2;

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            pixel = ColorTransformations.TransformHP1(pixel.V1, pixel.V2, pixel.V3);

            destination[i] = pixel.V1;
            destination[i + pixelStride] = pixel.V2;
            destination[i + (2 * pixelStride)] = pixel.V3;
        }
    }

    private static void CopyLine8Bit3ComponentsHP2(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(source);
        int pixelStride = pixelCount + 2;

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            pixel = ColorTransformations.TransformHP2(pixel.V1, pixel.V2, pixel.V3);

            destination[i] = pixel.V1;
            destination[i + pixelStride] = pixel.V2;
            destination[i + (2 * pixelStride)] = pixel.V3;
        }
    }

    private static void CopyLine8Bit3ComponentsHP3(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(source);
        int pixelStride = pixelCount + 2;

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            pixel = ColorTransformations.TransformHP3(pixel.V1, pixel.V2, pixel.V3);

            destination[i] = pixel.V1;
            destination[i + pixelStride] = pixel.V2;
            destination[i + (2 * pixelStride)] = pixel.V3;
        }
    }

    private static void CopyToLine8Bit4Components(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Quad<byte>>(source);
        int pixelStride = pixelCount + 2;

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];

            destination[i] = (byte)(pixel.V1 & mask);
            destination[i + pixelStride] = (byte)(pixel.V2 & mask);
            destination[i + (2 * pixelStride)] = (byte)(pixel.V3 & mask);
            destination[i + (3 * pixelStride)] = (byte)(pixel.V4 & mask);
        }
    }

    private static void CopyPixels8BitHP1(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(destination);
        pixelCount /= 3;

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = ColorTransformations.TransformHP1(pixel.V1, pixel.V2, pixel.V3);
        }
    }

    private static void CopyPixels8BitHP2(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(destination);
        pixelCount /= 3;

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = ColorTransformations.TransformHP2(pixel.V1, pixel.V2, pixel.V3);
        }
    }

    private static void CopyPixels8BitHP3(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(destination);
        pixelCount /= 3;

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = ColorTransformations.TransformHP3(pixel.V1, pixel.V2, pixel.V3);
        }
    }

    private static void CopyLine16Bit3Components(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(source);
        var destinationUshort = MemoryMarshal.Cast<byte, ushort>(destination);
        int pixelStride = pixelCount + 2;

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];

            destinationUshort[i] = (ushort)(pixel.V1 & mask);
            destinationUshort[i + pixelStride] = (ushort)(pixel.V2 & mask);
            destinationUshort[i + (2 * pixelStride)] = (ushort)(pixel.V3 & mask);
        }
    }

    private static void CopyLine16Bit3ComponentsHP1(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
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
            destinationUshort[i + (2 * pixelStride)] = pixel.V3;
        }
    }

    private static void CopyLine16Bit3ComponentsHP2(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
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
            destinationUshort[i + (2 * pixelStride)] = pixel.V3;
        }
    }

    private static void CopyLine16Bit3ComponentsHP3(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
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
            destinationUshort[i + (2 * pixelStride)] = pixel.V3;
        }
    }

    private static void CopyLine16Bit4Components(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Quad<ushort>>(source);
        var destinationUshort = MemoryMarshal.Cast<byte, ushort>(destination);
        int pixelStride = pixelCount + 2;

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];

            destinationUshort[i] = (ushort)(pixel.V1 & mask);
            destinationUshort[i + pixelStride] = (ushort)(pixel.V2 & mask);
            destinationUshort[i + (2 * pixelStride)] = (ushort)(pixel.V3 & mask);
            destinationUshort[i + (3 * pixelStride)] = (ushort)(pixel.V4 & mask);
        }
    }

    private static void CopyPixels16BitHP1(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(destination);
        pixelCount /= 3;

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = ColorTransformations.TransformHP1(pixel.V1, pixel.V2, pixel.V3);
        }
    }

    private static void CopyPixels16BitHP2(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(destination);
        pixelCount /= 3;

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = ColorTransformations.TransformHP2(pixel.V1, pixel.V2, pixel.V3);
        }
    }

    private static void CopyPixels16BitHP3(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(destination);
        pixelCount /= 3;

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = ColorTransformations.TransformHP3(pixel.V1, pixel.V2, pixel.V3);
        }
    }
}
