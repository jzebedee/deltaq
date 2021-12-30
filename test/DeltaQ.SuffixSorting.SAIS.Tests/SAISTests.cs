using DeltaQ.SuffixSorting.SAIS;
using Microsoft.Toolkit.HighPerformance.Buffers;
using System;
using System.Buffers;
using Xunit;

namespace DeltaQ.Tests
{
    using static SAISChecker;
    public class SAISTests
    {
        private readonly SAIS _sais = new SAIS();

        private static SpanOwner<byte> GetOwnedRandomBuffer(int size)
        {
            var rand = new Random(63 * 13 * 63 * 13);

            var owner = SpanOwner<byte>.Allocate(size);
#if NETFRAMEWORK
            var buf = new byte[size];
            rand.NextBytes(buf);
            buf.CopyTo(owner.Span);
#else
            rand.NextBytes(owner.Span);
#endif

            return owner;
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(8)]
        [InlineData(16)]
        [InlineData(32)]
        [InlineData(51)]
        [InlineData(0x8000 - 1)]
        [InlineData(0x8000)]
        public void CheckRandomBuffer(int size)
        {
            using var ownedT = GetOwnedRandomBuffer(size);
            Span<byte> T = ownedT.Span;

            using var ownedSA = _sais.Sort(T);
            Span<int> SA = ownedSA.Span;

            var result = Check(T, SA, T.Length, false);
            Assert.Equal(0, result);
        }
    }
}
