// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Runtime.InteropServices;

namespace CharLS.JpegLS;

internal class ProcessEncodedSingleComponent : IProcessLineEncoded
{
    public void NewLineRequested(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
    {
        source[..pixelCount].CopyTo(destination);
    }
}

internal class ProcessEncodedSingleComponent8BitHP1 : IProcessLineEncoded
{
    public void NewLineRequested(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
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
}

internal class ProcessEncodedSingleComponent16BitHP1 : IProcessLineEncoded
{
    public void NewLineRequested(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
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
}

internal class ProcessEncodedSingleComponent8BitHP2 : IProcessLineEncoded
{
    public void NewLineRequested(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
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
}

internal class ProcessEncodedSingleComponent8BitHP3 : IProcessLineEncoded
{
    public void NewLineRequested(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
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
}


internal class ProcessEncodedSingleComponentToLine8Bit3Components : IProcessLineEncoded
{
    public void NewLineRequested(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(source);
        int pixelStride = pixelCount + 2;

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            ////const triplet<SampleType> color_transformed{ transform(color.v1 & mask, color.v2 & mask, color.v3 & mask)};

            destination[i] = pixel.V1;
            destination[i + pixelStride] = pixel.V2;
            destination[i + 2 * pixelStride] = pixel.V3;
        }
    }
}

internal class ProcessEncodedSingleComponentToLine16Bit3Components : IProcessLineEncoded
{
    public void NewLineRequested(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<ushort>>(source);
        var destinationUshort = MemoryMarshal.Cast<byte, ushort>(destination);
        int pixelStride = pixelCount + 2;

        for (int i = 0; i < pixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            ////const triplet<SampleType> color_transformed{ transform(color.v1 & mask, color.v2 & mask, color.v3 & mask)};

            destinationUshort[i] = pixel.V1;
            destinationUshort[i + pixelStride] = pixel.V2;
            destinationUshort[i + 2 * pixelStride] = pixel.V3;
        }
    }
}

internal class ProcessEncodedSingleComponentToLine8Bit4Components : IProcessLineEncoded
{
    public void NewLineRequested(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
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
}

internal class ProcessEncodedSingleComponentToLine16Bit4Components : IProcessLineEncoded
{
    public void NewLineRequested(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
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
}
