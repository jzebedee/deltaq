using System;
using System.Runtime.CompilerServices;

namespace DeltaQ.SuffixSorting.LibDivSufSort;

internal static class SpanExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Swap<T>(this Span<T> span, int i, int j)
        => (span[j], span[i]) = (span[i], span[j]);
}
