// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Buffers.Binary;

namespace CharLS.JpegLS;

internal abstract class ScanDecoder(
    FrameInfo frameInfo,
    JpegLSPresetCodingParameters presetCodingParameters,
    CodingParameters codingParameters)
    : ScanCodec(frameInfo, presetCodingParameters, codingParameters)
{
    private int _position;
    private int _endPosition;
    private int _positionFF;
    private int _length;
    private int _validBits;
    private ulong _readCache; // TODO: change for 32-bit build
    private const int CacheBitCount = sizeof(ulong) * 8;
    private const int MaxReadableCacheBits = CacheBitCount - 8;

    // Used to determine how large runs should be encoded at a time.
    // Defined by the JPEG-LS standard, A.2.1., Initialization step 3.
    protected static readonly int[] J =
    [
        0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 9, 10,
        11, 12, 13, 14, 15
    ];

    protected static readonly GolombCodeTable[] ColombCodeTable =
    [
        GolombCodeTable.Create(0), GolombCodeTable.Create(1), GolombCodeTable.Create(2), GolombCodeTable.Create(3),
        GolombCodeTable.Create(4), GolombCodeTable.Create(5), GolombCodeTable.Create(6), GolombCodeTable.Create(7),
        GolombCodeTable.Create(8), GolombCodeTable.Create(9), GolombCodeTable.Create(10), GolombCodeTable.Create(11),
        GolombCodeTable.Create(12), GolombCodeTable.Create(13), GolombCodeTable.Create(14), GolombCodeTable.Create(15)
    ];

    public abstract uint DecodeScan(ReadOnlySpan<byte> source, Span<byte> destination, int stride);

    protected internal void Initialize(ReadOnlySpan<byte> source)
    {
        _position = 0;
        _endPosition = source.Length;

        FindJpegMarkerStartByte(source);
        FillReadCache(source);
    }

    protected void Reset(ReadOnlySpan<byte> source)
    {
        _validBits = 0;
        _readCache = 0;

        FindJpegMarkerStartByte(source);
        FillReadCache(source);
    }

    protected void SkipBits(int length)
    {
        //ASSERT(length);

        _validBits -= length; // Note: valid_bits_ may become negative to indicate that extra bits are needed.
        _readCache = _readCache << length;
    }

    protected void ResetParameters(int range)
    {
        var regularModeContext = new RegularModeContext(range);
        for (int i = 0; i < RegularModeContext.Length; i++)
        {
            RegularModeContext[i] = regularModeContext;
        }

        RunModeContexts[0] = new RunModeContext(0, range);
        RunModeContexts[1] = new RunModeContext(1, range);
        RunIndex = 0;
    }

    protected void EndScan(ReadOnlySpan<byte> source)
    {
        if (_position >= _endPosition)
            throw Util.CreateInvalidDataException(JpegLSError.SourceBufferTooSmall);

        if (source[_position] != Constants.JpegMarkerStartByte)
        {
            ReadBit(source);

            if (_position >= _endPosition)
                throw Util.CreateInvalidDataException(JpegLSError.SourceBufferTooSmall);

            if (source[_position] != Constants.JpegMarkerStartByte)
                throw Util.CreateInvalidDataException(JpegLSError.TooMuchEncodedData);
        }

        if (_readCache != 0)
            throw Util.CreateInvalidDataException(JpegLSError.TooMuchEncodedData);
    }

    protected int DecodeValue(ReadOnlySpan<byte> source, int k, int limit, int quantizedBitsPerPixel)
    {
        int highBits = ReadHighBits(source);

        if (highBits >= limit - (quantizedBitsPerPixel + 1))
            return ReadValue(source, quantizedBitsPerPixel) + 1;

        if (k == 0)
            return highBits;

        return (highBits << k) + ReadValue(source, k);
    }

    protected int ReadValue(ReadOnlySpan<byte> source, int length)
    {
        if (_validBits < length)
        {
            FillReadCache(source);
            if (_validBits < length)
                throw Util.CreateInvalidDataException(JpegLSError.InvalidEncodedData);
        }

        ////ASSERT(length != 0 && length <= valid_bits_);
        ////ASSERT(length< 32);
        int result = (int)(_readCache >> (CacheBitCount - length));
        SkipBits(length);
        return result;
    }

    protected byte PeekByte(ReadOnlySpan<byte> source)
    {
        if (_validBits < 8)
        {
            FillReadCache(source);
        }

        return (byte)(_readCache >> MaxReadableCacheBits);
    }

    protected bool ReadBit(ReadOnlySpan<byte> source)
    {
        if (_validBits <= 0)
        {
            FillReadCache(source);
        }

        bool set = (_readCache & 1UL << (CacheBitCount - 1)) != 0;
        SkipBits(1);
        return set;
    }

    protected int peek_0_bits(ReadOnlySpan<byte> source)
    {
        if (_validBits < 16)
        {
            FillReadCache(source);
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

    protected int ReadHighBits(ReadOnlySpan<byte> source)
    {
        int count = peek_0_bits(source);
        if (count >= 0)
        {
            SkipBits(count + 1);
            return count;
        }

        SkipBits(15);

        for (int highBitsCount = 15; ; ++highBitsCount)
        {
            if (ReadBit(source))
                return highBitsCount;
        }
    }

    protected byte ReadByte(ReadOnlySpan<byte> source)
    {
        if (_position == _endPosition)
            throw Util.CreateInvalidDataException(JpegLSError.SourceBufferTooSmall);

        byte value = source[_position];
        ++_position;
        return value;
    }

    protected void ReadRestartMarker(ReadOnlySpan<byte> source, int expectedRestartMarkerId)
    {
        byte value = ReadByte(source);

        if (value != Constants.JpegMarkerStartByte)
            throw Util.CreateInvalidDataException(JpegLSError.RestartMarkerNotFound);

        // Read all preceding 0xFF fill bytes until a non 0xFF byte has been found. (see T.81, B.1.1.2)
        do
        {
            value = ReadByte(source);
        } while (value == Constants.JpegMarkerStartByte);

        if (value != Constants.JpegRestartMarkerBase + expectedRestartMarkerId)
            throw Util.CreateInvalidDataException(JpegLSError.RestartMarkerNotFound);
    }

    private void FindJpegMarkerStartByte(ReadOnlySpan<byte> source)
    {
        _positionFF = source[_position..].IndexOf(Constants.JpegMarkerStartByte);
        if (_positionFF == -1)
        {
            _positionFF = _endPosition;
        }
    }

    private void FillReadCache(ReadOnlySpan<byte> source)
    {
        // ASSERT(valid_bits_ <= max_readable_cache_bits);

        if (FillReadCacheOptimistic(source))
            return;

        do
        {
            if (_position >= _endPosition)
            {
                if (_validBits == 0)
                {
                    // Decoding process expects at least some bits to be added to the cache.
                    throw Util.CreateInvalidDataException(JpegLSError.InvalidEncodedData);
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
                    throw Util.CreateInvalidDataException(JpegLSError.InvalidEncodedData);
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

        FindJpegMarkerStartByte(source);
    }

    private bool FillReadCacheOptimistic(ReadOnlySpan<byte> source)
    {
        // Easy & fast: if there is no 0xFF byte in sight, we can read without bit stuffing
        if (_position >= _positionFF - (sizeof(ulong) - 1))
            return false;

        _readCache |= BinaryPrimitives.ReadUInt64BigEndian(source) >> _validBits;
        int bytesToRead = (CacheBitCount - _validBits) / 8;
        _position += bytesToRead;
        _validBits += bytesToRead * 8;
        //ASSERT(valid_bits_ >= max_readable_cache_bits);
        return true;
    }
}
