// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using Xunit;

namespace CharLS.JpegLS.Test;

public class AlgorithmTest
{
    [Fact]
    public void Log2Ceiling()
    {
        CallAndCompareLog2Ceiling(1);
        CallAndCompareLog2Ceiling(2);
        CallAndCompareLog2Ceiling(32);
        CallAndCompareLog2Ceiling(31);
        CallAndCompareLog2Ceiling(33);
        CallAndCompareLog2Ceiling(ushort.MaxValue);
        CallAndCompareLog2Ceiling(ushort.MaxValue + 1);
        CallAndCompareLog2Ceiling((int)(uint.MaxValue >> 2));
    }

    [Fact]
    public void InitializationValueForA()
    {
        int minValue = Algorithm.InitializationValueForA(4);
        int maxValue = Algorithm.InitializationValueForA(ushort.MaxValue + 1);

        Assert.Equal(2, minValue);
        Assert.Equal(1024, maxValue);
    }

    [Fact]
    public void MapErrorValue()
    {
        MapErrorValueAlgorithm(0);
        MapErrorValueAlgorithm(1);
        MapErrorValueAlgorithm(-1);
        MapErrorValueAlgorithm(ushort.MaxValue);
        MapErrorValueAlgorithm(ushort.MinValue);
        MapErrorValueAlgorithm(int.MaxValue / 2);
        MapErrorValueAlgorithm(int.MinValue / 2);
    }

    [Fact]
    public void UnmapErrorValue()
    {
        UnmapErrorValueAlgorithm(0);
        UnmapErrorValueAlgorithm(1);
        UnmapErrorValueAlgorithm(2);
        UnmapErrorValueAlgorithm(ushort.MaxValue);
        UnmapErrorValueAlgorithm(int.MaxValue - 2);
        UnmapErrorValueAlgorithm(int.MaxValue - 1);
        UnmapErrorValueAlgorithm(int.MaxValue);
    }

    [Fact]
    public void MapUnmapErrorValue()
    {
        MapUnmapErrorValueAlgorithm(0);
        MapUnmapErrorValueAlgorithm(1);
        MapUnmapErrorValueAlgorithm(-1);
        MapUnmapErrorValueAlgorithm(ushort.MaxValue);
        MapUnmapErrorValueAlgorithm(ushort.MinValue);
        MapUnmapErrorValueAlgorithm(int.MaxValue / 2);
        MapUnmapErrorValueAlgorithm(int.MinValue / 2);
    }

    private static void MapErrorValueAlgorithm(int errorValue)
    {
        int actual = Algorithm.MapErrorValue(errorValue);
        int expected1 = MapErrorValueOriginal(errorValue);

        Assert.True(actual >= 0);
        Assert.Equal(expected1, actual);
    }

    private static void UnmapErrorValueAlgorithm(int mappedErrorValue)
    {
        int actual = Algorithm.UnmapErrorValue(mappedErrorValue);
        int expected1 = UnmapErrorValueOriginal(mappedErrorValue);

        Assert.Equal(expected1, actual);
    }

    private static void MapUnmapErrorValueAlgorithm(int errorValue)
    {
        int mappedErrorValue = Algorithm.MapErrorValue(errorValue);
        int actual = Algorithm.UnmapErrorValue(mappedErrorValue);

        Assert.Equal(errorValue, actual);
    }

    private static void CallAndCompareLog2Ceiling(int arg)
    {
        // Use the standard floating point algorithm to compute the expected value.
        int expected = (int)Math.Ceiling(Math.Log2(arg));
        Assert.Equal(expected, Algorithm.Log2Ceiling(arg));
    }

    /// <summary>
    /// This is the original algorithm of ISO/IEC 14495-1, A.5.2, Code Segment A.11 (second else branch)
    /// It will map signed values to unsigned values.
    /// </summary>
    private static int MapErrorValueOriginal(int errorValue)
    {
        if (errorValue >= 0)
            return 2 * errorValue;

        // ReSharper disable once IntVariableOverflowInUncheckedContext
        return -2 * errorValue - 1;
    }

    /// <summary>
    /// This is the original inverse algorithm of ISO/IEC 14495-1, A.5.2, Code Segment A.11 (second else branch)
    /// It will map unsigned values back to unsigned values.
    /// </summary>
    private static int UnmapErrorValueOriginal(int mappedErrorValue)
    {
        if (mappedErrorValue % 2 == 0)
            return mappedErrorValue / 2;

        return (mappedErrorValue / -2) - 1;
    }
}
