namespace LBuffers.Tests
{
    /// <summary>
    /// This class is a test of <see cref="BufferSlice{T}"/> and <see cref="BufferOwner{T}"/>.
    /// </summary>
    public static class BufferSliceAndOwnerTest
    {
        /// <summary>
        /// Writes some mock data to a buffer rented from the pool, then takes slices of that buffer and reads an integer from it. 
        /// </summary>
        /// <returns>A test value.</returns>
        public static int Test()
        {
            //For example lets rent a buffer of 32 bytes from the pool.
            //This will not allocate on the heap, but will give us a buffer we can work with.
            using (BufferOwner<byte> owner = BufferOwner<byte>.Rent(32))
            {
                //Get a slice of the entire buffer.
                BufferSlice<byte> fullSlice = owner.Slice();

                //Fill the buffer with some mock data (this is just an example, you must fill it with real data in your projects).
                FillWithMockData(fullSlice);

                //Take bytes from 8 to 20.
                BufferSlice<byte> headerSlice = fullSlice.Slice(8, 20);

                //You can also take a slice of a slice.
                //There we skipping 8 bytes and then taking 4 bytes, dont forget than previously we skipped 8 bytes so we will get bytes from 16 to 20.
                BufferSlice<byte> sliceInAnotherSlice = headerSlice.Slice(8, 4);

                //There is out test integer, and we can read it directly from the slice without any allocations or copying.
                int testInteger = sliceInAnotherSlice.ReadInt32LittleEndian();

                return testInteger;
            }
        }

        /// <summary>
        /// Feels the provided bufferslice with mock data.
        /// </summary>
        /// <param name="slice">A bufferslice to fill with mock data.</param>
        private static void FillWithMockData(BufferSlice<byte> slice)
        {
            //Writes 67 to the bufferslice with the offset of 16 bytes.
            slice.WriteInt32LittleEndian(1001011, 16);
        }
    }
}