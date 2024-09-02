// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed.Test;

public class CopyFromLineBufferTest
{
    [Fact]
    public void GetCopyMethod8BitInterleaveModeNone()
    {
        var method = CopyFromLineBuffer.GetCopyMethod(8, InterleaveMode.None, 1,  ColorTransformation.None);
        Assert.NotNull(method);
    }

    [Fact]
    public void GetCopyMethod16BitInterleaveModeNone()
    {
        var method = CopyFromLineBuffer.GetCopyMethod(16, InterleaveMode.None, 1,  ColorTransformation.None);
        Assert.NotNull(method);
    }

    [Fact]
    public void GetCopyMethodNotUsed()
    {
        var method = CopyFromLineBuffer.GetCopyMethod(8, InterleaveMode.None, 1, ColorTransformation.None);
        Assert.NotNull(method);
    }
}
