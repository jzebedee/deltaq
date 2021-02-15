using DeltaQ.SuffixSorting;
using DeltaQ.SuffixSorting.SAIS;
using System.Diagnostics;
using Xunit;

namespace DeltaQ.Tests
{
    using static SAISChecker;
    public class SAISTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(8)]
        [InlineData(16)]
        [InlineData(32)]
        [InlineData(51)]
        [InlineData(0x8000)]
        public void CheckRandomBuffer(int size)
        {
            byte[] T = new byte[size];

            var provider = new System.Security.Cryptography.RNGCryptoServiceProvider();
            provider.GetBytes(T);

            ISuffixSort sort = new SAIS();
            var sw = Stopwatch.StartNew();
            int[] SA = sort.Sort(T);
            sw.Stop();

            Debug.WriteLine(sw.Elapsed);

            var result = Check(T, SA, T.Length, false);
            Assert.Equal(0, result);
        }
    }
}
