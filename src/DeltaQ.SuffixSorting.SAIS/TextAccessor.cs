using System;
using System.Runtime.CompilerServices;

namespace DeltaQ.SuffixSorting.SAIS
{
    internal ref struct TextAccessor<T> where T : unmanaged, IConvertible
    {
        private readonly ReadOnlySpan<T> _text;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TextAccessor(ReadOnlySpan<T> text) => _text = text;

        public readonly int this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _text[index].ToInt32(null);
        }
    }
}
