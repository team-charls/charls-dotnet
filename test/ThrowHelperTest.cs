// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

namespace CharLS.Managed.Test;

public class ThrowHelperTest
{
    [Fact]
    public void UseErrorCodeNoneForCodeCoverage()
    {
        var exception = ThrowHelper.CreateInvalidDataException(ErrorCode.None);

        Assert.True(string.IsNullOrEmpty(exception.Message));
        Assert.Equal(ErrorCode.None, exception.GetErrorCode());
    }
}
