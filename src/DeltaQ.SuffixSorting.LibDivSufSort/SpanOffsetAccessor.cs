using System;

namespace DeltaQ.SuffixSorting.LibDivSufSort;

internal ref struct SpanOffsetAccessor<T>
{
    private readonly Span<T> _span;
    private readonly int _offset;

    public SpanOffsetAccessor(Span<T> span, int offset)
    {
        _span = span;
        _offset = offset;
    }

    public ref T this[int index] => ref _span[_offset + index];
}