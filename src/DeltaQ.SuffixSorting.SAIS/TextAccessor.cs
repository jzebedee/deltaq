using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaQ.SuffixSorting.SAIS
{
    internal ref struct TextAccessor<T>
    {
        private readonly ReadOnlySpan<T> _text;
        public TextAccessor(ReadOnlySpan<T> text)
        {
            _text = text;
        }

        public ref readonly T this[int index] => ref _text[index];
    }
}
