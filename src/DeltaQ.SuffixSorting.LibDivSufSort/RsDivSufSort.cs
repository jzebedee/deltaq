﻿using Microsoft.Toolkit.HighPerformance.Buffers;
using System;
using System.Diagnostics;
using Idx = System.Int32;
using SAPtr = System.Int32;

namespace DeltaQ.SuffixSorting.LibDivSufSort
{
    public static class DivSufSort
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
                    //case 2:
                    //    if(T[0] < T[1])
                    //    {
                    //        SA.copy
                    //    }
                    //    break;
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

                            //TODO: check this
                            //SA[j] = !s;
                            SA[j] = ~s;
                            s -= 1;
                            c0 = T[s];
                            if ((0 < s) && (T[s - 1] > c0))
                            {
                                //TODO: check this
                                //s = !s;
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
                            //TODO: check this
                            //SA[j] = !s;
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
                            //TODO: check this
                            //s = !s;
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
                        //TODO: check this
                        //SA[i] = !s;
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

            public int this[Idx index] => span[index];
            public int Length => span.Length;
        }

        //fn sort_typeBstar(T: &Text, SA: &mut SuffixArray) -> SortTypeBstarResult {
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

            //JZ: so far, so good

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
                //TODO: get rid of this Enumerable
                //foreach(var ini in Enumerable.Range(0, m - 2).Reverse())
                for (i = m - 2; i > 0; i--)
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
                            //SA_dump!(&SA.range(i..j), "sssort(A)");
                            sssort(
                                T,
                                SA,
                                PAb,
                                ref i,
                                (SAPtr)j,
                                ref buf,
                                ref bufsize,
                                2,
                                n,
                                SA[i] == (m - 1));
                            //SA_dump!(&SA.range(i..j), "sssort(B)");
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
                        //TODO: check this
                        //SA[i] = !SA[i];
                        SA[i] = ~SA[i];
                        {
                            var idx = ISAb + SA[i];
                            SA[idx] = j;
                        }

                        i -= 1;
                        if (!(SA[i] < 0))
                        {
                            break;
                        }
                    }
                    {
                        var idx = ISAb + SA[i];
                        SA[idx] = j;
                    }

                    i -= 1;
                }

                // Construct the inverse suffix array of type B* suffixes using trsort.
                trsort(ISAb, SA, m, 1);

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
        private static void sssort(IntAccessor T, Span<int> SA, SAPtr PA, ref SAPtr first, SAPtr last, ref SAPtr buf, ref Idx bufsize, Idx depth, Idx n, bool lastsuffix)
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
                crosscheck("ss_mintrosort (espresso) a={} depth={}", a - PA, depth);
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
                    crosscheck("ss_swapmerge {}", k);
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

            crosscheck("ss_mintrosort (pre-mariachi) a={} depth={}", a - PA, depth);
            ss_mintrosort(T, SA, PA, a, middle, depth);

            //SA_dump!(&SA.range(first..last), "pre-mariachi");

            // MARIACHI
            k = SS_BLOCKSIZE;
            while (i != 0)
            {
                if ((i & 1) > 0)
                {
                    //SA_dump!(&SA.range(first..last), "in-mariachi pre-swap");
                    crosscheck(
                        "a={} middle={} bufsize={} depth={}",
                        a - first,
                        middle - first,
                        bufsize,
                        depth
                    );
                    ss_swapmerge(T, SA, PA, a - k, a, middle, buf, bufsize, depth);
                    //SA_dump!(&SA.range(first..last), "in-mariachi post-swap");
                    a -= k;
                }

                // iter
                k <<= 1;
                i >>= 1;
            }
            //SA_dump!(&SA.range(first..last), "post-mariachi");

            if (limit != 0)
            {
                crosscheck("ss_mintrosort limit!=0");
                ss_mintrosort(T, SA, PA, middle, last, depth);
                //SA_dump!(&SA.range(first..last), "post-mintrosort limit!=0");
                ss_inplacemerge(T, SA, PA, first, middle, last, depth);
                //SA_dump!(&SA.range(first..last), "post-inplacemerge limit!=0");
            }
            //SA_dump!(&SA.range(first..last), "post-limit!=0");

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

        private static int ss_compare(IntAccessor t, Span<int> pAi, int v1, Span<int> sA, int v2, int depth)
        {
            throw new NotImplementedException();
        }

        private static void ss_inplacemerge(IntAccessor t, Span<int> sA, int pA, int first, int middle, int last, int depth)
        {
            throw new NotImplementedException();
        }

        private static void ss_swapmerge(IntAccessor t, Span<int> sA, int pA, int v1, int b, int v2, int curbuf, int curbufsize, int depth)
        {
            throw new NotImplementedException();
        }


        private struct SsStackItem
        {
            public SAPtr a;
            public SAPtr b;
            public SAPtr c;
            public Idx d;
        }

        private const int SS_STACK_SIZE = 16;
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

        private ref struct TdPAStarAccessor
        {
            private readonly Span<int> _SA;
            private readonly Span<int> _PA;
            private readonly IntAccessor _TD;

            public TdPAStarAccessor(ReadOnlySpan<byte> T, Span<int> SA, int partitionOffset, int tdOffset)
            {
                _SA = SA;
                _PA = SA[partitionOffset..];
                _TD = new(T[tdOffset..]);
            }

            public int this[int index] => _TD[_PA[_SA[index]]];
        }

        /// <summary>
        /// Multikey introsort for medium size groups
        /// </summary>
        private static void ss_mintrosort(IntAccessor T, Span<int> SA, SAPtr partitionOffset, /*ref*/ SAPtr first, /*ref*/ SAPtr last, /*ref*/ Idx depth)
        {
            //PA($x) => 
            var PA = SA[partitionOffset..];//new SpanOffsetAccessor<int>(SA, PA);

            var stack = new SsStack();

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

                //Td!($x) => T[Td + $x]
                var tdOffset = depth;
                var Td = T.span[tdOffset..];

                //TdPAStar!($x) => Td!(PA!(SA[$x]))
                //TdPAStar!($x) => T[Td + SA[PA + SA[$x]]]
                //var TdPAStar = Td[PA[SA[$x]]];
                var TdPAStar = new TdPAStarAccessor(T.span, SA, partitionOffset, tdOffset);

                /*readonly*/
                var old_limit = limit;
                limit -= 1;
                if (old_limit == 0)
                {
                    //SA_dump!(&SA.range(first..last), "before heapsort");
                    ss_heapsort(T, tdOffset, SA, partitionOffset, first, (last - first));
                    //SA_dump!(&SA.range(first..last), "after heapsort");
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

                    if (Td[PA[SA[first]] - 1] < v)
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
                    b = v <= Td[PA[SA[a]] - 1] ? a : ss_partition(SA, partitionOffset, a, c, depth);

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
                    if (Td[PA[SA[first]] - 1] < v)
                    {
                        first = ss_partition(SA, partitionOffset, first, last, depth);
                        limit = ss_ilg(last - first);
                    }
                    depth += 1;
                }
            }
        }

        private static int ss_pivot(IntAccessor t, int td, Span<int> sA, int pA, int first, int last)
        {
            throw new NotImplementedException();
        }

        private static int ss_partition(Span<int> sA, int pA, int first, int a, int depth)
        {
            throw new NotImplementedException();
        }

        private static void ss_insertionsort(IntAccessor t, Span<int> sA, int pA, int first, int last, int depth)
        {
            throw new NotImplementedException();
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

        private static void ss_heapsort(IntAccessor t, int td, Span<int> sA, int pA, int first, object p)
        {
            throw new NotImplementedException();
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

        private static readonly int[] lg_table_array = new[]
        {
         -1,0,1,1,2,2,2,2,3,3,3,3,3,3,3,3,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,
          5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,
          6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,
          6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,
          7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
          7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
          7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
          7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7
        };
        private static ReadOnlySpan<int> lg_table => lg_table_array;

        private static int tr_ilg(int n)
        {
            if ((n & 0xffff_0000) > 0)
            {
                if ((n & 0xff00_0000) > 0)
                {
                    return 24 + lg_table[((n >> 24) & 0xff)];
                }
                else
                {
                    return 16 + lg_table[((n >> 16) & 0xff)];
                }
            }
            else
            {
                if ((n & 0x0000_ff00) > 0)
                {
                    return 8 + lg_table[((n >> 8) & 0xff)];
                }
                else
                {
                    return 0 + lg_table[((n >> 0) & 0xff)];
                }
            }
        }

        private ref struct Budget
        {
            public int Chance;
            public int Remain;
            public int IncVal;
            public int Count;

            public Budget(int chance, int incVal)
            {
                Chance = chance;
                Remain = incVal;
                IncVal = incVal;
                Count = 0;
            }

            public bool Check(int size)
            {
                if (size <= Remain)
                {
                    Remain -= size;
                    return true;
                }

                if (Chance == 0)
                {
                    Count += size;
                    return false;
                }

                Remain += IncVal - size;
                Chance -= 1;
                return true;
            }
        }

        /// Tandem repeat sort
        private static void trsort(SAPtr ISA, Span<int> SA, int n, int depth)
        {
            SAPtr ISAd;
            SAPtr first;
            SAPtr last;
            /*Index*/
            int t;
            /*Index*/
            int skip;
            /*Index*/
            int unsorted;
            Budget budget = new(tr_ilg(n) * 2 / 3, n);

            //macro_rules! ISA {
            //    ($x: expr) => {
            //        SA[ISA + $x]
            //    };
            //}

            //ref int getISA(int x) => ref SA[ISA + x];

            // JERRY
            ISAd = ISA + depth;
            while (-n < SA[0])
            {
                first = 0;
                skip = 0;
                unsorted = 0;

                // PETER
                while (true)
                {
                    t = SA[first];
                    if (t < 0)
                    {
                        first -= t;
                        skip += t;
                    }
                    else
                    {
                        if (skip != 0)
                        {
                            SA[first + skip] = skip;
                            skip = 0;
                        }
                        last = SA[ISA + (t)] + 1;
                        if (1 < (last - first))
                        {
                            budget.Count = 0;
                            tr_introsort(ISA, ref ISAd, SA, ref first, ref last, budget);
                            if (budget.Count != 0)
                            {
                                unsorted += budget.Count;
                            }
                            else
                            {
                                skip = first - last;
                            }
                        }
                        else if ((last - first) == 1)
                        {
                            skip = -1;
                        }
                        first = last;
                    }

                    // cond (PETER)
                    if (!(first < n))
                    {
                        break;
                    }
                }

                if (skip != 0)
                {
                    SA[first + skip] = skip;
                }
                if (unsorted == 0)
                {
                    break;
                }

                // iter
                ISAd += ISAd - ISA;
            }
        }

        private struct TrStackItem
        {
            public SAPtr a;
            public SAPtr b;
            public SAPtr c;
            public Idx d;
            public Idx e;
        }

        private const int TR_STACK_SIZE = 64;
        private ref struct TrStack
        {
            public readonly Span<TrStackItem> Items;
            public int Size;

            public TrStack(Span<TrStackItem> items)
            {
                Items = items;
                Size = 0;
            }

            public void Push(SAPtr a, SAPtr b, SAPtr c, Idx d, Idx e)
            {
                Debug.Assert(Size < Items.Length);
                ref TrStackItem item = ref Items[Size++];
                item.a = a;
                item.b = b;
                item.c = c;
                item.d = d;
                item.e = e;
            }
            public bool Pop(ref SAPtr a, ref SAPtr b, ref SAPtr c, ref Idx d, ref Idx e)
            {
                //Debug.Assert(Size > 0);
                if (Size == 0) return false;

                ref TrStackItem item = ref Items[--Size];
                a = item.a;
                b = item.b;
                c = item.c;
                d = item.d;
                e = item.e;
                return true;
            }
        }

        private const Idx TR_INSERTIONSORT_THRESHOLD = 8;
        private static void tr_introsort(SAPtr ISA, ref SAPtr ISAd, Span<int> SA, ref SAPtr first, ref SAPtr last, Budget budget)
        {
            SAPtr a = 0;
            SAPtr b = 0;
            SAPtr c;
            Idx t, v, x;
            Idx incr = ISAd - ISA;
            Idx next;
            Idx trlink = -1;

            TrStack stack = new(stackalloc TrStackItem[TR_STACK_SIZE]);

            /*
               macro_rules! ISA {
                   ($x: expr) => {
                       SA[ISA + $x]
                   };
               }
               macro_rules! ISAd {
                   ($x: expr) => {
                       SA[ISAd + $x]
                   };
               }
            */

            Idx limit = tr_ilg(last - first);

            // PASCAL
            while (true)
            {
                //TODO: crosscheck
                crosscheck("pascal limit={} first={} last={}", limit, first, last);
                if (limit < 0)
                {
                    if (limit == -1)
                    {
                        // tandem repeat partition
                        tr_partition(
                            SA,
                            ISAd - incr,
                            first,
                            first,
                            last,
                            ref a,
                            ref b,
                            (last - 1)
                        );

                        // update ranks
                        if (a < last)
                        {
                            //TODO: crosscheck
                            crosscheck("ranks a<last");

                            // JONAS
                            c = first;
                            v = (a - 1);
                            while (c < a)
                            {
                                {
                                    SA[ISA + SA[c]] = v;
                                }

                                // iter (JONAS)
                                c += 1;
                            }
                        }
                        if (b < last)
                        {
                            //TODO: crosscheck
                            crosscheck("ranks b<last");

                            // AHAB
                            c = a;
                            v = (b - 1);
                            while (c < b)
                            {
                                {
                                    SA[ISA + (SA[c])] = v;
                                }

                                // iter (AHAB)
                                c += 1;
                            }
                        }

                        // push
                        if (1 < (b - a))
                        {
                            //TODO: crosscheck
                            crosscheck("1<(b-a)");
                            crosscheck("push NULL {} {} {} {}", a, b, 0, 0);
                            stack.Push(0, a, b, 0, 0);
                            crosscheck("push {} {} {} {} {}", ISAd - incr, first, last, -2, trlink);
                            stack.Push(ISAd - incr, first, last, -2, trlink);
                            trlink = stack.Size - 2;
                        }

                        if ((a - first) <= (last - b))
                        {
                            //TODO: crosscheck
                            crosscheck("star");
                            if (1 < (a - first))
                            {
                                //TODO: crosscheck
                                crosscheck("board");
                                crosscheck(
                                    "push {} {} {} {} {}",
                                    ISAd,
                                    b,
                                    last,
                                    tr_ilg(last - b),
                                    trlink
                                );
                                stack.Push(ISAd, b, last, tr_ilg(last - b), trlink);
                                last = a;
                                limit = tr_ilg(a - first);
                            }
                            else if (1 < (last - b))
                            {
                                //TODO: crosscheck
                                crosscheck("north");
                                first = b;
                                limit = tr_ilg(last - b);
                            }
                            else
                            {
                                //TODO: crosscheck
                                crosscheck("denny");
                                if (!stack.Pop(ref ISAd, ref first, ref last, ref limit, ref trlink))
                                {
                                    return;
                                }
                                crosscheck("denny-post");
                            }
                        }
                        else
                        {
                            crosscheck("moon");
                            if (1 < (last - b))
                            {
                                crosscheck("land");
                                crosscheck(
                                    "push {} {} {} {} {}",
                                    ISAd,
                                    first,
                                    a,
                                    tr_ilg(a - first),
                                    trlink
                                );
                                stack.Push(ISAd, first, a, tr_ilg(a - first), trlink);
                                first = b;
                                limit = tr_ilg(last - b);
                            }
                            else if (1 < (a - first))
                            {
                                crosscheck("ship");
                                last = a;
                                limit = tr_ilg(a - first);
                            }
                            else
                            {
                                crosscheck("clap");
                                if (!stack.Pop(ref ISAd, ref first, ref last, ref limit, ref trlink))
                                {
                                    return;
                                }
                                crosscheck("clap-post");
                            }
                        }
                    }
                    else if (limit == -2)
                    {
                        // end if limit == -1

                        // tandem repeat copy
                        ref TrStackItem item = ref stack.Items[--stack.Size];
                        a = item.b;
                        b = item.c;
                        if (item.d == 0)
                        {
                            tr_copy(ISA, SA, first, a, b, last, ISAd - ISA);
                        }
                        else
                        {
                            if (0 <= trlink)
                            {
                                stack.Items[trlink].d = -1;
                            }
                            tr_partialcopy(ISA, SA, first, a, b, last, ISAd - ISA);
                        }
                        if (!stack.Pop(ref ISAd, ref first, ref last, ref limit, ref trlink))
                        {
                            return;
                        }
                    }
                    else
                    {
                        // end if limit == -2

                        // sorted partition
                        if (0 <= SA[first])
                        {
                            crosscheck("0<=*first");
                            a = first;
                            // GEMINI
                            while (true)
                            {
                                {
                                    SA[ISA + SA[a]] = a;
                                }

                                // cond (GEMINI)
                                a += 1;
                                if (!((a < last) && (0 <= SA[a])))
                                {
                                    break;
                                }
                            }
                            first = a;
                        }

                        if (first < last)
                        {
                            crosscheck("first<last");
                            a = first;
                            // MONSTRO
                            while (true)
                            {
                                //TODO: checkme
                                //SA[a] = !SA[a];
                                SA[a] = ~SA[a];

                                a += 1;
                                if (!(SA[a] < 0))
                                {
                                    break;
                                }
                            }

                            next = SA[ISA + SA[a]] != SA[ISAd + SA[a]] ? tr_ilg(a - first + 1) : -1;
                            a += 1;
                            if (a < last)
                            {
                                crosscheck("++a<last");
                                // CLEMENTINE
                                b = first;
                                v = a - 1;
                                while (b < a)
                                {
                                    {
                                        SA[ISA + SA[b]] = v;
                                    }
                                    b += 1;
                                }
                            }

                            // push
                            if (budget.Check(a - first))
                            {
                                crosscheck("budget pass");
                                if ((a - first) <= (last - a))
                                {
                                    crosscheck("push {} {} {} {} {}", ISAd, a, last, -3, trlink);
                                    stack.Push(ISAd, a, last, -3, trlink);
                                    ISAd += incr;
                                    last = a;
                                    limit = next;
                                }
                                else
                                {
                                    if (1 < (last - a))
                                    {
                                        crosscheck(
                                            "push {} {} {} {} {}",
                                            ISAd + incr,
                                            first,
                                            a,
                                            next,
                                            trlink
                                        );
                                        stack.Push(ISAd + incr, first, a, next, trlink);
                                        first = a;
                                        limit = -3;
                                    }
                                    else
                                    {
                                        ISAd += incr;
                                        last = a;
                                        limit = next;
                                    }
                                }
                            }
                            else
                            {
                                crosscheck("budget fail");
                                if (0 <= trlink)
                                {
                                    crosscheck("0<=trlink");
                                    stack.Items[trlink].d = -1;
                                }
                                if (1 < (last - a))
                                {
                                    crosscheck("1<(last-a)");
                                    first = a;
                                    limit = -3;
                                }
                                else
                                {
                                    crosscheck("1<(last-a) not");
                                    if (!stack.Pop(ref ISAd, ref first, ref last, ref limit, ref trlink))
                                    {
                                        return;
                                    }
                                    crosscheck("1<(last-a) not post");
                                    crosscheck(
                                        "were popped: ISAd={} first={} last={} limit={} trlink={}",
                                        ISAd,
                                        first,
                                        last,
                                        limit,
                                        trlink
                                    );
                                }
                            }
                        }
                        else
                        {
                            crosscheck("times pop");
                            if (!stack.Pop(ref ISAd, ref first, ref last, ref limit, ref trlink))
                            {
                                return;
                            }
                            crosscheck("times pop-post");
                            crosscheck(
                                "were popped: ISAd={} first={} last={} limit={} trlink={}",
                                ISAd,
                                first,
                                last,
                                limit,
                                trlink
                            );
                        } // end if first < last
                    } // end if limit == -1, -2, or something else
                    continue;
                } // end if limit < 0

                if ((last - first) <= TR_INSERTIONSORT_THRESHOLD)
                {
                    crosscheck("insertionsort last-first={}", last - first);
                    tr_insertionsort(SA, ISAd, first, last);
                    limit = -3;
                    continue;
                }

                var old_limit = limit;
                limit -= 1;
                if (old_limit == 0)
                {
                    crosscheck(
                        "heapsort ISAd={} first={} last={} last-first={}",
                        ISAd,
                        first,
                        last,
                        last - first
                    );
                    SA_dump(SA[first..last], "before tr_heapsort");
                    tr_heapsort(ISAd, SA, first, (last - first));
                    SA_dump(SA[first..last], "after tr_heapsort");

                    // YOHAN
                    a = last - 1;
                    while (first < a)
                    {
                        // VINCENT
                        x = SA[ISAd + SA[a]];
                        b = a - 1;
                        while ((first <= b) && (SA[ISAd + SA[b]]) == x)
                        {
                            //!
                            SA[b] = ~SA[b];

                            // iter (VINCENT)
                            b -= 1;
                        }

                        // iter (YOHAN)
                        a = b;
                    }
                    limit = -3;
                    crosscheck("post-vincent continue");
                    continue;
                }

                // choose pivot
                a = tr_pivot(SA, ISAd, first, last);
                crosscheck("picked pivot {}", a);
                SA.Swap(first, a);
                v = SA[ISAd + (SA[first])];

                // partition
                tr_partition(SA, ISAd, first, first + 1, last, ref a, ref b, v);
                if ((last - first) != (b - a))
                {
                    crosscheck("pre-nolwenn");
                    next = SA[ISA + (SA[a])] != v ? tr_ilg(b - a) : -1;

                    // update ranks
                    // NOLWENN
                    c = first;
                    v = (a - 1);
                    while (c < a)
                    {
                        {
                            SA[ISA + (SA[c])] = v;
                        }
                        c += 1;
                    }
                    if (b < last)
                    {
                        // ARTHUR
                        c = a;
                        v = (b - 1);
                        while (c < b)
                        {
                            {
                                SA[ISA + (SA[c])] = v;
                            }
                            c += 1;
                        }
                    }

                    // push
                    if ((1 < (b - a)) && budget.Check(b - a))
                    {
                        crosscheck("a");
                        if ((a - first) <= (last - b))
                        {
                            crosscheck("aa");
                            if ((last - b) <= (b - a))
                            {
                                crosscheck("aaa");
                                if (1 < (a - first))
                                {
                                    crosscheck("aaaa");
                                    crosscheck("push {} {} {} {} {}", ISAd + incr, a, b, next, trlink);
                                    stack.Push(ISAd + incr, a, b, next, trlink);
                                    crosscheck("push {} {} {} {} {}", ISAd, b, last, limit, trlink);
                                    stack.Push(ISAd, b, last, limit, trlink);
                                    last = a;
                                }
                                else if (1 < (last - b))
                                {
                                    crosscheck("aaab");
                                    crosscheck("push {} {} {} {} {}", ISAd + incr, a, b, next, trlink);
                                    stack.Push(ISAd + incr, a, b, next, trlink);
                                    first = b;
                                }
                                else
                                {
                                    crosscheck("aaac");
                                    ISAd += incr;
                                    first = a;
                                    last = b;
                                    limit = next;
                                }
                            }
                            else if ((a - first) <= (b - a))
                            {
                                crosscheck("aab");
                                if (1 < (a - first))
                                {
                                    crosscheck("aaba");
                                    crosscheck("push {} {} {} {} {}", ISAd, b, last, limit, trlink);
                                    stack.Push(ISAd, b, last, limit, trlink);
                                    crosscheck("push {} {} {} {} {}", ISAd + incr, a, b, next, trlink);
                                    stack.Push(ISAd + incr, a, b, next, trlink);
                                    last = a;
                                }
                                else
                                {
                                    crosscheck("aabb");
                                    crosscheck("push {} {} {} {} {}", ISAd, b, last, limit, trlink);
                                    stack.Push(ISAd, b, last, limit, trlink);
                                    ISAd += incr;
                                    first = a;
                                    last = b;
                                    limit = next;
                                }
                            }
                            else
                            {
                                crosscheck("aac");
                                crosscheck("push {} {} {} {} {}", ISAd, b, last, limit, trlink);
                                stack.Push(ISAd, b, last, limit, trlink);
                                crosscheck("push {} {} {} {} {}", ISAd, first, a, limit, trlink);
                                stack.Push(ISAd, first, a, limit, trlink);
                                ISAd += incr;
                                first = a;
                                last = b;
                                limit = next;
                            }
                        }
                        else
                        {
                            crosscheck("ab");
                            if ((a - first) <= (b - a))
                            {
                                crosscheck("aba");
                                if (1 < (last - b))
                                {
                                    crosscheck("abaa");
                                    crosscheck("push {} {} {} {} {}", ISAd + incr, a, b, next, trlink);
                                    stack.Push(ISAd + incr, a, b, next, trlink);
                                    crosscheck("push {} {} {} {} {}", ISAd, first, a, limit, trlink);
                                    stack.Push(ISAd, first, a, limit, trlink);
                                    first = b;
                                }
                                else if (1 < (a - first))
                                {
                                    crosscheck("abab");
                                    crosscheck("push {} {} {} {} {}", ISAd + incr, a, b, next, trlink);
                                    stack.Push(ISAd + incr, a, b, next, trlink);
                                    last = a;
                                }
                                else
                                {
                                    crosscheck("abac");
                                    ISAd += incr;
                                    first = a;
                                    last = b;
                                    limit = next;
                                }
                            }
                            else if ((last - b) <= (b - a))
                            {
                                crosscheck("abb");
                                if (1 < (last - b))
                                {
                                    crosscheck("abba");
                                    crosscheck("push {} {} {} {} {}", ISAd, first, a, limit, trlink);
                                    stack.Push(ISAd, first, a, limit, trlink);
                                    crosscheck("push {} {} {} {} {}", ISAd + incr, a, b, next, trlink);
                                    stack.Push(ISAd + incr, a, b, next, trlink);
                                    first = b;
                                }
                                else
                                {
                                    crosscheck("abbb");
                                    crosscheck("push {} {} {} {} {}", ISAd, first, a, limit, trlink);
                                    stack.Push(ISAd, first, a, limit, trlink);
                                    ISAd += incr;
                                    first = a;
                                    last = b;
                                    limit = next;
                                }
                            }
                            else
                            {
                                crosscheck("abc");
                                crosscheck("push {} {} {} {} {}", ISAd, first, a, limit, trlink);
                                stack.Push(ISAd, first, a, limit, trlink);
                                crosscheck("push {} {} {} {} {}", ISAd, b, last, limit, trlink);
                                stack.Push(ISAd, b, last, limit, trlink);
                                ISAd += incr;
                                first = a;
                                last = b;
                                limit = next;
                            }
                        }
                    }
                    else
                    {
                        crosscheck("b");
                        if ((1 < (b - a)) && (0 <= trlink))
                        {
                            crosscheck("ba");
                            stack.Items[trlink].d = -1;
                        }
                        if ((a - first) <= (last - b))
                        {
                            crosscheck("bb");
                            if (1 < (a - first))
                            {
                                crosscheck("bba");
                                crosscheck("push {} {} {} {} {}", ISAd, b, last, limit, trlink);
                                stack.Push(ISAd, b, last, limit, trlink);
                                last = a;
                            }
                            else if (1 < (last - b))
                            {
                                crosscheck("bbb");
                                first = b;
                            }
                            else
                            {
                                crosscheck("bbc");
                                if (!stack.Pop(ref ISAd, ref first, ref last, ref limit, ref trlink))
                                {
                                    return;
                                }
                            }
                        }
                        else
                        {
                            crosscheck("bc");
                            if (1 < (last - b))
                            {
                                crosscheck("bca");
                                crosscheck("push {} {} {} {} {}", ISAd, first, a, limit, trlink);
                                stack.Push(ISAd, first, a, limit, trlink);
                                first = b;
                            }
                            else if (1 < (a - first))
                            {
                                crosscheck("bcb");
                                last = a;
                            }
                            else
                            {
                                crosscheck("bcc");
                                if (!stack.Pop(ref ISAd, ref first, ref last, ref limit, ref trlink))
                                {
                                    return;
                                }
                                crosscheck("bcc post");
                            }
                        }
                    }
                }
                else
                {
                    crosscheck("c");
                    if (budget.Check(last - first))
                    {
                        crosscheck("ca");
                        limit = tr_ilg(last - first);
                        ISAd += incr;
                    }
                    else
                    {
                        crosscheck("cb");
                        if (0 <= trlink)
                        {
                            crosscheck("cba");
                            stack.Items[trlink].d = -1;
                        }
                        if (!stack.Pop(ref ISAd, ref first, ref last, ref limit, ref trlink))
                        {
                            return;
                        }
                        crosscheck("cb post");
                    }
                }
            } // end PASCAL
        }

        private static void SA_dump(Span<int> span, string v)
        {
            throw new NotImplementedException();
        }

        private static int tr_pivot(Span<int> sA, int iSAd, int first, int last)
        {
            throw new NotImplementedException();
        }

        private static void tr_heapsort(int iSAd, Span<int> sA, int first, int v)
        {
            throw new NotImplementedException();
        }

        private static void tr_insertionsort(Span<int> sA, int iSAd, int first, int last)
        {
            throw new NotImplementedException();
        }

        private static void tr_partialcopy(int iSA, Span<int> sA, int first, int a, int b, int last, int v)
        {
            throw new NotImplementedException();
        }

        private static void tr_copy(int iSA, Span<int> sA, int first, int a, int b, int last, int v)
        {
            throw new NotImplementedException();
        }

        [Conditional("DEBUG")]
        private static void crosscheck(string v, params object[] args)
        {
            //Debug.WriteLine(format: v, args: args);
        }

        private static void tr_partition(Span<int> sA, int v1, int first1, int first2, int last, ref int a, ref int b, int v2)
        {
            throw new NotImplementedException();
        }
    }
}
