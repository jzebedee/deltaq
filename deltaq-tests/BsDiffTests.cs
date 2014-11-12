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
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.MemoryMappedFiles;
using System.Linq;
using deltaq;
using NUnit.Framework;

namespace deltaq_tests
{
    [TestFixture]
    public class BsDiffTests
    {
        private static readonly int[] Sizes = { 0, 1, 512, 999, 1024, 0x10000 };

        private static byte[] GetBuffer(int size)
        {
            var rand = SetupRandomizer();

            var buf = new byte[size];
            rand.NextBytes(buf);

            return buf;
        }

        private static IEnumerable<byte[]> GetBuffers(IEnumerable<int> sizes)
        {
            return sizes.Select(GetBuffer);
        }

        private static Randomizer SetupRandomizer()
        {
            var seed = Randomizer.RandomSeed;
            Debug.WriteLine("Randomizer seed: {0}", seed);

            return new Randomizer(seed);
        }

        [Test]
        public void BsDiffCreateFromBuffers()
        {
            foreach (var oldBuffer in GetBuffers(Sizes))
                foreach (var newBuffer in GetBuffers(Sizes))
                {
                    var patchBuf = BsDiffCreate(oldBuffer, newBuffer);
                    var finishedBuf = BsDiffApply(oldBuffer, patchBuf);

                    Assert.AreEqual(newBuffer, finishedBuf);
                }
        }

        [Test]
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

                    Assert.AreEqual(newBuffer, bytesOut);
                }
        }

        [TestCaseSource(typeof(BsDiffTests), "BsDiffCreateNullArguments_TestData")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void BsDiffCreateNullArguments(byte[] oldData, byte[] newData, Stream outStream)
        {
            BsDiff.Create(oldData, newData, outStream);
        }

        private static IEnumerable BsDiffCreateNullArguments_TestData()
        {
            var emptybuf = new byte[0];
            var ms = new MemoryStream();
            yield return new object[] { null, emptybuf, ms };
            yield return new object[] { emptybuf, null, ms };
            yield return new object[] { emptybuf, emptybuf, null };
        }

        [TestCaseSource(typeof(BsDiffTests), "BsDiffCreateBadStreams_TestData")]
        [ExpectedException(typeof(ArgumentException))]
        public void BsDiffCreateBadStreams(byte[] oldData, byte[] newData, Stream outStream)
        {
            BsDiff.Create(oldData, newData, outStream);
        }

        private static IEnumerable BsDiffCreateBadStreams_TestData()
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
