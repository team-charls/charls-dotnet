// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System.Buffers;

namespace CharLS.Managed;

/// <summary>
/// Helper class to enable dispose pattern with ArrayPool, slightly more efficient than MemoryPool as it returns a struct.
/// </summary>
internal static class ArrayPoolHelper
{
    /// <remark>
    /// Will rent an array and clear it, ArrayPool doesn't guarantee that it returns a cleared array.
    /// </remark>
    internal static ArrayFromPool<T> Rent<T>(int minimumLength)
    {
        return new ArrayFromPool<T>(minimumLength);
    }

#if DEBUG
    internal struct ArrayFromPool<T> : IDisposable
    {
        private bool _disposed;

#else
    internal readonly struct ArrayFromPool<T> : IDisposable
    {
#endif
        internal ArrayFromPool(int minimumLength)
        {
            Value = ArrayPool<T>.Shared.Rent(minimumLength);
            Array.Clear(Value);
        }

        internal T[] Value { get; }

        void IDisposable.Dispose()
        {
            ArrayPool<T>.Shared.Return(Value);
#if DEBUG
            Debug.Assert(!_disposed);
            _disposed = true;
#endif
        }
    }
}
