using Microsoft.Toolkit.HighPerformance.Buffers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SAPtr = System.Int32;
using Idx = System.Int32;

namespace DeltaQ.SuffixSorting.LibDivSufSort
{
    public partial class DivSufSort
    {
        private const int ALPHABET_SIZE = sizeof(byte) + 1;
        private const int BUCKET_A_SIZE = ALPHABET_SIZE;
        private const int BUCKET_B_SIZE = ALPHABET_SIZE * ALPHABET_SIZE;

        public void divsufsort(ReadOnlySpan<byte> T, Span<int> SA)
        {
            Debug.Assert(T.Length == SA.Length);

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

            var result = sort_typeBstar(T, SA);
            construct_SA(T, SA, result.A, result.B, result.m);
        }

        private void construct_SA(ReadOnlySpan<byte> t, Span<int> sA, Span<int> a, Span<int> b, int m)
        {
            throw new NotImplementedException();
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

        //fn sort_typeBstar(T: &Text, SA: &mut SuffixArray) -> SortTypeBstarResult {
        public SortTypeBstarResult sort_typeBstar(in ReadOnlySpan<byte> T, Span<int> SA)
        {
            var n = T.Length;

            using var owner_A = SpanOwner<int>.Allocate(BUCKET_A_SIZE);
            using var owner_B = SpanOwner<int>.Allocate(BUCKET_B_SIZE);

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

                for (i = m - 2; i > 0; i--)
                {
                    //for i in (0.. = (m - 2)).rev() {
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
                            Debugger.Break();
                            //SA_dump!(&SA.range(i..j), "sssort(A)");
                            //sssort::sssort(
                            //    T,
                            //    SA,
                            //    PAb,
                            //    SAPtr(i),
                            //    SAPtr(j),
                            //    buf,
                            //    bufsize,
                            //    2,
                            //    n,
                            //    SA[i] == (m - 1),

                            //);
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
        //}

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

            public Budget(int chance, int incVal) : this()
            {
                Chance = chance;
                IncVal = incVal;
            }
        }

        /// Tandem repeat sort
        private void trsort(SAPtr ISA, Span<int> SA, int n, int depth)
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

        private struct StackItem
        {
            public SAPtr a;
            public SAPtr b;
            public SAPtr c;
            public Idx d;
            public Idx e;
        }

        private const int STACK_SIZE = 64;
        private ref struct TrStack
        {
            public readonly Span<StackItem> Items;
            public int Size;

            public TrStack(Span<StackItem> items)
            {
                Items = items;
                Size = 0;
            }

            public void Push(SAPtr a, SAPtr b, SAPtr c, Idx d, Idx e)
            {
                Debug.Assert(Size < Items.Length);
                ref StackItem item = ref Items[Size++];
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

                ref StackItem item = ref Items[--Size];
                a = item.a;
                b = item.b;
                c = item.c;
                d = item.d;
                e = item.e;
                return true;
            }
        }

        private void tr_introsort(SAPtr ISA, ref SAPtr ISAd, Span<int> SA, ref SAPtr first, ref SAPtr last, Budget budget)
        {
            SAPtr a = 0;
            SAPtr b = 0;
            SAPtr c;
            Idx t, v, x;
            Idx incr = ISAd - ISA;
            Idx next;
            Idx trlink = -1;

            TrStack stack = new(stackalloc StackItem[STACK_SIZE]);

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
                //crosscheck!("pascal limit={} first={} last={}", limit, first, last);
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
                            //crosscheck!("ranks a<last");

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
                            //crosscheck!("ranks b<last");

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
                            //crosscheck!("1<(b-a)");
                            //crosscheck!("push NULL {} {} {} {}", a, b, 0, 0);
                            stack.Push(0, a, b, 0, 0);
                            //crosscheck!("push {} {} {} {} {}", ISAd - incr, first, last, -2, trlink);
                            stack.Push(ISAd - incr, first, last, -2, trlink);
                            trlink = stack.Size - 2;
                        }

                        if ((a - first) <= (last - b))
                        {
                            //TODO: crosscheck
                            //crosscheck!("star");
                            if (1 < (a - first))
                            {
                                //TODO: crosscheck
                                //crosscheck!("board");
                                //crosscheck!(
                                //    "push {} {} {} {} {}",
                                //    ISAd,
                                //    b,
                                //    last,
                                //    tr_ilg(last - b),
                                //    trlink
                                //);
                                stack.Push(ISAd, b, last, tr_ilg(last - b), trlink);
                                last = a;
                                limit = tr_ilg(a - first);
                            }
                            else if (1 < (last - b))
                            {
                                //TODO: crosscheck
                                //crosscheck!("north");
                                first = b;
                                limit = tr_ilg(last - b);
                            }
                            else
                            {
                                //TODO: crosscheck
                                //crosscheck!("denny");
                                if (!stack.Pop(ref ISAd, ref first, ref last, ref limit, ref trlink))
                                {
                                    return;
                                }
                                //crosscheck!("denny-post");
                            }
                        }
                        else
                        {
                            //crosscheck!("moon");
                            if (1 < (last - b))
                            {
                                //crosscheck!("land");
                                //crosscheck!(
                                //    "push {} {} {} {} {}",
                                //    ISAd,
                                //    first,
                                //    a,
                                //    tr_ilg(a - first),
                                //    trlink
                                //);
                                stack.Push(ISAd, first, a, tr_ilg(a - first), trlink);
                                first = b;
                                limit = tr_ilg(last - b);
                            }
                            else if (1 < (a - first))
                            {
                                //crosscheck!("ship");
                                last = a;
                                limit = tr_ilg(a - first);
                            }
                            else
                            {
                                //crosscheck!("clap");
                                if (!stack.Pop(ref ISAd, ref first, ref last, ref limit, ref trlink))
                                {
                                    return;
                                }
                                //crosscheck!("clap-post");
                            }
                        }
                    }
                    else if (limit == -2)
                    {
                        // end if limit == -1

                        // tandem repeat copy
                        ref StackItem item = ref stack.Items[--stack.Size];
                        a = item.b;
                        b = item.c;
                        if (item.d == 0)
                        {
                            tr_copy(ISA, SA, first, a, b, last, ISAd - ISA);
                        }
                        else
                        {
                            if(0 <= trlink) {
                                stack.Items[trlink].d = -1;
                            }
                            tr_partialcopy(ISA, SA, first, a, b, last, ISAd - ISA);
                        }
                        if(!stack.Pop(ref ISAd, ref first, ref last, ref limit, ref trlink))
                        {
                            return;
                        }
                    }
                    else
                    {
                        // end if limit == -2

                        // sorted partition
                        if 0 <= SA[first] {
                            crosscheck!("0<=*first");
                            a = first;
                            // GEMINI
                            loop {
                                {
                                    let SA_a = SA[a];
                                    ISA!(SA_a) = a.0;
                                }

                                // cond (GEMINI)
                                a += 1;
                                if !((a < last) && (0 <= SA[a])) {
                                    break;
                                }
                            }
                            first = a;
                        }

                        if first < last {
                            crosscheck!("first<last");
                            a = first;
                            // MONSTRO
                            loop {
                                SA[a] = !SA[a];

                                a += 1;
                                if !(SA[a] < 0) {
                                    break;
                                }
                            }

                            next = if ISA!(SA[a]) != ISAd!(SA[a]) {
                                tr_ilg(a - first + 1)
                            }
                            else
                            {
                                -1
                           };
                            a += 1;
                            if a < last {
                                crosscheck!("++a<last");
                                // CLEMENTINE
                                b = first;
                                v = (a - 1).0;
                                while b < a {
                                    {
                                        let SA_b = SA[b];
                                        ISA!(SA_b) = v;
                                    }
                                    b += 1;
                                }
                            }

                            // push
                            if (budget.check((a - first).0))
                            {
                                crosscheck!("budget pass");
                                if (a - first) <= (last - a) {
                                    crosscheck!("push {} {} {} {} {}", ISAd, a, last, -3, trlink);
                                    stack.push(ISAd, a, last, -3, trlink);
                                    ISAd += incr;
                                    last = a;
                                    limit = next;
                                } else
                                {
                                    if 1 < (last - a) {
                                        crosscheck!(
                                            "push {} {} {} {} {}",
                                            ISAd + incr,
                                            first,
                                            a,
                                            next,
                                            trlink
                                        );
                                        stack.push(ISAd + incr, first, a, next, trlink);
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
                                crosscheck!("budget fail");
                                if 0 <= trlink {
                                    crosscheck!("0<=trlink");
                                    stack.items[trlink as usize].d = -1;
                                }
                                if 1 < (last - a) {
                                    crosscheck!("1<(last-a)");
                                    first = a;
                                    limit = -3;
                                }
                                else
                                {
                                    crosscheck!("1<(last-a) not");
                                    if !stack
                                        .pop(&mut ISAd, &mut first, &mut last, &mut limit, &mut trlink)
                                        .is_ok()
                                    {
                                        return;
                                    }
                                    crosscheck!("1<(last-a) not post");
                                    crosscheck!(
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
                            crosscheck!("times pop");
                            if !stack
                                .pop(&mut ISAd, &mut first, &mut last, &mut limit, &mut trlink)
                                .is_ok()
                            {
                                return;
                            }
                            crosscheck!("times pop-post");
                            crosscheck!(
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

                if (last - first) <= TR_INSERTIONSORT_THRESHOLD {
                    crosscheck!("insertionsort last-first={}", last - first);
                    tr_insertionsort(SA, ISAd, first, last);
                    limit = -3;
                    continue;
                }

                let old_limit = limit;
                limit -= 1;
                if (old_limit == 0)
                {
                    crosscheck!(
                        "heapsort ISAd={} first={} last={} last-first={}",
                        ISAd,
                        first,
                        last,
                        last - first
                    );
                    SA_dump!(&SA.range(first..last), "before tr_heapsort");
                    tr_heapsort(ISAd, SA, first, (last - first).0);
                    SA_dump!(&SA.range(first..last), "after tr_heapsort");

                    // YOHAN
                    a = last - 1;
                    while first < a {
                        // VINCENT
                        x = ISAd!(SA[a]);
                        b = a - 1;
                        while (first <= b) && (ISAd!(SA[b])) == x {
                            SA[b] = !SA[b];

                            // iter (VINCENT)
                            b -= 1;
                        }

                        // iter (YOHAN)
                        a = b;
                    }
                    limit = -3;
                    crosscheck!("post-vincent continue");
                    continue;
                }

                // choose pivot
                a = tr_pivot(SA, ISAd, first, last);
                crosscheck!("picked pivot {}", a);
                SA.swap(first, a);
                v = ISAd!(SA[first]);

                // partition
                tr_partition(SA, ISAd, first, first + 1, last, &mut a, &mut b, v);
                if (last - first) != (b - a) {
                    crosscheck!("pre-nolwenn");
                    next = if ISA!(SA[a]) != v { tr_ilg(b - a) } else { -1 };

                    // update ranks
                    // NOLWENN
                    c = first;
                    v = (a - 1).0;
                    while c < a {
                        {
                            let SAc = SA[c];
                            ISA!(SAc) = v;
                        }
                        c += 1;
                    }
                    if b < last {
                        // ARTHUR
                        c = a;
                        v = (b - 1).0;
                        while c < b {
                            {
                                let SAc = SA[c];
                                ISA!(SAc) = v;
                            }
                            c += 1;
                        }
                    }

                    // push
                    if (1 < (b - a)) && budget.check(b - a) {
                        crosscheck!("a");
                        if (a - first) <= (last - b) {
                            crosscheck!("aa");
                            if (last - b) <= (b - a) {
                                crosscheck!("aaa");
                                if 1 < (a - first) {
                                    crosscheck!("aaaa");
                                    crosscheck!("push {} {} {} {} {}", ISAd + incr, a, b, next, trlink);
                                    stack.push(ISAd + incr, a, b, next, trlink);
                                    crosscheck!("push {} {} {} {} {}", ISAd, b, last, limit, trlink);
                                    stack.push(ISAd, b, last, limit, trlink);
                                    last = a;
                                }
                                else if 1 < (last - b) {
                                    crosscheck!("aaab");
                                    crosscheck!("push {} {} {} {} {}", ISAd + incr, a, b, next, trlink);
                                    stack.push(ISAd + incr, a, b, next, trlink);
                                    first = b;
                                }
                                else
                                {
                                    crosscheck!("aaac");
                                    ISAd += incr;
                                    first = a;
                                    last = b;
                                    limit = next;
                                }
                            } else if (a - first) <= (b - a) {
                                crosscheck!("aab");
                                if 1 < (a - first) {
                                    crosscheck!("aaba");
                                    crosscheck!("push {} {} {} {} {}", ISAd, b, last, limit, trlink);
                                    stack.push(ISAd, b, last, limit, trlink);
                                    crosscheck!("push {} {} {} {} {}", ISAd + incr, a, b, next, trlink);
                                    stack.push(ISAd + incr, a, b, next, trlink);
                                    last = a;
                                }
                                else
                                {
                                    crosscheck!("aabb");
                                    crosscheck!("push {} {} {} {} {}", ISAd, b, last, limit, trlink);
                                    stack.push(ISAd, b, last, limit, trlink);
                                    ISAd += incr;
                                    first = a;
                                    last = b;
                                    limit = next;
                                }
                            } else
                            {
                                crosscheck!("aac");
                                crosscheck!("push {} {} {} {} {}", ISAd, b, last, limit, trlink);
                                stack.push(ISAd, b, last, limit, trlink);
                                crosscheck!("push {} {} {} {} {}", ISAd, first, a, limit, trlink);
                                stack.push(ISAd, first, a, limit, trlink);
                                ISAd += incr;
                                first = a;
                                last = b;
                                limit = next;
                            }
                        } else
                        {
                            crosscheck!("ab");
                            if (a - first) <= (b - a) {
                                crosscheck!("aba");
                                if 1 < (last - b) {
                                    crosscheck!("abaa");
                                    crosscheck!("push {} {} {} {} {}", ISAd + incr, a, b, next, trlink);
                                    stack.push(ISAd + incr, a, b, next, trlink);
                                    crosscheck!("push {} {} {} {} {}", ISAd, first, a, limit, trlink);
                                    stack.push(ISAd, first, a, limit, trlink);
                                    first = b;
                                }
                                else if 1 < (a - first) {
                                    crosscheck!("abab");
                                    crosscheck!("push {} {} {} {} {}", ISAd + incr, a, b, next, trlink);
                                    stack.push(ISAd + incr, a, b, next, trlink);
                                    last = a;
                                }
                                else
                                {
                                    crosscheck!("abac");
                                    ISAd += incr;
                                    first = a;
                                    last = b;
                                    limit = next;
                                }
                            } else if (last - b) <= (b - a) {
                                crosscheck!("abb");
                                if 1 < (last - b) {
                                    crosscheck!("abba");
                                    crosscheck!("push {} {} {} {} {}", ISAd, first, a, limit, trlink);
                                    stack.push(ISAd, first, a, limit, trlink);
                                    crosscheck!("push {} {} {} {} {}", ISAd + incr, a, b, next, trlink);
                                    stack.push(ISAd + incr, a, b, next, trlink);
                                    first = b;
                                }
                                else
                                {
                                    crosscheck!("abbb");
                                    crosscheck!("push {} {} {} {} {}", ISAd, first, a, limit, trlink);
                                    stack.push(ISAd, first, a, limit, trlink);
                                    ISAd += incr;
                                    first = a;
                                    last = b;
                                    limit = next;
                                }
                            } else
                            {
                                crosscheck!("abc");
                                crosscheck!("push {} {} {} {} {}", ISAd, first, a, limit, trlink);
                                stack.push(ISAd, first, a, limit, trlink);
                                crosscheck!("push {} {} {} {} {}", ISAd, b, last, limit, trlink);
                                stack.push(ISAd, b, last, limit, trlink);
                                ISAd += incr;
                                first = a;
                                last = b;
                                limit = next;
                            }
                        }
                    } else
                    {
                        crosscheck!("b");
                        if (1 < (b - a)) && (0 <= trlink) {
                            crosscheck!("ba");
                            stack.items[trlink as usize].d = -1;
                        }
                        if (a - first) <= (last - b) {
                            crosscheck!("bb");
                            if 1 < (a - first) {
                                crosscheck!("bba");
                                crosscheck!("push {} {} {} {} {}", ISAd, b, last, limit, trlink);
                                stack.push(ISAd, b, last, limit, trlink);
                                last = a;
                            }
                            else if 1 < (last - b) {
                                crosscheck!("bbb");
                                first = b;
                            }
                            else
                            {
                                crosscheck!("bbc");
                                if !stack
                                    .pop(&mut ISAd, &mut first, &mut last, &mut limit, &mut trlink)
                                    .is_ok()
                                {
                                    return;
                                }
                            }
                        } else
                        {
                            crosscheck!("bc");
                            if 1 < (last - b) {
                                crosscheck!("bca");
                                crosscheck!("push {} {} {} {} {}", ISAd, first, a, limit, trlink);
                                stack.push(ISAd, first, a, limit, trlink);
                                first = b;
                            }
                            else if 1 < (a - first) {
                                crosscheck!("bcb");
                                last = a;
                            }
                            else
                            {
                                crosscheck!("bcc");
                                if !stack
                                    .pop(&mut ISAd, &mut first, &mut last, &mut limit, &mut trlink)
                                    .is_ok()
                                {
                                    return;
                                }
                                crosscheck!("bcc post");
                            }
                        }
                    }
                } else
                {
                    crosscheck!("c");
                    if budget.check(last - first) {
                        crosscheck!("ca");
                        limit = tr_ilg(last - first);
                        ISAd += incr;
                    }
                    else
                    {
                        crosscheck!("cb");
                        if 0 <= trlink {
                            crosscheck!("cba");
                            stack.items[trlink as usize].d = -1;
                        }
                        if !stack
                            .pop(&mut ISAd, &mut first, &mut last, &mut limit, &mut trlink)
                            .is_ok()
                        {
                            return;
                        }
                        crosscheck!("cb post");
                    }
                }
            } // end PASCAL
        }

        private void tr_partition(Span<int> sA, int v1, int first1, int first2, int last, ref int a, ref int b, int v2)
        {
            throw new NotImplementedException();
        }
    }
}
