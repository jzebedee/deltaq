using System;

namespace DeltaQ.SuffixSorting.LibDivSufSort
{
    public static class SpanExtensions
    {
        public static void Swap<T>(this Span<T> span, int indexA, int indexB)
        {
            ref var itemA = ref span[indexA];
            ref var itemB = ref span[indexB];
            span[indexA] = itemB;
            span[indexB] = itemA;
        }
    }
}
