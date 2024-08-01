// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

internal class ScanCodecFactory
{
    internal static ScanDecoder CreateScanDecoder(FrameInfo frameInfo, JpegLSPresetCodingParameters presetCodingParameters, CodingParameters codingParameters)
    {
        var traits = new LosslessTraits8();
        return new ScanDecoderImpl<byte, byte>(frameInfo, presetCodingParameters, codingParameters, traits);
    }
}
