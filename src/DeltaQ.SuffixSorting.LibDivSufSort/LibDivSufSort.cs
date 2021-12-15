using Microsoft.Toolkit.HighPerformance.Buffers;
using System;
using System.Buffers;

namespace DeltaQ.SuffixSorting.LibDivSufSort
{
    public partial class LibDivSufSort : ISuffixSort
    {
        public IMemoryOwner<int> Sort(ReadOnlySpan<byte> textBuffer)
        {
            var owner = MemoryOwner<int>.Allocate(textBuffer.Length);

            Sort(textBuffer, suffixBuffer: owner.Span);

            return owner;
        }

        public int Sort(ReadOnlySpan<byte> textBuffer, Span<int> suffixBuffer)
        {
            if(textBuffer.Length != suffixBuffer.Length)
            {
                ThrowHelper();
            }

            //TODO: add 0/1/2 fast cases

            DivSufSort.divsufsort(textBuffer, suffixBuffer);
            return suffixBuffer.Length;
        }

        private static void ThrowHelper() => throw new ArgumentException("Text and suffix buffers should have the same length");
    }
}
