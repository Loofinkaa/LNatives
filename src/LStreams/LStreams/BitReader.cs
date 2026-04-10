using System;
using System.Runtime.CompilerServices;

namespace LStreams
{
    /// <summary>
    /// A low-level bitstream read. This structure allows to read data bit by bit.
    /// </summary>
    public ref struct BitReader 
    {
        /// <summary>Target (readonly) span from where bits reading.</summary>
        private readonly ReadOnlySpan<byte> _buffer;

        /// <summary>Current endianess for this reader.</summary>
        private readonly BitOrder _bitOrder;

        /// <summary>This is current stream position.</summary>
        private int _bitPosition;

        /// <summary>
        /// Shows how many bits remaining to read.
        /// </summary>
        // To calculate how many bits left use this formula:
        // (length*8)-position
        public int BitsRemaining => (_buffer.Length * 8) - _bitPosition;

        /// <summary>
        /// Shows how many bytes remaining to read.
        /// </summary>
        // To calculate how many bits left use this formula:
        // length - ((position + 7) / 8)
        public int BytesRemaining => _buffer.Length - ((_bitPosition + 7) / 8);

        /// <summary>
        /// Initializes this bitreader.
        /// </summary>
        /// <param name="buffer">The (readonly) span passed in this argument will be the buffer used in this bitreader.</param>
        /// <param name="bitOrder">The order type that will be used to read bits rightly from the buffer. By default this setted to Big-endian (as in the bitwriter).</param>
        public BitReader(ReadOnlySpan<byte> buffer, BitOrder bitOrder = BitOrder.MsbFirst)
        {
            _buffer = buffer;
            _bitOrder = bitOrder;
            _bitPosition = 0;
        }

        /// <summary>
        /// This function tries to read a bit from the buffer.
        /// </summary>
        /// <param name="value">A bit readed from the buffer.</param>
        /// <returns>If position is equal or over than the length of the buffer, this function returns false. Other case when everything is success this function returns true.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadBit(out bool value)
        {
            value = false;

            if (_bitPosition >= _buffer.Length * 8)
                return false;

            int bytePosition = _bitPosition >> 3;
            int bitInByte = _bitPosition & 7;

            byte mask = _bitOrder == BitOrder.MsbFirst
                ? (byte)(0x80 >> bitInByte)
                : (byte)(1 << bitInByte);

            value = (_buffer[bytePosition] & mask) != 0;

            _bitPosition++;
            return true;
        }

        /// <summary>
        /// This function tries to read some count of bits from the buffer and return they.
        /// </summary>
        /// <param name="bitCount">A count of bits to read.</param>
        /// <param name="value">This ulong number contains readed bits in hexadecimal cast.</param>
        /// <returns>If bit count is less than zero or greater than 64, or if after this operation the position would be longer than the buffer size, this function returns false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadBits(int bitCount, out ulong value)
        {
            value = 0;

            if (bitCount < 0 || bitCount > 64 || _bitPosition + bitCount > _buffer.Length * 8)
                return false;

            for (int i = 0; i < bitCount; i++)
            {
                if (TryReadBit(out bool bit))
                {
                    if (_bitOrder == BitOrder.MsbFirst)
                        value = (value << 1) | (bit ? 1UL : 0UL);
                    else
                        value |= (bit ? 1UL : 0UL) << i;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Very similar to the <see cref="BitWriter.TryFlushByte"/>. This function tries to skip unused bits of current byte in the buffer to align position to the next byte.
        /// </summary>
        public void AlignToByte()
        {
            int remainder = _bitPosition & 7;

            if (remainder > 0)
                _bitPosition += 8 - remainder;
        }
    }
}