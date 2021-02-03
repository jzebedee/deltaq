using System.IO;
using System.Security.Cryptography;
using Xunit;

namespace DeltaQ.Tests
{
    public class BsPatchTests
    {
        private static RNGCryptoServiceProvider _cryptoRNG = new RNGCryptoServiceProvider();
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
            BsDiff.BsDiff.Create(oldBuffer, newBuffer, wrappedPatchMs);

            var patchBuffer = patchMs.ToArray();

            var reconstructMs = new MemoryStream();
            var wrappedReconstructMs = new BufferedStream(reconstructMs);
            BsDiff.BsPatch.Apply(oldBuffer, patchBuffer, wrappedReconstructMs);

            var reconstructedBuffer = reconstructMs.ToArray();

            Assert.Equal(newBuffer, reconstructedBuffer);
        }
    }
}
