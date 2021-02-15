using System;

namespace deltaq.SuffixSort
{
    public interface ISuffixSort
    {
        int[] Sort(ReadOnlySpan<byte> buffer);
    }
}