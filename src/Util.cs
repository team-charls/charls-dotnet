// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

internal static class Util
{
    internal static InvalidDataException CreateInvalidDataException(JpegLSError error)
    {
        InvalidDataException exception;

        switch (error)
        {
            case JpegLSError.TooMuchEncodedData:
            case JpegLSError.ParameterValueNotSupported:
            case JpegLSError.InvalidEncodedData:
            case JpegLSError.SourceBufferTooSmall:
            case JpegLSError.BitDepthForTransformNotSupported:
            case JpegLSError.ColorTransformNotSupported:
            case JpegLSError.EncodingNotSupported:
            case JpegLSError.UnknownJpegMarkerFound:
            case JpegLSError.JpegMarkerStartByteNotFound:
            case JpegLSError.StartOfImageMarkerNotFound:
            case JpegLSError.UnexpectedMarkerFound:
            case JpegLSError.InvalidMarkerSegmentSize:
            case JpegLSError.DuplicateStartOfImageMarker:
            case JpegLSError.DuplicateStartOfFrameMarker:
            case JpegLSError.DuplicateComponentIdInStartOfFrameSegment:
            case JpegLSError.UnexpectedEndOfImageMarker:
            case JpegLSError.InvalidJpegLSPresetParameterType:
            case JpegLSError.JpeglsPresetExtendedParameterTypeNotSupported:
            case JpegLSError.MissingEndOfSpiffDirectory:
            case JpegLSError.InvalidParameterWidth:
            case JpegLSError.InvalidParameterHeight:
            case JpegLSError.InvalidParameterComponentCount:
            case JpegLSError.InvalidParameterBitsPerSample:
            case JpegLSError.InvalidParameterInterleaveMode:
            case JpegLSError.InvalidParameterNearLossless:
            case JpegLSError.InvalidParameterJpeglsPresetCodingParameters:
            case JpegLSError.UnexpectedFailure:
            case JpegLSError.NotEnoughMemory:
            case JpegLSError.UnexpectedRestartMarker:
                exception = new InvalidDataException("TODO");
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(error), error, null);
        }

        exception.Data.Add(nameof(JpegLSError), error);
        return exception;
    }

    /// <summary>
    /// Computes how many bytes are needed to hold the number of bits.
    /// </summary>
    internal static int BitToByteCount(int bitCount)
    {
        return (bitCount + 7) / 8;
    }

    internal static int CalculateMaximumSampleValue(int bitsPerSample)
    {
        return 1 << bitsPerSample - 1;
    }

    internal static int ComputeMaximumNearLossless(int maximumSampleValue)
    {
        return Math.Min(Constants.MaximumNearLossless, maximumSampleValue / 2); // As defined by ISO/IEC 14495-1, C.2.3
    }
}
