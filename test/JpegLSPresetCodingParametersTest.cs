// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed.Test;

public sealed class JpegLSPresetCodingParametersTest
{
    [Fact]
    public void DefaultIsAllZeros()
    {
        var defaultParameters = JpegLSPresetCodingParameters.Default;

        Assert.Equal(0, defaultParameters.MaximumSampleValue);
        Assert.Equal(0, defaultParameters.ResetValue);
        Assert.Equal(0, defaultParameters.Threshold1);
        Assert.Equal(0, defaultParameters.Threshold2);
        Assert.Equal(0, defaultParameters.Threshold3);
    }

    [Fact]
    public void DefaultIsDefault()
    {
        var defaultParameters = JpegLSPresetCodingParameters.Default;
        Assert.True(defaultParameters.IsDefault(256, 0));
    }

    [Fact]
    public void CreateDefault()
    {
        var defaultParameters = new JpegLSPresetCodingParameters(256, 3, 7, 21, 64);
        Assert.True(defaultParameters.IsDefault(256, 0));
    }

    [Fact]
    public void IsNotDefaultThreshold2()
    {
        var defaultParameters = new JpegLSPresetCodingParameters(256, 3, 7 + 1, 21, 64);
        Assert.False(defaultParameters.IsDefault(256, 0));
    }

    [Fact]
    public void IsNotDefaultThreshold3()
    {
        var defaultParameters = new JpegLSPresetCodingParameters(256, 3, 7, 21 + 1, 64);
        Assert.False(defaultParameters.IsDefault(256, 0));
    }

    [Fact]
    public void MaxValueLossless()
    {
        var expected = ComputeDefaultsUsingReferenceImplementation(ushort.MaxValue, 0);
        var parameters = JpegLSPresetCodingParameters.ComputeDefault(ushort.MaxValue, 0);

        Assert.Equal(expected.MaxValue, parameters.MaximumSampleValue);
        Assert.Equal(expected.T1, parameters.Threshold1);
        Assert.Equal(expected.T2, parameters.Threshold2);
        Assert.Equal(expected.T3, parameters.Threshold3);
        Assert.Equal(expected.Reset, parameters.ResetValue);
    }

    [Fact]
    public void MinValueLossless()
    {
        var expected = ComputeDefaultsUsingReferenceImplementation(3, 0);
        var parameters = JpegLSPresetCodingParameters.ComputeDefault(3, 0);

        Assert.Equal(expected.MaxValue, parameters.MaximumSampleValue);
        Assert.Equal(expected.T1, parameters.Threshold1);
        Assert.Equal(expected.T2, parameters.Threshold2);
        Assert.Equal(expected.T3, parameters.Threshold3);
        Assert.Equal(expected.Reset, parameters.ResetValue);
    }

    [Fact]
    public void MinHighValueLossless()
    {
        var expected = ComputeDefaultsUsingReferenceImplementation(128, 0);
        var parameters = JpegLSPresetCodingParameters.ComputeDefault(128, 0);

        Assert.Equal(expected.MaxValue, parameters.MaximumSampleValue);
        Assert.Equal(expected.T1, parameters.Threshold1);
        Assert.Equal(expected.T2, parameters.Threshold2);
        Assert.Equal(expected.T3, parameters.Threshold3);
        Assert.Equal(expected.Reset, parameters.ResetValue);
    }

    [Fact]
    public void MaxLowValueLossless()
    {
        var expected = ComputeDefaultsUsingReferenceImplementation(127, 0);
        var parameters = JpegLSPresetCodingParameters.ComputeDefault(127, 0);

        Assert.Equal(expected.MaxValue, parameters.MaximumSampleValue);
        Assert.Equal(expected.T1, parameters.Threshold1);
        Assert.Equal(expected.T2, parameters.Threshold2);
        Assert.Equal(expected.T3, parameters.Threshold3);
        Assert.Equal(expected.Reset, parameters.ResetValue);
    }

    [Fact]
    public void MaxValueLossy()
    {
        var expected = ComputeDefaultsUsingReferenceImplementation(ushort.MaxValue, 255);
        var parameters = JpegLSPresetCodingParameters.ComputeDefault(ushort.MaxValue, 255);

        Assert.Equal(expected.MaxValue, parameters.MaximumSampleValue);
        Assert.Equal(expected.T1, parameters.Threshold1);
        Assert.Equal(expected.T2, parameters.Threshold2);
        Assert.Equal(expected.T3, parameters.Threshold3);
        Assert.Equal(expected.Reset, parameters.ResetValue);
    }

    [Fact]
    public void MinValueLossy()
    {
        var expected = ComputeDefaultsUsingReferenceImplementation(3, 1);
        var parameters = JpegLSPresetCodingParameters.ComputeDefault(3, 1);

        Assert.Equal(expected.MaxValue, parameters.MaximumSampleValue);
        Assert.Equal(expected.T1, parameters.Threshold1);
        Assert.Equal(expected.T2, parameters.Threshold2);
        Assert.Equal(expected.T3, parameters.Threshold3);
        Assert.Equal(expected.Reset, parameters.ResetValue);
    }

    [Fact]
    public void IsValidDefault()
    {
        const int bitsPerSample = 16;
        const int maximumComponentValue = (1 << bitsPerSample) - 1;
        JpegLSPresetCodingParameters p = new();

        Assert.True(p.TryMakeExplicit(maximumComponentValue, 0, out _));
    }

    [Fact]
    public void IsValidThresholdsZero()
    {
        const int bitsPerSample = 16;
        const int maximumComponentValue = (1 << bitsPerSample) - 1;
        JpegLSPresetCodingParameters p = new(maximumComponentValue, 0, 0, 0, 63);

        Assert.True(p.TryMakeExplicit(maximumComponentValue, 0, out _));
    }

    // Threshold function of JPEG-LS reference implementation.
    internal static Thresholds ComputeDefaultsUsingReferenceImplementation(int maxValue, ushort near)
    {
        Thresholds result = new() { MaxValue = maxValue, Reset = 64 };

        if (result.MaxValue >= 128)
        {
            int factor = result.MaxValue;
            if (factor > 4095)
                factor = 4095;
            factor = (factor + 128) >> 8;
            result.T1 = (factor * (3 - 2)) + 2 + (3 * near);
            if (result.T1 > result.MaxValue || result.T1 < near + 1)
                result.T1 = near + 1;
            result.T2 = (factor * (7 - 3)) + 3 + (5 * near);
            if (result.T2 > result.MaxValue || result.T2 < result.T1)
                result.T2 = result.T1;
            result.T3 = (factor * (21 - 4)) + 4 + (7 * near);
            if (result.T3 > result.MaxValue || result.T3 < result.T2)
                result.T3 = result.T2;
        }
        else
        {
            int factor = 256 / (result.MaxValue + 1);
            result.T1 = (3 / factor) + (3 * near);
            if (result.T1 < 2)
                result.T1 = 2;
            if (result.T1 > result.MaxValue || result.T1 < near + 1)
                result.T1 = near + 1;
            result.T2 = (7 / factor) + (5 * near);
            if (result.T2 < 3)
                result.T2 = 3;
            if (result.T2 > result.MaxValue || result.T2 < result.T1)
                result.T2 = result.T1;
            result.T3 = (21 / factor) + (7 * near);
            if (result.T3 < 4)
                result.T3 = 4;
            if (result.T3 > result.MaxValue || result.T3 < result.T2)
                result.T3 = result.T2;
        }

        return result;
    }

    internal struct Thresholds
    {
        internal int MaxValue;
        internal int T1;
        internal int T2;
        internal int T3;
        internal int Reset;
    }
}
