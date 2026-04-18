using System.Buffers.Binary;

namespace LBuffers.Tests
{
    /// <summary>
    /// This class is only needed to test the buffer lib. I recommend to use the streams lib insted.
    /// </summary>
    public static class AdditionalTestingExtensions
    {
        /// <summary>
        /// Writes a value of type int to the buffer in little-endian format.
        /// </summary>
        public static void WriteInt32LittleEndian(this BufferSlice<byte> slice, int value, int offset = 0)
        {
            BinaryPrimitives.WriteInt32LittleEndian(slice.Span.Slice(offset), value);
        }

        /// <summary>
        /// Writes a value of type int to the buffer in big-endian format.
        /// </summary>
        public static void WriteInt32BigEndian(this BufferSlice<byte> slice, int value, int offset = 0)
        {
            BinaryPrimitives.WriteInt32BigEndian(slice.Span.Slice(offset), value);
        }

        /// <summary>
        /// Reads a value of type int from the buffer in little-endian format.
        /// </summary>
        public static int ReadInt32LittleEndian(this BufferSlice<byte> slice, int offset = 0)
        {
            return BinaryPrimitives.ReadInt32LittleEndian(slice.Span.Slice(offset));
        }

        /// <summary>
        /// Reads a value of type int from the buffer in big-endian format.
        /// </summary>
        public static int ReadInt32BigEndian(this BufferSlice<byte> slice, int offset = 0)
        {
            return BinaryPrimitives.ReadInt32BigEndian(slice.Span.Slice(offset));
        }
    }
}