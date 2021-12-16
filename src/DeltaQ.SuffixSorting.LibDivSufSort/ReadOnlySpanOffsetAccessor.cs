using System;

namespace DeltaQ.SuffixSorting.LibDivSufSort;

internal ref struct ReadOnlySpanOffsetAccessor<T>
{
    private readonly ReadOnlySpan<T> _span;
    private readonly int _offset;

    public ReadOnlySpanOffsetAccessor(ReadOnlySpan<T> span, int offset)
    {
        _span = span;
        _offset = offset;
    }

    public ref readonly T this[int index] => ref _span[_offset + index];
}
