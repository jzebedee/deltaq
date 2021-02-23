using System;
using System.Buffers;

namespace DeltaQ.SuffixSorting
{
    public interface ISuffixSort
    {
        ReadOnlyMemory<int> Sort(ReadOnlySpan<byte> textBuffer);
        IMemoryOwner<int> SortOwned(ReadOnlySpan<byte> textBuffer);
        int Sort(ReadOnlySpan<byte> textBuffer, Span<int> suffixBuffer);
    }
}