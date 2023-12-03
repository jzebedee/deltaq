using System;
using System.Runtime.CompilerServices;
using Text = System.ReadOnlySpan<byte>;

namespace DeltaQ.SuffixSorting.LibDivSufSort;

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
internal readonly ref struct TdPAStarAccessor(Text T, ReadOnlySpan<int> SA, int partitionOffset, int tdOffset)
{
    private readonly ReadOnlySpan<byte> _TO = T;
    private readonly ReadOnlySpan<int> _SA = SA;
    private readonly ReadOnlySpan<int> _PA = SA[partitionOffset..];

    public readonly int this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _TO[_PA[_SA[index]] + tdOffset];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int AsOffset(int index) => _TO[index + tdOffset];
}
