// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Diagnostics;

namespace CharLS.Managed;

internal abstract class ScanEncoder(
    FrameInfo frameInfo,
    JpegLSPresetCodingParameters presetCodingParameters,
    CodingParameters codingParameters)
    : ScanCodec(frameInfo, presetCodingParameters, codingParameters)
{
    private Memory<byte> _destination;
    private uint _bitBuffer;
    private int _freeBitCount = 4 * 8;
    private int _position;
    private int _compressedLength;
    private bool _is_ff_written;
    private int _bytes_written;

    public abstract int EncodeScan(ReadOnlyMemory<byte> source, Memory<byte> destination, int stride);

    internal void Initialize(Memory<byte> destination)
    {
        _destination = destination;
        _compressedLength = destination.Length;
    }

    protected void EncodeRunPixels(int runLength, bool endOfLine)
    {
        while (runLength >= 1 << J[RunIndex])
        {
            append_ones_to_bit_stream(1);
            runLength = runLength - (1 << J[RunIndex]);
            IncrementRunIndex();
        }

        if (endOfLine)
        {
            if (runLength != 0)
            {
                append_ones_to_bit_stream(1);
            }
        }
        else
        {
            append_to_bit_stream((uint)runLength, J[RunIndex] + 1); // leading 0 + actual remaining length
        }
    }

    protected void append_to_bit_stream(uint bits, int bitCount)
    {
        Debug.Assert(bitCount is >= 0 and < 32);
        Debug.Assert((bits | ((1U << bitCount) - 1U)) == ((1U << bitCount) - 1U)); // Not used bits must be set to zero.

        _freeBitCount -= bitCount;
        if (_freeBitCount >= 0)
        {
            _bitBuffer |= bits << _freeBitCount;
        }
        else
        {
            // Add as many bits in the remaining space as possible and flush.
            _bitBuffer |= bits >> -_freeBitCount;
            Flush();

            // A second flush may be required if extra marker detect bits were needed and not all bits could be written.
            if (_freeBitCount < 0)
            {
                _bitBuffer |= bits >> -_freeBitCount;
                Flush();
            }

            Debug.Assert(_freeBitCount >= 0);
            _bitBuffer |= bits << _freeBitCount;
        }
    }

    private void append_ones_to_bit_stream(int bitCount)
    {
        append_to_bit_stream((1U << bitCount) - 1U, bitCount);
    }

    protected void EndScan()
    {
        Flush();

        // if a 0xff was written, Flush() will force one unset bit anyway
        if (_is_ff_written)
        {
            append_to_bit_stream(0, (_freeBitCount - 1) % 8);
        }

        Flush();
        Debug.Assert(_freeBitCount == 32);
    }

    protected int GetLength()
    {
        return _bytes_written - ((_freeBitCount) - 32) / 8;
    }

    private void Flush()
    {
        if (_compressedLength < 4)
            throw Util.CreateInvalidDataException(ErrorCode.SourceBufferTooSmall);

        for (int i = 0; i < 4; ++i)
        {
            if (_freeBitCount >= 32)
            {
                _freeBitCount = 32;
                break;
            }

            if (_is_ff_written)
            {
                // JPEG-LS requirement (T.87, A.1) to detect markers: after a xFF value a single 0 bit needs to be inserted.
                _destination.Span[_position] = (byte)(_bitBuffer >> 25);
                _bitBuffer = _bitBuffer << 7;
                _freeBitCount += 7;
            }
            else
            {
                _destination.Span[_position] = (byte)(_bitBuffer >> 24);
                _bitBuffer = _bitBuffer << 8;
                _freeBitCount += 8;
            }

            _is_ff_written = _destination.Span[_position] == Constants.JpegMarkerStartByte;
            ++_position;
            --_compressedLength;
            ++_bytes_written;
        }
    }


}
