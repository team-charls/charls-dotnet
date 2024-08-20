// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed.Test;

public class CopyFromLineBufferTest
{
    [Fact]
    public void GetMethod8BitInterleaveModeNone()
    {
        var method = CopyFromLineBuffer.GetMethod(8, 1, InterleaveMode.None, ColorTransformation.None);
        Assert.NotNull(method);
    }

    [Fact]
    public void GetMethod16BitInterleaveModeNone()
    {
        var method = CopyFromLineBuffer.GetMethod(16, 1, InterleaveMode.None, ColorTransformation.None);
        Assert.NotNull(method);
    }
}
