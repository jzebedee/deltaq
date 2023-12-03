using CommunityToolkit.HighPerformance.Buffers;
using System;
using System.Buffers;

namespace DeltaQ.SuffixSorting.LibDivSufSort
{
    /// <summary>
    /// An implementation of the divsufsort suffix array construction algorithm.
    /// </summary>
    public class LibDivSufSort : ISuffixSort
    {
        public IMemoryOwner<int> Sort(ReadOnlySpan<byte> textBuffer)
        {
            var owner = MemoryOwner<int>.Allocate(textBuffer.Length);

            DivSufSort.divsufsort(textBuffer, owner.Span);

            return owner;
        }

        public void Sort(ReadOnlySpan<byte> textBuffer, Span<int> suffixBuffer)
        {
            if(textBuffer.Length != suffixBuffer.Length)
            {
                ThrowHelper();
            }

            DivSufSort.divsufsort(textBuffer, suffixBuffer);
        }

        private static void ThrowHelper() => throw new ArgumentException("Text and suffix buffers should have the same length");
    }
}
