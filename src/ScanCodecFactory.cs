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
            if (codingParameters.InterleaveMode == InterleaveMode.Sample)
            {
                if (frameInfo.ComponentCount == 3 && frameInfo.BitsPerSample == 8)
                {
                    var traits = new LosslessTraitsTriplet(maximumSampleValue, 0, presetCodingParameters.ResetValue);
                    return new ScanDecoderImpl<byte, Triplet<byte>>(frameInfo, presetCodingParameters, codingParameters, traits);
                }
                if (frameInfo.ComponentCount == 4 && frameInfo.BitsPerSample == 8)
                {
                    var traits = new LosslessTraitsQuad(maximumSampleValue, 0, presetCodingParameters.ResetValue);
                    return new ScanDecoderImpl<byte, Quad<byte>>(frameInfo, presetCodingParameters, codingParameters, traits);
                }
                if (frameInfo.ComponentCount == 4 && frameInfo.BitsPerSample == 16)
                {
                    var traits = new LosslessTraitsQuad(maximumSampleValue, 0, presetCodingParameters.ResetValue);
                    return new ScanDecoderImpl<byte, Quad<byte>>(frameInfo, presetCodingParameters, codingParameters, traits);
                }

                throw new NotImplementedException();
            }
            switch (frameInfo.BitsPerSample)
            {
                case 8:
                    return new ScanDecoderImpl<byte, byte>(frameInfo, presetCodingParameters, codingParameters,
                        new LosslessTraits8(maximumSampleValue, 0, presetCodingParameters.ResetValue));

                case 12:
                    return new ScanDecoderImpl<ushort, ushort>(frameInfo, presetCodingParameters, codingParameters,
                        new LosslessTraitsImpl(maximumSampleValue, 0, presetCodingParameters.ResetValue));

                case 16:
                    return new ScanDecoderImpl<ushort, ushort>(frameInfo, presetCodingParameters, codingParameters,
                        new LosslessTraits16(maximumSampleValue, 0, presetCodingParameters.ResetValue));
            }
        }

        if (frameInfo.BitsPerSample <= 8)
        {
            var traits = new DefaultTraits(maximumSampleValue, codingParameters.NearLossless, presetCodingParameters.ResetValue);
            return new ScanDecoderImpl<byte, byte>(frameInfo, presetCodingParameters, codingParameters, traits);
        }
        else
        {
            var traits = new DefaultTraits(maximumSampleValue, codingParameters.NearLossless, presetCodingParameters.ResetValue);
            return new ScanDecoderImpl<ushort, ushort>(frameInfo, presetCodingParameters, codingParameters, traits);
        }
    }

    internal static ScanEncoder CreateScanEncoder(FrameInfo frameInfo,
        JpegLSPresetCodingParameters presetCodingParameters, CodingParameters codingParameters)
    {
        int maximumSampleValue = Algorithm.CalculateMaximumSampleValue(frameInfo.BitsPerSample);
        var traits = new DefaultTraits(maximumSampleValue, codingParameters.NearLossless, presetCodingParameters.ResetValue);
        return new ScanEncoderImpl(frameInfo, presetCodingParameters, codingParameters, traits);
    }
}
