// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

internal class ProcessEncodedSingleComponent : IProcessLineEncoded
{
    public void NewLineRequested(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount)
    {
        source[..pixelCount].CopyTo(destination);
    }
}
