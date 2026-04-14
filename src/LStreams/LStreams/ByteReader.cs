using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

#nullable enable

namespace LStreams
{
    /// <summary>
    /// High-performance byte stream reader.
    /// </summary>
    public sealed class ByteReader : IDisposable
    {
        /// <summary>Rented byte array for this reader. Can be null.</summary>
        private byte[]? _buffer;

        /// <summary>The underlying stream from where this reader is reading. Can be null.</summary>
        private Stream? _underlyingStream;

        private int _position;
        private readonly int _length;

        /// <summary>Leave underlying stream open after end of read?</summary>
        private readonly bool _leaveOpen;

        //State flags
        private readonly bool _ownsBuffer;
        private bool _isDisposed;

        /// <summary>Current reader position.</summary>
        public int Position => _position;

        /// <summary>The length of the buffer.</summary>
        public int Length => _length;

        /// <summary>The size of remain data in the buffer.</summary>
        public int Remaining => _length - _position;

        /// <summary>This field represents the buffer as a <see cref="ReadOnlySpan{byte}"/> with only remain data.</summary>
        public ReadOnlySpan<byte> RemainingSpan => _buffer.AsSpan(_position, Remaining);

        /// <summary>
        /// Initializes this bytereader.
        /// </summary>
        /// <param name="buffer">A given buffer to read.</param>
        /// <param name="startIndex">The index from where we start to read from the buffer.</param>
        /// <param name="length">The length of a given buffer.</param>
        /// <exception cref="ArgumentNullException">Throws if a given buffer is null.</exception>
        public ByteReader(byte[] buffer, int startIndex = 0, int? length = null)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _length = length ?? buffer.Length;
            _position = startIndex;
            _ownsBuffer = false;
        }

        /// <summary>
        /// Initializes this bytereader.
        /// </summary>
        /// <param name="span">A <see cref="Span{byte}"/> from where this bytereader will read data.</param>
        public ByteReader(ReadOnlySpan<byte> span)
        {
            _buffer = ArrayPool<byte>.Shared.Rent(span.Length);
            span.CopyTo(_buffer);
            _length = span.Length;
            _position = 0;
            _ownsBuffer = true;
        }

        /// <summary>
        /// Initializes this bytereader.
        /// </summary>
        /// <param name="stream">A given stream to read.</param>
        /// <param name="leaveOpen">Leave the stream open after end of read?</param>
        /// <exception cref="ArgumentNullException">Throws if a given stream is null.</exception>
        public ByteReader(Stream stream, bool leaveOpen = false)
        {
            _underlyingStream = stream ?? throw new ArgumentNullException(nameof(stream));
            _leaveOpen = leaveOpen;
            _buffer = ArrayPool<byte>.Shared.Rent((int)Math.Min(stream.Length, int.MaxValue));
            _length = stream.Read(_buffer, 0, _buffer.Length);
            _position = 0;
            _ownsBuffer = true;
        }

        #region Primitives

        /// <summary>
        /// Reads a <see cref="byte"/> from the buffer.
        /// </summary>
        /// <returns>Readed byte.</returns>
        /// <exception cref="EndOfStreamException">Throws if the buffer remaining no bytes.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            if (_position >= _length)
                throw new EndOfStreamException();

            return _buffer![_position++];
        }

        /// <summary>
        /// Tries to read a <see cref="byte"/> from the buffer.
        /// </summary>
        /// <param name="value">Readed byte.</param>
        /// <returns>If a byte readed successfully it will return true.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadByte(out byte value)
        {
            if (_position >= _length)
            {
                value = 0;
                return false;
            }

            value = _buffer![_position++];
            return true;
        }

        /// <summary>
        /// Reads a <see cref="bool"/> from the buffer.
        /// </summary>
        /// <returns>Readed byte representated as <see cref="bool"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBool() => ReadByte() == (byte)1 ? true : false;

        /// <summary>
        /// Tries to read a <see cref="bool"/> from the buffer.
        /// </summary>
        /// <returns>If a bool readed successfully it will return true.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadBool()
        {
            TryReadByte(out byte allocatedByte);
            return allocatedByte == (byte)1 ? true : false;
        }

        /// <summary>
        /// Reads bytes from the buffer until the destination span will be filled.
        /// </summary>
        /// <param name="destination">The destination span.</param>
        /// <returns>A count of bytes that was readed.</returns>
        public int ReadBytes(Span<byte> destination)
        {
            int bytesToRead = Math.Min(destination.Length, Remaining);

            _buffer.AsSpan(_position, bytesToRead).CopyTo(destination);
            _position += bytesToRead;

            return bytesToRead;
        }

        /// <summary>
        /// Reads <see cref="int"/> with the size of 16 bits from the buffer.
        /// </summary>
        /// <param name="littleEndian">Read value as a little-endian?</param>
        /// <returns>Readed value from the buffer.</returns>
        /// <exception cref="EndOfStreamException">Throws if the buffer remains less than 16 bits before end.</exception>
        public short ReadInt16(bool littleEndian = true)
        {
            if (Remaining < 2)
                throw new EndOfStreamException();

            var span = _buffer.AsSpan(_position, 2);
            _position += 2;

            return littleEndian
                ? BinaryPrimitives.ReadInt16LittleEndian(span)
                : BinaryPrimitives.ReadInt16BigEndian(span);
        }

        /// <summary>
        /// Reads <see cref="int"/> with the size of 32 bits from the buffer.
        /// </summary>
        /// <param name="littleEndian">Read value as a little-endian?</param>
        /// <returns>Readed value from the buffer.</returns>
        /// <exception cref="EndOfStreamException">Throws if the buffer remains less than 32 bits before end.</exception>
        public int ReadInt32(bool littleEndian = true)
        {
            if (Remaining < 4)
                throw new EndOfStreamException();

            var span = _buffer.AsSpan(_position, 4);
            _position += 4;

            return littleEndian
                ? BinaryPrimitives.ReadInt32LittleEndian(span)
                : BinaryPrimitives.ReadInt32BigEndian(span);
        }

        /// <summary>
        /// Reads <see cref="int"/> with the size of 64 bits from the buffer.
        /// </summary>
        /// <param name="littleEndian">Read value as a little-endian?</param>
        /// <returns>Readed value from the buffer.</returns>
        /// <exception cref="EndOfStreamException">Throws if the buffer remains less than 64 bits before end.</exception>
        public long ReadInt64(bool littleEndian = true)
        {
            if (Remaining < 8)
                throw new EndOfStreamException();

            var span = _buffer.AsSpan(_position, 8);
            _position += 8;

            return littleEndian
                ? BinaryPrimitives.ReadInt64LittleEndian(span)
                : BinaryPrimitives.ReadInt64BigEndian(span);
        }

        /// <summary>
        /// Reads string with a 4-byte length prefix.
        /// </summary>
        /// <param name="encoding">By default its setting to UTF8, but it can be changed to read with other encoding.</param>
        /// <returns>A string decoded from the bytes with prefix of 32 bits.</returns>
        /// <exception cref="InvalidDataException">Throws if length is less than zero or the buffer remaining size is less than length.</exception>
        public string ReadStringWithInt32Prefix(Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;

            int length = ReadInt32();

            if (length == 0)
                return string.Empty;

            if (length < 0 || length > Remaining)
                throw new InvalidDataException($"Invalid string length: {length}");

            string result = encoding.GetString(_buffer, _position, length);
            _position += length;

            return result;
        }

        /// <summary>
        /// Reads string with a 2-byte length prefix.
        /// </summary>
        /// <param name="encoding">By default its setting to UTF8, but it can be changed to read with other encoding.</param>
        /// <returns>A string decoded from the bytes with prefix of 16 bits.</returns>
        /// <exception cref="InvalidDataException">Throws if length is less than zero or the buffer remaining size is less than length.</exception>
        public string ReadStringWithInt16Prefix(Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;

            short length = ReadInt16();

            if (length == 0)
                return string.Empty;

            if (length < 0 || length > Remaining)
                throw new InvalidDataException($"Invalid string length: {length}");

            string result = encoding.GetString(_buffer, _position, length);
            _position += length;

            return result;
        }

        #endregion

        /// <summary>
        /// This function skips some bytes with some count.
        /// </summary>
        /// <param name="count">A bytes to skip.</param>
        /// <exception cref="ArgumentOutOfRangeException">Throws if count of bytes to skip is less than zero.</exception>
        public void Skip(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            _position = Math.Min(_position + count, _length);
        }

        /// <summary>
        /// Reads the buffer to end and returns remain data..
        /// </summary>
        /// <returns>Remaining data from the buffer.</returns>
        public ReadOnlySpan<byte> ReadToEnd()
        {
            var span = RemainingSpan;
            _position = _length;
            return span;
        }

        /// <summary>
        /// Disposes this reader closing the stream if its not null.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            if (_ownsBuffer && _buffer != null)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = null;
            }

            if (!_leaveOpen)
                _underlyingStream?.Dispose();

            _isDisposed = true;
        }
    }
}