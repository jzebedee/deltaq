using System;
using System.Buffers;

namespace DeltaQ.SuffixSorting.LibDivSufSort
{
    public partial class LibDivSufSort : ISuffixSort
    {
        public IMemoryOwner<int> Sort(ReadOnlySpan<byte> textBuffer)
        {
            throw new NotImplementedException();
        }

        public int Sort(ReadOnlySpan<byte> textBuffer, Span<int> suffixBuffer)
        {
            if(textBuffer.Length != suffixBuffer.Length)
            {
                throw new ArgumentException($"{nameof(textBuffer)} and {nameof(suffixBuffer)} should have the same length");
            }

            //TODO: add 0/1/2 fast cases

            //let T = Text(T);
            //let mut SA = SuffixArray(SA);

            //// Suffixsort.
            //construct_SA(&T, &mut SA, res.A, res.B, res.m);
            var res = sort_typeBstar(textBuffer, SA);
            //construct_SA(&T, &mut SA, res.A, res.B, res.m);
            construct_SA(textBuffer, suffixBuffer, res.A, res.B, res.m);
        }

    }
}
