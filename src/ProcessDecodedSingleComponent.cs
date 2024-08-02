// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Diagnostics;

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
}
