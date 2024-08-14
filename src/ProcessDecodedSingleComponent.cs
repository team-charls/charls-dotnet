// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Runtime.InteropServices;

namespace CharLS.Managed;

internal class ProcessDecodedSingleComponent : IProcessLineDecoded
{
    public int LineDecoded(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        source[..pixelCount].CopyTo(destination);
        return pixelCount;
    }
}


// Purpose: this class will copy the 3 decoded lines to destination buffer in by-p
internal class ProcessDecodedSingleComponentToLine8Bit3Components : IProcessLineDecoded
{
    public int LineDecoded(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(destination);

        for (int i = 0; i < pixelCount; ++i)
        {
            destinationTriplet[i] = new Triplet<byte>(
                source[i], source[i + sourceStride], source[i + 2 * sourceStride]);
        }

        return pixelCount * 3;
    }
}

// Purpose: this class will copy the 3 decoded lines to destination buffer in by-p
internal class ProcessDecodedSingleComponentToLine16Bit3Components : IProcessLineDecoded
{
    public int LineDecoded(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
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
}

internal class ProcessDecodedSingleComponentToLine3Components8BitHP1 : IProcessLineDecoded
{
    public int LineDecoded(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
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
}

internal class ProcessDecodedSingleComponentToLine3Components16BitHP1 : IProcessLineDecoded
{
    public int LineDecoded(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
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
}

internal class ProcessDecodedSingleComponentToLine3Components8BitHP2 : IProcessLineDecoded
{
    public int LineDecoded(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
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
}

internal class ProcessDecodedSingleComponentToLine3Components16BitHP2 : IProcessLineDecoded
{
    public int LineDecoded(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
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
}

internal class ProcessDecodedSingleComponentToLine3Components8BitHP3 : IProcessLineDecoded
{
    public int LineDecoded(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
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
}

internal class ProcessDecodedSingleComponentToLine3Components16BitHP3 : IProcessLineDecoded
{
    public int LineDecoded(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
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
}

internal class ProcessDecodedSingleComponentToLine8Bit4Components : IProcessLineDecoded
{
    public int LineDecoded(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        var destinationQuad = MemoryMarshal.Cast<byte, Quad<byte>>(destination);

        for (int i = 0; i < pixelCount; ++i)
        {
            destinationQuad[i] = new Quad<byte>(
                source[i], source[i + sourceStride], source[i + 2 * sourceStride], source[i + 3 * sourceStride]);
        }

        return pixelCount * 4;
    }
}

internal class ProcessDecodedSingleComponentToLine16Bit4Components : IProcessLineDecoded
{
    public int LineDecoded(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
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
}


internal class ProcessDecodedTripletComponent8Bit : IProcessLineDecoded
{
    public int LineDecoded(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        int bytesCount = pixelCount * 3;
        source[..bytesCount].CopyTo(destination);
        return bytesCount;
    }
}

internal class ProcessDecodedTripletComponent8BitHP1 : IProcessLineDecoded
{
    public int LineDecoded(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
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
}

internal class ProcessDecodedTripletComponent16BitHP1 : IProcessLineDecoded
{
    public int LineDecoded(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
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
}


internal class ProcessDecodedTripletComponent8BitHP2 : IProcessLineDecoded
{
    public int LineDecoded(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
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
}


internal class ProcessDecodedTripletComponent16BitHP2 : IProcessLineDecoded
{
    public int LineDecoded(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
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
}


internal class ProcessDecodedTripletComponent8BitHP3 : IProcessLineDecoded
{
    public int LineDecoded(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
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
}


internal class ProcessDecodedTripletComponent16BitHP3 : IProcessLineDecoded
{
    public int LineDecoded(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
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
}


internal class ProcessDecodedTripletComponent16Bit : IProcessLineDecoded
{
    public int LineDecoded(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        int bytesCount = pixelCount * 3 * 2;
        source[..bytesCount].CopyTo(destination);
        return bytesCount;
    }
}


internal class ProcessDecodedQuadComponent8Bit : IProcessLineDecoded
{
    public int LineDecoded(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        int bytesCount = pixelCount * 4;
        source[..bytesCount].CopyTo(destination);
        return bytesCount;
    }
}

internal class ProcessDecodedQuadComponent16Bit : IProcessLineDecoded
{
    public int LineDecoded(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        int bytesCount = pixelCount * 4 * 2;
        source[..bytesCount].CopyTo(destination);
        return bytesCount;
    }
}
