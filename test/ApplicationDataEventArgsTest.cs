// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed.Test;

public class ApplicationDataEventArgsTest
{
    [Fact]
    public void Create()
    {
        byte[] data = [1, 2, 3];
        var eventArgs = new ApplicationDataEventArgs(3, data);

        Assert.Equal(3, eventArgs.Id);
        Assert.True(eventArgs.Data.Span.SequenceEqual(data));
    }
}
