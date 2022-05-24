﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DiscUtils.Streams.Compatibility
{
    public abstract class CompatibilityStream : Stream
    {
        public abstract override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
        public abstract override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        public abstract override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default);
        public abstract override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default);
        public abstract override int Read(Span<byte> buffer);
        public abstract override void Write(ReadOnlySpan<byte> buffer);
#else
        public abstract ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default);
        public abstract ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default);
        public abstract int Read(Span<byte> buffer);
        public abstract void Write(ReadOnlySpan<byte> buffer);
#endif
    }

    public abstract class ReadOnlyCompatibilityStream : CompatibilityStream
    {
        public override sealed bool CanWrite => false;
        public override sealed void Write(byte[] buffer, int offset, int count) => throw new InvalidOperationException("Attempt to write to read-only stream");
        public override sealed void Write(ReadOnlySpan<byte> buffer) => throw new InvalidOperationException("Attempt to write to read-only stream");
        public override sealed Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new InvalidOperationException("Attempt to write to read-only stream");
        public override sealed ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Attempt to write to read-only stream");
        public override sealed void WriteByte(byte value) => throw new InvalidOperationException("Attempt to write to read-only stream");
        public override sealed void Flush() { }
        public override sealed Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public override sealed void SetLength(long value) => throw new InvalidOperationException("Attempt to change length of read-only stream");
    }

    public static class CompatExtensions
    {
        public static int ReadFrom<T>(this T serializable, byte[] bytes, int offset) where T : class, IByteArraySerializable =>
            serializable.ReadFrom(bytes.AsSpan(offset));

        public static void WriteTo<T>(this T serializable, byte[] bytes, int offset) where T : class, IByteArraySerializable =>
            serializable.WriteTo(bytes.AsSpan(offset));

        public static int ReadFrom<T>(ref this T serializable, byte[] bytes, int offset) where T : struct, IByteArraySerializable =>
            serializable.ReadFrom(bytes.AsSpan(offset));

        public static void WriteTo<T>(ref this T serializable, byte[] bytes, int offset) where T : struct, IByteArraySerializable =>
            serializable.WriteTo(bytes.AsSpan(offset));

#if !NET6_0_OR_GREATER
        public static ReadOnlyMemory<char> TrimStart(this ReadOnlyMemory<char> str)
            => str.Slice(str.Span.TrimStart().Length - str.Length);

        public static ReadOnlyMemory<char> TrimEnd(this ReadOnlyMemory<char> str)
            => str.Slice(0, str.Span.TrimEnd().Length);

        public static ReadOnlyMemory<char> Trim(this ReadOnlyMemory<char> str)
            => str.TrimStart().TrimEnd();

        public static ReadOnlyMemory<char> TrimStart(this ReadOnlyMemory<char> str, char chr)
            => str.Slice(str.Span.TrimStart(chr).Length - str.Length);

        public static ReadOnlyMemory<char> TrimEnd(this ReadOnlyMemory<char> str, char chr)
            => str.Slice(0, str.Span.TrimEnd(chr).Length);

        public static ReadOnlyMemory<char> Trim(this ReadOnlyMemory<char> str, char chr)
            => str.TrimStart().TrimEnd(chr);

        public static ReadOnlyMemory<char> TrimStart(this ReadOnlyMemory<char> str, ReadOnlySpan<char> chr)
            => str.Slice(str.Span.TrimStart(chr).Length - str.Length);

        public static ReadOnlyMemory<char> TrimEnd(this ReadOnlyMemory<char> str, ReadOnlySpan<char> chr)
            => str.Slice(0, str.Span.TrimEnd(chr).Length);

        public static ReadOnlyMemory<char> Trim(this ReadOnlyMemory<char> str, ReadOnlySpan<char> chr)
            => str.TrimStart().TrimEnd(chr);

#endif

#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP
        public static string[] Split(this string str, char separator, int count, StringSplitOptions options = StringSplitOptions.None) =>
            str.Split(new[] { separator }, count, options);

        public static string[] Split(this string str, char separator, StringSplitOptions options = StringSplitOptions.None) =>
            str.Split(new[] { separator }, options);

        public static int GetCharCount(this Decoder decoder, ReadOnlySpan<byte> bytes, bool flush)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(bytes.Length);
            try
            {
                bytes.CopyTo(buffer);
                return decoder.GetCharCount(buffer, 0, bytes.Length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public static int GetChars(this Decoder decoder, ReadOnlySpan<byte> bytes, Span<char> chars, bool flush)
        {
            var bytesBuffer = ArrayPool<byte>.Shared.Rent(bytes.Length);
            try
            {
                var charsBuffer = ArrayPool<char>.Shared.Rent(chars.Length);
                try
                {
                    bytes.CopyTo(bytesBuffer);
                    var i = decoder.GetChars(bytesBuffer, 0, bytes.Length, charsBuffer, 0);
                    charsBuffer.AsSpan(0, i).CopyTo(chars);
                    return i;
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(charsBuffer);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytesBuffer);
            }
        }

        public static void Convert(this Encoder decoder, ReadOnlySpan<char> chars, Span<byte> bytes, bool flush, out int charsUsed, out int bytesUsed, out bool completed)
        {
            var bytesBuffer = ArrayPool<byte>.Shared.Rent(bytes.Length);
            try
            {
                var charsBuffer = ArrayPool<char>.Shared.Rent(chars.Length);
                try
                {
                    chars.CopyTo(charsBuffer);
                    decoder.Convert(charsBuffer, 0, chars.Length, bytesBuffer, 0, bytes.Length, flush, out charsUsed, out bytesUsed, out completed);
                    bytesBuffer.AsSpan(0, bytesUsed).CopyTo(bytes);
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(charsBuffer);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytesBuffer);
            }
        }

        public static string GetString(this Encoding encoding, ReadOnlySpan<byte> bytes)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(bytes.Length);
            try
            {
                bytes.CopyTo(buffer);
                return encoding.GetString(buffer);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public static int GetBytes(this Encoding encoding, ReadOnlySpan<char> chars, Span<byte> bytes)
        {
            var str = ArrayPool<char>.Shared.Rent(chars.Length);
            try
            {
                chars.CopyTo(str);
                var buffer = ArrayPool<byte>.Shared.Rent(encoding.GetByteCount(str, 0, chars.Length));
                try
                {
                    var length = encoding.GetBytes(str, 0, chars.Length, buffer, 0);
                    buffer.AsSpan().CopyTo(bytes);
                    return length;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(str);
            }
        }

        public static void NextBytes(this Random random, Span<byte> buffer)
        {
            var bytes = new byte[buffer.Length];
            random.NextBytes(bytes);
            bytes.AsSpan().CopyTo(buffer);
        }

        public static int Read(this Stream stream, Span<byte> buffer)
        {
            if (stream is CompatibilityStream compatibilityStream)
            {
                return compatibilityStream.Read(buffer);
            }

            return ReadUsingArray(stream, buffer);
        }

        public static int ReadUsingArray(Stream stream, Span<byte> buffer)
        {
            var bytes = ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                var numRead = stream.Read(bytes, 0, buffer.Length);
                bytes.AsSpan(0, numRead).CopyTo(buffer);
                return numRead;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }

        public static ValueTask<int> ReadAsync(this Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            if (stream is CompatibilityStream compatibilityStream)
            {
                return compatibilityStream.ReadAsync(buffer, cancellationToken);
            }

            return ReadUsingArrayAsync(stream, buffer, cancellationToken);
        }

        public static async ValueTask<int> ReadUsingArrayAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            if (MemoryMarshal.TryGetArray<byte>(buffer, out var arraySegment))
            {
                return await stream.ReadAsync(arraySegment.Array, arraySegment.Offset, arraySegment.Count, cancellationToken).ConfigureAwait(false);
            }

            var bytes = ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                var numRead = await stream.ReadAsync(bytes, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                bytes.AsSpan(0, numRead).CopyTo(buffer.Span);
                return numRead;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }

        public static void Write(this Stream stream, ReadOnlySpan<byte> buffer)
        {
            if (stream is CompatibilityStream compatibilityStream)
            {
                compatibilityStream.Write(buffer);
                return;
            }

            var bytes = ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                buffer.CopyTo(bytes);
                stream.Write(bytes, 0, buffer.Length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }

        public static async ValueTask WriteAsync(this Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            if (stream is CompatibilityStream compatibilityStream)
            {
                await compatibilityStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (MemoryMarshal.TryGetArray<byte>(buffer, out var arraySegment))
            {
                await stream.WriteAsync(arraySegment.Array, arraySegment.Offset, arraySegment.Count, cancellationToken).ConfigureAwait(false);
                return;
            }

            var bytes = ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                buffer.CopyTo(bytes);
                await stream.WriteAsync(bytes, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }

        public static void AppendData(this IncrementalHash hash, ReadOnlySpan<byte> data)
        {
            var bytes = ArrayPool<byte>.Shared.Rent(data.Length);
            try
            {
                data.CopyTo(bytes);
                hash.AppendData(bytes, 0, data.Length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }
#endif
    }

}