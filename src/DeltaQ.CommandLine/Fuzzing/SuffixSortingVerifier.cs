using System;

namespace DeltaQ.CommandLine.Fuzzing;

internal static class SuffixSortingVerifier
{
    public static void Verify(ReadOnlySpan<byte> input, ReadOnlySpan<int> sa)
    {
        for (int i = 0; i < input.Length - 1; i++)
        {
            var cur = input[sa[i]..];
            var next = input[sa[i + 1]..];
            var cmp = cur.SequenceCompareTo(next);
            if (!(cmp < 0))
            {
                var ex = new InvalidOperationException("Input was unsorted");
                ex.Data["i"] = i;
                ex.Data["j"] = i + 1;
                throw ex;
            }
        }
    }
}
