// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CharLS.JpegLS;

internal class ProcessDecodedSingleComponent : IProcessLineDecoded
{
    private int _stride;
    private int _bytesPerPixel;

    internal ProcessDecodedSingleComponent(int stride, int bytesPerPixel)
    {
        _stride = stride;
        _bytesPerPixel = bytesPerPixel;
    }

    public int LineDecoded(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        int bytesCount = pixelCount * _bytesPerPixel;
        Debug.Assert(bytesCount <= _stride);
        source[..bytesCount].CopyTo(destination);
        return _stride;
    }

    public int LineDecoded(Span<Triplet<byte>> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        throw new NotImplementedException();
    }

    public int LineDecoded(Span<ushort> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        throw new NotImplementedException();
    }
}


// Purpose: this class will copy the 3 decoded lines to destination buffer in by-p
internal class ProcessDecodedSingleComponentToLine3Components : IProcessLineDecoded
{
    private int _stride;
    private int _bytesPerPixel;

    internal ProcessDecodedSingleComponentToLine3Components(int stride, int bytesPerPixel)
    {
        _stride = stride;
        _bytesPerPixel = bytesPerPixel;
    }

    public int LineDecoded(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(destination);

        int bytesCount = pixelCount * _bytesPerPixel;
        Debug.Assert(bytesCount <= _stride);

        for (int i = 0; i < pixelCount; ++i)
        {
            destinationTriplet[i] = new Triplet<byte>(
                source[i], source[i + sourceStride], source[i + 2 * sourceStride]);
        }

        return _stride;
    }

    public int LineDecoded(Span<Triplet<byte>> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        throw new NotImplementedException();
    }

    public int LineDecoded(Span<ushort> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        throw new NotImplementedException();
    }
}


internal class ProcessDecodedSingleComponentToLine4Components : IProcessLineDecoded
{
    private int _stride;
    private int _bytesPerPixel;

    internal ProcessDecodedSingleComponentToLine4Components(int stride, int bytesPerPixel)
    {
        _stride = stride;
        _bytesPerPixel = bytesPerPixel;
    }

    public int LineDecoded(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        var destinationQuad = MemoryMarshal.Cast<byte, Quad<byte>>(destination);

        int bytesCount = pixelCount * _bytesPerPixel;
        Debug.Assert(bytesCount <= _stride);

        for (int i = 0; i < pixelCount; ++i)
        {
            destinationQuad[i] = new Quad<byte>(
                source[i], source[i + sourceStride], source[i + 2 * sourceStride], source[i + 3 * sourceStride]);
        }

        return _stride;
    }

    public int LineDecoded(Span<Triplet<byte>> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        throw new NotImplementedException();
    }

    public int LineDecoded(Span<ushort> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        var destinationQuad = MemoryMarshal.Cast<byte, Quad<ushort>>(destination);

        int bytesCount = pixelCount * _bytesPerPixel;
        Debug.Assert(bytesCount <= _stride);

        for (int i = 0; i < pixelCount; ++i)
        {
            destinationQuad[i] = new Quad<ushort>(
                source[i], source[i + sourceStride], source[i + 2 * sourceStride], source[i + 3 * sourceStride]);
        }

        return _stride;
    }
}


internal class ProcessDecodedTripletComponent : IProcessLineDecoded
{
    private int _stride;
    private int _bytesPerPixel;

    internal ProcessDecodedTripletComponent(int stride, int bytesPerPixel)
    {
        _stride = stride;
        _bytesPerPixel = bytesPerPixel;
    }

    public int LineDecoded(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        throw new NotImplementedException();
    }

    public int LineDecoded(Span<Triplet<byte>> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(destination);

        int bytesCount = pixelCount * _bytesPerPixel;
        Debug.Assert(bytesCount <= _stride);
        source[..pixelCount].CopyTo(destinationTriplet);
        return _stride;
    }

    public int LineDecoded(Span<ushort> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        throw new NotImplementedException();
    }
}

internal class ProcessDecodedQuadComponent : IProcessLineDecoded
{
    private int _stride;
    private int _bytesPerPixel;

    internal ProcessDecodedQuadComponent(int stride, int bytesPerPixel)
    {
        _stride = stride;
        _bytesPerPixel = bytesPerPixel;
    }

    public int LineDecoded(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        int bytesCount = pixelCount * 4;
        Debug.Assert(bytesCount <= _stride);
        source[..bytesCount].CopyTo(destination);
        return bytesCount;
    }

    public int LineDecoded(Span<Triplet<byte>> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        throw new NotImplementedException();
    }

    public int LineDecoded(Span<ushort> source, Span<byte> destination, int pixelCount, int sourceStride)
    {
        throw new NotImplementedException();
    }
}


