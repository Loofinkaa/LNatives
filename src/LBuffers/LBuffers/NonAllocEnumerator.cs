using System;

namespace LBuffers
{
    /// <inheritdoc/>
    public readonly ref partial struct BufferSlice<T>
    {
        /// <summary>
        /// Internal struct for enumerating over the elements of the buffer slice without allocations.
        /// </summary>
        public ref struct NonAllocEnumerator
        {
            private Span<T> _span;
            private int _index;

            internal NonAllocEnumerator(Span<T> span)
            {
                _span = span;
                _index = -1;
            }

            public ref T Current => ref _span[_index];
            public bool MoveNext() => ++_index < _span.Length;
        }
    }
}