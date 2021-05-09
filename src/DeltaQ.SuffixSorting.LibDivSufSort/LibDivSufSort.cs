using System;
using System.Buffers;

namespace DeltaQ.SuffixSorting.LibDivSufSort
{
    public partial class LibDivSufSort : ISuffixSort
    {
        public IMemoryOwner<int> Sort(ReadOnlySpan<byte> textBuffer)
        {
            throw new NotImplementedException();
        }

        public int Sort(ReadOnlySpan<byte> textBuffer, Span<int> suffixBuffer)
        {
            throw new NotImplementedException();
        }

    }
}
