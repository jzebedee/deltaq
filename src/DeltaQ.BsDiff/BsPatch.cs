/*
 * BsPatch.cs for DeltaQ
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

using Microsoft.Toolkit.HighPerformance.Buffers;
using Microsoft.Toolkit.HighPerformance.Extensions;
using System;
using System.IO;

namespace DeltaQ.BsDiff
{
    public static class BsPatch
    {
        /// <summary>
        /// Opens a BSDIFF-format patch at a specific position
        /// </summary>
        /// <param name="offset">Zero-based offset into the patch</param>
        /// <param name="length">Length of the Stream from offset, or 0 for the rest of the patch</param>
        /// <returns>Readable, seekable stream with specified offset and length</returns>
        public delegate Stream OpenPatchStream(long offset, long length);

        /// <summary>
        /// Applies a BSDIFF-format patch to an original and produces the updated version
        /// </summary>
        /// <param name="input">Byte array of the original (older) data</param>
        /// <param name="diff">Byte array of the BSDIFF-format patch data</param>
        /// <param name="output">Writable stream where the updated data will be written</param>
        public static void Apply(ReadOnlyMemory<byte> input, ReadOnlyMemory<byte> diff, Stream output)
        {
            var newSize = CreatePatchStreams(openPatchStream, out Stream controlStream, out Stream diffStream, out Stream extraStream);

            // prepare to read three parts of the patch in parallel
            ApplyInternal(newSize, input.AsStream(), controlStream, diffStream, extraStream, output);
            return;

            Stream openPatchStream(long offset, long length)
                => diff.Slice((int)offset, length > 0 ? (int)length : diff.Length - (int)offset).AsStream();
        }

        /// <summary>
        /// Applies a BSDIFF-format patch to an original and produces the updated version
        /// </summary>
        /// <param name="input">Readable, seekable stream of the original (older) data</param>
        /// <param name="openPatchStream"><see cref="OpenPatchStream"/></param>
        /// <param name="output">Writable stream where the updated data will be written</param>
        public static void Apply(Stream input, OpenPatchStream openPatchStream, Stream output)
        {
            var newSize = CreatePatchStreams(openPatchStream, out Stream controlStream, out Stream diffStream, out Stream extraStream);

            // prepare to read three parts of the patch in parallel
            ApplyInternal(newSize, input, controlStream, diffStream, extraStream, output);
        }

        private static long CreatePatchStreams(OpenPatchStream openPatchStream, out Stream ctrl, out Stream diff, out Stream extra)
        {
            // read header
            long controlLength, diffLength, newSize;
            using (var patchStream = openPatchStream(0, BsDiff.HeaderSize))
            {
                // check patch stream capabilities
                if (!patchStream.CanRead)
                    throw new ArgumentException("Patch stream must be readable", nameof(openPatchStream));
                if (!patchStream.CanSeek)
                    throw new ArgumentException("Patch stream must be seekable", nameof(openPatchStream));

                Span<byte> header = stackalloc byte[BsDiff.HeaderSize];
                patchStream.Read(header);

                // check for appropriate magic
                var signature = header.ReadPackedLong();
                if (signature != BsDiff.Signature)
                    throw new InvalidOperationException("Corrupt patch");

                // read lengths from header
                controlLength = header.Slice(sizeof(long)).ReadPackedLong();
                diffLength = header.Slice(sizeof(long) * 2).ReadPackedLong();
                newSize = header.Slice(sizeof(long) * 3).ReadPackedLong();

                if (controlLength < 0 || diffLength < 0 || newSize < 0)
                    throw new InvalidOperationException("Corrupt patch");
            }

            // prepare to read three parts of the patch in parallel
            Stream
                compressedControlStream = openPatchStream(BsDiff.HeaderSize, controlLength),
                compressedDiffStream = openPatchStream(BsDiff.HeaderSize + controlLength, diffLength),
                compressedExtraStream = openPatchStream(BsDiff.HeaderSize + controlLength + diffLength, 0);

            // decompress each part (to read it)
            ctrl = BsDiff.GetEncodingStream(compressedControlStream, false);
            diff = BsDiff.GetEncodingStream(compressedDiffStream, false);
            extra = BsDiff.GetEncodingStream(compressedExtraStream, false);

            return newSize;
        }

        private static void ApplyInternal(long newSize, Stream input, Stream ctrl, Stream diff, Stream extra, Stream output, int bufferSize = 0x1000)
        {
            if (!input.CanRead)
                throw new ArgumentException("Input stream must be readable", nameof(input));
            if (!input.CanSeek)
                throw new ArgumentException("Input stream must be seekable", nameof(input));
            if (!output.CanWrite)
                throw new ArgumentException("Output stream must be writable", nameof(output));

            using (ctrl)
            using (diff)
            using (extra)
            {
                using var diffBufferOwner = SpanOwner<byte>.Allocate(bufferSize);
                using var inputBufferOwner = SpanOwner<byte>.Allocate(bufferSize);

                Span<byte> ctrlBuffer = stackalloc byte[sizeof(long) * 3];

                var diffBuffer = diffBufferOwner.Span;
                var inputBuffer = inputBufferOwner.Span;
                while (output.Position < newSize)
                {
                    //read control data:
                    // set of triples (x,y,z) meaning
                    ctrl.Read(ctrlBuffer);

                    // add x bytes from oldfile to x bytes from the diff block;
                    var addSize = ctrlBuffer.ReadPackedLong();
                    // copy y bytes from the extra block;
                    var copySize = ctrlBuffer.Slice(sizeof(long)).ReadPackedLong();
                    // seek forwards in oldfile by z bytes;
                    var seekAmount = ctrlBuffer.Slice(sizeof(long) * 2).ReadPackedLong();

                    // sanity-check
                    if (output.Position + addSize > newSize)
                        throw new InvalidOperationException("Corrupt patch");

                    // read diff string in chunks

                    while (addSize > 0)
                    {
                        var diffBytesRead = diff.Read(diffBuffer.SliceUpTo((int)addSize));
                        var inputBytesRead = input.Read(inputBuffer);

                        if (inputBytesRead != diffBytesRead)
                            throw new InvalidOperationException("Corrupt patch");

                        // add old data to diff string
                        for (var i = 0; i < diffBytesRead; i++)
                            diffBuffer[i] += inputBuffer[i];

                        output.Write(diffBuffer.Slice(0, diffBytesRead));
                        addSize -= diffBytesRead;
                    }

                    // sanity-check
                    if (output.Position + copySize > newSize)
                        throw new InvalidOperationException("Corrupt patch");

                    // read extra string in chunks
                    while (copySize > 0)
                    {
                        var bytesRead = extra.Read(diffBuffer.SliceUpTo((int)copySize));
                        output.Write(diffBuffer.Slice(0, bytesRead));
                        copySize -= bytesRead;
                    }

                    // adjust position
                    input.Seek(seekAmount, SeekOrigin.Current);
                }
            }

            output.Flush();
        }
    }
}
