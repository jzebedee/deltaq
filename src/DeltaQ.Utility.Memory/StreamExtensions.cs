using System;
using System.Buffers;
using System.IO;

namespace DeltaQ.Utility.Memory
{
    public static class StreamExtensions
    {
#if !(NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
        private static ArrayPool<byte> DefaultPool => ArrayPool<byte>.Shared;

        /// <summary>
        /// Reads a sequence of bytes from a given <see cref="Stream"/> instance.
        /// </summary>
        /// <param name="stream">The source <see cref="Stream"/> to read data from.</param>
        /// <param name="buffer">The target <see cref="Span{T}"/> to write data to.</param>
        /// <returns>The number of bytes that have been read.</returns>
        public static int Read(this Stream stream, Span<byte> buffer)
        {
            byte[] array = DefaultPool.Rent(buffer.Length);
            try
            {
                int bytesRead = stream.Read(array, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    array.AsSpan(0, bytesRead).CopyTo(buffer);
                }

                return bytesRead;
            }
            finally
            {
                DefaultPool.Return(array);
            }
        }

        /// <summary>
        /// Writes a sequence of bytes to a given <see cref="Stream"/> instance.
        /// </summary>
        /// <param name="stream">The destination <see cref="Stream"/> to write data to.</param>
        /// <param name="buffer">The source <see cref="Span{T}"/> to read data from.</param>
        public static void Write(this Stream stream, ReadOnlySpan<byte> buffer)
        {
            byte[] array = DefaultPool.Rent(buffer.Length);
            try
            {
                buffer.CopyTo(array);
                stream.Write(array, 0, buffer.Length);
            }
            finally
            {
                DefaultPool.Return(array);
            }
        }
#endif
    }
}
