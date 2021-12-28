using Microsoft.Toolkit.HighPerformance;
using Microsoft.Toolkit.HighPerformance.Buffers;
using System;
using System.IO;

namespace DeltaQ.BsDiff
{
    public static class Patch
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
            using var inputStream = input.AsStream();
            ApplyInternal(newSize, inputStream, controlStream, diffStream, extraStream, output);
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
            using (var headerStream = openPatchStream(0, Diff.HeaderSize))
            {
                // check patch stream capabilities
                if (!headerStream.CanRead)
                    throw new ArgumentException("Patch stream must be readable", nameof(openPatchStream));
                if (!headerStream.CanSeek)
                    throw new ArgumentException("Patch stream must be seekable", nameof(openPatchStream));

                Span<byte> header = stackalloc byte[Diff.HeaderSize];
                headerStream.Read(header);

                // check for appropriate magic
                var signature = header.ReadPackedLong();
                if (signature != Diff.Signature)
                    throw new InvalidOperationException("Corrupt patch");

                // read lengths from header
                controlLength = header[sizeof(long)..].ReadPackedLong();
                diffLength = header[(sizeof(long) * 2)..].ReadPackedLong();
                newSize = header[(sizeof(long) * 3)..].ReadPackedLong();

                if (controlLength < 0 || diffLength < 0 || newSize < 0)
                    throw new InvalidOperationException("Corrupt patch");
                }

            // prepare to read three parts of the patch in parallel
            Stream
                compressedControlStream = openPatchStream(Diff.HeaderSize, controlLength),
                compressedDiffStream = openPatchStream(Diff.HeaderSize + controlLength, diffLength),
                compressedExtraStream = openPatchStream(Diff.HeaderSize + controlLength + diffLength, 0);

            // decompress each part (to read it)
            ctrl = Diff.GetEncodingStream(compressedControlStream, false);
            diff = Diff.GetEncodingStream(compressedDiffStream, false);
            extra = Diff.GetEncodingStream(compressedExtraStream, false);

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
                    var copySize = ctrlBuffer[sizeof(long)..].ReadPackedLong();
                    // seek forwards in oldfile by z bytes;
                    var seekAmount = ctrlBuffer[(sizeof(long) * 2)..].ReadPackedLong();

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

                        output.Write(diffBuffer[..diffBytesRead]);
                        addSize -= diffBytesRead;
                    }

                    // sanity-check
                    if (output.Position + copySize > newSize)
                        throw new InvalidOperationException("Corrupt patch");

                    // read extra string in chunks
                    while (copySize > 0)
                    {
                        var bytesRead = extra.Read(diffBuffer.SliceUpTo((int)copySize));
                        output.Write(diffBuffer[..bytesRead]);
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
