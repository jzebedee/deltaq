using System;
using System.Runtime.CompilerServices;
using Text = System.ReadOnlySpan<byte>;

namespace DeltaQ.SuffixSorting.LibDivSufSort;

internal ref struct TdPAStarAccessor
{
    private readonly ReadOnlySpanOffsetAccessor<byte> _TO;
    private readonly ReadOnlySpan<int> _SA;
    private readonly ReadOnlySpan<int> _PA;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TdPAStarAccessor(Text T, ReadOnlySpan<int> SA, int partitionOffset, int tdOffset)
    {
        _TO = new ReadOnlySpanOffsetAccessor<byte>(T, tdOffset);
        _SA = SA;
        _PA = SA[partitionOffset..];
    }

    public readonly int this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _TO[_PA[_SA[index]]];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int AsOffset(int index) => _TO[index];
}
