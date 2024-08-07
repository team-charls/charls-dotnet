// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

internal class ScanCodecFactory
{
    internal static ScanDecoder CreateScanDecoder(FrameInfo frameInfo, JpegLSPresetCodingParameters presetCodingParameters, CodingParameters codingParameters)
    {
        int maximumSampleValue = Algorithm.CalculateMaximumSampleValue(frameInfo.BitsPerSample);

        if (codingParameters.NearLossless == 0)
        {
            if (codingParameters.InterleaveMode == JpegLSInterleaveMode.Sample)
            {
                if (frameInfo.ComponentCount == 3 && frameInfo.BitsPerSample == 8)
                {
                    var traits = new LosslessTraitsTriplet<byte>(maximumSampleValue, 0);
                    return new ScanDecoderImpl<byte, Triplet<byte>>(frameInfo, presetCodingParameters, codingParameters, traits);
                }

                throw new NotImplementedException();
            }
            switch (frameInfo.BitsPerSample)
            {
                case 8:
                    return new ScanDecoderImpl<byte, byte>(frameInfo, presetCodingParameters, codingParameters, new LosslessTraits8());

                case 12:
                    return new ScanDecoderImpl<ushort, ushort>(frameInfo, presetCodingParameters, codingParameters,
                        new LosslessTraitsImplT<ushort, ushort>(maximumSampleValue, 0));

                case 16:
                    return new ScanDecoderImpl<ushort, ushort>(frameInfo, presetCodingParameters, codingParameters, new LosslessTraits16(16));
            }
        }

        if (frameInfo.BitsPerSample <= 8)
        {
            var traits = new DefaultTraits<byte, byte>(maximumSampleValue, frameInfo.BitsPerSample, codingParameters.NearLossless);
            return new ScanDecoderImpl<byte, byte>(frameInfo, presetCodingParameters, codingParameters, traits);
        }

        throw new NotImplementedException();
    }
}
