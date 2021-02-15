﻿/*
 * BsDiff.cs for deltaq
 * Copyright (c) 2014 J. Zebedee
 * 
 * BsDiff.net is Copyright 2010 Logos Bible Software
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
using System.IO;
using bz2core;
using deltaq.SuffixSort;

namespace deltaq.BsDiff
{
    public static class BsDiff
    {
        internal const int HeaderSize = 32;
        internal const long Signature = 0x3034464649445342; //"BSDIFF40"

        private static readonly ISuffixSort DefaultSuffixSort = new SAIS();

        internal static Stream GetEncodingStream(Stream stream, bool output)
        {
            if (output)
                return new BZip2OutputStream(stream) { IsStreamOwner = false };
            return new BZip2InputStream(stream);
        }

        /// <summary>
        /// Creates a BSDIFF-format patch from two byte arrays
        /// </summary>
        /// <param name="oldData">Byte array of the original (older) data</param>
        /// <param name="newData">Byte array of the changed (newer) data</param>
        /// <param name="output">Seekable, writable stream where the patch will be written</param>
        /// <param name="suffixSort">Suffix sort implementation to use for comparison, or null to use a default sorter</param>
        public static void Create(ReadOnlySpan<byte> oldData, ReadOnlySpan<byte> newData, Stream output, ISuffixSort suffixSort = null)
        {
            CreateInternal(oldData, newData, output, suffixSort ?? DefaultSuffixSort);
        }

        private static void CreateInternal(ReadOnlySpan<byte> oldData, ReadOnlySpan<byte> newData, Stream output, ISuffixSort suffixSort)
        {
            // check arguments
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            if (suffixSort == null)
                throw new ArgumentNullException(nameof(suffixSort));
            if (!output.CanSeek)
                throw new ArgumentException("Output stream must be seekable.", nameof(output));
            if (!output.CanWrite)
                throw new ArgumentException("Output stream must be writable.", nameof(output));

            /* Header is
                0	8	 "BSDIFF40"
                8	8	length of bzip2ed ctrl block
                16	8	length of bzip2ed diff block
                24	8	length of new file
               File is
                0	32	Header
                32	??	Bzip2ed ctrl block
                ??	??	Bzip2ed diff block
                ??	??	Bzip2ed extra block */
            Span<byte> header = stackalloc byte[HeaderSize];
            header.WriteLong(Signature);
            header.WriteLongAt(24, newData.Length);

            var startPosition = output.Position;
            output.Write(header);

            var I = suffixSort.Sort(oldData);

            using (var msControl = new MemoryStream())
            using (var msDiff = new MemoryStream())
            using (var msExtra = new MemoryStream())
            {
                using (var ctrlStream = GetEncodingStream(msControl, true))
                using (var diffStream = GetEncodingStream(msDiff, true))
                using (var extraStream = GetEncodingStream(msExtra, true))
                {
                    var scan = 0;
                    var pos = 0;
                    var len = 0;
                    var lastscan = 0;
                    var lastpos = 0;
                    var lastoffset = 0;
                    
                    //backing for ctrl writes
                    Span<byte> ctrlWriteBuf = stackalloc byte[24];

                    // compute the differences, writing ctrl as we go
                    while (scan < newData.Length)
                    {
                        var oldscore = 0;

                        for (var scsc = scan += len; scan < newData.Length; scan++)
                        {
                            len = Search(I, oldData, newData.Slice(scan), 0, oldData.Length, out pos);

                            for (; scsc < scan + len; scsc++)
                            {
                                if ((scsc + lastoffset < oldData.Length) && (oldData[scsc + lastoffset] == newData[scsc]))
                                    oldscore++;
                            }

                            if ((len == oldscore && len != 0) || (len > oldscore + 8))
                                break;

                            if ((scan + lastoffset < oldData.Length) && (oldData[scan + lastoffset] == newData[scan]))
                                oldscore--;
                        }

                        if (len != oldscore || scan == newData.Length)
                        {
                            var s = 0;
                            var sf = 0;
                            var lenf = 0;
                            for (var i = 0; (lastscan + i < scan) && (lastpos + i < oldData.Length); )
                            {
                                if (oldData[lastpos + i] == newData[lastscan + i])
                                    s++;
                                i++;
                                if (s * 2 - i > sf * 2 - lenf)
                                {
                                    sf = s;
                                    lenf = i;
                                }
                            }

                            var lenb = 0;
                            if (scan < newData.Length)
                            {
                                s = 0;
                                var sb = 0;
                                for (var i = 1; (scan >= lastscan + i) && (pos >= i); i++)
                                {
                                    if (oldData[pos - i] == newData[scan - i])
                                        s++;
                                    if (s * 2 - i > sb * 2 - lenb)
                                    {
                                        sb = s;
                                        lenb = i;
                                    }
                                }
                            }

                            if (lastscan + lenf > scan - lenb)
                            {
                                var overlap = (lastscan + lenf) - (scan - lenb);
                                s = 0;
                                var ss = 0;
                                var lens = 0;
                                for (var i = 0; i < overlap; i++)
                                {
                                    if (newData[lastscan + lenf - overlap + i] == oldData[lastpos + lenf - overlap + i])
                                        s++;
                                    if (newData[scan - lenb + i] == oldData[pos - lenb + i])
                                        s--;
                                    if (s > ss)
                                    {
                                        ss = s;
                                        lens = i + 1;
                                    }
                                }

                                lenf += lens - overlap;
                                lenb -= lens;
                            }

                            //write diff string
                            for (var i = 0; i < lenf; i++)
                                diffStream.WriteByte((byte)(newData[lastscan + i] - oldData[lastpos + i]));

                            //write extra string
                            var extraLength = (scan - lenb) - (lastscan + lenf);
                            if (extraLength > 0)
                                extraStream.Write(newData.Slice(lastscan + lenf, extraLength));

                            //write ctrl block
                            ctrlWriteBuf.WriteLong(lenf);
                            ctrlWriteBuf.Slice(8).WriteLong(extraLength);
                            ctrlWriteBuf.Slice(16).WriteLong((pos - lenb) - (lastpos + lenf));
                            
                            ctrlStream.Write(ctrlWriteBuf);

                            lastscan = scan - lenb;
                            lastpos = pos - lenb;
                            lastoffset = pos - scan;
                        }
                    }
                }

                //write compressed ctrl data
                msControl.Seek(0, SeekOrigin.Begin);
                msControl.CopyTo(output);

                // compute size of compressed ctrl data
                header.WriteLongAt(8, msControl.Length);

                // write compressed diff data
                msDiff.Seek(0, SeekOrigin.Begin);
                msDiff.CopyTo(output);

                // compute size of compressed diff data
                header.WriteLongAt(16, msDiff.Length);

                // write compressed extra data
                msExtra.Seek(0, SeekOrigin.Begin);
                msExtra.CopyTo(output);
            }

            // seek to the beginning, write the header, then seek back to end
            var endPosition = output.Position;
            output.Position = startPosition;
            output.Write(header);
            output.Position = endPosition;
        }

        private static int CompareBytes(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
        {
            var diff = 0;
            for (var i = 0; i < left.Length && i < right.Length; i++)
            {
                diff = left[i] - right[i];
                if (diff != 0)
                    break;
            }
            return diff;
        }

        private static int MatchLength(ReadOnlySpan<byte> oldData, ReadOnlySpan<byte> newData)
        {
            int i;
            for (i = 0; i < oldData.Length && i < newData.Length; i++)
            {
                if (oldData[i] != newData[i])
                    break;
            }

            return i;
        }

        private static int Search(ReadOnlySpan<int> I, ReadOnlySpan<byte> oldData, ReadOnlySpan<byte> newData, int start, int end, out int pos)
        {
            while (true)
            {
                if (end - start < 2)
                {
                    var startLength = MatchLength(oldData.Slice(I[start]), newData);
                    var endLength = MatchLength(oldData.Slice(I[end]), newData);

                    if (startLength > endLength)
                    {
                        pos = I[start];
                        return startLength;
                    }

                    pos = I[end];
                    return endLength;
                }

                var midPoint = start + (end - start) / 2;
                if (CompareBytes(oldData.Slice(I[midPoint]), newData) < 0)
                {
                    start = midPoint;
                    continue;
                }

                end = midPoint;
            }
        }
    }
}
