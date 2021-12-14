using DeltaQ.BsDiff;
using System.IO;
using System.Security.Cryptography;
using Xunit;

namespace DeltaQ.Tests
{
    public class BsPatchTests
    {
        private static RandomNumberGenerator _cryptoRNG = RandomNumberGenerator.Create();
        private static byte[] GetRandomFilledBuffer(int count)
        {
            var buffer = new byte[count];
            _cryptoRNG.GetBytes(buffer);
            return buffer;
        }

        [Fact]
        public void BsPatchFlushesOutput()
        {
            var oldBuffer = GetRandomFilledBuffer(0x123);
            var newBuffer = GetRandomFilledBuffer(0x4567);

            //can't use MemoryStream directly as Flush has no effect
            var patchMs = new MemoryStream();
            var wrappedPatchMs = new BufferedStream(patchMs);
            Diff.Create(oldBuffer, newBuffer, wrappedPatchMs, new SuffixSorting.SAIS.SAIS());

            var patchBuffer = patchMs.ToArray();

            var reconstructMs = new MemoryStream();
            var wrappedReconstructMs = new BufferedStream(reconstructMs);
            Patch.Apply(oldBuffer, patchBuffer, wrappedReconstructMs);

            var reconstructedBuffer = reconstructMs.ToArray();

            Assert.Equal(newBuffer, reconstructedBuffer);
        }
    }
}
