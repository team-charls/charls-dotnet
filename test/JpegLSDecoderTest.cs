// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Reflection;

namespace CharLS.Managed.Test;

public class JpegLSDecoderTest
{
    [Fact]
    public void SetSourceTwiceThrows()
    {
        var buffer = new byte[2000];
        JpegLSDecoder decoder = new() { Source = buffer };

        var exception = Assert.Throws<InvalidOperationException>(() => decoder.Source = buffer);
        Assert.False(string.IsNullOrEmpty(exception.Message));
    }

    [Fact]
    public void ReadHeaderWithoutSource()
    {
        JpegLSDecoder decoder = new();

        var exception = Assert.Throws<InvalidOperationException>(() => decoder.ReadHeader());
        Assert.False(string.IsNullOrEmpty(exception.Message));
    }

    [Fact]
    public void DestinationSizeWithoutReadingHeader()
    {
        JpegLSDecoder decoder = new();

        var exception = Assert.Throws<InvalidOperationException>(() => decoder.GetDestinationSize());
        Assert.False(string.IsNullOrEmpty(exception.Message));
    }

    [Fact]
    public void ReadHeaderWithoutSourceThrows()
    {
        JpegLSDecoder decoder = new();

        var exception = Assert.Throws<InvalidOperationException>(() => decoder.ReadHeader());
        Assert.False(string.IsNullOrEmpty(exception.Message));
    }

    //TEST_METHOD(read_header_from_non_jpegls_data) // NOLINT
    //{
    //    const vector<uint8_t> source(100);
    //    jpegls_decoder decoder{ source, false};

    //    error_code ec;
    //    decoder.read_header(ec);

    //    Assert::IsTrue(ec == jpegls_errc::jpeg_marker_start_byte_not_found);
    //}

    [Fact]
    public void FrameInfoWithoutReadHeaderThrows()
    {
        var buffer = new byte[2000];
        JpegLSDecoder decoder = new() { Source = buffer };

        var exception = Assert.Throws<InvalidOperationException>(() => decoder.FrameInfo);
        Assert.False(string.IsNullOrEmpty(exception.Message));
    }

    [Fact]
    public void InterleaveModeWithoutReadHeader()
    {
        var buffer = new byte[2000];
        JpegLSDecoder decoder = new() { Source = buffer };

        var exception = Assert.Throws<InvalidOperationException>(() => decoder.InterleaveMode);
        Assert.False(string.IsNullOrEmpty(exception.Message));
    }

    [Fact]
    public void NearLosslessWithoutReadHeader()
    {
        var buffer = new byte[2000];
        JpegLSDecoder decoder = new() { Source = buffer };

        var exception = Assert.Throws<InvalidOperationException>(() => decoder.NearLossless);
        Assert.False(string.IsNullOrEmpty(exception.Message));
    }

    [Fact]
    public void PresetCodingParametersWithoutReadHeader()
    {
        var buffer = new byte[2000];
        JpegLSDecoder decoder = new() { Source = buffer };

        var exception = Assert.Throws<InvalidOperationException>(() => decoder.PresetCodingParameters);
        Assert.False(string.IsNullOrEmpty(exception.Message));
    }

    [Fact]
    public void DestinationSize()
    {
        JpegLSDecoder decoder = new(ReadAllBytes("conformance/t8c0e0.jls"));

        const int expectedDestinationSize = 256 * 256 * 3;
        Assert.Equal(expectedDestinationSize, decoder.GetDestinationSize());
    }

    //TEST_METHOD(destination_size_stride_interleave_none) // NOLINT
    //{
    //    const vector<uint8_t> source{ read_file("DataFiles/t8c0e0.jls")};

    //    const jpegls_decoder decoder{ source, true};

    //    constexpr size_t stride{ 512};
    //    constexpr size_t expected_destination_size{ stride * 256 * 3};
    //    Assert.Equal(expected_destination_size, decoder.destination_size(stride));
    //}

    //TEST_METHOD(destination_size_stride_interleave_line) // NOLINT
    //{
    //    const vector<uint8_t> source{ read_file("DataFiles/t8c1e0.jls")};

    //    const jpegls_decoder decoder{ source, true};

    //    constexpr size_t stride{ 1024};
    //    constexpr size_t expected_destination_size{ stride * 256};
    //    Assert.Equal(expected_destination_size, decoder.destination_size(stride));
    //}

    //TEST_METHOD(destination_size_stride_interleave_sample) // NOLINT
    //{
    //    const vector<uint8_t> source{ read_file("DataFiles/t8c2e0.jls")};

    //    const jpegls_decoder decoder{ source, true};

    //    constexpr size_t stride = 1024;
    //    constexpr size_t expected_destination_size{ stride * 256};
    //    Assert.Equal(expected_destination_size, decoder.destination_size(stride));
    //}

    //TEST_METHOD(decode_reference_file_from_buffer) // NOLINT
    //{
    //    const vector<uint8_t> source{ read_file("DataFiles/t8c0e0.jls")};

    //    const jpegls_decoder decoder{ source, true};

    //    vector<uint8_t> destination(decoder.destination_size());
    //    decoder.decode(destination);

    //    portable_anymap_file reference_file{
    //        read_anymap_reference_file("DataFiles/test8.ppm", decoder.interleave_mode(), decoder.frame_info())};

    //    const auto&reference_image_data = reference_file.image_data();
    //    for (size_t i{ }; i != destination.size(); ++i)
    //    {
    //        Assert.Equal(reference_image_data[i], destination[i]);
    //    }
    //}

    //TEST_METHOD(decode_with_default_pc_parameters_before_each_sos) // NOLINT
    //{
    //    vector<uint8_t> source{ read_file("DataFiles/t8c0e0.jls")};
    //    insert_pc_parameters_segments(source, 3);

    //    const jpegls_decoder decoder{ source, true};

    //    vector<uint8_t> destination(decoder.destination_size());
    //    decoder.decode(destination);

    //    portable_anymap_file reference_file{
    //        read_anymap_reference_file("DataFiles/test8.ppm", decoder.interleave_mode(), decoder.frame_info())};

    //    const auto&reference_image_data = reference_file.image_data();
    //    for (size_t i{ }; i != destination.size(); ++i)
    //    {
    //        Assert.Equal(reference_image_data[i], destination[i]);
    //    }
    //}

    //TEST_METHOD(decode_with_destination_as_return) // NOLINT
    //{
    //    const vector<uint8_t> source{ read_file("DataFiles/t8c0e0.jls")};
    //    const jpegls_decoder decoder{ source, true};
    //    const auto destination{ decoder.decode<vector<uint8_t>>()};

    //    portable_anymap_file reference_file{
    //        read_anymap_reference_file("DataFiles/test8.ppm", decoder.interleave_mode(), decoder.frame_info())};

    //    const auto&reference_image_data{ reference_file.image_data()};
    //    for (size_t i{ }; i != destination.size(); ++i)
    //    {
    //        Assert.Equal(reference_image_data[i], destination[i]);
    //    }
    //}

    //TEST_METHOD(decode_with_16_bit_destination_as_return) // NOLINT
    //{
    //    const vector<uint8_t> source{ read_file("DataFiles/t8c0e0.jls")};
    //    const jpegls_decoder decoder{ source, true};
    //    const auto destination{ decoder.decode<vector<uint16_t>>()};

    //    portable_anymap_file reference_file{
    //        read_anymap_reference_file("DataFiles/test8.ppm", decoder.interleave_mode(), decoder.frame_info())};

    //    const auto&reference_image_data{ reference_file.image_data()};
    //    const auto* destination_as_bytes{ reinterpret_cast <const uint8_t*> (destination.data())};
    //    for (size_t i{ }; i != reference_image_data.size(); ++i)
    //    {
    //        Assert.Equal(reference_image_data[i], destination_as_bytes[i]);
    //    }
    //}

    //TEST_METHOD(decode_without_reading_header) // NOLINT
    //{
    //    const jpegls_decoder decoder;

    //    vector<uint8_t> buffer(1000);
    //    assert_expect_exception(jpegls_errc::invalid_operation, [&decoder, &buffer] { decoder.decode(buffer); });
    //}

    //TEST_METHOD(decode_reference_to_mapping_table_selector) // NOLINT
    //{
    //    jpeg_test_stream_writer writer;

    //    writer.write_start_of_image();
    //    writer.write_start_of_frame_segment(10, 10, 8, 3);
    //    writer.mapping_table_selector = 1;
    //    writer.write_start_of_scan_segment(0, 1, 0, interleave_mode::none);

    //    jpegls_decoder decoder{ writer.buffer, false};

    //    assert_expect_exception(jpegls_errc::parameter_value_not_supported, [&decoder] { decoder.read_header(); });
    //}

    [Fact (Skip = "WIP")]
    public void ReadSpiffHeader()
    {
        var source = Util.CreateTestSpiffHeader();
        JpegLSDecoder decoder = new(source);

        Assert.NotNull(decoder.SpiffHeader);

        var header = decoder.SpiffHeader;
        Assert.Equal(SpiffProfileId.None, header.ProfileId);
        Assert.Equal(3, header.ComponentCount);
        Assert.Equal(800, header.Height);
        Assert.Equal(600, header.Width);
        Assert.Equal(SpiffColorSpace.Rgb, header.ColorSpace);
        Assert.Equal(8, header.BitsPerSample);
        Assert.Equal(SpiffCompressionType.JpegLS, header.CompressionType);
        Assert.Equal(SpiffResolutionUnit.DotsPerInch, header.ResolutionUnit);
        Assert.Equal(96, header.VerticalResolution);
        Assert.Equal(1024, header.HorizontalResolution);
    }

    //TEST_METHOD(read_spiff_header_from_non_jpegls_data) // NOLINT
    //{
    //    const vector<uint8_t> source(100);
    //    jpegls_decoder decoder{ source, false};

    //    error_code ec;
    //    ignore = decoder.read_spiff_header(ec);

    //    Assert::IsTrue(ec == jpegls_errc::jpeg_marker_start_byte_not_found);
    //}

    //TEST_METHOD(read_spiff_header_from_jpegls_without_spiff) // NOLINT
    //{
    //    const vector<uint8_t> source{ read_file("DataFiles/t8c0e0.jls")};

    //    const jpegls_decoder decoder{ source, true};

    //    Assert::IsFalse(decoder.spiff_header_has_value());

    //    const frame_info&frame_info{ decoder.frame_info()};

    //    Assert.Equal(3, frame_info.component_count);
    //    Assert.Equal(8, frame_info.bits_per_sample);
    //    Assert.Equal(256U, frame_info.height);
    //    Assert.Equal(256U, frame_info.width);
    //}

    //TEST_METHOD(read_header_twice) // NOLINT
    //{
    //    const vector<uint8_t> source{ read_file("DataFiles/t8c0e0.jls")};

    //    jpegls_decoder decoder{ source, true};

    //    assert_expect_exception(jpegls_errc::invalid_operation, [&decoder] { ignore = decoder.read_header(); });
    //}

    //TEST_METHOD(simple_decode) // NOLINT
    //{
    //    const vector<uint8_t> encoded_source{ read_file("DataFiles/t8c0e0.jls")};

    //    vector<uint8_t> decoded_destination;
    //    frame_info frame_info;
    //    interleave_mode interleave_mode;
    //    tie(frame_info, interleave_mode) = jpegls_decoder::decode(encoded_source, decoded_destination);

    //    Assert.Equal(3, frame_info.component_count);
    //    Assert.Equal(8, frame_info.bits_per_sample);
    //    Assert.Equal(256U, frame_info.height);
    //    Assert.Equal(256U, frame_info.width);
    //    Assert.Equal(interleave_mode::none, interleave_mode);

    //    const size_t expected_size = static_cast<size_t>(frame_info.height) * frame_info.width * frame_info.component_count;
    //    Assert.Equal(expected_size, decoded_destination.size());
    //}

    //TEST_METHOD(simple_decode_to_uint16_buffer) // NOLINT
    //{
    //    const vector<uint8_t> encoded_source{ read_file("DataFiles/t8c0e0.jls")};

    //    vector<uint16_t> decoded_destination;
    //    frame_info frame_info;
    //    interleave_mode interleave_mode;
    //    tie(frame_info, interleave_mode) = jpegls_decoder::decode(encoded_source, decoded_destination);

    //    Assert.Equal(3, frame_info.component_count);
    //    Assert.Equal(8, frame_info.bits_per_sample);
    //    Assert.Equal(256U, frame_info.height);
    //    Assert.Equal(256U, frame_info.width);
    //    Assert.Equal(interleave_mode::none, interleave_mode);

    //    const size_t expected_size{ static_cast<size_t>(frame_info.height) * frame_info.width * frame_info.component_count};
    //    Assert.Equal(expected_size, decoded_destination.size() * sizeof(uint16_t));
    //}

    //TEST_METHOD(decode_file_with_ff_in_entropy_data) // NOLINT
    //{
    //    const vector<uint8_t> source{ read_file("ff_in_entropy_data.jls")};

    //    const jpegls_decoder decoder{ source, true};

    //    const auto&frame_info{ decoder.frame_info()};
    //    Assert.Equal(1, frame_info.component_count);
    //    Assert.Equal(12, frame_info.bits_per_sample);
    //    Assert.Equal(1216U, frame_info.height);
    //    Assert.Equal(968U, frame_info.width);

    //    vector<uint8_t> destination(decoder.destination_size());

    //    assert_expect_exception(jpegls_errc::invalid_encoded_data,
    //                            [&decoder, &destination] { decoder.decode(destination); });
    //}

    //TEST_METHOD(decode_file_with_golomb_large_then_k_max) // NOLINT
    //{
    //    const vector<uint8_t> source{ read_file("fuzzy_input_golomb_16.jls")};

    //    const jpegls_decoder decoder{ source, true};

    //    const auto&frame_info{ decoder.frame_info()};
    //    Assert.Equal(3, frame_info.component_count);
    //    Assert.Equal(16, frame_info.bits_per_sample);
    //    Assert.Equal(65516U, frame_info.height);
    //    Assert.Equal(1U, frame_info.width);

    //    vector<uint8_t> destination(decoder.destination_size());

    //    assert_expect_exception(jpegls_errc::invalid_encoded_data,
    //                            [&decoder, &destination] { decoder.decode(destination); });
    //}

    //TEST_METHOD(decode_file_with_missing_restart_marker) // NOLINT
    //{
    //    vector<uint8_t> source{ read_file("DataFiles/t8c0e0.jls")};

    //    // Insert a DRI marker segment to trigger that restart markers are used.
    //    jpeg_test_stream_writer stream_writer;
    //    stream_writer.write_define_restart_interval(10, 3);
    //    const auto it = source.begin() + 2;
    //    source.insert(it, stream_writer.buffer.cbegin(), stream_writer.buffer.cend());

    //    const jpegls_decoder decoder{ source, true};
    //    vector<uint8_t> destination(decoder.destination_size());

    //    assert_expect_exception(jpegls_errc::restart_marker_not_found,
    //                            [&decoder, &destination] { decoder.decode(destination); });
    //}

    //TEST_METHOD(decode_file_with_incorrect_restart_marker) // NOLINT
    //{
    //    vector<uint8_t> source{ read_file("DataFiles/test8_ilv_none_rm_7.jls")};

    //    // Change the first restart marker to the second.
    //    auto it{ find_scan_header(source.begin(), source.end())};
    //    it = find_first_restart_marker(it + 1, source.end());
    //    ++it;
    //    *it = 0xD1;

    //    const jpegls_decoder decoder{ source, true};
    //    vector<uint8_t> destination(decoder.destination_size());

    //    assert_expect_exception(jpegls_errc::restart_marker_not_found,
    //                            [&decoder, &destination] { decoder.decode(destination); });
    //}

    //TEST_METHOD(decode_file_with_extra_begin_bytes_for_restart_marker_code) // NOLINT
    //{
    //    vector<uint8_t> source{ read_file("DataFiles/test8_ilv_none_rm_7.jls")};

    //    // Add additional 0xFF marker begin bytes
    //    auto it{ find_scan_header(source.begin(), source.end())};
    //    it = find_first_restart_marker(it + 1, source.end());
    //    const array<uint8_t, 7> extra_begin_bytes{ 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF};
    //    source.insert(it, extra_begin_bytes.cbegin(), extra_begin_bytes.cend());

    //    const jpegls_decoder decoder{ source, true};
    //    portable_anymap_file reference_file{
    //        read_anymap_reference_file("DataFiles/test8.ppm", decoder.interleave_mode(), decoder.frame_info())};

    //    test_compliance(source, reference_file.image_data(), false);
    //}

    //TEST_METHOD(read_comment) // NOLINT
    //{
    //    jpeg_test_stream_writer writer;
    //    writer.write_start_of_image();
    //    writer.write_segment(jpeg_marker_code::comment, "hello", 5);
    //    writer.write_start_of_frame_segment(512, 512, 8, 3);
    //    writer.write_start_of_scan_segment(0, 1, 0, interleave_mode::none);

    //    jpegls_decoder decoder;
    //    decoder.source(writer.buffer.data(), writer.buffer.size());

    //    const void* actual_data{ };
    //    size_t actual_size{ };
    //    decoder.at_comment([&actual_data, &actual_size](const void* data, const size_t size) noexcept {
    //        actual_data = data;
    //        actual_size = size;
    //    });

    //    decoder.read_header();

    //    Assert.Equal(static_cast<size_t>(5), actual_size);
    //    Assert::IsTrue(memcmp("hello", actual_data, actual_size) == 0);
    //}

    //TEST_METHOD(read_comment_while_already_unregisted) // NOLINT
    //{
    //    jpeg_test_stream_writer writer;
    //    writer.write_start_of_image();
    //    writer.write_segment(jpeg_marker_code::comment, "hello", 5);
    //    writer.write_start_of_frame_segment(512, 512, 8, 3);
    //    writer.write_start_of_scan_segment(0, 1, 0, interleave_mode::none);

    //    jpegls_decoder decoder;
    //    decoder.source(writer.buffer.data(), writer.buffer.size());

    //    bool callback_called{ };
    //    decoder.at_comment([&callback_called](const void*, const size_t) noexcept { callback_called = true; })
    //        .at_comment(nullptr);

    //    decoder.read_header();

    //    Assert::IsFalse(callback_called);
    //}

    //TEST_METHOD(read_comment_throw_exception) // NOLINT
    //{
    //    jpeg_test_stream_writer writer;
    //    writer.write_start_of_image();
    //    writer.write_segment(jpeg_marker_code::comment, "hello", 5);
    //    writer.write_start_of_frame_segment(512, 512, 8, 3);
    //    writer.write_start_of_scan_segment(0, 1, 0, interleave_mode::none);

    //    jpegls_decoder decoder;
    //    decoder.source(writer.buffer.data(), writer.buffer.size());

    //    decoder.at_comment([](const void*, const size_t) { throw "something"; });

    //    assert_expect_exception(jpegls_errc::callback_failed, [&decoder] { decoder.read_header(); });
    //}

    private static byte[] ReadAllBytes(string path, int bytesToSkip = 0)
    {
        var fullPath = Path.Join(DataFileDirectory, path);

        if (bytesToSkip == 0)
            return File.ReadAllBytes(fullPath);

        using var stream = File.OpenRead(fullPath);
        var result = new byte[new FileInfo(fullPath).Length - bytesToSkip];

        _ = stream.Seek(bytesToSkip, SeekOrigin.Begin);
        _ = stream.Read(result, 0, result.Length);
        return result;
    }

    private static string DataFileDirectory
    {
        get
        {
            var assemblyLocation = new Uri(Assembly.GetExecutingAssembly().Location);
            return Path.GetDirectoryName(assemblyLocation.LocalPath)!;
        }
    }


}
