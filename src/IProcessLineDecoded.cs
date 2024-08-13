// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed;

internal interface IProcessLineDecoded
{
    int LineDecoded(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride);

    int LineDecoded(Span<Triplet<byte>> source, Span<byte> destination, int pixelCount, int sourceStride);

    int LineDecoded(Span<ushort> source, Span<byte> destination, int pixelCount, int sourceStride);
}
