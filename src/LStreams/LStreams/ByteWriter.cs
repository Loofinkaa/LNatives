using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace LStreams
{
    /// <summary>
    /// High-performance byte stream writer.
    /// </summary>
    public sealed class ByteWriter : IDisposable
    {
        /// <summary>Rented byte array for this writer.</summary>
        private byte[]? _rentedBuffer;

        /// <summary>The underlying stream where the buffer data is dropping when writer is flushing.</summary>
        private Stream? _underlyingStream;

        private int _position;
        private int _capacity;

        /// <summary>Leave underlying stream open after end of write?</summary>
        private readonly bool _leaveOpen;

        //State flags
        private bool _isDisposed;
        private readonly bool _ownsBuffer;

        /// <summary>Default capacity for the buffer.</summary>
        private const int DefaultCapacity = 256;

        /// <summary>Current writer position.</summary>
        public int Position => _position;

        /// <summary>The capacity of the buffer.</summary>
        public int Capacity => _capacity;

        /// <summary>The size of remain capacity in the buffer.</summary>
        public int RemainingCapacity => _capacity - _position;

        /// <summary>This field represents the buffer as a <see cref="ReadOnlySpan{byte}"/> with only written data.</summary>
        public ReadOnlySpan<byte> WrittenSpan => (_rentedBuffer ?? Array.Empty<byte>()).AsSpan(0, _position);

        /// <summary>This field represents the buffer as a <see cref="ArraySegment{byte}"/> with only written data.</summary>
        public ArraySegment<byte> WrittenSegment => new(_rentedBuffer ?? Array.Empty<byte>(), 0, _position);

        /// <summary>
        /// Initializes this bytewriter.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the buffer.</param>
        public ByteWriter(int initialCapacity = DefaultCapacity)
        {
            _rentedBuffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
            _capacity = initialCapacity;
            _position = 0;
            _ownsBuffer = true;
            _leaveOpen = false;
        }

        /// <summary>
        /// Initializes this bytewriter.
        /// </summary>
        /// <param name="stream">The underlying stream for write operations.</param>
        /// <param name="leaveOpen">Leave the underlying after end of write?</param>
        /// <exception cref="ArgumentNullException">Throws when the underlying stream is null.</exception>
        public ByteWriter(Stream stream, bool leaveOpen = false)
        {
            _underlyingStream = stream ?? throw new ArgumentNullException(nameof(stream));
            _leaveOpen = leaveOpen;
            _rentedBuffer = ArrayPool<byte>.Shared.Rent(DefaultCapacity);
            _capacity = DefaultCapacity;
            _position = 0;
            _ownsBuffer = true;
        }

        /// <summary>
        /// Initializes this bytewriter.
        /// </summary>
        /// <param name="buffer">Refers to the buffer into which the data will be written.</param>
        /// <param name="startIndex">The index from where we start to write.</param>
        /// <exception cref="ArgumentNullException">Throws when the refered buffer is null.</exception>
        public ByteWriter(byte[] buffer, int startIndex = 0)
        {
            _rentedBuffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _capacity = buffer.Length;
            _position = startIndex;
            _ownsBuffer = false;
            _leaveOpen = false;
        }

        /// <summary>
        /// This function tries to ensure capacity for the buffer. Only call if the buffer is owns by this writer (not refered).
        /// </summary>
        /// <param name="additionalBytes">Amount that will be added to capacity.</param>
        /// <exception cref="InvalidOperationException">Throws if the buffer is not owned.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureCapacity(int additionalBytes)
        {
            int required = _position + additionalBytes;

            if (required <= _capacity)
                return;

            if (!_ownsBuffer)
                throw new InvalidOperationException("Buffer is not owned and cannot be resized.");

            int newCapacity = Math.Max(required, _capacity * 2);
            byte[] newBuffer = ArrayPool<byte>.Shared.Rent(newCapacity);

            if (_position > 0)
                _rentedBuffer.AsSpan(0, _position).CopyTo(newBuffer);

            if (_rentedBuffer != null)
                ArrayPool<byte>.Shared.Return(_rentedBuffer);

            _rentedBuffer = newBuffer;
            _capacity = newCapacity;
        }

        /// <summary>
        /// Reserves space and return a <see cref="Span{byte}"/> for direct recording.
        /// </summary>
        public Span<byte> Reserve(int length)
        {
            EnsureCapacity(length);
            var span = _rentedBuffer.AsSpan(_position, length);
            _position += length;
            return span;
        }

        /// <summary>
        /// Reserves space for a structure and return its reference.
        /// </summary>
        public ref T Reserve<T>() where T : unmanaged
        {
            int size = Unsafe.SizeOf<T>();
            EnsureCapacity(size);

            ref T result = ref Unsafe.As<byte, T>(ref _rentedBuffer![_position]);
            _position += size;
            return ref result;
        }

        #region Primitives

        /// <summary>
        /// Writes a <see cref="byte"/> to the buffer.
        /// </summary>
        /// <param name="value">A value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByte(byte value)
        {
            EnsureCapacity(1);
            _rentedBuffer![_position++] = value;
        }

        /// <summary>
        /// Writes a <see cref="bool"/> to the buffer.
        /// </summary>
        /// <param name="value">A value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBool(bool value) => WriteByte(value ? (byte)1 : (byte)0);

        /// <summary>
        /// Writes a <see cref="ReadOnlySpan{byte}"/> to the buffer.
        /// </summary>
        /// <param name="value">A value.</param>
        public void WriteBytes(ReadOnlySpan<byte> value)
        {
            EnsureCapacity(value.Length);
            value.CopyTo(_rentedBuffer.AsSpan(_position));
            _position += value.Length;
        }

        /// <summary>
        /// Writes signed integer with the size of 16 bits to the buffer.
        /// </summary>
        /// <param name="value">A value.</param>
        /// <param name="littleEndian">Write value as little endian?</param>
        public void WriteInt16(short value, bool littleEndian = true)
        {
            EnsureCapacity(2);
            if (littleEndian)
                BinaryPrimitives.WriteInt16LittleEndian(_rentedBuffer.AsSpan(_position), value);
            else
                BinaryPrimitives.WriteInt16BigEndian(_rentedBuffer.AsSpan(_position), value);
            _position += 2;
        }

        /// <summary>
        /// Writes signed integer with the size of 32 bits to the buffer.
        /// </summary>
        /// <param name="value">A value.</param>
        /// <param name="littleEndian">Write value as little endian?</param>
        public void WriteInt32(int value, bool littleEndian = true)
        {
            EnsureCapacity(4);
            if (littleEndian)
                BinaryPrimitives.WriteInt32LittleEndian(_rentedBuffer.AsSpan(_position), value);
            else
                BinaryPrimitives.WriteInt32BigEndian(_rentedBuffer.AsSpan(_position), value);
            _position += 4;
        }

        /// <summary>
        /// Writes signed integer with the size of 64 bits to the buffer.
        /// </summary>
        /// <param name="value">A value.</param>
        /// <param name="littleEndian">Write value as little endian?</param>
        public void WriteInt64(long value, bool littleEndian = true)
        {
            EnsureCapacity(8);
            if (littleEndian)
                BinaryPrimitives.WriteInt64LittleEndian(_rentedBuffer.AsSpan(_position), value);
            else
                BinaryPrimitives.WriteInt64BigEndian(_rentedBuffer.AsSpan(_position), value);
            _position += 8;
        }

        /// <summary>
        /// Writes a string with a 4-byte length prefix.
        /// </summary>
        /// <param name="value">A string to write.</param>
        /// <param name="encoding">Leave this parameter as null if you prefer to use UTF8 encoding.</param>
        /// <exception cref="ArgumentException">Throws if encoded bytes of a strng is over than 32 bits size.</exception>
        public void WriteStringWithInt32Prefix(string value, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;

            if (string.IsNullOrEmpty(value))
            {
                WriteInt32(0);
                return;
            }

            byte[] bytes = encoding.GetBytes(value);

            if (bytes.Length > int.MaxValue)
                throw new ArgumentException($"String too long: {bytes.Length} bytes, maximum is {int.MaxValue}");

            WriteInt32(bytes.Length);
            WriteBytes(bytes);
        }

        /// <summary>
        /// Writes a string with a 2-byte length prefix.
        /// </summary>
        /// <param name="value">A string to write.</param>
        /// <param name="encoding">Leave this parameter as null if you prefer to use UTF8 encoding.</param>
        /// <exception cref="ArgumentException">Throws if encoded bytes of a strng is over than 16 bits size.</exception>
        public void WriteStringWithInt16Prefix(string value, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;

            if (string.IsNullOrEmpty(value))
            {
                WriteInt16(0);
                return;
            }

            byte[] bytes = encoding.GetBytes(value);

            if (bytes.Length > ushort.MaxValue)
                throw new ArgumentException($"String too long: {bytes.Length} bytes, maximum is {ushort.MaxValue}");

            WriteInt16((short)bytes.Length);
            WriteBytes(bytes);
        }

        #endregion

        /// <summary>
        /// Flushes the buffer if the underlying stream is not null.
        /// </summary>
        public void Flush()
        {
            if (_underlyingStream != null && _position > 0)
            {
                _underlyingStream.Write(_rentedBuffer, 0, _position);
                _underlyingStream.Flush();
                _position = 0;
            }
        }

        /// <summary>
        /// Asyncronly flush the buffer if the underlying stream is not null.
        /// </summary>
        /// <param name="cancellationToken">Flushing can be stopped over this cancellation token.</param>
        public async ValueTask FlushAsync(CancellationToken cancellationToken)
        {
            if (_underlyingStream != null && _position > 0)
            {
                await _underlyingStream.WriteAsync(_rentedBuffer, 0, _position, cancellationToken).ConfigureAwait(false);
                await _underlyingStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                _position = 0;
            }
        }

        /// <summary>
        /// Disposes this writer.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            Flush();

            if (_ownsBuffer && _rentedBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(_rentedBuffer);
                _rentedBuffer = null;
            }

            if (!_leaveOpen)
                _underlyingStream?.Dispose();

            _isDisposed = true;
        }

        /// <summary>
        /// Converts data from the buffer to the bytes array.
        /// </summary>
        /// <returns>Bytes array.</returns>
        public byte[] ToArray()
        {
            if (_position == 0)
                return Array.Empty<byte>();

            var result = new byte[_position];
            Array.Copy(_rentedBuffer!, result, _position);

            return result;
        }

        /// <summary>
        /// Gets the buffer over cast to <see cref="ArraySegment{T}"/>.
        /// </summary>
        /// <returns>A new <see cref="ArraySegment{T}"/> with data from the buffer.</returns>
        public ArraySegment<byte> GetBuffer()
        {
            return new ArraySegment<byte>(_rentedBuffer!, 0, _position);
        }

        /// <summary>
        /// Resets current writer position.
        /// </summary>
        public void Reset()
        {
            _position = 0;
        }

        /// <summary>
        /// Resets current writer position and clears the buffer.
        /// </summary>
        public void Clear()
        {
            _position = 0;

            if (_rentedBuffer != null)
                Array.Clear(_rentedBuffer, 0, _capacity);
        }
    }
}