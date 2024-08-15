// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

namespace CharLS.Managed.Benchmark.Micro;

internal struct Triplet<T>
    where T : unmanaged
{
    internal T V1;
    internal T V2;
    internal T V3;

    internal Triplet(T v1, T v2, T v3)
    {
        V1 = v1;
        V2 = v2;
        V3 = v3;
    }
}

internal struct TripletByte
{
    internal byte V1;
    internal byte V2;
    internal byte V3;

    internal TripletByte(byte v1, byte v2, byte v3)
    {
        V1 = v1;
        V2 = v2;
        V3 = v3;
    }
}


public class ColorTransformation
{
    private byte[] _source = default!;
    private byte[] _destination = default!;

    [Params(2000)]
    public int PixelCount { get; set; }

    internal static Triplet<byte> TransformHP1(byte red, byte green, byte blue)
    {
        const int range = 1 << (sizeof(byte) * 8);

        return new Triplet<byte>(
            (byte)(red - green + range / 2),
            green,
            (byte)(blue - green + range / 2));
    }

    internal static Triplet<byte> TransformHP1IntParameters(int red, int green, int blue)
    {
        const int range = 1 << (sizeof(byte) * 8);

        return new Triplet<byte>(
            (byte)(red - green + range / 2),
            (byte)green,
            (byte)(blue - green + range / 2));
    }

    internal static TripletByte TransformHP1IntParametersReturnTripletByte(int red, int green, int blue)
    {
        const int range = 1 << (sizeof(byte) * 8);

        return new TripletByte(
            (byte)(red - green + range / 2),
            (byte)green,
            (byte)(blue - green + range / 2));
    }

    internal static int TransformHP1IntParametersReturnUint(int red, int green, int blue)
    {
        const int range = 1 << (sizeof(byte) * 8);

        return
            (((byte)(red - green + range / 2)) << 16) |
            ((byte)green << 8) |
            (byte)(blue - green + range / 2);
    }

    internal static Triplet<T> TransformHP1Generic<T>(int red, int green, int blue)
        where T : unmanaged, IBinaryInteger<T>
    {
        const int range = 1 << (sizeof(byte) * 8);

        return new Triplet<T>(
            T.CreateTruncating((red - green + range / 2)),
            T.CreateTruncating(green),
            T.CreateTruncating(blue - green + range / 2));
    }

    [GlobalSetup]
    public void Init()
    {
        _source = new byte[PixelCount * 3];
        _source.AsSpan().Fill(99);
        _destination = new byte[PixelCount * 3];
    }

    [Benchmark]
    public void TransformHP1Explicit()
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(_source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(_destination);

        for (int i = 0; i < PixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = TransformHP1(pixel.V1, pixel.V2, pixel.V3);
        }
    }

    [Benchmark]
    public void TransformHP1ExplicitIntParameters()
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(_source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(_destination);

        for (int i = 0; i < PixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = TransformHP1IntParameters(pixel.V1, pixel.V2, pixel.V3);
        }
    }

    [Benchmark]
    public void TransformHP1ExplicitIntParametersReturnTripletByte()
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, TripletByte>(_source);
        var destinationTriplet = MemoryMarshal.Cast<byte, TripletByte>(_destination);

        for (int i = 0; i < PixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = TransformHP1IntParametersReturnTripletByte(pixel.V1, pixel.V2, pixel.V3);
        }
    }

    [Benchmark]
    public void TransformHP1ExplicitIntParametersWithRef()
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, TripletByte>(_source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(_destination);

        var s = sourceTriplet.GetPinnableReference();
        var d = destinationTriplet.GetPinnableReference();

        for (int i = 0; i < PixelCount; ++i)
        {
            d = TransformHP1IntParameters(s.V1, s.V2, s.V3);
            s = Unsafe.Add(ref s, 1);
            d = Unsafe.Add(ref d, 1);
        }
    }

    [Benchmark]
    public unsafe void TransformHP1ExplicitIntParametersWithPointers()
    {
        fixed (byte* bptr = _source, pd = _destination)
        {
            Triplet<byte>* sourcePTr = (Triplet<byte>*)bptr;
            Triplet<byte>* destinationPtr = (Triplet<byte>*)pd;
            Triplet<byte>* endPtr = sourcePTr + PixelCount;

            for (; sourcePTr != endPtr; sourcePTr++, destinationPtr++)
            {
                *destinationPtr = TransformHP1IntParameters(sourcePTr->V1, sourcePTr->V2, sourcePTr->V3);
            }
        }
    }

    [Benchmark]
    public void TransformHP1PerByte()
    {
        int byteCount = PixelCount * 3;

        for (int i = 0; i < byteCount; i += 3)
        {
            int pixel = TransformHP1IntParametersReturnUint(_source[i], _source[i + 1], _source[i + 2]);
            _destination[i] = (byte)(pixel >> 16);
            _destination[i + 1] = (byte)(pixel >> 8);
            _destination[i + 2] = (byte)(pixel);
        }
    }

    [Benchmark]
    public void TransformHP1Generic()
    {
        var sourceTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(_source);
        var destinationTriplet = MemoryMarshal.Cast<byte, Triplet<byte>>(_destination);

        for (int i = 0; i < PixelCount; ++i)
        {
            var pixel = sourceTriplet[i];
            destinationTriplet[i] = TransformHP1Generic<byte>(pixel.V1, pixel.V2, pixel.V3);
        }
    }
}
