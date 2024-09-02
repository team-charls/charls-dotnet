// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed.Test;

public class GolombCodeMatchTableTest
{
    [Fact]
    public void GolombTableCreate()
    {
        // Default created table is filled with zero's:
        GolombCodeMatchTable golombMatchTable = new(0);

        Assert.Equal(0, golombMatchTable.Get(0).BitCount);
        Assert.Equal(0, golombMatchTable.Get(0).ErrorValue);
        Assert.Equal(1, golombMatchTable.Get(255).BitCount);
        Assert.Equal(0, golombMatchTable.Get(255).ErrorValue);
    }
}
