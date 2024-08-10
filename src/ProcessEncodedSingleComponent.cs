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

internal class ProcessEncodedSingleComponentToLine3Components : IProcessLineEncoded
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

internal class ProcessEncodedSingleComponentToLine4Components : IProcessLineEncoded
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
