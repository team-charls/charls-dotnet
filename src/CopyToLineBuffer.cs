// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Runtime.InteropServices;

namespace CharLS.Managed;

internal class CopyToLineBuffer
{
    internal delegate void Method(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask);

    internal static Method GetCopyMethod(int bitsPerSample, int componentCount, InterleaveMode interleaveMode, ColorTransformation colorTransformation)
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
                    case 2:
                        return CopyLine8Bit2Components;

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
                switch (componentCount)
                {
                    case 2:
                        return GetMethodCopyPixels8Bit2Components(bitsPerSample);

                    case 3:
                        switch (colorTransformation)
                        {
                            default:
                                Debug.Assert(colorTransformation == ColorTransformation.None);
                                return GetMethodCopyPixels8Bit3Components(bitsPerSample);
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

                    default:
                        Debug.Assert(componentCount == 4);
                        return GetMethodCopyPixels8Bit4Components(bitsPerSample);
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
                        return GetMethodCopyPixels16Bit2Components(bitsPerSample);

                    case 3:
                        switch (colorTransformation)
                        {
                            default:
                                Debug.Assert(colorTransformation == ColorTransformation.None);
                                return GetMethodCopyPixels16Bit3Components(bitsPerSample);
                            case ColorTransformation.HP1:
                                return CopyPixels16BitHP1;
                            case ColorTransformation.HP2:
                                return CopyPixels16BitHP2;
                            case ColorTransformation.HP3:
                                return CopyPixels16BitHP3;
                        }

                    default:
                        Debug.Assert(componentCount == 4);
                        return GetMethodCopyPixels16Bit4Components(bitsPerSample);
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
        for (int i = 0; i != pixelCount; ++i)
        {
            destination[i] = (byte)(source[i] & mask);
        }
    }

    private static void CopySamplesMasked16Bit(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        var sourceUint16 = MemoryMarshal.Cast<byte, ushort>(source);
        var destinationUint16 = MemoryMarshal.Cast<byte, ushort>(destination);

        for (int i = 0; i != pixelCount; ++i)
        {
            destinationUint16[i] = (ushort)(sourceUint16[i] & mask);
        }
    }

    private static void CopyLine8Bit2Components(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Pair<byte>>(source);
        int pixelStride = PixelCountToPixelStride(pixelCount);

        for (int i = 0; i != pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];

            destination[i] = (byte)(pixel.V1 & mask);
            destination[i + pixelStride] = (byte)(pixel.V2 & mask);
        }
    }

    private static void CopyLine8Bit3Components(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(source);
        int pixelStride = PixelCountToPixelStride(pixelCount);

        for (int i = 0; i != pixelCount; ++i)
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
        int pixelStride = PixelCountToPixelStride(pixelCount);

        for (int i = 0; i != pixelCount; ++i)
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
        int pixelStride = PixelCountToPixelStride(pixelCount);

        for (int i = 0; i != pixelCount; ++i)
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
        int pixelStride = PixelCountToPixelStride(pixelCount);

        for (int i = 0; i != pixelCount; ++i)
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
        int pixelStride = PixelCountToPixelStride(pixelCount);

        for (int i = 0; i != pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];

            destination[i] = (byte)(pixel.V1 & mask);
            destination[i + pixelStride] = (byte)(pixel.V2 & mask);
            destination[i + (2 * pixelStride)] = (byte)(pixel.V3 & mask);
            destination[i + (3 * pixelStride)] = (byte)(pixel.V4 & mask);
        }
    }

    private static Method GetMethodCopyPixels8Bit2Components(int bitsPerSample)
    {
        bool maskNeeded = bitsPerSample != 8;
        return maskNeeded ? CopyPixelsMasked8Bit2Components : CopySamples8Bit2Components;
    }

    private static void CopySamples8Bit2Components(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        source[..(pixelCount * 2)].CopyTo(destination);
    }

    private static void CopyPixelsMasked8Bit2Components(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        for (int i = 0; i != pixelCount * 2; ++i)
        {
            destination[i] = (byte)(source[i] & mask);
        }
    }

    private static Method GetMethodCopyPixels8Bit3Components(int bitsPerSample)
    {
        bool maskNeeded = bitsPerSample != 8;
        return maskNeeded ? CopyPixelsMasked8Bit3Components : CopySamples8Bit3Components;
    }

    private static void CopySamples8Bit3Components(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        source[..(pixelCount * 3)].CopyTo(destination);
    }

    private static void CopyPixelsMasked8Bit3Components(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        for (int i = 0; i != pixelCount * 3; ++i)
        {
            destination[i] = (byte)(source[i] & mask);
        }
    }

    private static void CopyPixels8BitHP1(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(destination);

        for (int i = 0; i != pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = ColorTransformations.TransformHP1(pixel.V1, pixel.V2, pixel.V3);
        }
    }

    private static void CopyPixels8BitHP2(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(destination);

        for (int i = 0; i != pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = ColorTransformations.TransformHP2(pixel.V1, pixel.V2, pixel.V3);
        }
    }

    private static void CopyPixels8BitHP3(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(destination);

        for (int i = 0; i != pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = ColorTransformations.TransformHP3(pixel.V1, pixel.V2, pixel.V3);
        }
    }

    private static Method GetMethodCopyPixels8Bit4Components(int bitsPerSample)
    {
        bool maskNeeded = bitsPerSample != 8;
        return maskNeeded ? CopyPixelsMasked8Bit4Components : CopySamples8Bit4Components;
    }

    private static void CopySamples8Bit4Components(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        source[..(pixelCount * 4)].CopyTo(destination);
    }

    private static void CopyPixelsMasked8Bit4Components(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        for (int i = 0; i != pixelCount * 4; ++i)
        {
            destination[i] = (byte)(source[i] & mask);
        }
    }

    private static void CopyLine16Bit2Components(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        var sourcePair = MemoryMarshal.Cast<byte, Pair<ushort>>(source);
        var destinationUshort = MemoryMarshal.Cast<byte, ushort>(destination);
        int pixelStride = PixelCountToPixelStride(pixelCount);

        for (int i = 0; i != pixelCount; ++i)
        {
            var pixel = sourcePair[i];

            destinationUshort[i] = (ushort)(pixel.V1 & mask);
            destinationUshort[i + pixelStride] = (ushort)(pixel.V2 & mask);
        }
    }

    private static void CopyLine16Bit3Components(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(source);
        var destinationUshort = MemoryMarshal.Cast<byte, ushort>(destination);
        int pixelStride = PixelCountToPixelStride(pixelCount);

        for (int i = 0; i != pixelCount; ++i)
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
        int pixelStride = PixelCountToPixelStride(pixelCount);

        for (int i = 0; i != pixelCount; ++i)
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
        int pixelStride = PixelCountToPixelStride(pixelCount);

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
        int pixelStride = PixelCountToPixelStride(pixelCount);

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
        int pixelStride = PixelCountToPixelStride(pixelCount);

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];

            destinationUshort[i] = (ushort)(pixel.V1 & mask);
            destinationUshort[i + pixelStride] = (ushort)(pixel.V2 & mask);
            destinationUshort[i + (2 * pixelStride)] = (ushort)(pixel.V3 & mask);
            destinationUshort[i + (3 * pixelStride)] = (ushort)(pixel.V4 & mask);
        }
    }

    private static Method GetMethodCopyPixels16Bit2Components(int bitsPerSample)
    {
        bool maskNeeded = bitsPerSample != 16;
        return maskNeeded ? CopyPixelsMasked16Bit2Components : CopyPixels16Bit2Components;
    }

    private static void CopyPixels16Bit2Components(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        source[..(pixelCount * 2 * 2)].CopyTo(destination);
    }

    private static void CopyPixelsMasked16Bit2Components(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        var sourceUint16 = MemoryMarshal.Cast<byte, ushort>(source);
        var destinationUint16 = MemoryMarshal.Cast<byte, ushort>(destination);

        for (int i = 0; i != pixelCount * 2; ++i)
        {
            destinationUint16[i] = (ushort)(sourceUint16[i] & mask);
        }
    }

    private static Method GetMethodCopyPixels16Bit3Components(int bitsPerSample)
    {
        bool maskNeeded = bitsPerSample != 16;
        return maskNeeded ? CopyPixelsMasked16Bit3Components : CopyPixels16Bit3Components;
    }

    private static void CopyPixels16Bit3Components(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        source[..(pixelCount * 2 * 3)].CopyTo(destination);
    }

    private static void CopyPixelsMasked16Bit3Components(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        var sourceUint16 = MemoryMarshal.Cast<byte, ushort>(source);
        var destinationUint16 = MemoryMarshal.Cast<byte, ushort>(destination);

        for (int i = 0; i != pixelCount * 3; ++i)
        {
            destinationUint16[i] = (ushort)(sourceUint16[i] & mask);
        }
    }

    private static void CopyPixels16BitHP1(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(destination);

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

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = ColorTransformations.TransformHP3(pixel.V1, pixel.V2, pixel.V3);
        }
    }

    private static Method GetMethodCopyPixels16Bit4Components(int bitsPerSample)
    {
        bool maskNeeded = bitsPerSample != 16;
        return maskNeeded ? CopyPixelsMasked16Bit4Components : CopySamples16Bit4Components;
    }

    private static void CopySamples16Bit4Components(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        source[..(pixelCount * 2 * 4)].CopyTo(destination);
    }

    private static void CopyPixelsMasked16Bit4Components(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount, int mask)
    {
        var sourceUint16 = MemoryMarshal.Cast<byte, ushort>(source);
        var destinationUint16 = MemoryMarshal.Cast<byte, ushort>(destination);

        for (int i = 0; i != pixelCount * 4; ++i)
        {
            destinationUint16[i] = (ushort)(sourceUint16[i] & mask);
        }
    }

    private static int PixelCountToPixelStride(int pixelCount)
    {
        // The line buffer is allocated with 2 extra pixels for the edges.
        return pixelCount + 2;
    }
}
