// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed.Test;

public class CommentEventArgsTest
{
    [Fact]
    public void Create()
    {
        byte[] data = [1, 2, 3];
        var eventArgs = new CommentEventArgs(data);

        Assert.True(eventArgs.Data.Span.SequenceEqual(data));
    }
}
