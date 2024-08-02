// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

internal class ScanCodecFactory
{
    internal static ScanDecoder CreateScanDecoder(FrameInfo frameInfo, JpegLSPresetCodingParameters presetCodingParameters, CodingParameters codingParameters)
    {
        if (codingParameters.NearLossless == 0)
        {
            switch (frameInfo.BitsPerSample)
            {
                case 8:
                    var traits = new LosslessTraits8();
                    return new ScanDecoderImpl<byte, byte>(frameInfo, presetCodingParameters, codingParameters, traits);
                case 12:
                    throw new NotImplementedException();

                case 16:
                    throw new NotImplementedException();
            }
        }

        int maximumSampleValue = Algorithm.CalculateMaximumSampleValue(frameInfo.BitsPerSample);

        if (frameInfo.BitsPerSample <= 8)
        {
            var traits = new DefaultTraits<byte, byte>(maximumSampleValue, frameInfo.BitsPerSample, codingParameters.NearLossless);
            return new ScanDecoderImpl<byte, byte>(frameInfo, presetCodingParameters, codingParameters, traits);
        }

        throw new NotImplementedException();
    }
}
