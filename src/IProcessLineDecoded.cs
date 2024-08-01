// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

public interface IProcessLineDecoded
{
    void LineDecoded(Span<byte> source, Span<byte> destination, int pixelCount, int sourceStride);
}
