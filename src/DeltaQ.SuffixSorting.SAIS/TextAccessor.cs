using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaQ.SuffixSorting.SAIS
{
    internal ref struct TextAccessor<T> where T : unmanaged, IConvertible
    {
        private readonly ReadOnlySpan<T> _text;
        public TextAccessor(ReadOnlySpan<T> text)
        {
            _text = text;
        }

        public readonly int this[int index] => _text[index].ToInt32(null);
    }
}
