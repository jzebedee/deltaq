using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using deltaq;
using NUnit.Framework;

namespace deltaq_tests
{
    [TestFixture]
    public class BsDiffTests
    {
        private Randomizer _randomizer;

        private byte[] _oldBuffer, _newBuffer;

        [TestFixtureSetUp]
        public void Setup()
        {
            var seed = Randomizer.RandomSeed;
            Debug.WriteLine("Randomizer seed: {0}", seed);

            _randomizer = new Randomizer(seed);

            const int count = 0x1000;
            _oldBuffer = new byte[count];
            _newBuffer = new byte[count];

            _randomizer.NextBytes(_oldBuffer);
            _randomizer.NextBytes(_newBuffer);
        }

        [Test]
        public void BsDiffFromBuffers()
        {
            BsDiffCreateApply(_oldBuffer, _newBuffer);
        }

        [Test]
        public void BsDiffFromStreams()
        {
            const int outputSize = 0x10000;

            byte[] bytesOut;
            using (var mmf = MemoryMappedFile.CreateNew(null, outputSize, MemoryMappedFileAccess.ReadWrite))
            {
                using (var mmfStream = mmf.CreateViewStream())
                {
                    BsDiff.Create(_oldBuffer, _newBuffer, mmfStream);
                }

                using (var msA = new MemoryStream(_oldBuffer))
                using (var msOutput = new MemoryStream())
                {
                    BsPatch.Apply(msA, mmf.CreateViewStream, msOutput);
                    bytesOut = msOutput.ToArray();
                }
            }

            Assert.AreEqual(_newBuffer, bytesOut);
        }

        private static void BsDiffCreateApply(byte[] oldBuf, byte[] newBuf)
        {
            byte[] outputBuf;
            using (var outputStream = new MemoryStream())
            {
                BsDiff.Create(oldBuf, newBuf, outputStream);
                outputBuf = outputStream.ToArray();
            }

            byte[] finishedBuf;
            using (var outputStream = new MemoryStream())
            {
                BsPatch.Apply(oldBuf, outputBuf, outputStream);
                finishedBuf = outputStream.ToArray();
            }

            Assert.AreEqual(newBuf, finishedBuf);
        }
    }
}
