// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.JpegLS;

internal abstract class ScanEncoder(
    FrameInfo frameInfo,
    JpegLSPresetCodingParameters presetCodingParameters,
    CodingParameters codingParameters)
    : ScanCodec(frameInfo, presetCodingParameters, codingParameters)
{
    private Memory<byte> _destination;
    private uint _bitBuffer;
    /// <summary>
    /// /private int _freeBitCount = 4 * 8;
    /// </summary>
    private int _position;

    public abstract int EncodeScan(ReadOnlyMemory<byte> source, Memory<byte> destination, int stride);

    internal void Initialize(Memory<byte> destination)
    {
        _destination = destination;
    }
}
