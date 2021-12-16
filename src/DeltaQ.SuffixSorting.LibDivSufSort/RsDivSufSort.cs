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

    //[DebuggerDisplay("")]
    //public ref struct SAPtr
    //{
    //    public readonly Index Index;
    //    public SAPtr(Index idx)
    //    {
    //        this.Index = idx;
    //    }
    //}

    public ref struct IntAccessor
    {
        public readonly ReadOnlySpan<byte> span;
        public IntAccessor(ReadOnlySpan<byte> span) => this.span = span;

        public readonly int this[Idx index] => span[index];
        public readonly int Length => span.Length;
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
                        sssort(T, SA, PAb, i, j, buf, bufsize, 2, n, SA[i] == (m - 1));
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

    private const Idx SS_BLOCKSIZE = 1024;

    /// <summary>
    /// Substring sort
    /// </summary>
    private static void sssort(IntAccessor T, Span<int> SA, SAPtr PA, SAPtr first, SAPtr last, SAPtr buf, Idx bufsize, Idx depth, Idx n, bool lastsuffix)
    {
        // Note: in most of this file "PA" seems to mean "Partition Array" - we're
        // working on a slice of SA. This is also why SA (or a mutable reference to it)
        // is passed around, so we don't run into lifetime issues.

        SAPtr a;
        SAPtr b;
        SAPtr middle;
        SAPtr curbuf;
        Idx j;
        Idx k;
        Idx curbufsize;
        Idx limit;
        Idx i;

        if (lastsuffix)
        {
            first += 1;
        }

        limit = ss_isqrt(last - first);
        if ((bufsize < SS_BLOCKSIZE) && (bufsize < (last - first)) && (bufsize < limit))
        {
            if (SS_BLOCKSIZE < limit)
            {
                limit = SS_BLOCKSIZE;
            }
            middle = last - limit;
            buf = middle;
            bufsize = limit;
        }
        else
        {
            middle = last;
            limit = 0;
        }

        // ESPRESSO
        a = first;
        i = 0;
        while (SS_BLOCKSIZE < (middle - a))
        {
            crosscheck($"ss_mintrosort (espresso) a={a - PA} depth={depth}");
            ss_mintrosort(T, SA, PA, a, a + SS_BLOCKSIZE, depth);

            curbufsize = (last - (a + SS_BLOCKSIZE));
            curbuf = a + SS_BLOCKSIZE;
            if (curbufsize <= bufsize)
            {
                curbufsize = bufsize;
                curbuf = buf;
            }

            // FRESCO
            b = a;
            k = SS_BLOCKSIZE;
            j = i;
            while ((j & 1) > 0)
            {
                crosscheck($"ss_swapmerge {k}");
                ss_swapmerge(T, SA, PA, b - k, b, b + k, curbuf, curbufsize, depth);

                // iter
                b -= k;
                k <<= 1;
                j >>= 1;
            }

            // iter
            a += SS_BLOCKSIZE;
            i += 1;
        }

        crosscheck($"ss_mintrosort (pre-mariachi) a={a - PA} depth={depth}");
        ss_mintrosort(T, SA, PA, a, middle, depth);

        SA_dump(SA[first..last], "pre-mariachi");

        // MARIACHI
        k = SS_BLOCKSIZE;
        while (i != 0)
        {
            if ((i & 1) > 0)
            {
                SA_dump(SA[first..last], "in-mariachi pre-swap");
                crosscheck($"a={a - first} middle={middle - first} bufsize={bufsize} depth={depth}");
                ss_swapmerge(T, SA, PA, a - k, a, middle, buf, bufsize, depth);
                SA_dump(SA[first..last], "in-mariachi post-swap");
                a -= k;
            }

            // iter
            k <<= 1;
            i >>= 1;
        }
        SA_dump(SA[first..last], "post-mariachi");

        if (limit != 0)
        {
            crosscheck("ss_mintrosort limit!=0");
            ss_mintrosort(T, SA, PA, middle, last, depth);
            SA_dump(SA[first..last], "post-mintrosort limit!=0");
            ss_inplacemerge(T, SA, PA, first, middle, last, depth);
            SA_dump(SA[first..last], "post-inplacemerge limit!=0");
        }
        SA_dump(SA[first..last], "post-limit!=0");

        if (lastsuffix)
        {
            crosscheck("lastsuffix!");

            // Insert last type B* suffix
            Span<Idx> PAi = stackalloc Idx[2] { SA[PA + SA[first - 1]], n - 2 };
            //let mut PAi:[Idx; 2] = [SA[PA + SA[first - 1]], n - 2];
            //let SAI = SuffixArray(&mut PAi);

            a = first;
            i = SA[first - 1];

            // CELINE
            while ((a < last) && ((SA[a] < 0) || (0 < ss_compare(T, PAi, (SAPtr)0, SA, PA + SA[a], depth))))
            {
                // body
                SA[a - 1] = SA[a];

                // iter
                a += 1;
            }
            SA[a - 1] = i;
        }
    }

    /// <summary>
    /// Compare two suffixes
    /// </summary>
    private static int ss_compare(IntAccessor T, Span<int> SAp1, SAPtr p1, Span<int> SAp2, SAPtr p2, Idx depth)
    {
        //TODO: possible perf improvement - JZ

        var U1 = depth + SAp1[p1];
        var U2 = depth + SAp2[p2];
        var U1n = SAp1[p1 + 1] + 2;
        var U2n = SAp2[p2 + 1] + 2;

        while ((U1 < U1n) && (U2 < U2n) && (T[U1] == T[U2]))
        {
            U1 += 1;
            U2 += 1;
        }

        if (U1 < U1n)
        {
            if (U2 < U2n)
            {
                return T[U1] - T[U2];
            }
            else
            {
                return 1;
            }
        }
        else
        {
            if (U2 < U2n)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }
    }

    private static void ss_inplacemerge(IntAccessor T, Span<int> SA, SAPtr PA, SAPtr first, SAPtr middle, SAPtr last, Idx depth)
    {
        SAPtr p;
        SAPtr a;
        SAPtr b;
        Idx len;
        Idx half;
        Idx q;
        Idx r;
        Idx x;

        var original_first = first;
        var original_last = last;

        SA_dump(SA[original_first..original_last], "inplacemerge start");

        // FERRIS
        while (true)
        {
            if (SA[last - 1] < 0)
            {
                x = 1;
                p = PA + ~SA[last - 1];
            }
            else
            {
                x = 0;
                p = PA + SA[last - 1];
            }

            // LOIS
            a = first;
            len = (middle - first)/*.0*/;
            half = len >> 1;
            r = -1;
            while (0 < len)
            {
                b = a + half;
                q = ss_compare(T, SA, PA + (0 <= SA[b] ? SA[b] : ~SA[b]), SA, p, depth);
                if (q < 0)
                {
                    a = b + 1;
                    half -= (len & 1) ^ 1;
                }
                else
                {
                    r = q;
                }

                // iter
                len = half;
                half >>= 1;
            }
            SA_dump(SA[original_first..original_last], "post-lois");

            if (a < middle)
            {
                if (r == 0)
                {
                    SA[a] = ~SA[a];
                }
                ss_rotate(SA, a, middle, last);
                SA_dump(SA[original_first..original_last], "post-rotate");
                last -= middle - a;
                middle = a;
                if (first == middle)
                {
                    break;
                }
            }

            last -= 1;
            if (x != 0)
            {
                // TIMMY
                last -= 1;
                while (SA[last] < 0)
                {
                    last -= 1;
                }
                SA_dump(SA[original_first..original_last], "post-timmy");
            }
            if (middle == last)
            {
                break;
            }

            SA_dump(SA[original_first..original_last], "ferris-wrap");
        }
    }

    private static void ss_rotate(Span<int> SA, SAPtr first, SAPtr middle, SAPtr last)
    {
        SAPtr a;
        SAPtr b;
        Idx t;
        Idx l;
        Idx r;

        var original_first = first;
        var original_last = last;

        l = (middle - first)/*.0*/;
        r = (last - middle)/*.0*/;

        SA_dump(SA[original_first..original_last], "pre-brendan");

        // BRENDAN
        while ((0 < l) && (0 < r))
        {
            if (l == r)
            {
                ss_blockswap(SA, first, middle, l);
                SA_dump(SA[original_first..original_last], "post-blockswap");
                break;
            }

            if (l < r)
            {
                a = last - 1;
                b = middle - 1;
                t = SA[a];

                // ALICE
                while (true)
                {
                    SA[a] = SA[b];
                    a -= 1;
                    SA[b] = SA[a];
                    b -= 1;
                    if (b < first)
                    {
                        SA[a] = t;
                        last = a;
                        r -= l + 1;
                        if (r <= l)
                        {
                            break;
                        }
                        a -= 1;
                        b = middle - 1;
                        t = SA[a];
                    }
                }
                SA_dump(SA[original_first..original_last], "post-alice");
            }
            else
            {
                a = first;
                b = middle;
                t = SA[a];
                // ROBERT
                while (true)
                {
                    SA[a] = SA[b];
                    a += 1;
                    SA[b] = SA[a];
                    b += 1;
                    if (last <= b)
                    {
                        SA[a] = t;
                        first = a + 1;

                        l -= r + 1;
                        if (l <= r)
                        {
                            break;
                        }
                        a += 1;
                        b = middle;
                        t = SA[a];
                    }
                }
                SA_dump(SA[original_first..original_last], "post-robert");
            }
        }
    }

    private static void ss_blockswap(Span<int> SA, SAPtr a, SAPtr b, Idx n)
    {
        for (int i = 0; i < n; i++)
        {
            SA.Swap(a + i, b + i);
        }
    }

    /// D&C based merge
    private static void ss_swapmerge(IntAccessor T, Span<int> SA, SAPtr PA, SAPtr first, SAPtr middle, SAPtr last, SAPtr buf, Idx bufsize, Idx depth)
    {
        static Idx get_idx(Idx a) => 0 <= a ? a : ~a;

        void merge_check(IntAccessor T, Span<int> SA, Idx a, Idx b, Idx c)
        {
            crosscheck($"mc c={c}");
            if (((c & 1) > 0) || (((c & 2) > 0) && (ss_compare(T, SA, PA + get_idx(SA[a - 1]), SA, PA + SA[a], depth) == 0)))
            {
                crosscheck($"swapping a-first={a - first}");
                SA[a] = ~SA[a];
            }
            if (((c & 4) > 0) && (ss_compare(T, SA, PA + get_idx(SA[b - 1]), SA, PA + SA[b], depth) == 0))
            {
                crosscheck($"swapping b-first={b - first}");
                SA[b] = ~SA[b];
            }
        }

        //MergeStack is the same as SsStack
        using var stackOwner = SpanOwner<SsStackItem>.Allocate(MERGE_STACK_SIZE, AllocationMode.Clear);
        var stack = new SsStack(stackOwner.Span);

        SAPtr l;
        SAPtr r;
        SAPtr lm;
        SAPtr rm;

        Idx m;
        Idx len;
        Idx half;
        Idx check;
        Idx next;

        // BARBARIAN
        check = 0;
        while (true)
        {
            crosscheck($"barbarian check={check}");
            SA_dump(SA[first..last], "ss_swapmerge barbarian");
            SA_dump(SA[buf..(buf + bufsize)], "ss_swapmerge barbarian buf");
            if ((last - middle) <= bufsize)
            {
                crosscheck("<=bufsize");
                if ((first < middle) && (middle < last))
                {
                    crosscheck("f<m&&m<l");
                    ss_mergebackward(T, SA, PA, first, middle, last, buf, depth);
                    SA_dump(SA[first..last], "ss_swapmerge post-mergebackward");
                    SA_dump(SA[buf..(buf + bufsize)], "ss_swapmerge post-mergebackward buf");
                }
                merge_check(T, SA, first, last, check);

                SA_dump(SA[first..last], "ss_swapmerge pop 1");
                if (!stack.Pop(ref first, ref middle, ref last, ref check))
                {
                    return;
                }
                SA_dump(SA[first..last], "ss_swapmerge pop 1 survived");
                continue;
            }

            if ((middle - first) <= bufsize)
            {
                crosscheck("m-f<=bufsize");
                if (first < middle)
                {
                    crosscheck("f<m");
                    ss_mergeforward(T, SA, PA, first, middle, last, buf, depth);
                    SA_dump(SA[first..last], "after mergeforward");
                }
                merge_check(T, SA, first, last, check);
                SA_dump(SA[first..last], "ss_swapmerge pop 2");
                if (!stack.Pop(ref first, ref middle, ref last, ref check))
                {
                    return;
                }
                continue;
            }

            // OLANNA
            m = 0;
            len = Math.Min(middle - first, last - middle);
            half = len >> 1;
            while (0 < len)
            {
                crosscheck($"in-olanna len={len} half={half}");
                if (ss_compare(
                    T,
                    SA,
                    PA + get_idx(SA[middle + m + half]),
                    SA,
                    PA + get_idx(SA[middle - m - half - 1]),
                    depth) < 0)
                {
                    m += half + 1;
                    half -= (len & 1) ^ 1;
                }

                // iter
                len = half;
                half >>= 1;
            }

            if (0 < m)
            {
                crosscheck($"0 < m, m={m}");
                lm = middle - m;
                rm = middle + m;
                ss_blockswap(SA, lm, middle, m);
                r = middle;
                l = middle;
                next = 0;
                if (rm < last)
                {
                    if (SA[rm] < 0)
                    {
                        SA[rm] = ~SA[rm];
                        if (first < lm)
                        {
                            // KOOPA
                            l -= 1;
                            while (SA[l] < 0)
                            {
                                l -= 1;
                            }
                            crosscheck($"post-koopa l-first={l - first}");
                            next |= 4;
                            crosscheck($"post-koopa next={next}");
                        }
                        next |= 1;
                    }
                    else if (first < lm)
                    {
                        // MUNCHER
                        while (SA[r] < 0)
                        {
                            r += 1;
                        }
                        crosscheck($"post-muncher r-first={r - first}");
                        next |= 2;
                    }
                }

                if ((l - first) <= (last - r))
                {
                    crosscheck("post-muncher l-f<l-r");
                    stack.Push(r, rm, last, (next & 3) | (check & 4));
                    middle = lm;
                    last = l;
                    crosscheck($"post-muncher check was={check} next was={next}");
                    check = (check & 3) | (next & 4);
                    crosscheck($"post-muncher check  is={check} next  is={next}");
                }
                else
                {
                    crosscheck("post-muncher not l-f<l-r");
                    if (((next & 2) > 0) && (r == middle))
                    {
                        crosscheck($"post-muncher next ^= 6 old={next}");
                        next ^= 6;
                        crosscheck($"post-muncher next ^= 6 new={next}");
                    }
                    stack.Push(first, lm, l, (check & 3) | (next & 4));
                    first = r;
                    middle = rm;
                    crosscheck($"post-muncher not, check was={check} next was={next}");
                    check = (next & 3) | (check & 4);
                    crosscheck($"post-muncher not, check  is={check} next  is={next}");
                }
            }
            else
            {
                if (ss_compare(
                    T,
                    SA,
                    PA + get_idx(SA[middle - 1]),
                    SA,
                    PA + SA[middle],
                    depth) == 0)
                {
                    SA[middle] = ~SA[middle];
                }
                merge_check(T, SA, first, last, check);
                SA_dump(SA[first..last], "ss_swapmerge pop 3");
                if (!stack.Pop(ref first, ref middle, ref last, ref check))
                {
                    return;
                }
            }
        }
    }

    /// Merge-backward with internal buffer
    private static void ss_mergebackward(IntAccessor T, Span<int> SA, SAPtr PA, SAPtr first, SAPtr middle, SAPtr last, SAPtr buf, Idx depth)
    {
        SAPtr p1;
        SAPtr p2;
        SAPtr a;
        SAPtr b;
        SAPtr c;
        SAPtr bufend;

        Idx t;
        Idx r;
        Idx x;

        bufend = buf + (last - middle) - 1;
        ss_blockswap(SA, buf, middle, (last - middle));

        x = 0;
        if (SA[bufend] < 0)
        {
            p1 = PA + ~SA[bufend];
            x |= 1;
        }
        else
        {
            p1 = PA + SA[bufend];
        }
        if (SA[middle - 1] < 0)
        {
            p2 = PA + ~SA[middle - 1];
            x |= 2;
        }
        else
        {
            p2 = PA + SA[middle - 1];
        }

        // MARTIN
        a = last - 1;
        t = SA[a];
        b = bufend;
        c = middle - 1;
        while (true)
        {
            r = ss_compare(T, SA, p1, SA, p2, depth);
            if (0 < r)
            {
                if ((x & 1) > 0)
                {
                    // BAPTIST
                    while (true)
                    {
                        SA[a] = SA[b];
                        a -= 1;
                        SA[b] = SA[a];
                        b -= 1;

                        // cond
                        if (!(SA[b] < 0))
                        {
                            break;
                        }
                    }
                    x ^= 1;
                }
                SA[a] = SA[b];
                a -= 1;
                if (b <= buf)
                {
                    SA[buf] = t;
                    break;
                }
                SA[b] = SA[a];
                b -= 1;
                if (SA[b] < 0)
                {
                    p1 = PA + ~SA[b];
                    x |= 1;
                }
                else
                {
                    p1 = PA + SA[b];
                }
            }
            else if (r < 0)
            {
                if ((x & 2) > 0)
                {
                    // JULES
                    while (true)
                    {
                        SA[a] = SA[c];
                        a -= 1;
                        SA[c] = SA[a];
                        c -= 1;

                        // cond
                        if (~SA[c] < 0)
                        {
                            break;
                        }
                    }
                    x ^= 2;
                }
                SA[a] = SA[c];
                a -= 1;
                SA[c] = SA[a];
                c -= 1;
                if (c < first)
                {
                    // GARAMOND
                    while (buf < b)
                    {
                        SA[a] = SA[b];
                        a -= 1;
                        SA[b] = SA[a];
                        b -= 1;
                    }
                    SA[a] = SA[b];
                    SA[b] = t;
                    break;
                }
                if (SA[c] < 0)
                {
                    p2 = PA + ~SA[c];
                    x |= 2;
                }
                else
                {
                    p2 = PA + SA[c];
                }
            }
            else
            {
                if ((x & 1) > 0)
                {
                    // XAVIER
                    while (true)
                    {
                        SA[a] = SA[b];
                        a -= 1;
                        SA[b] = SA[a];
                        b -= 1;
                        if (!(SA[b] < 0))
                        {
                            break;
                        }
                    }
                    x ^= 1;
                }
                SA[a] = ~SA[b];
                a -= 1;
                if (b <= buf)
                {
                    SA[buf] = t;
                    break;
                }
                SA[b] = SA[a];
                b -= 1;
                if ((x & 2) > 0)
                {
                    // WALTER
                    while (true)
                    {
                        SA[a] = SA[c];
                        a -= 1;
                        SA[c] = SA[a];
                        c -= 1;

                        // cond
                        if (!(SA[c] < 0))
                        {
                            break;
                        }
                    }
                    x ^= 2;
                }
                SA[a] = SA[c];
                a -= 1;
                SA[c] = SA[a];
                c -= 1;
                if (c < first)
                {
                    // ZENITH
                    while (buf < b)
                    {
                        SA[a] = SA[b];
                        a -= 1;
                        SA[b] = SA[a];
                        b -= 1;
                    }
                    SA[a] = SA[b];
                    SA[b] = t;
                    break;
                }
                if (SA[b] < 0)
                {
                    p1 = PA + ~SA[b];
                    x |= 1;
                }
                else
                {
                    p1 = PA + SA[b];
                }
                if (SA[c] < 0)
                {
                    p2 = PA + ~SA[c];
                    x |= 2;
                }
                else
                {
                    p2 = PA + SA[c];
                }
            }
        }
    }

    /// Merge-forward with internal buffer
    private static void ss_mergeforward(IntAccessor T, Span<int> SA, SAPtr PA, SAPtr first, SAPtr middle, SAPtr last, SAPtr buf, Idx depth)
    {
        SAPtr a;
        SAPtr b;
        SAPtr c;
        SAPtr bufend;
        Idx t;
        Idx r;

        SA_dump(SA[first..last], "ss_mergeforward start");

        bufend = buf + (middle - first) - 1;
        ss_blockswap(SA, buf, first, middle - first);

        // IGNACE
        a = first;
        t = SA[a];
        b = buf;
        c = middle;
        while (true)
        {
            r = ss_compare(T, SA, PA + SA[b], SA, PA + SA[c], depth);
            if (r < 0)
            {
                // RONALD
                while (true)
                {
                    SA[a] = SA[b];
                    a += 1;
                    if (bufend <= b)
                    {
                        SA[bufend] = t;
                        return;
                    }
                    SA[b] = SA[a];
                    b += 1;

                    // cond
                    if (!(SA[b] < 0))
                    {
                        break;
                    }
                }
            }
            else if (r > 0)
            {
                // JEREMY
                while (true)
                {
                    SA[a] = SA[c];
                    a += 1;
                    SA[c] = SA[a];
                    c += 1;
                    if (last <= c)
                    {
                        // TONY
                        while (b < bufend)
                        {
                            SA[a] = SA[b];
                            a += 1;
                            SA[b] = SA[a];
                            b += 1;
                        }
                        SA[a] = SA[b];
                        SA[b] = t;
                        return;
                    }

                    // cond (JEMERY)
                    if (!(SA[c] < 0))
                    {
                        break;
                    }
                }
            }
            else
            {
                SA[c] = ~SA[c];
                // JENS
                while (true)
                {
                    SA[a] = SA[b];
                    a += 1;
                    if (bufend <= b)
                    {
                        SA[bufend] = t;
                        return;
                    }
                    SA[b] = SA[a];
                    b += 1;

                    // cond (JENS)
                    if (!(SA[b] < 0))
                    {
                        break;
                    }
                }

                // DIMITER
                while (true)
                {
                    SA[a] = SA[c];
                    a += 1;
                    SA[c] = SA[a];
                    c += 1;
                    if (last <= c)
                    {
                        // MIDORI
                        while (b < bufend)
                        {
                            SA[a] = SA[b];
                            a += 1;
                            SA[b] = SA[a];
                            b += 1;
                        }
                        SA[a] = SA[b];
                        SA[b] = t;
                        return;
                    }

                    // cond (DIMITER)
                    if (!(SA[c] < 0))
                    {
                        break;
                    }
                }
            }
        }
    }

    private struct SsStackItem
    {
        public SAPtr a;
        public SAPtr b;
        public SAPtr c;
        public Idx d;
    }

    private const int SS_STACK_SIZE = 16;
    private const int MERGE_STACK_SIZE = 32;
    private ref struct SsStack
    {
        public readonly Span<SsStackItem> Items;
        public int Size;

        public SsStack(Span<SsStackItem> items)
        {
            Items = items;
            Size = 0;
        }

        public void Push(SAPtr a, SAPtr b, SAPtr c, Idx d)
        {
            Debug.Assert(Size < Items.Length);
            ref SsStackItem item = ref Items[Size++];
            item.a = a;
            item.b = b;
            item.c = c;
            item.d = d;
        }
        public bool Pop(ref SAPtr a, ref SAPtr b, ref SAPtr c, ref Idx d)
        {
            //Debug.Assert(Size > 0);
            if (Size == 0) return false;

            ref SsStackItem item = ref Items[--Size];
            a = item.a;
            b = item.b;
            c = item.c;
            d = item.d;
            return true;
        }
    }

    private const Idx SS_INSERTIONSORT_THRESHOLD = 8;

    private ref struct SpanOffsetAccessor<T>
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

    private ref struct ReadOnlySpanOffsetAccessor<T>
    {
        private readonly ReadOnlySpan<T> _span;
        private readonly int _offset;

        public ReadOnlySpanOffsetAccessor(ReadOnlySpan<T> span, int offset)
        {
            _span = span;
            _offset = offset;
        }

        public ref readonly T this[int index] => ref _span[_offset + index];
    }

    private ref struct TdPAStarAccessor
    {
        private readonly ReadOnlySpanOffsetAccessor<byte> _TO;
        private readonly ReadOnlySpan<int> _SA;
        private readonly ReadOnlySpan<int> _PA;
        private readonly IntAccessor _TD;

        public TdPAStarAccessor(ReadOnlySpan<byte> T, ReadOnlySpan<int> SA, int partitionOffset, int tdOffset)
        {
            _TO = new ReadOnlySpanOffsetAccessor<byte>(T, tdOffset);

            _SA = SA;
            _PA = SA[partitionOffset..];
            _TD = new(T[tdOffset..]);
        }

        public readonly int this[int index] => _TD[_PA[_SA[index]]];

        public readonly int AsOffset(int index) => _TO[index];
    }

    /// <summary>
    /// Multikey introsort for medium size groups
    /// </summary>
    private static void ss_mintrosort(IntAccessor T, Span<int> SA, SAPtr partitionOffset, SAPtr first, SAPtr last, Idx depth)
    {
        var PA = SA[partitionOffset..];

        using var stackOwner = SpanOwner<SsStackItem>.Allocate(SS_STACK_SIZE);
        var stack = new SsStack(stackOwner.Span);

        SAPtr a;
        SAPtr b;
        SAPtr c;
        SAPtr d;
        SAPtr e;
        SAPtr f;

        Idx s;
        Idx t;

        Idx limit;
        Idx v;
        Idx x = 0;

        // RENEE
        limit = ss_ilg(last - first);
        while (true)
        {
            if ((last - first) <= SS_INSERTIONSORT_THRESHOLD)
            {
                if (1 < (last - first))
                {
                    ss_insertionsort(T, SA, partitionOffset, first, last, depth);
                }
                if (!stack.Pop(ref first, ref last, ref depth, ref limit))
                {
                    return;
                }
                continue;
            }

            var tdOffset = depth;
            var TdPAStar = new TdPAStarAccessor(T.span, SA, partitionOffset, tdOffset);

            /*readonly*/
            var old_limit = limit;
            limit -= 1;
            if (old_limit == 0)
            {
                SA_dump(SA[first..last], "before heapsort");
                ss_heapsort(T, tdOffset, SA, partitionOffset, first, (last - first));
                SA_dump(SA[first..last], "after heapsort");
            }

            if (limit < 0)
            {
                a = first + 1;
                v = TdPAStar[first];

                // DAVE
                while (a < last)
                {
                    x = TdPAStar[a];
                    if (x != v)
                    {
                        if (1 < (a - first))
                        {
                            break;
                        }
                        v = x;
                        first = a;
                    }

                    // loop iter
                    a += 1;
                }

                if (TdPAStar.AsOffset(PA[SA[first]] - 1) < v)
                {
                    first = ss_partition(SA, partitionOffset, first, a, depth);
                }
                if ((a - first) <= (last - a))
                {
                    if (1 < (a - first))
                    {
                        stack.Push(a, last, depth, -1);
                        last = a;
                        depth += 1;
                        limit = ss_ilg(a - first);
                    }
                    else
                    {
                        first = a;
                        limit = -1;
                    }
                }
                else
                {
                    if (1 < (last - a))
                    {
                        stack.Push(first, a, depth + 1, ss_ilg(a - first));
                        first = a;
                        limit = -1;
                    }
                    else
                    {
                        last = a;
                        depth += 1;
                        limit = ss_ilg(a - first);
                    }
                }
                continue;
            }

            // choose pivot
            a = ss_pivot(T, tdOffset, SA, partitionOffset, first, last);
            v = TdPAStar[a];
            SA.Swap(first, a);

            // partition
            // NORA
            b = first;
            while (true)
            {
                b += 1;
                if (!(b < last))
                {
                    break;
                }
                x = TdPAStar[b];
                if (!(x == v))
                {
                    break;
                }
                // body
            }
            a = b;
            if ((a < last) && (x < v))
            {
                // STAN
                while (true)
                {
                    b += 1;
                    if (!(b < last))
                    {
                        break;
                    }
                    x = TdPAStar[b];
                    if (!(x <= v))
                    {
                        break;
                    }
                    // body
                    if (x == v)
                    {
                        SA.Swap(b, a);
                        a += 1;
                    }
                }
            }

            // NATHAN
            c = last;
            while (true)
            {
                c -= 1;
                if (!(b < c))
                {
                    break;
                }
                x = TdPAStar[c];
                if (!(x == v))
                {
                    break;
                }
                // body
            }
            d = c;
            if ((b < d) && (x > v))
            {
                // JACOB
                while (true)
                {
                    c -= 1;
                    if (!(b < c))
                    {
                        break;
                    }
                    x = TdPAStar[c];
                    if (!(x >= v))
                    {
                        break;
                    }
                    // body
                    if (x == v)
                    {
                        SA.Swap(c, d);
                        d -= 1;
                    }
                }
            }

            // RITA
            while (b < c)
            {
                SA.Swap(b, c);
                // ROMEO
                while (true)
                {
                    b += 1;
                    if (!(b < c))
                    {
                        break;
                    }
                    x = TdPAStar[b];
                    if (!(x <= v))
                    {
                        break;
                    }
                    // body
                    if (x == v)
                    {
                        SA.Swap(b, a);
                        a += 1;
                    }
                }
                // JULIET
                while (true)
                {
                    c -= 1;
                    if (!(b < c))
                    {
                        break;
                    }
                    x = TdPAStar[c];
                    if (!(x >= v))
                    {
                        break;
                    }
                    // body
                    if (x == v)
                    {
                        SA.Swap(c, d);
                        d -= 1;
                    }
                }
            }

            if (a <= d)
            {
                c = b - 1;
                s = (a - first)/*.0*/;
                t = (b - a)/*.0*/;
                if (s > t)
                {
                    s = t;
                }

                // JOSHUA
                e = first;
                f = b - s;
                while (0 < s)
                {
                    SA.Swap(e, f);
                    s -= 1;
                    e += 1;
                    f += 1;
                }
                s = (d - c)/*.0*/;
                t = (last - d - 1)/*.0*/;
                if (s > t)
                {
                    s = t;
                }
                // BERENICE
                e = b;
                f = last - s;
                while (0 < s)
                {
                    SA.Swap(e, f);
                    s -= 1;
                    e += 1;
                    f += 1;
                }

                a = first + (b - a);
                c = last - (d - c);
                b = v <= TdPAStar.AsOffset(PA[SA[a]] - 1) ? a : ss_partition(SA, partitionOffset, a, c, depth);

                if ((a - first) <= (last - c))
                {
                    if ((last - c) <= (c - b))
                    {
                        stack.Push(b, c, depth + 1, ss_ilg(c - b));
                        stack.Push(c, last, depth, limit);
                        last = a;
                    }
                    else if ((a - first) <= (c - b))
                    {
                        stack.Push(c, last, depth, limit);
                        stack.Push(b, c, depth + 1, ss_ilg(c - b));
                        last = a;
                    }
                    else
                    {
                        stack.Push(c, last, depth, limit);
                        stack.Push(first, a, depth, limit);
                        first = b;
                        last = c;
                        depth += 1;
                        limit = ss_ilg(c - b);
                    }
                }
                else
                {
                    if ((a - first) <= (c - b))
                    {
                        stack.Push(b, c, depth + 1, ss_ilg(c - b));
                        stack.Push(first, a, depth, limit);
                        first = c;
                    }
                    else if ((last - c) <= (c - b))
                    {
                        stack.Push(first, a, depth, limit);
                        stack.Push(b, c, depth + 1, ss_ilg(c - b));
                        first = c;
                    }
                    else
                    {
                        stack.Push(first, a, depth, limit);
                        stack.Push(c, last, depth, limit);
                        first = b;
                        last = c;
                        depth += 1;
                        limit = ss_ilg(c - b);
                    }
                }
            }
            else
            {
                limit += 1;
                if (TdPAStar.AsOffset(PA[SA[first]] - 1) < v)
                {
                    first = ss_partition(SA, partitionOffset, first, last, depth);
                    limit = ss_ilg(last - first);
                }
                depth += 1;
            }
        }
    }

    /// <summary>
    /// Returns the pivot element
    /// </summary>
    private static SAPtr ss_pivot(IntAccessor T, Idx Td, Span<int> SA, SAPtr PA, SAPtr first, SAPtr last)
    {
        Idx t = (last - first)/*.0*/;
        SAPtr middle = first + (t / 2);

        if (t <= 512)
        {
            if (t <= 32)
            {
                return ss_median3(T, Td, SA, PA, first, middle, last - 1);
            }
            else
            {
                t >>= 2;
                return ss_median5(
                    T,
                    Td,
                    SA,
                    PA,
                    first,
                    first + t,
                    middle,
                    last - 1 - t,
                    last - 1);
            }
        }

        t >>= 3;
        first = ss_median3(T, Td, SA, PA, first, first + t, first + (t << 1));
        middle = ss_median3(T, Td, SA, PA, middle - t, middle, middle + t);
        last = ss_median3(T, Td, SA, PA, last - 1 - (t << 1), last - 1 - t, last - 1);

        return ss_median3(T, Td, SA, PA, first, middle, last);
    }

    /// Returns the median of five elements
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SAPtr ss_median5(IntAccessor T, Idx Td, ReadOnlySpan<int> SA, SAPtr PA, SAPtr v1, SAPtr v2, SAPtr v3, SAPtr v4, SAPtr v5)
    {
        var get = new TdPAStarAccessor(T.span, SA, PA, Td);
        if (get[v2] > get[v3])
        {
            Swap(ref v2, ref v3);
        }
        if (get[v4] > get[v5])
        {
            Swap(ref v4, ref v5);
        }
        if (get[v2] > get[v4])
        {
            Swap(ref v2, ref v4);
            Swap(ref v3, ref v5);
        }
        if (get[v1] > get[v3])
        {
            Swap(ref v1, ref v3);
        }
        if (get[v1] > get[v4])
        {
            Swap(ref v1, ref v4);
            Swap(ref v3, ref v5);
        }
        if (get[v3] > get[v4])
        {
            return v4;
        }
        else
        {
            return v3;
        }
    }

    /// <summary>
    /// Returns the median of three elements
    /// </summary>
    private static int ss_median3(IntAccessor T, Idx Td, Span<int> SA, SAPtr PA, SAPtr v1, SAPtr v2, SAPtr v3)
    {
        //int get(int x) => T[Td + SA[PA + SA[x]]]
        var get = new TdPAStarAccessor(T.span, SA, PA, Td);

        if (get[v1] > get[v2])
        {
            Swap(ref v1, ref v2);
        }

        if (get[v2] > get[v3])
        {
            if (get[v1] > get[v3])
            {
                return v1;
            }
            else
            {
                return v3;
            }
        }
        else
        {
            return v2;
        }
    }

    /// Binary partition for substrings.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SAPtr ss_partition(Span<int> SA, SAPtr paOffset, SAPtr first, SAPtr last, Idx depth)
    {
        Span<int> PA = SA[paOffset..];

        // JIMMY
        var a = first - 1;
        var b = last;

        while (true)
        {
            // JANINE
            while (true)
            {
                a += 1;
                if (!(a < b))
                {
                    break;
                }
                if (!((PA[SA[a]] + depth) >= (PA[SA[a] + 1] + 1)))
                {
                    break;
                }

                // loop body
                SA[a] = ~SA[a];
            }

            // GEORGIO
            while (true)
            {
                b -= 1;
                if (!(a < b))
                {
                    break;
                }
                if (!((PA[SA[b]] + depth) < (PA[SA[b] + 1] + 1)))
                {
                    break;
                }

                // loop body is empty
            }

            if (b <= a)
            {
                break;
            }

            var t = ~SA[b];
            SA[b] = SA[a];
            SA[a] = t;
        }

        if (first < a)
        {
            SA[first] = ~SA[first];
        }
        return a;
    }

    private static void ss_insertionsort(IntAccessor T, Span<int> SA, int PA, int first, int last, int depth)
    {
        SAPtr i;
        SAPtr j;
        Idx t;
        Idx r;

        i = last - 2;
        // for 1
        while (first <= i)
        {
            t = SA[i];
            j = i + 1;

            // for 2
            while (true)
            {
                // cond for 2
                r = ss_compare(T, SA, PA + t, SA, PA + SA[j], depth);
                if (!(0 < r))
                {
                    break;
                }

                // body for 2

                // do while
                while (true)
                {
                    SA[j - 1] = SA[j];

                    j += 1;
                    if (!((j < last) && SA[j] < 0))
                    {
                        break;
                    }
                }

                if (last <= j)
                {
                    break;
                }

                // iter for 2 (empty)
            }

            if (r == 0)
            {
                SA[j] = ~SA[j];
            }
            SA[j - 1] = t;

            // iter
            i -= 1;
        }
    }

    /// <summary>
    /// Fast log2, using lookup tables
    /// </summary>
    private static int ss_ilg(int n)
    {
        if ((n & 0xff00) > 0)
        {
            return 8 + lg_table[((n >> 8) & 0xff)];
        }
        else
        {
            return 0 + lg_table[((n >> 0) & 0xff)];
        }
    }

    /// Simple top-down heapsort.
    private static void ss_heapsort(IntAccessor T, Idx tdOffset, Span<int> SA_top, SAPtr paOffset, SAPtr first, Idx size)
    {
        Idx i;
        var m = size;
        Idx t;

        var Td = new IntAccessor(T.span[tdOffset..]);
        var PA = SA_top[paOffset..];
        var SA = SA_top[first..];

        if ((size % 2) == 0)
        {
            m -= 1;
            if (Td[PA[SA[m / 2]]] < Td[PA[SA[m]]])
            {
                SA.Swap(m, m / 2);
            }
        }

        // LADY
        //TODO: checkme
        for (i = (m / 2) - 1; i >= 0; i--)
        {
            ss_fixdown(Td, PA, SA, i, m);
        }

        if ((size % 2) == 0)
        {
            SA.Swap(0, m);
            ss_fixdown(Td, PA, SA, 0, m);
        }

        // TRUMPET
        //TODO: checkme
        for (i = m - 1; i > 0; i--)
        {
            t = SA[0];
            SA[0] = SA[i];
            ss_fixdown(Td, PA, SA, 0, i);
            SA[i] = t;
        }
    }

    private static void ss_fixdown(IntAccessor Td, Span<int> PA, Span<int> SA, Idx i, Idx size)
    {
        Idx j, v, c, d, e, k;

        v = SA[i];
        c = Td[PA[v]];

        // BEAST
        while (true)
        {
            // cond
            j = 2 * i + 1;
            if (!(j < size))
            {
                break;
            }

            // body
            k = j;
            j += 1;

            d = Td[PA[SA[k]]];
            e = Td[PA[SA[j]]];
            if (d < e)
            {
                k = j;
                d = e;
            }
            if (d <= c)
            {
                break;
            }

            // iter
            SA[i] = SA[k];
            i = k;
        }
        SA[i] = v;
    }

    private static readonly Idx[] sqq_table_array = new[]
    {
          0,  16,  22,  27,  32,  35,  39,  42,  45,  48,  50,  53,  55,  57,  59,  61,
         64,  65,  67,  69,  71,  73,  75,  76,  78,  80,  81,  83,  84,  86,  87,  89,
         90,  91,  93,  94,  96,  97,  98,  99, 101, 102, 103, 104, 106, 107, 108, 109,
        110, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126,
        128, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142,
        143, 144, 144, 145, 146, 147, 148, 149, 150, 150, 151, 152, 153, 154, 155, 155,
        156, 157, 158, 159, 160, 160, 161, 162, 163, 163, 164, 165, 166, 167, 167, 168,
        169, 170, 170, 171, 172, 173, 173, 174, 175, 176, 176, 177, 178, 178, 179, 180,
        181, 181, 182, 183, 183, 184, 185, 185, 186, 187, 187, 188, 189, 189, 190, 191,
        192, 192, 193, 193, 194, 195, 195, 196, 197, 197, 198, 199, 199, 200, 201, 201,
        202, 203, 203, 204, 204, 205, 206, 206, 207, 208, 208, 209, 209, 210, 211, 211,
        212, 212, 213, 214, 214, 215, 215, 216, 217, 217, 218, 218, 219, 219, 220, 221,
        221, 222, 222, 223, 224, 224, 225, 225, 226, 226, 227, 227, 228, 229, 229, 230,
        230, 231, 231, 232, 232, 233, 234, 234, 235, 235, 236, 236, 237, 237, 238, 238,
        239, 240, 240, 241, 241, 242, 242, 243, 243, 244, 244, 245, 245, 246, 246, 247,
        247, 248, 248, 249, 249, 250, 250, 251, 251, 252, 252, 253, 253, 254, 254, 255
    };
    private static ReadOnlySpan<Idx> sqq_table => sqq_table_array;

    /// <summary>
    /// Fast sqrt, using lookup tables
    /// </summary>
    private static int ss_isqrt(int x)
    {
        if (x >= (SS_BLOCKSIZE * SS_BLOCKSIZE))
        {
            return SS_BLOCKSIZE;
        }

        Idx e;
        if ((x & 0xffff_0000) > 0)
        {
            if ((x & 0xff00_0000) > 0)
            {
                e = 24 + lg_table[((x >> 24) & 0xff)];
            }
            else
            {
                e = 16 + lg_table[((x >> 16) & 0xff)];
            }
        }
        else
        {
            if ((x & 0x0000_ff00) > 0)
            {
                e = 8 + lg_table[(((x >> 8) & 0xff))];
            }
            else
            {
                e = 0 + lg_table[(((x >> 0) & 0xff))];
            }
        };

        Idx y;
        if (e >= 16)
        {
            y = sqq_table[(x >> ((e - 6) - (e & 1)))] << ((e >> 1) - 7);
            if (e >= 24)
            {
                y = (y + 1 + x / y) >> 1;
            }
            y = (y + 1 + x / y) >> 1;
        }
        else if (e >= 8)
        {
            y = (sqq_table[(x >> ((e - 6) - (e & 1)))] >> (7 - (e >> 1))) + 1;
        }
        else
        {
            return sqq_table[x] >> 4;
        }

        if (x < (y * y))
        {
            return y - 1;
        }
        else
        {
            return y;
        }
    }
}
