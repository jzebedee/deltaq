using System;
using System.Buffers;

namespace DeltaQ.SuffixSorting
{
    public interface ISuffixSort
    {
        IMemoryOwner<int> Sort(ReadOnlySpan<byte> textBuffer);
        int Sort(ReadOnlySpan<byte> textBuffer, Span<int> suffixBuffer);
    }
}