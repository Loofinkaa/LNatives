using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LBuffers
{
    /// <summary>
    /// This class provides a slice of the data getten from the <see cref="BufferOwner{T}"/>.
    /// </summary>
    /// <remarks>
    /// A ref struct guarantees that it won't leave the stack or be pushed to the heap. This allows the use of <see cref="Span{T}"/> and reference fields without being checked by the GC.
    /// </remarks>
    /// <typeparam name="T">The type of elements in the buffer slice.</typeparam>
    public readonly ref partial struct BufferSlice<T>
    {
        /// <summary>The owner of this bufferslice.</summary>
        private readonly BufferOwner<T> _owner;

        /// <summary>The span that contains the data getten from current owner.</summary>
        private readonly Span<T> _span;

        /// <summary>Gets the number of elements in the memory of this bufferslice.</summary>
        public int Length => _span.Length;

        /// <summary>Is this bufferslice is empty (has zero length).</summary>
        public bool IsEmpty => _span.IsEmpty;

        /// <summary>Access to the raw <see cref="Span{T}"/> for quick operations.</summary>
        public Span<T> Span => _span;

        /// <summary>
        /// Zero-allocation access to elements by index.
        /// </summary>
        /// <param name="index">The index of the element to access.</param>
        /// <returns>A reference to the element at the specified index.</returns>
        public ref T this[int index] => ref _span[index];

        /// <summary>
        /// Initializes this bufferslice. Internal only.
        /// </summary>
        /// <param name="owner">The bufferowner from where the data was get.</param>
        /// <param name="start">The start index of the data to get.</param>
        /// <param name="length">The length of the data to get.</param>
        internal BufferSlice(BufferOwner<T> owner, int start, int length)
        {
            _owner = owner;
            _span = owner.Span.Slice(start, length);
        }

        /// <summary>
        /// Creates a new bufferslice based on the current one with no allocations.
        /// </summary>
        /// <param name="start">The start index of the data to get.</param>
        /// <returns>A new <see cref="BufferSlice{T}"/> starting at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Throws when the start index is out of range.</exception>
        public BufferSlice<T> Slice(int start)
        {
            if ((uint)start > (uint)_span.Length)
                throw new ArgumentOutOfRangeException(nameof(start));

            return new BufferSlice<T>(_owner, start + GetOriginalOffset(), _span.Length - start);
        }

        /// <summary>
        /// Creates a new bufferslice based on the current one with no allocations.
        /// </summary>
        /// <param name="start">The start index of the data to get.</param>
        /// <param name="length">The length of the data to get.</param>
        /// <returns>A new <see cref="BufferSlice{T}"/> starting at the specified index with the specified length.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Throws when the start index or the length is out of range.</exception>
        public BufferSlice<T> Slice(int start, int length)
        {
            if ((uint)start > (uint)_span.Length || (uint)length > (uint)(_span.Length - start))
                throw new ArgumentOutOfRangeException();

            return new BufferSlice<T>(_owner, start + GetOriginalOffset(), length);
        }

        /// <summary>
        /// This function restores the absolute position in the original array.
        /// </summary>
        /// <returns>The original offset of the slice within the bufferowner.</returns>
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
        /// Tries to copy data to another span trimming to the minimum length.
        /// </summary>
        public void CopyToSafe(Span<T> destination)
        {
            int copyLength = Math.Min(_span.Length, destination.Length);
            _span.Slice(0, copyLength).CopyTo(destination.Slice(0, copyLength));
        }

        /// <summary>
        /// This function allocates a new array with the data from this bufferslice.
        /// </summary>
        /// <returns>A new array with the data from this bufferslice.</returns>
        public T[] ToArray()
        {
            return _span.ToArray();
        }

        public NonAllocEnumerator GetEnumerator() => new NonAllocEnumerator(_span);
    }
}