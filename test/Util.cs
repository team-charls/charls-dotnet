// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Reflection;
using Xunit;

namespace CharLS.JpegLS.Test;

internal sealed class Util
{
    private static string DataFileDirectory
    {
        get
        {
            var assemblyLocation = new Uri(Assembly.GetExecutingAssembly().Location);
            return Path.GetDirectoryName(assemblyLocation.LocalPath)!;
        }
    }


    internal static byte[] ReadFile(string path, int bytesToSkip = 0)
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

    internal static PortableAnymapFile ReadAnymapReferenceFile(string filename, JpegLSInterleaveMode interleave_mode, FrameInfo frameInfo)
    {
        PortableAnymapFile referenceFile = new(filename);

        if (interleave_mode == JpegLSInterleaveMode.None && frameInfo.ComponentCount == 3)
        {
            //triplet_to_planar(reference_file.image_data(), frameInfo.width, frameInfo.height);
        }

        return referenceFile;
    }


    internal static void TestCompliance(byte[] encodedSource, byte[] uncompressedSource, bool checkEncode)
    {
        JpegLSDecoder decoder = new(encodedSource);

        if (checkEncode)
        {
            //Assert::IsTrue(verify_encoded_bytes(uncompressed_source, encoded_source));
        }

        var destination = new byte[decoder.GetDestinationSize()];
        decoder.Decode(destination);

        if (decoder.NearLossless == 0)
        {
            for (int i = 0; i != uncompressedSource.Length; i++)
            {
                if (uncompressedSource[i] != destination[i]) // AreEqual is very slow, pre-test to speed up 50X
                {
                    Assert.Equal(uncompressedSource[i], destination[i]);
                }
            }
        }
        else
        {
            throw new NotImplementedException();
        }
        //    const frameInfo frameInfo{ decoder.frameInfo()};
        //    const auto near_lossless{ decoder.near_lossless()};

        //    if (frameInfo.bits_per_sample <= 8)
        //    {
        //        for (size_t i{ }; i != uncompressed_source.size(); ++i)
        //        {
        //            if (abs(uncompressed_source[i] - destination[i]) >
        //                near_lossless) // AreEqual is very slow, pre-test to speed up 50X
        //            {
        //                Assert::AreEqual(uncompressed_source[i], destination[i]);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        const auto* source16 = reinterpret_cast <const uint16_t*> (uncompressed_source.data());
        //        const auto* destination16 = reinterpret_cast <const uint16_t*> (destination.data());

        //        for (size_t i{ }; i != uncompressed_source.size() / 2; ++i)
        //        {
        //            if (abs(source16[i] - destination16[i]) > near_lossless) // AreEqual is very slow, pre-test to speed up 50X
        //            {
        //                Assert::AreEqual(static_cast<int>(source16[i]), static_cast<int>(destination16[i]));
        //            }
        //        }
        //    }
        //}
    }

}
