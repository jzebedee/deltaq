/*
 * Extensions.cs for DeltaQ
 * Copyright (c) 2014 J. Zebedee
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace DeltaQ.BsDiff
{
    internal static class Extensions
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

        public static Span<T> SliceUpTo<T>(this Span<T> span, int max) => span.Slice(0, Math.Min(span.Length, max));
    }
}
