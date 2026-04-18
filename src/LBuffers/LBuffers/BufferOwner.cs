using System;
using System.Buffers;

#nullable enable

namespace LBuffers
{
    /// <summary>
    /// This class provides zero-allocation control over the array.
    /// </summary>
    /// <typeparam name="T">The type of data to store in the array.</typeparam>
    public sealed class BufferOwner<T> : IDisposable
    {
        /// <summary>Current array. Might be null.</summary>
        private T[]? _array;

        /// <summary>The pool where we'll push the array when this bufferowner gonna dispose.</summary>
        private readonly ArrayPool<T> _pool;

        private readonly int _length;
        private bool _disposed;

        /// <summary>The length of current array.</summary>
        public int Length => _length;

        /// <summary>Is this bufferowner disposed?</summary>
        public bool IsDisposed => _disposed;

        /// <summary>Direct access to external <see cref="Span{T}"/>.</summary>
        public Span<T> Span
        {
            get
            {
                ThrowIfDisposed();
                return _array.AsSpan(0, _length);
            }
        }

        /// <summary>Direct access to external <see cref="Memory{T}"/>.</summary>
        public Memory<T> Memory
        {
            get
            {
                ThrowIfDisposed();

                return _array.AsMemory(0, _length);
            }
        }

        /// <summary>
        /// Initializes this bufferowner.
        /// </summary>
        /// <param name="array">A given array that will be owned by this bufferowner.</param>
        /// <param name="length">The length of a given array.</param>
        /// <param name="pool">The pool where a given array will be pushed after the use.</param>
        private BufferOwner(T[] array, int length, ArrayPool<T> pool)
        {
            _array = array;
            _length = length;
            _pool = pool;
        }

        /// <summary>
        /// Rents a buffer of the specified size from the shared pool.
        /// </summary>
        public static BufferOwner<T> Rent(int minimumLength, ArrayPool<T>? pool = null)
        {
            if (minimumLength < 0)
                throw new ArgumentOutOfRangeException(nameof(minimumLength));

            var actualPool = pool ?? ArrayPool<T>.Shared;
            var array = actualPool.Rent(minimumLength);

            return new BufferOwner<T>(array, minimumLength, actualPool);
        }

        /// <summary>
        /// Creates a new bufferowner from an existing array.
        /// </summary>
        public static BufferOwner<T> FromArray(T[] array)
        {
            return new BufferOwner<T>(array, array.Length, ArrayPool<T>.Shared);
        }

        /// <summary>
        /// Gets a slice over the entire buffer.
        /// </summary>
        public BufferSlice<T> Slice()
        {
            ThrowIfDisposed();

            return new BufferSlice<T>(this, 0, _length);
        }

        /// <summary>
        /// Gets a slice over a segment of the buffer.
        /// </summary>
        public BufferSlice<T> Slice(int start, int length)
        {
            ThrowIfDisposed();

            if ((uint)start > (uint)_length || (uint)length > (uint)(_length - start))
                throw new ArgumentOutOfRangeException();

            return new BufferSlice<T>(this, start, length);
        }

        /// <summary>
        /// This function drops the exception if the buffer is already disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BufferOwner<T>));
        }

        /// <summary>
        /// Disposes the buffer.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed && _array != null)
            {
                _pool.Return(_array);
                _array = null;
                _disposed = true;
            }
        }
    }
}