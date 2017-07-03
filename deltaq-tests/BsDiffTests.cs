/*
 * BsDiffTests.cs for deltaq
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.MemoryMappedFiles;
using System.Linq;
using deltaq.BsDiff;
using Xunit;

namespace deltaq_tests
{
    public class BsDiffTests
    {
        private static readonly int[] Sizes = { 0, 1, 512, 999, 1024, 0x10000 };

        private static byte[] GetBuffer(int size)
        {
            var rand = new Random(63*13*63*13);

            var buf = new byte[size];
            rand.NextBytes(buf);

            return buf;
        }

        private static IEnumerable<byte[]> GetBuffers(IEnumerable<int> sizes)
        {
            return sizes.Select(GetBuffer);
        }

        [Fact]
        public void BsDiffCreateFromBuffers()
        {
            foreach (var oldBuffer in GetBuffers(Sizes))
                foreach (var newBuffer in GetBuffers(Sizes))
                {
                    var patchBuf = BsDiffCreate(oldBuffer, newBuffer);
                    var finishedBuf = BsDiffApply(oldBuffer, patchBuf);

                    Assert.Equal(newBuffer, finishedBuf);
                }
        }

        [Fact]
        public void BsDiffCreateFromBuffers_Identical()
        {
            foreach (var oldBuffer in GetBuffers(Sizes))
            {
                var newBuffer = new byte[oldBuffer.Length];
                Buffer.BlockCopy(oldBuffer, 0, newBuffer, 0, oldBuffer.Length);
                
                var patchBuf = BsDiffCreate(oldBuffer, newBuffer);
                var finishedBuf = BsDiffApply(oldBuffer, patchBuf);

                Assert.Equal(oldBuffer, finishedBuf);
                Assert.Equal(newBuffer, finishedBuf);
            }
        }

        [Fact]
        public void BsDiffCreateFromStreams()
        {
            const int outputSize = 0x2A000;

            foreach (var oldBuffer in GetBuffers(Sizes))
                foreach (var newBuffer in GetBuffers(Sizes))
                {
                    byte[] bytesOut;
                    using (var mmf = MemoryMappedFile.CreateNew(null, outputSize, MemoryMappedFileAccess.ReadWrite))
                    {
                        using (var mmfStream = mmf.CreateViewStream())
                        {
                            BsDiff.Create(oldBuffer, newBuffer, mmfStream);
                        }

                        using (var msA = new MemoryStream(oldBuffer))
                        using (var msOutput = new MemoryStream())
                        {
                            BsPatch.Apply(msA, mmf.CreateViewStream, msOutput);
                            bytesOut = msOutput.ToArray();
                        }
                    }

                    Assert.Equal(newBuffer, bytesOut);
                }
        }

        [Theory]
        [MemberData(nameof(BsDiffCreateNullArguments_TestData))]
        public void BsDiffCreateNullArguments(byte[] oldData, byte[] newData, Stream outStream)
        {
            Assert.Throws<ArgumentNullException>(() => BsDiff.Create(oldData, newData, outStream));
        }

        public static IEnumerable<object[]> BsDiffCreateNullArguments_TestData()
        {
            var emptybuf = new byte[0];
            var ms = new MemoryStream();
            yield return new object[] { null, emptybuf, ms };
            yield return new object[] { emptybuf, null, ms };
            yield return new object[] { emptybuf, emptybuf, null };
        }

        [Theory]
        [MemberData(nameof(BsDiffCreateBadStreams_TestData))]
        public void BsDiffCreateBadStreams(byte[] oldData, byte[] newData, Stream outStream)
        {
            Assert.Throws<ArgumentException>(() => BsDiff.Create(oldData, newData, outStream));
        }

        public static IEnumerable<object[]> BsDiffCreateBadStreams_TestData()
        {
            var emptybuf = new byte[0];
            yield return new object[] { emptybuf, emptybuf, new MemoryStream(emptybuf, false) };
            yield return new object[] { emptybuf, emptybuf, new DeflateStream(new MemoryStream(), CompressionMode.Compress) };
        }

        private static byte[] BsDiffCreate(byte[] oldBuf, byte[] newBuf)
        {
            using (var outputStream = new MemoryStream())
            {
                BsDiff.Create(oldBuf, newBuf, outputStream);
                return outputStream.ToArray();
            }
        }

        private static byte[] BsDiffApply(byte[] oldBuffer, byte[] patchBuffer)
        {
            using (var outputStream = new MemoryStream())
            {
                BsPatch.Apply(oldBuffer, patchBuffer, outputStream);
                return outputStream.ToArray();
            }
        }
    }
}
