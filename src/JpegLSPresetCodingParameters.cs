// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Diagnostics.CodeAnalysis;

namespace CharLS.Managed;

/// <summary>
/// Hold information about JPEG-LS preset coding parameters.
/// </summary>
public sealed record JpegLSPresetCodingParameters
{
    /// <summary>
    /// Default JPEG-LS preset coding parameters that contains all zero values.
    /// </summary>
    public static readonly JpegLSPresetCodingParameters Default = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="JpegLSPresetCodingParameters"/> class.
    /// </summary>
    public JpegLSPresetCodingParameters()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JpegLSPresetCodingParameters"/> class.
    /// </summary>
    /// <param name="maximumSampleValue">The maximum sample value of the pixels.</param>
    /// <param name="threshold1">The threshold 1 parameter.</param>
    /// <param name="threshold2">The threshold 2 parameter.</param>
    /// <param name="threshold3">The threshold 3 parameter.</param>
    /// <param name="resetValue">The reset value parameter.</param>
    public JpegLSPresetCodingParameters(int maximumSampleValue, int threshold1, int threshold2, int threshold3, int resetValue)
    {
        MaximumSampleValue = maximumSampleValue;
        Threshold1 = threshold1;
        Threshold2 = threshold2;
        Threshold3 = threshold3;
        ResetValue = resetValue;
    }

    /// <summary>
    /// Gets the maximum sample value of the pixel data.
    /// </summary>
    public int MaximumSampleValue { get; init; }

    /// <summary>
    /// Gets the threshold 1 parameter.
    /// </summary>
    public int Threshold1 { get; init; }

    /// <summary>
    /// Gets the threshold 2 parameter.
    /// </summary>
    public int Threshold2 { get; init; }

    /// <summary>
    /// Gets the threshold 3 parameter.
    /// </summary>
    public int Threshold3 { get; init; }

    /// <summary>
    /// Gets the reset value parameter.
    /// </summary>
    public int ResetValue { get; init; }

    internal bool IsDefault(int maximumSampleValue, int nearLossless)
    {
        if (MaximumSampleValue == 0 && Threshold1 == 0 && Threshold2 == 0 && Threshold3 == 0 && ResetValue == 0)
            return true;

        var defaultParameters = ComputeDefault(maximumSampleValue, nearLossless);
        if (MaximumSampleValue != defaultParameters.MaximumSampleValue)
            return false;

        if (Threshold1 != defaultParameters.Threshold1)
            return false;

        if (Threshold2 != defaultParameters.Threshold2)
            return false;

        if (Threshold3 != defaultParameters.Threshold3)
            return false;

        return ResetValue == defaultParameters.ResetValue;
    }

    internal bool TryMakeExplicit(
        int maximumComponentValue,
        int nearLossless,
        [NotNullWhen(returnValue: true)] out JpegLSPresetCodingParameters? explicitParameters)
    {
        // ISO/IEC 14495-1, C.2.4.1.1, Table C.1 defines the valid JPEG-LS preset coding parameters values.
        if (MaximumSampleValue != 0 &&
            (MaximumSampleValue < 1 || MaximumSampleValue > maximumComponentValue))
        {
            explicitParameters = null;
            return false;
        }

        int maximumSampleValue = MaximumSampleValue != 0 ? MaximumSampleValue : maximumComponentValue;
        if (Threshold1 != 0 && (Threshold1 < nearLossless + 1 || Threshold1 > maximumSampleValue))
        {
            explicitParameters = null;
            return false;
        }

        var defaultParameters = ComputeDefault(maximumSampleValue, nearLossless);

        int threshold1 = Threshold1 != 0 ? Threshold1 : defaultParameters.Threshold1;
        if (Threshold2 != 0 && (Threshold2 < threshold1 || Threshold2 > maximumSampleValue))
        {
            explicitParameters = null;
            return false;
        }

        int threshold2 = Threshold2 != 0 ? Threshold2 : defaultParameters.Threshold2;
        if (Threshold3 != 0 && (Threshold3 < threshold2 || Threshold3 > maximumSampleValue))
        {
            explicitParameters = null;
            return false;
        }

        if (ResetValue != 0 && (ResetValue < 3 || ResetValue > Math.Max(255, maximumSampleValue)))
        {
            explicitParameters = null;
            return false;
        }

        explicitParameters = new JpegLSPresetCodingParameters(
            maximumSampleValue,
            Threshold1 != 0 ? Threshold1 : defaultParameters.Threshold1,
            Threshold2 != 0 ? Threshold2 : defaultParameters.Threshold2,
            Threshold3 != 0 ? Threshold3 : defaultParameters.Threshold3,
            ResetValue != 0 ? ResetValue : defaultParameters.ResetValue);
        return true;
    }

    /// <summary>Default coding threshold values as defined by ISO/IEC 14495-1, C.2.4.1.1.1.</summary>
    internal static JpegLSPresetCodingParameters ComputeDefault(int maximumSampleValue, int nearLossless)
    {
        Debug.Assert(maximumSampleValue <= ushort.MaxValue);
        Debug.Assert(nearLossless >= 0 && nearLossless <= Algorithm.ComputeMaximumNearLossless(maximumSampleValue));

        // Default threshold values for JPEG-LS statistical modeling as defined in ISO/IEC 14495-1, table C.3
        // for the case MAXVAL = 255 and NEAR = 0.
        const int defaultThreshold1 = 3;  // BASIC_T1
        const int defaultThreshold2 = 7;  // BASIC_T2
        const int defaultThreshold3 = 21; // BASIC_T3

        if (maximumSampleValue >= 128)
        {
            int factor = (Math.Min(maximumSampleValue, 4095) + 128) / 256;
            int threshold1 =
                Clamp((factor * (defaultThreshold1 - 2)) + 2 + (3 * nearLossless), nearLossless + 1, maximumSampleValue);
            int threshold2 =
                Clamp((factor * (defaultThreshold2 - 3)) + 3 + (5 * nearLossless), threshold1, maximumSampleValue);

            return new JpegLSPresetCodingParameters(
                maximumSampleValue,
                threshold1,
                threshold2,
                Clamp((factor * (defaultThreshold3 - 4)) + 4 + (7 * nearLossless), threshold2, maximumSampleValue),
                Constants.DefaultResetThreshold);
        }
        else
        {
            int factor = 256 / (maximumSampleValue + 1);
            int threshold1 =
                Clamp(
                    Math.Max(2, (defaultThreshold1 / factor) + (3 * nearLossless)), nearLossless + 1, maximumSampleValue);
            int threshold2 =
                Clamp(Math.Max(3, (defaultThreshold2 / factor) + (5 * nearLossless)), threshold1, maximumSampleValue);

            return new JpegLSPresetCodingParameters(
                maximumSampleValue,
                threshold1,
                threshold2,
                Clamp(Math.Max(4, (defaultThreshold3 / factor) + (7 * nearLossless)), threshold2, maximumSampleValue),
                Constants.DefaultResetThreshold);
        }
    }

    /// <summary>Clamping function as defined by ISO/IEC 14495-1, Figure C.3.</summary>
    private static int Clamp(int i, int j, int maximumSampleValue)
    {
        return i > maximumSampleValue || i < j ? j : i;
    }
}
