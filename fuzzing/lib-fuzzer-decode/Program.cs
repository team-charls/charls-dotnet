// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using SharpFuzz;

namespace CharLS.Managed.LibFuzzerDecode;

internal sealed class Program
{
    public static void Main()
    {
        Fuzzer.LibFuzzer.Run(readOnlyInput =>
        {
            try
            {
                byte[] input = readOnlyInput.ToArray();
                var decoder = new JpegLSDecoder(input, false);
                decoder.ReadHeader();
                int size = decoder.GetDestinationSize();
                if (size > 8192 * 8192 * 3)
                    return;

                byte[] destination = new byte[size];
                decoder.Decode(destination);
            }
            catch (InvalidDataException) { }
        });
    }
}
