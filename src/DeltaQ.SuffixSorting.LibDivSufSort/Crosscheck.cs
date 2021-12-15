using System;
using System.Diagnostics;

namespace DeltaQ.SuffixSorting.LibDivSufSort
{
    internal static class Crosscheck
    {
        [Conditional("DEBUG")]
        internal static void SA_dump(ReadOnlySpan<int> span, string v)
        {
            Debug.WriteLine($":: {v}");
            for (int i = 0; i < span.Length; i++)
            {
                Debug.Write($"{span[i]} ");
                Debug.WriteLineIf((i + 1) % 25 == 0, "");
            }
            Debug.WriteLine("");
        }

        [Conditional("DEBUG")]
        internal static void crosscheck(string v, params object[] args) => Debug.WriteLine(v, args);
    }
}
