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

#if NET461
        private static void RandomFillBuffer(byte[] buffer)
        {
            var rand = new Random(63 * 13 * 63 * 13);
            rand.NextBytes(buffer);
        }
#else
        private static MemoryOwner<byte> GetOwnedRandomBuffer(int size)
        {
            var rand = new Random(63 * 13 * 63 * 13);

            var owner = MemoryOwner<byte>.Allocate(size);
            rand.NextBytes(owner.Span);

            return owner;
        }
#endif

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
        [InlineData(0x80000)]
        [InlineData(0x800000)]
        public void CheckRandomBuffer(int size)
        {
#if NET461
            var ownedT = ArrayPool<byte>.Shared.Rent(size);
            try
#else
            using (var ownedT = GetOwnedRandomBuffer(size))
#endif
            {
#if NET461
                RandomFillBuffer(ownedT);
                Span<byte> T = ownedT;
#else
                Span<byte> T = ownedT.Span;
#endif
                using (var ownedSA = _sais.Sort(T))
                {
                    Span<int> SA = ownedSA.Span;
                    var result = Check(T, SA, T.Length, false);
                    Assert.Equal(0, result);
                }
            }
#if NET461
            finally
            {
                ArrayPool<byte>.Shared.Return(ownedT);
            }
#endif
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
        [InlineData(0x1000)]
        public void CheckRandomBufferContinuous(int size)
        {
            const int repetitions = 2_000;
            for (int i = 0; i < repetitions; i++)
            {
                CheckRandomBuffer(size);

                if (i % 100 == 0)
                {
                    System.Diagnostics.Debug.WriteLine("Gen0:{0} Gen1:{1} Gen2:{2}", GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2));
                }
            }
        }
    }
}
