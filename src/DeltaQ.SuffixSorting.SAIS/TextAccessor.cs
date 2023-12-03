﻿using System;
using System.Runtime.CompilerServices;

namespace DeltaQ.SuffixSorting.SAIS;

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
internal readonly ref struct TextAccessor<T>(ReadOnlySpan<T> text) where T : unmanaged, IConvertible
{
    private readonly ReadOnlySpan<T> _text = text;

    public readonly int this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _text[index].ToInt32(null);
    }

    public readonly int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _text.Length;
    }

    public readonly bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _text.IsEmpty;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TextAccessor<T> Slice(int start) => new(_text.Slice(start));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TextAccessor<T> Slice(int start, int length) => new(_text.Slice(start, length));
}
