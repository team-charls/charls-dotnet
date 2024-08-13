// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Buffers.Binary;
using System.Diagnostics;

namespace CharLS.Managed;

internal abstract class ScanDecoder(
    FrameInfo frameInfo,
    JpegLSPresetCodingParameters presetCodingParameters,
    CodingParameters codingParameters)
    : ScanCodec(frameInfo, presetCodingParameters, codingParameters)
{
    private ReadOnlyMemory<byte> _source;
    private int _position;
    private int _endPosition;
    private int _positionFF;
    private int _validBits;
    private ulong _readCache; // TODO: change for 32-bit build
    private const int CacheBitCount = sizeof(ulong) * 8;
    private const int MaxReadableCacheBits = CacheBitCount - 8;

    protected static readonly GolombCodeTable[] ColombCodeTable =
    [
        GolombCodeTable.Create(0), GolombCodeTable.Create(1), GolombCodeTable.Create(2), GolombCodeTable.Create(3),
        GolombCodeTable.Create(4), GolombCodeTable.Create(5), GolombCodeTable.Create(6), GolombCodeTable.Create(7),
        GolombCodeTable.Create(8), GolombCodeTable.Create(9), GolombCodeTable.Create(10), GolombCodeTable.Create(11),
        GolombCodeTable.Create(12), GolombCodeTable.Create(13), GolombCodeTable.Create(14), GolombCodeTable.Create(15)
    ];

    public abstract int DecodeScan(ReadOnlyMemory<byte> source, Span<byte> destination, int stride);

    protected void Initialize(ReadOnlyMemory<byte> source)
    {
        _source = source;
        _position = 0;
        _endPosition = source.Length;

        FindJpegMarkerStartByte();
        FillReadCache();
    }

    protected void Reset()
    {
        _validBits = 0;
        _readCache = 0;

        FindJpegMarkerStartByte();
        FillReadCache();
    }

    protected void SkipBits(int bitCount)
    {
        Debug.Assert(bitCount > 0);

        _validBits -= bitCount; // Note: valid_bits_ may become negative to indicate that extra bits are needed.
        _readCache = _readCache << bitCount;
    }

    protected void EndScan()
    {
        if (_position >= _endPosition)
            throw Util.CreateInvalidDataException(ErrorCode.SourceBufferTooSmall);

        var source = _source.Span;
        if (source[_position] != Constants.JpegMarkerStartByte)
        {
            _ = ReadBit();

            if (_position >= _endPosition)
                throw Util.CreateInvalidDataException(ErrorCode.SourceBufferTooSmall);

            if (source[_position] != Constants.JpegMarkerStartByte)
                throw Util.CreateInvalidDataException(ErrorCode.TooMuchEncodedData);
        }

        if (_readCache != 0)
            throw Util.CreateInvalidDataException(ErrorCode.TooMuchEncodedData);
    }

    protected int get_cur_byte_pos()
    {
        int validBits = _validBits;

        var source = _source.Span;
        for (;;)
        {
            int lastBitsCount = source[_position - 1] == Constants.JpegMarkerStartByte ? 7 : 8;

            if (validBits < lastBitsCount)
                return _position;

            validBits -= lastBitsCount;
            --_position;
        }
    }

    protected int DecodeValue(int k, int limit, int quantizedBitsPerPixel)
    {
        int highBits = ReadHighBits();

        if (highBits >= limit - (quantizedBitsPerPixel + 1))
            return ReadValue(quantizedBitsPerPixel) + 1;

        if (k == 0)
            return highBits;

        return (highBits << k) + ReadValue(k);
    }

    protected int ReadValue(int bitCount)
    {
        Debug.Assert(bitCount is > 0 and < 32);

        if (_validBits < bitCount)
        {
            FillReadCache();
            if (_validBits < bitCount)
                throw Util.CreateInvalidDataException(ErrorCode.InvalidEncodedData);
        }

        Debug.Assert(bitCount <= _validBits);
        int result = (int)(_readCache >> (CacheBitCount - bitCount));
        SkipBits(bitCount);
        return result;
    }

    protected byte PeekByte()
    {
        if (_validBits < 8)
        {
            FillReadCache();
        }

        return (byte)(_readCache >> MaxReadableCacheBits);
    }

    protected bool ReadBit()
    {
        if (_validBits <= 0)
        {
            FillReadCache();
        }

        bool set = (_readCache & 1UL << (CacheBitCount - 1)) != 0;
        SkipBits(1);
        return set;
    }

    protected int peek_0_bits()
    {
        if (_validBits < 16)
        {
            FillReadCache();
        }

        var valueTest = _readCache;
        for (int count = 0; count < 16; ++count)
        {
            if ((valueTest & (1UL << (CacheBitCount - 1))) != 0)
                return count;

            valueTest <<= 1;
        }
        return -1;
    }

    protected int ReadHighBits()
    {
        int count = peek_0_bits();
        if (count >= 0)
        {
            SkipBits(count + 1);
            return count;
        }

        SkipBits(15);

        for (int highBitsCount = 15; ; ++highBitsCount)
        {
            if (ReadBit())
                return highBitsCount;
        }
    }

    protected byte ReadByte()
    {
        if (_position == _endPosition)
            throw Util.CreateInvalidDataException(ErrorCode.SourceBufferTooSmall);

        byte value = _source.Span[_position];
        ++_position;
        return value;
    }

    protected void ReadRestartMarker(int expectedRestartMarkerId)
    {
        byte value = ReadByte();

        if (value != Constants.JpegMarkerStartByte)
            throw Util.CreateInvalidDataException(ErrorCode.RestartMarkerNotFound);

        // Read all preceding 0xFF fill bytes until a non 0xFF byte has been found. (see T.81, B.1.1.2)
        do
        {
            value = ReadByte();
        } while (value == Constants.JpegMarkerStartByte);

        if (value != Constants.JpegRestartMarkerBase + expectedRestartMarkerId)
            throw Util.CreateInvalidDataException(ErrorCode.RestartMarkerNotFound);
    }

    private void FindJpegMarkerStartByte()
    {
        int positionFF = _source[_position..].Span.IndexOf(Constants.JpegMarkerStartByte);
        _positionFF = positionFF == -1 ? _endPosition : _position + positionFF;
    }

    private void FillReadCache()
    {
        // ASSERT(valid_bits_ <= max_readable_cache_bits);

        if (FillReadCacheOptimistic())
            return;

        var source = _source.Span;
        do
        {
            if (_position >= _endPosition)
            {
                if (_validBits == 0)
                {
                    // Decoding process expects at least some bits to be added to the cache.
                    throw Util.CreateInvalidDataException(ErrorCode.InvalidEncodedData);
                }

                return;
            }

            ulong newByteValue = source[_position];

            // JPEG-LS bit stream rule: if FF is followed by a 1 bit then it is a marker
            if (newByteValue == Constants.JpegMarkerStartByte &&
                (_position == _endPosition - 1 || (source[_position + 1] & 0x80) != 0))
            {
                if (_validBits <= 0)
                {
                    // Decoding process expects at least some bits to be added to the cache.
                    throw Util.CreateInvalidDataException(ErrorCode.InvalidEncodedData);
                }

                // End of buffer or marker detected. Typical found markers are EOI, SOS (next scan) or RSTm.
                return;
            }

            _readCache |= newByteValue << (MaxReadableCacheBits - _validBits);
            _validBits += 8;
            ++_position;

            if (newByteValue == Constants.JpegMarkerStartByte)
            {
                // The next bit after an 0xFF needs to be ignored, compensate for the next read (see ISO/IEC 14495-1,A.1)
                --_validBits;
            }

        } while (_validBits < MaxReadableCacheBits);

        FindJpegMarkerStartByte();
    }

    private bool FillReadCacheOptimistic()
    {
        if (_position >= _positionFF - (sizeof(ulong) - 1))
            return false;

        // Easy & fast: there is no 0xFF byte in sight, read without bit stuffing
        _readCache |= BinaryPrimitives.ReadUInt64BigEndian(_source.Span[_position..]) >> _validBits;
        int bytesConsumed = (CacheBitCount - _validBits) / 8;
        _position += bytesConsumed;
        _validBits += bytesConsumed * 8;
        Debug.Assert(_validBits >= MaxReadableCacheBits);
        return true;
    }
}
