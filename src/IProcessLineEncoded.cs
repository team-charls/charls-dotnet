// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed;

internal interface IProcessLineEncoded
{
    void NewLineRequested(ReadOnlySpan<byte> source, Span<byte> destination, int pixelCount);
}
