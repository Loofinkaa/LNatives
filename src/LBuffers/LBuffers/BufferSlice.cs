using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LBuffers
{
    /// <summary>
    /// A buffer slice. A ref struct guarantees that it won't leave the stack or be pushed to the heap.
    /// </summary>
    /// <remarks>
    /// C# compiler guarantees that an instance of such a type cannot be placed in the heap. This allows the use of <see cref="Span{T}"/> and reference fields without being checked by the GC.
    /// </remarks>
    /// <typeparam name="T">The type of elements in the buffer slice.</typeparam>
    public readonly ref partial struct BufferSlice<T>
    {
        /// <summary>
        /// The owner of this slice.
        /// </summary>
        private readonly BufferOwner<T> _owner;

        /// <summary>
        /// Current slice span.
        /// </summary>
        private readonly Span<T> _span;

        /// <summary>
        /// Gets the number of elements in the collection.
        /// </summary>
        public int Length => _span.Length;

        /// <summary>
        /// Is current span empty (has zero length).
        /// </summary>
        public bool IsEmpty => _span.IsEmpty;

        internal BufferSlice(BufferOwner<T> owner, int start, int length)
        {
            _owner = owner;
            _span = owner.Span.Slice(start, length);
        }

        /// <summary>
        /// Access to the raw <see cref="Span{T}"/> for quick operations.
        /// </summary>
        public Span<T> Span => _span;

        /// <summary>
        /// Zero-allocation access to elements by index.
        /// </summary>
        /// <param name="index">The index of the element to access.</param>
        /// <returns>A reference to the element at the specified index.</returns>
        public ref T this[int index] => ref _span[index];

        /// <summary>
        /// Creates a new slice based on the current one with no allocations.
        /// </summary>
        /// <param name="start">The starting index of the slice.</param>
        /// <returns>A new <see cref="BufferSlice{T}"/> starting at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Throws when the start index is out of range.</exception>
        public BufferSlice<T> Slice(int start)
        {
            if ((uint)start > (uint)_span.Length)
                throw new ArgumentOutOfRangeException(nameof(start));

            return new BufferSlice<T>(_owner, start + GetOriginalOffset(), _span.Length - start);
        }

        /// <summary>
        /// Creates a new slice based on the current one with no allocations.
        /// </summary>
        /// <param name="start">The starting index of the slice.</param>
        /// <param name="length">The length of the slice.</param>
        /// <returns>A new <see cref="BufferSlice{T}"/> starting at the specified index with the specified length.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Throws when the start or length is out of range.</exception>
        public BufferSlice<T> Slice(int start, int length)
        {
            if ((uint)start > (uint)_span.Length || (uint)length > (uint)(_span.Length - start))
                throw new ArgumentOutOfRangeException();

            return new BufferSlice<T>(_owner, start + GetOriginalOffset(), length);
        }

        /// <summary>
        /// Kludge for restoring the absolute position in the original array.
        /// Since Span does not store the original offset, we calculate it using pointers.
        /// This is a hack, but completely zero-alloc and safe within a single process.
        /// </summary>
        /// <returns></returns>
        private int GetOriginalOffset()
        {
            ref T localRef = ref MemoryMarshal.GetReference(_span);
            ref T ownerRef = ref MemoryMarshal.GetReference(_owner.Span);

            return (int)Unsafe.ByteOffset(ref ownerRef, ref localRef) / Unsafe.SizeOf<T>();
        }

        /// <summary>
        /// Copies data to another span.
        /// </summary>
        public void CopyTo(Span<T> destination) => _span.CopyTo(destination);

        /// <summary>
        /// Tries to copy trimming to the minimum length.
        /// </summary>
        public void CopyToSafe(Span<T> destination)
        {
            int copyLength = Math.Min(_span.Length, destination.Length);
            _span.Slice(0, copyLength).CopyTo(destination.Slice(0, copyLength));
        }

        /// <summary>
        /// This function allocates a new array.
        /// </summary>
        /// <returns></returns>
        public T[] ToArray()
        {
            return _span.ToArray();
        }

        public NonAllocEnumerator GetEnumerator() => new NonAllocEnumerator(_span);
    }
}
