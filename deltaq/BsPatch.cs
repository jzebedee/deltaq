using System;
using System.IO;

namespace deltaq
{
    public static class BsPatch
    {
        public delegate Stream OpenDiffStream(long offset, long length);

        public static void Apply(byte[] input, byte[] diff, Stream output)
        {
            OpenDiffStream openDiffStream = (uOffset, uLength) =>
            {
                //shaving down to int because we assume we're using 32bit indices
                var offset = (int)uOffset;
                var length = (int)uLength;
                return new MemoryStream(diff, offset,
                    uLength > 0
                        ? length
                        : diff.Length - offset);
            };

            Stream controlStream, diffStream, extraStream;
            var newSize = CreatePatchStreams(openDiffStream, out controlStream, out diffStream, out extraStream);

            // prepare to read three parts of the patch in parallel
            ApplyInternal(newSize, new MemoryStream(input), controlStream, diffStream, extraStream, output);
        }

        public static void Apply(Stream input, OpenDiffStream openDiffStream, Stream output)
        {
            Stream controlStream, diffStream, extraStream;
            var newSize = CreatePatchStreams(openDiffStream, out controlStream, out diffStream, out extraStream);

            // prepare to read three parts of the patch in parallel
            ApplyInternal(newSize, input, controlStream, diffStream, extraStream, output);
        }

        private static long CreatePatchStreams(OpenDiffStream openDiffStream, out Stream ctrl, out Stream diff, out Stream extra)
        {
            // read header
            long controlLength, diffLength, newSize;
            using (var diffStream = openDiffStream(0, BsDiff.HeaderSize))
            {
                // check patch stream capabilities
                if (!diffStream.CanRead)
                    throw new ArgumentException("Patch stream must be readable", "openDiffStream");
                if (!diffStream.CanSeek)
                    throw new ArgumentException("Patch stream must be seekable", "openDiffStream");

                var header = new byte[BsDiff.HeaderSize];
                diffStream.Read(header, 0, BsDiff.HeaderSize);

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
                compressedControlStream = openDiffStream(BsDiff.HeaderSize, controlLength),
                compressedDiffStream = openDiffStream(BsDiff.HeaderSize + controlLength, diffLength),
                compressedExtraStream = openDiffStream(BsDiff.HeaderSize + controlLength + diffLength, -1);

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
            using (BinaryReader
                diffReader = new BinaryReader(diff),
                extraReader = new BinaryReader(extra))
                while (output.Position < newSize)
                {
                    //read control data
                    //set of triples (x,y,z) meaning
                    // add x bytes from oldfile to x bytes from the diff block;
                    // copy y bytes from the extra block;
                    // seek forwards in oldfile by z bytes;
                    var addSize = ctrl.ReadLong();
                    var copySize = ctrl.ReadLong();
                    var seekAmount = ctrl.ReadLong();

                    // sanity-check
                    if (output.Position + addSize > newSize)
                        throw new InvalidOperationException("Corrupt patch.");

                    {
                        // read diff string
                        var newData = diffReader.ReadBytes((int)addSize);

                        // add old data to diff string
                        var availableInputBytes = (int)Math.Min(addSize, input.Length - input.Position);
                        for (var i = 0; i < availableInputBytes; i++)
                            newData[i] += (byte)input.ReadByte();

                        output.Write(newData, 0, (int)addSize);
                        //input.Seek(addSize, SeekOrigin.Current);
                    }

                    // sanity-check
                    if (output.Position + copySize > newSize)
                        throw new InvalidOperationException("Corrupt patch.");

                    // read extra string
                    {
                        var newData = extraReader.ReadBytes((int)copySize);
                        output.Write(newData, 0, (int)copySize);
                    }

                    // adjust position
                    input.Seek(seekAmount, SeekOrigin.Current);
                }
        }
    }
}
