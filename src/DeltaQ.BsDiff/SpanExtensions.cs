using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace DeltaQ.BsDiff
{
    internal static class SpanExtensions
    {
        public static void WritePackedLong(this Span<byte> span, long y)
        {
            if (y < 0)
            {
                y = -y;

                span[0] = (byte)y;
                span[1] = (byte)(y >>= 8);
                span[2] = (byte)(y >>= 8);
                span[3] = (byte)(y >>= 8);
                span[4] = (byte)(y >>= 8);
                span[5] = (byte)(y >>= 8);
                span[6] = (byte)(y >>= 8);
                span[7] = (byte)((y >> 8) | 0x80);
            }
            else
            {
                span[0] = (byte)y;
                span[1] = (byte)(y >>= 8);
                span[2] = (byte)(y >>= 8);
                span[3] = (byte)(y >>= 8);
                span[4] = (byte)(y >>= 8);
                span[5] = (byte)(y >>= 8);
                span[6] = (byte)(y >>= 8);
                span[7] = (byte)(y >> 8);
            }
        }

        public static long ReadPackedLong(this Span<byte> span)
        {
            long y = span[7] & 0x7F;
            y <<= 8; y += span[6];
            y <<= 8; y += span[5];
            y <<= 8; y += span[4];
            y <<= 8; y += span[3];
            y <<= 8; y += span[2];
            y <<= 8; y += span[1];
            y <<= 8; y += span[0];

            return (span[7] & 0x80) != 0 ? -y : y;
        }

        public static Span<T> SliceUpTo<T>(this Span<T> span, int max) => span[..Math.Min(span.Length, max)];
    }
}
