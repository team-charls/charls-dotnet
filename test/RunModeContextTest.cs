// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed.Test;

public class RunModeContextTest
{
    [Fact]
    public void UpdateVariables()
    {
        RunModeContext runModeContext = new(0, 4);

        runModeContext.UpdateVariables(3, 27, 0);

        Assert.Equal(3, runModeContext.ComputeGolombCodingParameter());
    }
}
