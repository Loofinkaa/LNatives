using System;
using System.Runtime.CompilerServices;

namespace LStreams
{
    /// <summary>
    /// A low-level bitstream writer. This structure allows to write data bit by bit.
    /// </summary>
    public ref struct BitWriter
    {
        /// <summary>Target span where bits going to.</summary>
        private readonly Span<byte> _buffer;

        /// <summary>Current endianess for this writer.</summary>
        private readonly BitOrder _bitOrder;

        /// <summary>This is current stream position.</summary>
        private int _bitPosition;

        /// <summary>
        /// Shows how many bits was written.
        /// </summary>
        // Technicaly it just represents current position. So we can trace how many bits were written.
        public int BitsWritten => _bitPosition;

        /// <summary>
        /// Shows how many bytes was written.
        /// </summary>
        // To calculate how many bytes was written we use a simple formula:
        // (position + 7) / 8
        public int BytesWritten => (_bitPosition + 7) / 8;

        /// <summary>
        /// Shows how many free bits remaining in the buffer.
        /// </summary>
        // To calculate remaining size in bits use this formula:
        // (length * 8) - position
        public int RemainingBits => (_buffer.Length * 8) - _bitPosition;

        /// <summary>
        /// Initializes this bitwriter.
        /// </summary>
        /// <param name="buffer">The span passed in this argument will be the buffer used in this bitwriter.</param>
        /// <param name="bitOrder">The order type that will be used to write bits rightly in the buffer. By default this setted to Big-endian.</param>
        public BitWriter(Span<byte> buffer, BitOrder bitOrder = BitOrder.MsbFirst)
        {
            _buffer = buffer;
            _bitOrder = bitOrder;
            _bitPosition = 0;
        }

        /// <summary>
        /// This function tries to write the bit to the buffer.
        /// </summary>
        /// <param name="bitValue">A bit representated by the boolean. If this boolean is true it means that the bit is 1, if this boolean is false it means that the bit is 0.</param>
        /// <returns>A bit will not write to the buffer if buffer is overflowed, so in this case this function returns false. Other case when everything is success this function returns true.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteBit(bool bitValue)
        {
            if (_bitPosition >= _buffer.Length * 8)
                return false;

            if (bitValue)
            {
                int bytePos = _bitPosition >> 3;
                int bitInByte = _bitPosition & 7;

                if (_bitOrder == BitOrder.MsbFirst)
                    _buffer[bytePos] |= (byte)(0x80 >> bitInByte);
                else
                    _buffer[bytePos] |= (byte)(1 << bitInByte);
            }

            _bitPosition++;
            return true;
        }

        /// <summary>
        /// This function tries to write bits (representated by ulong) to the buffer.
        /// </summary>
        /// <param name="value">A ulong number takes up 64 bits of memory. We use it to store several bits at once, transmitting it as a hexadecimal number.</param>
        /// <param name="bitCount">The number of bits that will be written to the buffer.</param>
        /// <returns>If bit count is less than zero or greater than 64, or if after this operation the position would be longer than the buffer size, this function returns false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteBits(ulong value, int bitCount)
        {
            if (bitCount < 0 || bitCount > 64 || _bitPosition + bitCount > _buffer.Length * 8)
                return false;

            for (int i = 0; i < bitCount; i++)
            {
                bool bit = _bitOrder == BitOrder.MsbFirst
                    ? ((value >> (bitCount - 1 - i)) & 1) == 1
                    : ((value >> i) & 1) == 1;

                TryWriteBit(bit);
            }

            return true;
        }

        /// <summary>
        /// This function aligns the current write position to the beginning of the next byte, padding the current byte with zero bits to the end.
        /// </summary>
        /// <returns>If alignment has occurred, this function will return true.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryFlushByte()
        {
            int remainder = _bitPosition & 7;

            if (remainder > 0)
            {
                _bitPosition += 8 - remainder;
                return true;
            }

            return false;
        }

        /// <summary>
        /// This function clears the buffer and sets the position to zero.
        /// </summary>
        public void Clear()
        {
            _buffer.Clear();
            _bitPosition = 0;
        }
    }
}