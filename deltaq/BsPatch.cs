using System;
using System.IO;

namespace deltaq
{
    public static class BsPatch
    {
        public delegate Stream OpenPatchStream(long offset, long length);

        public static void Apply(byte[] input, byte[] diff, Stream output)
        {
            OpenPatchStream openPatchStream = (uOffset, uLength) =>
            {
                var offset = (int)uOffset;
                var length = (int)uLength;
                return new MemoryStream(diff, offset,
                    uLength > 0
                        ? length
                        : diff.Length - offset);
            };

            Stream controlStream, diffStream, extraStream;
            var newSize = CreatePatchStreams(openPatchStream, out controlStream, out diffStream, out extraStream);

            // prepare to read three parts of the patch in parallel
            ApplyInternal(newSize, new MemoryStream(input), controlStream, diffStream, extraStream, output);
        }

        public static void Apply(Stream input, OpenPatchStream openPatchStream, Stream output)
        {
            Stream controlStream, diffStream, extraStream;
            var newSize = CreatePatchStreams(openPatchStream, out controlStream, out diffStream, out extraStream);

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
                    throw new ArgumentException("Patch stream must be readable", "openPatchStream");
                if (!patchStream.CanSeek)
                    throw new ArgumentException("Patch stream must be seekable", "openPatchStream");

                var header = new byte[BsDiff.HeaderSize];
                patchStream.Read(header, 0, BsDiff.HeaderSize);

                // check for appropriate magic
                var signature = header.ReadLong();
                if (signature != BsDiff.Signature)
                    throw new InvalidOperationException("Corrupt patch");

                // read lengths from header
                controlLength = header.ReadLongAt(8);
                diffLength = header.ReadLongAt(16);
                newSize = header.ReadLongAt(24);

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

        private static void ApplyInternal(long newSize, Stream input, Stream ctrl, Stream diff, Stream extra, Stream output)
        {
            using (ctrl)
            using (diff)
            using (extra)
            using (var inputReader = new BinaryReader(input))
                while (output.Position < newSize)
                {
                    //read control data:
                    // set of triples (x,y,z) meaning

                    // add x bytes from oldfile to x bytes from the diff block;
                    var addSize = ctrl.ReadLong();
                    // copy y bytes from the extra block;
                    var copySize = ctrl.ReadLong();
                    // seek forwards in oldfile by z bytes;
                    var seekAmount = ctrl.ReadLong();

                    // sanity-check
                    if (output.Position + addSize > newSize)
                        throw new InvalidOperationException("Corrupt patch.");

                    // read diff string in chunks
                    foreach (var newData in diff.BufferedRead(addSize))
                    {
                        var inputData = inputReader.ReadBytes(newData.Length);

                        // add old data to diff string
                        for (var i = 0; i < newData.Length; i++)
                            newData[i] += inputData[i];

                        output.Write(newData, 0, newData.Length);
                    }

                    // sanity-check
                    if (output.Position + copySize > newSize)
                        throw new InvalidOperationException("Corrupt patch.");

                    // read extra string in chunks
                    foreach (var extraData in extra.BufferedRead(copySize))
                    {
                        output.Write(extraData, 0, extraData.Length);
                    }

                    // adjust position
                    input.Seek(seekAmount, SeekOrigin.Current);
                }
        }
    }
}
