// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Diagnostics;

namespace CharLS.JpegLS;

internal class Decoder
{
    private ReadOnlyMemory<byte> _source;
    private int _position;
    private int _validBits;
    private ulong _readCache;
    private const int ulongBitCount = sizeof(ulong) * 8;
    private int _endPosition;
    private int _nextFFposition;
    private int _restartInterval;

    private FrameInfo _frameInfo;
    private JpegLSInterleaveMode _interleaveMode;
    private Memory<byte> _previousLine;
    private Memory<byte> _currentLine;
    private int _run_index;

    private int _t1;
    private int _t2;
    private int _t3;

    private readonly int[] _j = [
        0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2,  2,  3,  3,  3,  3,
        4, 4, 5, 5, 6, 6, 7, 7, 8, 9, 10, 11, 12, 13, 14, 15];


    internal Decoder(FrameInfo frameInfo, JpegLSInterleaveMode interleaveMode, ReadOnlyMemory<byte> source)
    {
        _frameInfo = frameInfo;
        _interleaveMode = interleaveMode;
        _source = source;
        _endPosition = source.Length;

        _nextFFposition = FindNextFF();
        MakeValid();
    }

    internal int DecodeScan(Span<byte> destination)
    {
        // Process images without a restart interval, as 1 large restart interval.
        if (_restartInterval == 0)
        {
            _restartInterval = _frameInfo.Height;
        }

        DecodeLines();

        //skip_bytes(compressed_data, static_cast<size_t>(Strategy::get_cur_byte_pos() - compressed_bytes));

        return 0;
    }

    private void DecodeLines()
    {
        int pixel_stride = _frameInfo.Width + 4;
        int componentCount = _interleaveMode == JpegLSInterleaveMode.Line ? _frameInfo.ComponentCount : 1;

        var line_buffer = new byte[2 * componentCount * pixel_stride];
        var run_index = new int[_frameInfo.ComponentCount];

        for (int line = 0; ;)
        {
            int lines_in_interval = Math.Min(_frameInfo.Height - line, _restartInterval);

            for (int mcu = 0; mcu < lines_in_interval; ++mcu, ++line)
            {
                _previousLine = line_buffer.AsMemory()[1..];
                _currentLine = line_buffer.AsMemory()[(1 + (componentCount * pixel_stride))..];
                if ((line & 1) == 1)
                {
                    //std::swap(previous_line_, current_line_);
                }

                for (int component = 0; component < componentCount; component++)
                {
                    _run_index = run_index[component];

                    // initialize edge pixels used for prediction
                    _previousLine.Span[_frameInfo.Width] = _previousLine.Span[_frameInfo.Width - 1];
                    _currentLine.Span[-1] = _previousLine.Span[0]; //TODO fix -1?

                    DoLine();

                    run_index[component] = _run_index;
                    _previousLine = line_buffer.AsMemory()[(1 + pixel_stride)..];
                    _currentLine = line_buffer.AsMemory()[(1 + (componentCount * pixel_stride * 2))..];
                }

                //on_line_end(current_line_ + - (static_cast<size_t>(component_count) * pixel_stride),
                //            rect_.Width,
                //                          pixel_stride);
            }

            if (line == _frameInfo.Height)
                break;

            //read_restart_marker();
            //restart_interval_counter_ = (restart_interval_counter_ + 1) % jpeg_restart_marker_range;

            // After a restart marker it is required to reset the decoder.
            //Strategy::reset();
            //std::fill(line_buffer.begin(), line_buffer.end(), pixel_type{ });
            //std::fill(run_index.begin(), run_index.end(), 0);
            //reset_parameters();
        }

        ////end_scan();
    }


    private void DoLine()
    {
        int index = 0;
        int rb = _previousLine.Span[index - 1];
        int rd = _previousLine.Span[index];

        while (index < _frameInfo.Width)
        {
            int ra = _currentLine.Span[index - 1];
            int rc = rb;
            rb = rd;
            rd = _previousLine.Span[index + 1];

            int qs = ComputeContextId(QuantizeGradient(rd - rb), QuantizeGradient(rb - rc), QuantizeGradient(rc - ra));
            if (qs != 0)
            {
                _currentLine.Span[index] = DoRegular(qs, GetPredictedValue(ra, rb, rc));
                index++;
            }
            else
            {
                index += DoRunMode(index);
                rb = _previousLine.Span[index - 1];
                rd = _previousLine.Span[index];
            }
        }
    }

    private byte DoRegular(int qs, int predicted)
    {
        ////const int32_t sign = bit_wise_sign(qs);
        ////jls_context& context = contexts_[apply_sign(qs, sign)];
        ////const int32_t k = context.get_golomb_coding_parameter();
        ////const int32_t predicted_value = traits_.correct_prediction(predicted + apply_sign(context.C, sign));

        ////int32_t error_value;
        ////const golomb_code& code = decoding_tables[k].get(Strategy::peek_byte());
        ////if (code.length() != 0)
        ////{
        ////    Strategy::skip(code.length());
        ////    error_value = code.value();
        ////    ASSERT(std::abs(error_value) < 65535);
        ////}
        ////else
        ////{
        ////    error_value = unmap_error_value(decode_value(k, traits_.limit, traits_.quantized_bits_per_pixel));
        ////    if (std::abs(error_value) > 65535)
        ////        impl::throw_jpegls_error(jpegls_errc::invalid_encoded_data);
        ////}
        ////if (k == 0)
        ////{
        ////    error_value = error_value ^ context.get_error_correction(traits_.near_lossless);
        ////}
        ////context.update_variables(error_value, traits_.near_lossless, traits_.reset_threshold);
        ////error_value = apply_sign(error_value, sign);
        ////return traits_.compute_reconstructed_sample(predicted_value, error_value);
        return 0; // dummy value.
    }

    private int DoRunMode(int startIndex)
    {
        byte ra = _currentLine.Span[startIndex - 1];

        int runLength = DecodeRunPixels(ra, startIndex, _frameInfo.Width - startIndex);
        int endIndex = startIndex + runLength;

        if (endIndex == _frameInfo.Width)
            return endIndex - startIndex;

        // run interruption
        byte rb = _previousLine.Span[endIndex];
        ////_currentLine.Span[endIndex] = decode_run_interruption_pixel(ra, rb);
        DecrementRunIndex();
        return endIndex - startIndex + 1;
        ////return 0; // dummy value.
    }

    private int DecodeRunPixels(byte ra, int start_pos, int pixel_count)
    {
        int index = 0;
        while (ReadBit())
        {
            int count = Math.Min(1 << _j[_run_index], pixel_count - index);
            index += count;
            Debug.Assert(index <= pixel_count);

            if (count == (1 << _j[_run_index]))
            {
                IncrementRunIndex();
            }

            if (index == pixel_count)
                break;
        }

        if (index != pixel_count)
        {
            // incomplete run.
            ////index += (_j[_run_index] > 0) ? read_value(_j[_run_index]) : 0;
        }

        if (index > pixel_count)
            throw Util.CreateInvalidDataException(JpegLSError.InvalidEncodedData);

        for (int i = 0; i < index; ++i)
        {
            _currentLine.Span[start_pos + i] = ra;
        }

        return index;
    }


    private void MakeValid()
    {
        Debug.Assert(_validBits <= ulongBitCount - 8);

        if (OptimizedRead())
            return;

        do
        {
            if (_position >= _endPosition)
            {
                if (_validBits <= 0)
                    throw Util.CreateInvalidDataException(JpegLSError.InvalidEncodedData);

                return;
            }

            ulong value_new = _source.Span[_position];

            if (value_new == 0xFF)
            {
                // JPEG bit stream rule: no FF may be followed by 0x80 or higher
                if (_position == _endPosition - 1 || (_source.Span[_position + 1] & 0x80) != 0)
                {
                    if (_validBits <= 0)
                        throw Util.CreateInvalidDataException(JpegLSError.InvalidEncodedData);

                    return;
                }
            }

            _readCache |= value_new << (ulongBitCount - 8 - _validBits);
            _position += 1;
            _validBits += 8;

            if (value_new == 0xFF)
            {
                _validBits--;
            }
        } while (_validBits < ulongBitCount - 8);

        _nextFFposition = FindNextFF();
    }


    private bool OptimizedRead()
    {
        // Easy & fast: if there is no 0xFF byte in sight, we can read without bit stuffing
        if (_position < _nextFFposition - (sizeof(ulong) - 1))
        {

            _readCache |= Read(_source[_position..].Span) >> _validBits;
            int bytesToRead = (ulongBitCount - _validBits) >> 3;
            _position += bytesToRead;
            _validBits += bytesToRead * 8;

            Debug.Assert(_validBits >= ulongBitCount - 8);
            return true;
        }
        return false;
    }

    private int FindNextFF()
    {
        int positionNextFF = _position;

        ReadOnlySpan<byte> source = _source.Span;
        while (positionNextFF < _endPosition)
        {
            if (source[positionNextFF] == 0xFF)
                break;

            positionNextFF++;
        }

        return positionNextFF;
    }

    private bool ReadBit()
    {
        if (_validBits <= 0)
        {
            MakeValid();
        }

        bool set = (_readCache & ((ulong)1 << (ulongBitCount - 1))) != 0;
        ////skip(1);
        return set;
    }

    private void IncrementRunIndex()
    {
        _run_index = Math.Min(31, _run_index + 1);
    }

    private void DecrementRunIndex()
    {
        _run_index = Math.Max(0, _run_index - 1);
    }

    public static ulong Read(ReadOnlySpan<byte> bytes)
    {
        return ((ulong)bytes[0] << 56) + ((ulong)bytes[1] << 48) + ((ulong)bytes[2] << 40)
               + ((ulong)bytes[3] << 32) + ((ulong)bytes[4] << 24) + ((ulong)bytes[5] << 16)
               + ((ulong)bytes[6] << 8) + ((ulong)bytes[7] << 0);
    }

    private static int ComputeContextId(int q1, int q2, int q3)
    {
        return (((q1 * 9) + q2) * 9) + q3;
    }

    private int QuantizeGradient(int di)
    {
        const int traitsNearLossless = 0;

        if (di <= -_t3)
            return -4;
        if (di <= -_t2)
            return -3;
        if (di <= -_t1)
            return -2;
        if (di < -traitsNearLossless)
            return -1;
        if (di <= traitsNearLossless)
            return 0;
        if (di < _t1)
            return 1;
        if (di < _t2)
            return 2;
        if (di < _t3)
            return 3;

        return 4;
    }


    private static int GetPredictedValue(int ra, int rb, int rc)
    {
        // sign trick reduces the number of if statements (branches)
        int sign = BitWiseSign(rb - ra);

        // is Ra between Rc and Rb?
        if ((sign ^ (rc - ra)) < 0)
        {
            return rb;
        }
        if ((sign ^ (rb - rc)) < 0)
        {
            return ra;
        }

        // default case, valid if Rc element of [Ra,Rb]
        return ra + rb - rc;
    }

    private static int BitWiseSign(int i)
    {
        const int int32_t_bit_count = sizeof(int) * 8;

        return i >> (int32_t_bit_count - 1);
    }


}