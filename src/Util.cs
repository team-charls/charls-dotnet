// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

internal static class Util
{
    internal static InvalidDataException CreateInvalidDataException(ErrorCode errorCode)
    {
        InvalidDataException exception;

        switch (errorCode)
        {
            case ErrorCode.TooMuchEncodedData:
            case ErrorCode.ParameterValueNotSupported:
            case ErrorCode.InvalidEncodedData:
            case ErrorCode.SourceBufferTooSmall:
            case ErrorCode.BitDepthForTransformNotSupported:
            case ErrorCode.ColorTransformNotSupported:
            case ErrorCode.EncodingNotSupported:
            case ErrorCode.UnknownJpegMarkerFound:
            case ErrorCode.JpegMarkerStartByteNotFound:
            case ErrorCode.StartOfImageMarkerNotFound:
            case ErrorCode.UnexpectedMarkerFound:
            case ErrorCode.InvalidMarkerSegmentSize:
            case ErrorCode.DuplicateStartOfImageMarker:
            case ErrorCode.DuplicateStartOfFrameMarker:
            case ErrorCode.DuplicateComponentIdInStartOfFrameSegment:
            case ErrorCode.UnexpectedEndOfImageMarker:
            case ErrorCode.InvalidJpegLSPresetParameterType:
            case ErrorCode.JpeglsPresetExtendedParameterTypeNotSupported:
            case ErrorCode.MissingEndOfSpiffDirectory:
            case ErrorCode.InvalidParameterWidth:
            case ErrorCode.InvalidParameterHeight:
            case ErrorCode.InvalidParameterComponentCount:
            case ErrorCode.InvalidParameterBitsPerSample:
            case ErrorCode.InvalidParameterInterleaveMode:
            case ErrorCode.InvalidParameterNearLossless:
            case ErrorCode.InvalidParameterJpeglsPresetCodingParameters:
            case ErrorCode.UnexpectedFailure:
            case ErrorCode.NotEnoughMemory:
            case ErrorCode.UnexpectedRestartMarker:
            case ErrorCode.RestartMarkerNotFound:
                exception = new InvalidDataException("TODO");
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(errorCode), errorCode, null);
        }

        exception.Data.Add(nameof(ErrorCode), errorCode);
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
