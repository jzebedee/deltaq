using System;

namespace DeltaQ.SuffixSorting.LibDivSufSort;

internal ref struct TdPAStarAccessor
{
    private readonly ReadOnlySpanOffsetAccessor<byte> _TO;
    private readonly ReadOnlySpan<int> _SA;
    private readonly ReadOnlySpan<int> _PA;
    private readonly IntAccessor _TD;

    public TdPAStarAccessor(ReadOnlySpan<byte> T, ReadOnlySpan<int> SA, int partitionOffset, int tdOffset)
    {
        _TO = new ReadOnlySpanOffsetAccessor<byte>(T, tdOffset);

        _SA = SA;
        _PA = SA[partitionOffset..];
        _TD = new(T[tdOffset..]);
    }

    public readonly int this[int index] => _TD[_PA[_SA[index]]];

    public readonly int AsOffset(int index) => _TO[index];
}
