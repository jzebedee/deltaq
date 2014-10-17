using System;
using System.IO;
using deltaq;
using NUnit.Framework;

namespace deltaq_tests
{
    [TestFixture]
    public class DiffTests
    {
        [Test]
        public void BsDiffCreateMany()
        {
            var rnd = new Random();
            for (int i = 0; i < 100; i++)
            {
                var oldBuf = new byte[i];
                rnd.NextBytes(oldBuf);

                for (int j = 0; j < 100; j++)
                {
                    var newBuf = new byte[j];
                    rnd.NextBytes(newBuf);

                    BsDiffCreate(oldBuf, newBuf);
                }
            }
        }

        private void BsDiffCreate(byte[] oldBuf, byte[] newBuf)
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
