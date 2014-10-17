using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace deltaq
{
    internal static class Extensions
    {
        public static ArraySegment<T> Slice<T>(this T[] buf, int offset, int count = -1)
        {
            return new ArraySegment<T>(buf, offset, count < 0 ? buf.Length - offset : count);
        }

        public static ArraySegment<T> Slice<T>(this IList<T> list, int offset, int count = -1)
        {
            var arraySegment = (ArraySegment<T>)list;
            var baseArray = arraySegment.Array;
            //substitute everything remaining after the offset, if count is subzero
            return new ArraySegment<T>(arraySegment.Array, offset, count < 0 ? baseArray.Length - offset : count);
        }

        public static void WriteLongAt(this byte[] pb, int offset, long y)
        {
            pb.Slice(offset, sizeof(long)).WriteLong(y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLong(this IList<byte> b, long y)
        {
            if (y < 0)
            {
                y = -y;

                b[0] = (byte)y;
                b[1] = (byte)(y >>= 8);
                b[2] = (byte)(y >>= 8);
                b[3] = (byte)(y >>= 8);
                b[4] = (byte)(y >>= 8);
                b[5] = (byte)(y >>= 8);
                b[6] = (byte)(y >>= 8);
                b[7] = (byte)((y >> 8) | 0x80);
            }
            else
            {
                b[0] = (byte)y;
                b[1] = (byte)(y >>= 8);
                b[2] = (byte)(y >>= 8);
                b[3] = (byte)(y >>= 8);
                b[4] = (byte)(y >>= 8);
                b[5] = (byte)(y >>= 8);
                b[6] = (byte)(y >>= 8);
                b[7] = (byte)(y >> 8);
            }
        }

        public static long ReadLong(this Stream stream)
        {
            var buf = new byte[sizeof(long)];
            if (stream.Read(buf, 0, sizeof(long)) != sizeof(long))
                throw new InvalidOperationException("Could not read long from stream");

            return buf.ReadLong();
        }

        public static long ReadLongAt(this byte[] buf, int offset)
        {
            return buf.Slice(offset, sizeof(long)).ReadLong();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadLong(this IList<byte> b)
        {
            long y = b[7] & 0x7F;
            y <<= 8; y += b[6];
            y <<= 8; y += b[5];
            y <<= 8; y += b[4];
            y <<= 8; y += b[3];
            y <<= 8; y += b[2];
            y <<= 8; y += b[1];
            y <<= 8; y += b[0];

            return (b[7] & 0x80) != 0 ? -y : y;
        }
    }
}
