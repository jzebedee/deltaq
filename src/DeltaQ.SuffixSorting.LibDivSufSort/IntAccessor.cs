using System;
using Idx = System.Int32;

namespace DeltaQ.SuffixSorting.LibDivSufSort;

internal ref struct IntAccessor
{
    public readonly ReadOnlySpan<byte> span;
    public IntAccessor(ReadOnlySpan<byte> span) => this.span = span;

    public readonly int this[Idx index] => span[index];
    public readonly int Length => span.Length;
}
