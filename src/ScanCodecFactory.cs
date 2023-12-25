// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

internal class ScanCodecFactory
{
    internal static Decoder CreateScanDecoder(FrameInfo frameInfo, JpegLSInterleaveMode interleaveMode, ReadOnlyMemory<byte> source)
    {
        return new Decoder(frameInfo, interleaveMode, source);
    }
}
