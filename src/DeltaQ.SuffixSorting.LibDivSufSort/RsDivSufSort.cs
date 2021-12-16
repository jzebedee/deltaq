using Microsoft.Toolkit.HighPerformance.Buffers;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Idx = System.Int32;
using SAPtr = System.Int32;

namespace DeltaQ.SuffixSorting.LibDivSufSort;
using static Crosscheck;
using static Utils;

internal static class DivSufSort
{
    private const int ALPHABET_SIZE = byte.MaxValue + 1;
    private const int BUCKET_A_SIZE = ALPHABET_SIZE;
    private const int BUCKET_B_SIZE = ALPHABET_SIZE * ALPHABET_SIZE;

    public static void divsufsort(ReadOnlySpan<byte> T, Span<int> SA)
    {
        Trace.Assert(T.Length == SA.Length);

        var n = T.Length;

        switch (n)
        {
            case 0: return;
            case 1:
                SA[0] = 0;
                return;
            case 2:
                if (T[0] < T[1])
                {
                    (stackalloc[] { 0, 1 }).CopyTo(SA);
                }
                else
                {
                    (stackalloc[] { 1, 0 }).CopyTo(SA);
                }
                return;
        }

        var result = sort_typeBstar(new IntAccessor(T), SA);
        construct_SA(T, SA, result.A, result.B, result.m);
    }

    private static void construct_SA(ReadOnlySpan<byte> T, Span<int> SA, Span<int> A, Span<int> B, int m)
    {
        Idx n = T.Length;

        BBucket Bb = new(B);
        BStarBucket Bstar = new(B);

        SAPtr i;
        SAPtr j;
        Idx k;
        Idx s;
        Idx c0;
        Idx c2;
        if (0 < m)
        {
            // Construct the sorted order of type B suffixes by using the
            // sorted order of type B* suffixes
            Idx c1 = ALPHABET_SIZE - 2;
            while (0 <= c1)
            {
                // Scan the suffix array from right to left
                i = Bstar[(c1, c1 + 1)];
                j = A[c1 + 1] - 1;
                k = 0;
                c2 = -1;

                while (i <= j)
                {
                    s = SA[j];
                    if (0 < s)
                    {
                        Trace.Assert(T[s] == c1);
                        Trace.Assert((s + 1) < n);
                        Trace.Assert(T[s] <= T[s + 1]);

                        SA[j] = ~s;
                        s -= 1;
                        c0 = T[s];
                        if ((0 < s) && (T[s - 1] > c0))
                        {
                            s = ~s;
                        }
                        if (c0 != c2)
                        {
                            if (0 <= c2)
                            {
                                Bb[(c2, c1)] = k;
                            }
                            c2 = c0;
                            k = Bb[(c2, c1)];
                        }
                        Trace.Assert(k < j);
                        SA[k] = s;
                        k -= 1;
                    }
                    else
                    {
                        Trace.Assert(((s == 0) && (T[s] == c1)) || (s < 0));
                        SA[j] = ~s;
                    }

                    // iter
                    j -= 1;
                }

                // iter
                c1 -= 1;
            }
        }

        // Construct the suffix array by using the sorted order of type B suffixes
        c2 = T[n - 1];
        k = A[c2];
        //TODO: check this
        //SA[k] = T[n - 2] < c2 ? !(n - 1) : n - 1;
        SA[k] = T[n - 2] < c2 ? ~(n - 1) : n - 1;
        k += 1;
        // Scan the suffix array from left to right
        {
            // init
            i = 0;
            j = n;

            while (i < j)
            {
                s = SA[i];
                if (0 < s)
                {
                    Trace.Assert(T[s - 1] >= T[s]);
                    s -= 1;
                    c0 = T[s];
                    if ((s == 0) || (T[s - 1] < c0))
                    {
                        s = ~s;
                    }
                    if (c0 != c2)
                    {
                        A[c2] = k;
                        c2 = c0;
                        k = A[c2];
                    }
                    Trace.Assert(i < k);
                    SA[k] = s;
                    k += 1;
                }
                else
                {
                    Trace.Assert(s < 0);
                    SA[i] = ~s;
                }

                // iter
                i += 1;
            }
        }
    }

    public ref struct SortTypeBstarResult
    {
        public Span<int> A;
        public Span<int> B;
        public int m;
    }

    public ref struct BStarBucket
    {
        public readonly Span<int> B;
        public BStarBucket(Span<int> B) => this.B = B;

        public ref int this[(int c0, int c1) index] => ref B[(index.c0 << 8) | index.c1];
    }

    public ref struct BBucket
    {
        public readonly Span<int> B;
        public BBucket(Span<int> B) => this.B = B;

        public ref int this[(int c0, int c1) index] => ref B[(index.c1 << 8) | index.c0];
    }

    public static SortTypeBstarResult sort_typeBstar(in IntAccessor T, Span<int> SA)
    {
        var n = T.Length;

        //These MUST be zeroed first
        using var owner_A = SpanOwner<int>.Allocate(BUCKET_A_SIZE, AllocationMode.Clear);
        using var owner_B = SpanOwner<int>.Allocate(BUCKET_B_SIZE, AllocationMode.Clear);

        Span<int> A = owner_A.Span;
        Span<int> B = owner_B.Span;

        BBucket Bb = new(B);
        BStarBucket Bstar = new(B);

        int c0, c1, i, j, k, t, m;

        // Count the number of occurences of the first one or two characters of each
        // type A, B and B* suffix. Moreover, store the beginning position of all
        // type B* suffixes into the array SA.
        i = n - 1;
        m = n;
        c0 = T[n - 1];

        while (0 <= i)
        {
            // type A suffix (originally do..while)
            while (true)
            {
                c1 = c0;
                A[c1] += 1;

                // original loop condition
                i -= 1;
                if (0 > i)
                {
                    break;
                }

                c0 = T[i];
                if (c0 < c1)
                {
                    break;
                }
            }

            if (0 <= i)
            {
                // type B* suffix
                Bstar[(c0, c1)] += 1;

                m -= 1;
                SA[m] = i;

                // type B suffix

                // init
                i -= 1;
                c1 = c0;

                while (true)
                {
                    // cond
                    if (0 > i)
                    {
                        break;
                    }
                    c0 = T[i];
                    if (c0 > c1)
                    {
                        break;
                    }

                    // body
                    Bb[(c0, c1)] += 1;

                    // iter
                    i -= 1;
                    c1 = c0;
                }
            }
        }
        m = n - m;

        // Note: A type B* suffix is lexicographically smaller than a type B suffix
        // that beings with the same first two characters.

        // Calculate the index of start/end point of each bucket.
        {
            i = 0;
            j = 0;
            for (c0 = 0; c0 < ALPHABET_SIZE; c0++)
            {
                // body
                t = i + A[c0];
                A[c0] = i + j; // start point
                i = t + Bb[(c0, c0)];

                for (c1 = c0 + 1; c1 < ALPHABET_SIZE; c1++)
                {
                    j += Bstar[(c0, c1)];
                    Bstar[(c0, c1)] = j; // end point
                    i += Bb[(c0, c1)];
                }
            }
        }

        if (0 < m)
        {
            // Sort the type B* suffixes by their first two characters
            SAPtr PAb = n - m;
            SAPtr ISAb = m;

            //for i in (0.. = (m - 2)).rev() {
            for (i = m - 2; i >= 0; i--)
            {
                t = SA[PAb + i];
                c0 = T[t];
                c1 = T[t + 1];
                Bstar[(c0, c1)] -= 1;
                SA[Bstar[(c0, c1)]] = i;
            }
            t = SA[PAb + m - 1];
            c0 = T[t];
            c1 = T[t + 1];
            Bstar[(c0, c1)] -= 1;
            SA[Bstar[(c0, c1)]] = m - 1;

            // Sort the type B* substrings using sssort.
            SAPtr buf = m;
            var bufsize = n - (2 * m);

            // init (outer)
            c0 = ALPHABET_SIZE - 2;
            j = m;
            while (0 < j)
            {
                // init (inner)
                c1 = ALPHABET_SIZE - 1;
                while (c0 < c1)
                {
                    // body (inner)
                    i = Bstar[(c0, c1)];

                    if (1 < (j - i))
                    {
                        SA_dump(SA[i..j], "sssort(A)");
                        SsSort.sssort(T, SA, PAb, i, j, buf, bufsize, 2, n, SA[i] == (m - 1));
                        SA_dump(SA[i..j], "sssort(B)");
                    }

                    // iter (inner)
                    j = i;
                    c1 -= 1;
                }

                // iter (outer)
                c0 -= 1;
            }

            // Compute ranks of type B* substrings
            i = m - 1;
            while (0 <= i)
            {
                if (0 <= SA[i])
                {
                    j = i;
                    while (true)
                    {
                        {
                            var SAi = SA[i];
                            SA[ISAb + SAi] = i;
                        }

                        i -= 1;
                        if (!((0 <= i) && (0 <= SA[i])))
                        {
                            break;
                        }
                    }

                    SA[i + 1] = i - j;
                    if (i <= 0)
                    {
                        break;
                    }
                }
                j = i;
                while (true)
                {
                    SA[i] = ~SA[i];
                    SA[ISAb + SA[i]] = j;

                    i -= 1;
                    if (!(SA[i] < 0))
                    {
                        break;
                    }
                }

                SA[ISAb + SA[i]] = j;
                i -= 1;
            }

            // Construct the inverse suffix array of type B* suffixes using trsort.
            SA_dump(SA, "trsort(A)");
            crosscheck($"enter trsort: ISAb={ISAb} m={m} depth={1}");
            TrSort.trsort(ISAb, SA, m, 1);
            SA_dump(SA, "trsort(B)");

            // Set the sorted order of type B* suffixes
            {
                // init
                i = n - 1;
                j = m;
                c0 = T[n - 1];
                while (0 <= i)
                {
                    // init
                    i -= 1;
                    c1 = c0;

                    while (true)
                    {
                        // cond
                        if (!(0 <= i))
                        {
                            break;
                        }
                        c0 = T[i];
                        if (!(c0 >= c1))
                        {
                            break;
                        }

                        // body (empty)

                        // iter
                        i -= 1;
                        c1 = c0;
                    }

                    if (0 <= i)
                    {
                        t = i;

                        // init
                        i -= 1;
                        c1 = c0;

                        while (true)
                        {
                            // cond
                            if (!(0 <= i))
                            {
                                break;
                            }
                            c0 = T[i];
                            if (!(c0 <= c1))
                            {
                                break;
                            }

                            // body (empty)

                            // iter
                            i -= 1;
                            c1 = c0;
                        }

                        j -= 1;
                        {
                            var pos = SA[ISAb + j];
                            //TODO: check complement
                            SA[pos] = (t == 0 || (1 < (t - i))) ? t : ~t;
                        }
                    }
                }
            } // End: Set the sorted order of type B* suffixes

            // Calculate the index of start/end point of each bucket
            {
                Bb[(ALPHABET_SIZE - 1, ALPHABET_SIZE - 1)] = n; // end point

                // init
                c0 = ALPHABET_SIZE - 2;
                k = m - 1;

                while (0 <= c0)
                {
                    i = A[c0 + 1] - 1;

                    // init
                    c1 = ALPHABET_SIZE - 1;
                    while (c0 < c1)
                    {
                        t = i - Bb[(c0, c1)];
                        Bb[(c0, c1)] = i; // end point

                        // Move all type B* suffixes to the correct position
                        {
                            // init
                            i = t;
                            j = Bstar[(c0, c1)];

                            while (j <= k)
                            {
                                SA[i] = SA[k];

                                // iter
                                i -= 1;
                                k -= 1;
                            }
                        } // End: Move all type B* suffixes to the correct position

                        // iter
                        c1 -= 1;
                    }
                    Bstar[(c0, c0 + 1)] = i - Bb[(c0, c0)] + 1;
                    Bb[(c0, c0)] = i; // end point

                    // iter
                    c0 -= 1;
                }
            } // End: Calculate the index of start/end point of each bucket
        }

        return new SortTypeBstarResult { A = A, B = B, m = m };
    }
}
