using System;
using System.Buffers;

#nullable enable

namespace LBuffers
{
    /// <uncommented/>
    public sealed class BufferOwner<T> : IDisposable
    {
        /// <uncommented/>
        private T[]? _array;

        /// <uncommented/>
        private readonly int _length;

        /// <uncommented/>
        private readonly ArrayPool<T> _pool;

        /// <uncommented/>
        private bool _disposed;

        /// <uncommented/>
        public int Length => _length;
        /// <uncommented/>
        public bool IsDisposed => _disposed;

        private BufferOwner(T[] array, int length, ArrayPool<T> pool)
        {
            _array = array;
            _length = length;
            _pool = pool;
        }

        /// <summary>
        /// Rent a buffer of the specified size from the shared pool.
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
        /// Creates an owner from an existing array.
        /// </summary>
        public static BufferOwner<T> FromArray(T[] array)
        {
            return new BufferOwner<T>(array, array.Length, ArrayPool<T>.Shared);
        }

        /// <summary>
        /// Gets a Slice (Span) over the entire buffer.
        /// </summary>
        public BufferSlice<T> Slice()
        {
            ThrowIfDisposed();
            return new BufferSlice<T>(this, 0, _length);
        }

        /// <summary>
        /// Gets a Slice (Span) over a segment of the buffer.
        /// </summary>
        public BufferSlice<T> Slice(int start, int length)
        {
            ThrowIfDisposed();
            if ((uint)start > (uint)_length || (uint)length > (uint)(_length - start))
                throw new ArgumentOutOfRangeException();

            return new BufferSlice<T>(this, start, length);
        }

        /// <summary>
        /// Direct access to external <see cref="Span{T}"/>.
        /// </summary>
        public Span<T> Span
        {
            get
            {
                ThrowIfDisposed();
                return _array.AsSpan(0, _length);
            }
        }

        /// <summary>
        /// Direct access to external <see cref="Memory{T}"/>.
        /// </summary>
        public Memory<T> Memory
        {
            get
            {
                ThrowIfDisposed();
                return _array.AsMemory(0, _length);
            }
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