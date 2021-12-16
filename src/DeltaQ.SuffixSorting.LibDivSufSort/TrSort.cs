using Microsoft.Toolkit.HighPerformance.Buffers;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Idx = System.Int32;
using SAPtr = System.Int32;

namespace DeltaQ.SuffixSorting.LibDivSufSort;
using static Crosscheck;
using static Utils;

internal static class TrSort
{
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

    /// Tandem repeat sort
    internal static void trsort(SAPtr ISA, Span<int> SA, int n, int depth)
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
                        crosscheck($"enter tr_introsort: ISA={ISA} ISAd={ISAd} first={first} last={last}");
                        crosscheck($"  budget: count={budget.Count} chance={budget.Chance} incval={budget.IncVal} remain={budget.Remain}");
                        SA_dump(SA, "tr_introsort(A)");
                        tr_introsort(ISA, ISAd, SA, first, last, ref budget);
                        SA_dump(SA, "tr_introsort(B)");
                        crosscheck($"exit tr_introsort");
                        crosscheck($"  budget: count={budget.Count} chance={budget.Chance} incval={budget.IncVal} remain={budget.Remain}");
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
            Trace.Assert(Size < Items.Length);
            ref TrStackItem item = ref Items[Size++];
            item.a = a;
            item.b = b;
            item.c = c;
            item.d = d;
            item.e = e;
        }
        public bool Pop(ref SAPtr a, ref SAPtr b, ref SAPtr c, ref Idx d, ref Idx e)
        {
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
    private static void tr_introsort(SAPtr isaOffset, SAPtr isadOffset, Span<int> SA, SAPtr first, SAPtr last, ref Budget budget)
    {
        SAPtr a = 0;
        SAPtr b = 0;
        SAPtr c;
        Idx v, x;
        Idx incr = isadOffset - isaOffset;
        Idx next;
        Idx trlink = -1;

        using var stackOwner = SpanOwner<TrStackItem>.Allocate(TR_STACK_SIZE, AllocationMode.Clear);
        TrStack stack = new(stackOwner.Span);

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
        var ISA = SA[isaOffset..];
        var ISAd = SA[isadOffset..];

        var limit = tr_ilg(last - first);

        // PASCAL
        while (true)
        {
            crosscheck($"pascal limit={limit} first={first} last={last}");
            if (limit < 0)
            {
                if (limit == -1)
                {
                    // tandem repeat partition
                    tr_partition(SA, isadOffset - incr, first, first, last, ref a, ref b, last - 1);

                    // update ranks
                    if (a < last)
                    {
                        crosscheck("ranks a<last");

                        // JONAS
                        c = first;
                        v = (a - 1);
                        while (c < a)
                        {
                            {
                                ISA[SA[c]] = v;
                            }

                            // iter (JONAS)
                            c += 1;
                        }
                    }
                    if (b < last)
                    {
                        crosscheck("ranks b<last");

                        // AHAB
                        c = a;
                        v = (b - 1);
                        while (c < b)
                        {
                            {
                                ISA[SA[c]] = v;
                            }

                            // iter (AHAB)
                            c += 1;
                        }
                    }

                    // push
                    if (1 < (b - a))
                    {
                        crosscheck("1<(b-a)");
                        crosscheck($"push NULL {a} {b} {0} {0}");
                        stack.Push(0, a, b, 0, 0);
                        crosscheck($"push {isadOffset - incr} {first} {last} {-2} {trlink}");
                        stack.Push(isadOffset - incr, first, last, -2, trlink);
                        trlink = stack.Size - 2;
                    }

                    if ((a - first) <= (last - b))
                    {
                        crosscheck("star");
                        if (1 < (a - first))
                        {
                            crosscheck("board");
                            crosscheck($"push {isadOffset} {b} {last} {tr_ilg(last - b)} {trlink}");
                            stack.Push(isadOffset, b, last, tr_ilg(last - b), trlink);
                            last = a;
                            limit = tr_ilg(a - first);
                        }
                        else if (1 < (last - b))
                        {
                            crosscheck("north");
                            first = b;
                            limit = tr_ilg(last - b);
                        }
                        else
                        {
                            crosscheck("denny");
                            if (!stack.Pop(ref isadOffset, ref first, ref last, ref limit, ref trlink))
                            {
                                return;
                            }
                            //JZ: ISAd update point
                            ISAd = SA[isadOffset..];
                            crosscheck("denny-post");
                        }
                    }
                    else
                    {
                        crosscheck("moon");
                        if (1 < (last - b))
                        {
                            crosscheck("land");
                            crosscheck($"push {isadOffset} {first} {a} {tr_ilg(a - first)} {trlink}");
                            stack.Push(isadOffset, first, a, tr_ilg(a - first), trlink);
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
                            if (!stack.Pop(ref isadOffset, ref first, ref last, ref limit, ref trlink))
                            {
                                return;
                            }
                            //JZ: ISAd update point
                            ISAd = SA[isadOffset..];
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
                        tr_copy(isaOffset, SA, first, a, b, last, isadOffset - isaOffset);
                    }
                    else
                    {
                        if (0 <= trlink)
                        {
                            stack.Items[trlink].d = -1;
                        }
                        tr_partialcopy(isaOffset, SA, first, a, b, last, isadOffset - isaOffset);
                    }
                    if (!stack.Pop(ref isadOffset, ref first, ref last, ref limit, ref trlink))
                    {
                        return;
                    }
                    //JZ: ISAd update point
                    ISAd = SA[isadOffset..];
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
                            //Debug.Assert(SA[isaOffset..] == ISA);
                            ISA[SA[a]] = a;

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
                            SA[a] = ~SA[a];

                            a += 1;
                            if (!(SA[a] < 0))
                            {
                                break;
                            }
                        }

                        next = ISA[SA[a]] != ISAd[SA[a]] ? tr_ilg(a - first + 1) : -1;
                        a += 1;
                        if (a < last)
                        {
                            crosscheck("++a<last");
                            // CLEMENTINE
                            b = first;
                            v = a - 1;
                            while (b < a)
                            {
                                ISA[SA[b]] = v;
                                b += 1;
                            }
                        }

                        // push
                        if (budget.Check(a - first))
                        {
                            crosscheck("budget pass");
                            if ((a - first) <= (last - a))
                            {
                                crosscheck($"push {isadOffset} {a} {last} {-3} {trlink}");
                                stack.Push(isadOffset, a, last, -3, trlink);
                                isadOffset += incr;
                                //JZ: ISAd update point
                                ISAd = ISAd[incr..];
                                last = a;
                                limit = next;
                            }
                            else
                            {
                                if (1 < (last - a))
                                {
                                    crosscheck($"push {isadOffset + incr} {first} {a} {next} {trlink}");
                                    stack.Push(isadOffset + incr, first, a, next, trlink);
                                    first = a;
                                    limit = -3;
                                }
                                else
                                {
                                    isadOffset += incr;
                                    //JZ: ISAd update point
                                    ISAd = ISAd[incr..];
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
                                if (!stack.Pop(ref isadOffset, ref first, ref last, ref limit, ref trlink))
                                {
                                    return;
                                }
                                //JZ: ISAd update point
                                ISAd = SA[isadOffset..];
                                crosscheck("1<(last-a) not post");
                                crosscheck($"were popped: ISAd={isadOffset} first={first} last={last} limit={limit} trlink={trlink}");
                            }
                        }
                    }
                    else
                    {
                        crosscheck("times pop");
                        if (!stack.Pop(ref isadOffset, ref first, ref last, ref limit, ref trlink))
                        {
                            return;
                        }
                        //JZ: ISAd update point
                        ISAd = SA[isadOffset..];
                        crosscheck("times pop-post");
                        crosscheck($"were popped: ISAd={isadOffset} first={first} last={last} limit={limit} trlink={trlink}");
                    } // end if first < last
                } // end if limit == -1, -2, or something else
                continue;
            } // end if limit < 0

            if ((last - first) <= TR_INSERTIONSORT_THRESHOLD)
            {
                crosscheck($"insertionsort last-first={last - first}");
                SA_dump(SA, "tr_insertionsort(A)");
                tr_insertionsort(SA, ISAd, first, last);
                SA_dump(SA, "tr_insertionsort(B)");
                limit = -3;
                continue;
            }

            var old_limit = limit;
            limit -= 1;
            if (old_limit == 0)
            {
                crosscheck($"heapsort ISAd={isadOffset} first={first} last={last} last-first={last - first}");
                SA_dump(SA[first..last], "before tr_heapsort");
                tr_heapsort(isadOffset, SA, first, (last - first));
                SA_dump(SA[first..last], "after tr_heapsort");

                // YOHAN
                a = last - 1;
                while (first < a)
                {
                    // VINCENT
                    x = ISAd[SA[a]];
                    b = a - 1;
                    while ((first <= b) && (ISAd[SA[b]]) == x)
                    {
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
            a = tr_pivot(SA, isadOffset, first, last);
            crosscheck($"picked pivot {a}");
            SA.Swap(first, a);
            v = ISAd[SA[first]];

            // partition
            tr_partition(SA, isadOffset, first, first + 1, last, ref a, ref b, v);
            if ((last - first) != (b - a))
            {
                crosscheck("pre-nolwenn");
                next = ISA[SA[a]] != v ? tr_ilg(b - a) : -1;

                // update ranks
                // NOLWENN
                c = first;
                v = (a - 1);
                while (c < a)
                {
                    {
                        ISA[SA[c]] = v;
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
                            ISA[SA[c]] = v;
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
                                crosscheck($"push {isadOffset + incr} {a} {b} {next} {trlink}");
                                stack.Push(isadOffset + incr, a, b, next, trlink);
                                crosscheck($"push {isadOffset} {b} {last} {limit} {trlink}");
                                stack.Push(isadOffset, b, last, limit, trlink);
                                last = a;
                            }
                            else if (1 < (last - b))
                            {
                                crosscheck("aaab");
                                crosscheck($"push {isadOffset + incr} {a} {b} {next} {trlink}");
                                stack.Push(isadOffset + incr, a, b, next, trlink);
                                first = b;
                            }
                            else
                            {
                                crosscheck("aaac");
                                isadOffset += incr;
                                //JZ: ISAd update point
                                ISAd = ISAd[incr..];
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
                                crosscheck($"push {isadOffset} {b} {last} {limit} {trlink}");
                                stack.Push(isadOffset, b, last, limit, trlink);
                                crosscheck($"push {isadOffset + incr} {a} {b} {next} {trlink}");
                                stack.Push(isadOffset + incr, a, b, next, trlink);
                                last = a;
                            }
                            else
                            {
                                crosscheck("aabb");
                                crosscheck($"push {isadOffset} {b} {last} {limit} {trlink}");
                                stack.Push(isadOffset, b, last, limit, trlink);
                                isadOffset += incr;
                                //JZ: ISAd update point
                                ISAd = ISAd[incr..];
                                first = a;
                                last = b;
                                limit = next;
                            }
                        }
                        else
                        {
                            crosscheck("aac");
                            crosscheck($"push {isadOffset} {b} {last} {limit} {trlink}");
                            stack.Push(isadOffset, b, last, limit, trlink);
                            crosscheck($"push {isadOffset} {first} {a} {limit} {trlink}");
                            stack.Push(isadOffset, first, a, limit, trlink);
                            isadOffset += incr;
                            //JZ: ISAd update point
                            ISAd = ISAd[incr..];
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
                                crosscheck($"push {isadOffset + incr} {a} {b} {next} {trlink}");
                                stack.Push(isadOffset + incr, a, b, next, trlink);
                                crosscheck($"push {isadOffset} {first} {a} {limit} {trlink}");
                                stack.Push(isadOffset, first, a, limit, trlink);
                                first = b;
                            }
                            else if (1 < (a - first))
                            {
                                crosscheck("abab");
                                crosscheck($"push {isadOffset + incr} {a} {b} {next} {trlink}");
                                stack.Push(isadOffset + incr, a, b, next, trlink);
                                last = a;
                            }
                            else
                            {
                                crosscheck("abac");
                                isadOffset += incr;
                                //JZ: ISAd update point
                                ISAd = ISAd[incr..];
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
                                crosscheck($"push {isadOffset} {first} {a} {limit} {trlink}");
                                stack.Push(isadOffset, first, a, limit, trlink);
                                crosscheck($"push {isadOffset + incr} {a} {b} {next} {trlink}");
                                stack.Push(isadOffset + incr, a, b, next, trlink);
                                first = b;
                            }
                            else
                            {
                                crosscheck("abbb");
                                crosscheck($"push {isadOffset} {first} {a} {limit} {trlink}");
                                stack.Push(isadOffset, first, a, limit, trlink);
                                isadOffset += incr;
                                //JZ: ISAd update point
                                ISAd = ISAd[incr..];
                                first = a;
                                last = b;
                                limit = next;
                            }
                        }
                        else
                        {
                            crosscheck("abc");
                            crosscheck($"push {isadOffset} {first} {a} {limit} {trlink}");
                            stack.Push(isadOffset, first, a, limit, trlink);
                            crosscheck($"push {isadOffset} {b} {last} {limit} {trlink}");
                            stack.Push(isadOffset, b, last, limit, trlink);
                            isadOffset += incr;
                            //JZ: ISAd update point
                            ISAd = ISAd[incr..];
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
                            crosscheck($"push {isadOffset} {b} {last} {limit} {trlink}");
                            stack.Push(isadOffset, b, last, limit, trlink);
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
                            if (!stack.Pop(ref isadOffset, ref first, ref last, ref limit, ref trlink))
                            {
                                return;
                            }
                            //JZ: ISAd update point
                            ISAd = SA[isadOffset..];
                        }
                    }
                    else
                    {
                        crosscheck("bc");
                        if (1 < (last - b))
                        {
                            crosscheck("bca");
                            crosscheck($"push {isadOffset} {first} {a} {limit} {trlink}");
                            stack.Push(isadOffset, first, a, limit, trlink);
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
                            if (!stack.Pop(ref isadOffset, ref first, ref last, ref limit, ref trlink))
                            {
                                return;
                            }
                            //JZ: ISAd update point
                            ISAd = SA[isadOffset..];
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
                    isadOffset += incr;
                    //JZ: ISAd update point
                    ISAd = ISAd[incr..];
                }
                else
                {
                    crosscheck("cb");
                    if (0 <= trlink)
                    {
                        crosscheck("cba");
                        stack.Items[trlink].d = -1;
                    }
                    if (!stack.Pop(ref isadOffset, ref first, ref last, ref limit, ref trlink))
                    {
                        return;
                    }
                    //JZ: ISAd update point
                    ISAd = SA[isadOffset..];
                    crosscheck("cb post");
                }
            }
        } // end PASCAL
    }

    /// Returns the pivot element
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SAPtr tr_pivot(Span<int> SA, SAPtr ISAd, SAPtr first, SAPtr last)
    {
        Idx t = last - first;
        SAPtr middle = first + t / 2;

        if (t <= 512)
        {
            if (t <= 32)
            {
                return tr_median3(SA, ISAd, first, middle, last - 1);
            }
            else
            {
                t >>= 2;
                return tr_median5(SA, ISAd, first, first + t, middle, last - 1 - t, last - 1);
            }
        }
        t >>= 3;
        first = tr_median3(SA, ISAd, first, first + t, first + (t << 1));
        middle = tr_median3(SA, ISAd, middle - t, middle, middle + t);
        last = tr_median3(SA, ISAd, last - 1 - (t << 1), last - 1 - t, last - 1);
        return tr_median3(SA, ISAd, first, middle, last);
    }

    /// Returns the median of five elements
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SAPtr tr_median5(Span<int> SA, SAPtr isadOffset, SAPtr v1, SAPtr v2, SAPtr v3, SAPtr v4, SAPtr v5)
    {
        Span<int> ISAd = SA[isadOffset..];

        //get(x) => ISAd[SA[x]]

        if (ISAd[SA[v2]] > ISAd[SA[v3]])
        {
            Swap(ref v2, ref v3);
        }
        if (ISAd[SA[v4]] > ISAd[SA[v5]])
        {
            Swap(ref v4, ref v5);
        }
        if (ISAd[SA[v2]] > ISAd[SA[v4]])
        {
            Swap(ref v2, ref v4);
            Swap(ref v3, ref v5);
        }
        if (ISAd[SA[v1]] > ISAd[SA[v3]])
        {
            Swap(ref v1, ref v3);
        }
        if (ISAd[SA[v1]] > ISAd[SA[v4]])
        {
            Swap(ref v1, ref v4);
            Swap(ref v3, ref v5);
        }
        if (ISAd[SA[v3]] > ISAd[SA[v4]])
        {
            return v4;
        }
        else
        {
            return v3;
        }
    }

    /// Returns the median of three elements
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SAPtr tr_median3(Span<int> SA, SAPtr isadOffset, SAPtr v1, SAPtr v2, SAPtr v3)
    {
        Span<int> ISAd = SA[isadOffset..];

        //get(x) => ISAd[SA[x]]

        if (ISAd[SA[v1]] > ISAd[SA[v2]])
        {
            Swap(ref v1, ref v2);
        }
        if (ISAd[SA[v2]] > ISAd[SA[v3]])
        {
            if (ISAd[SA[v1]] > ISAd[SA[v3]])
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

    /// Simple top-down heapsort
    private static void tr_heapsort(SAPtr isadOffset, Span<int> SA_top, SAPtr first, Idx size)
    {
        Idx i;
        Idx m;
        Idx t;

        Span<int> ISAd = SA_top[isadOffset..];

        Span<int> SA = SA_top[first..];

        m = size;
        if ((size % 2) == 0)
        {
            m -= 1;
            if (ISAd[SA[m / 2]] < ISAd[SA[m]])
            {
                SA_top.Swap(first + m, first + (m / 2));
            }
        }

        // LISA
        for (i = (m / 2) - 1; i >= 0; i--)
        {
            crosscheck($"LISA i={i}");
            tr_fixdown(ISAd, SA, i, m);
        }
        if ((size % 2) == 0)
        {
            SA_top.Swap(first + 0, first + m);
            tr_fixdown(ISAd, SA, 0, m);
        }
        // MARK
        for (i = m - 1; i > 0; i--)
        {
            crosscheck($"MARK i={i}");
            t = SA[0];
            SA[0] = SA[i];
            tr_fixdown(ISAd, SA, 0, i);
            SA[i] = t;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void tr_fixdown(Span<int> ISAd, Span<int> SA, Idx i, Idx size)
    {
        Idx j;
        Idx k;
        Idx d;
        Idx e;

        crosscheck($"fixdown i={i} size={size}");

        // WILMOT
        var v = SA[i];
        var c = ISAd[v];
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
            d = ISAd[SA[k]];
            j += 1;
            e = ISAd[SA[j]];
            if (d < e)
            {
                k = j;
                d = e;
            }
            if (d <= c)
            {
                break;
            }

            // iter (WILMOT)
            SA[i] = SA[k];
            i = k;
        }
        SA[i] = v;
    }

    /// <summary>
    /// Simple insertionsort for small size groups
    /// </summary>
    private static void tr_insertionsort(Span<int> SA, ReadOnlySpan<int> ISAd, SAPtr first, SAPtr last)
    {
        SAPtr a;
        SAPtr b;
        Idx t;
        Idx r;

        a = first + 1;
        // KAREN
        while (a < last)
        {
            // JEZEBEL
            t = SA[a];
            b = a - 1;
            while (true)
            {
                // cond (JEZEBEL)
                r = ISAd[t] - ISAd[SA[b]];
                if (!(0 > r))
                {
                    break;
                }

                // LILITH
                while (true)
                {
                    SA[b + 1] = SA[b];

                    // cond (LILITH)
                    b -= 1;
                    if (!((first <= b) && (SA[b] < 0)))
                    {
                        break;
                    }
                }

                // body (JEZEBEL)
                if (b < first)
                {
                    break;
                }
            }

            if (r == 0)
            {
                SA[b] = ~SA[b];
            }
            SA[b + 1] = t;

            // iter
            a += 1;
        }
    }

    private static void tr_partialcopy(SAPtr isaOffset, Span<int> SA, SAPtr first, SAPtr a, SAPtr b, SAPtr last, Idx depth)
    {
        SAPtr c, d, e;
        Idx s, v, rank, lastrank, newrank = -1;

        Span<int> ISA = SA[isaOffset..];

        v = (b - 1);
        lastrank = -1;
        // JETHRO
        c = first;
        d = a - 1;
        while (c <= d)
        {
            s = SA[c] - depth;
            if ((0 <= s) && (ISA[s] == v))
            {
                d += 1;
                SA[d] = s;
                rank = ISA[s + depth];
                if (lastrank != rank)
                {
                    lastrank = rank;
                    newrank = d;
                }
                ISA[s] = newrank;
            }

            // iter (JETHRO)
            c += 1;
        }

        lastrank = -1;
        // SCROOGE
        e = d;
        while (first <= e)
        {
            rank = ISA[SA[e]];
            if (lastrank != rank)
            {
                lastrank = rank;
                newrank = e;
            }
            if (newrank != rank)
            {
                {
                    var SA_e = SA[e];
                    ISA[SA_e] = newrank;
                }
            }

            // iter (SCROOGE)
            e -= 1;
        }

        lastrank = -1;
        // DEWEY
        c = last - 1;
        e = d + 1;
        d = b;
        while (e < d)
        {
            s = SA[c] - depth;
            if ((0 <= s) && (ISA[s] == v))
            {
                d -= 1;
                SA[d] = s;
                rank = ISA[s + depth];
                if (lastrank != rank)
                {
                    lastrank = rank;
                    newrank = d;
                }
                ISA[s] = newrank;
            }

            // iter (DEWEY)
            c -= 1;
        }
    }

    /// Tandem repeat copy
    private static void tr_copy(SAPtr isaOffset, Span<int> SA, SAPtr first, SAPtr a, SAPtr b, SAPtr last, Idx depth)
    {
        // sort suffixes of middle partition
        // by using sorted order of suffixes of left and right partition.
        SAPtr c;
        SAPtr d;
        SAPtr e;
        Idx s;
        Idx v;

        crosscheck($"tr_copy first={first} a={a} b={b} last={last}");

        v = (b - 1);

        Span<int> ISA = SA[isaOffset..];

        // JACK
        c = first;
        d = a - 1;
        while (c <= d)
        {
            s = SA[c] - depth;
            if ((0 <= s) && (ISA[s] == v))
            {
                d += 1;
                SA[d] = s;
                ISA[s] = d;
            }

            // iter (JACK)
            c += 1;
        }

        // JILL
        c = last - 1;
        e = d + 1;
        d = b;
        while (e < d)
        {
            s = SA[c] - depth;
            if ((0 <= s) && (ISA[s] == v))
            {
                d -= 1;
                SA[d] = s;
                ISA[s] = d;
            }

            // iter (JILL)
            c -= 1;
        }
    }

    /// <summary>
    /// Tandem repeat partition
    /// </summary>
    private static void tr_partition(Span<int> SA, SAPtr isadOffset, SAPtr first, SAPtr middle, SAPtr last, ref SAPtr pa, ref SAPtr pb, Idx v)
    {
        SAPtr a, b, c, d, e, f;
        Idx t, s, x = 0;

        Span<int> ISAd = SA[isadOffset..];

        // JOSEPH
        b = middle - 1;
        while (true)
        {
            // cond
            b += 1;
            if (!(b < last))
            {
                break;
            }
            x = ISAd[SA[b]];
            if (!(x == v))
            {
                break;
            }
        }
        a = b;
        if ((a < last) && (x < v))
        {
            // MARY
            while (true)
            {
                b += 1;
                if (!(b < last))
                {
                    break;
                }
                x = ISAd[SA[b]];
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

        // JEREMIAH
        c = last;
        while (true)
        {
            c -= 1;
            if (!(b < c))
            {
                break;
            }
            x = ISAd[SA[c]];
            if (!(x == v))
            {
                break;
            }
        }
        d = c;
        if ((b < d) && (x > v))
        {
            // BEDELIA
            while (true)
            {
                c -= 1;
                if (!(b < c))
                {
                    break;
                }
                x = ISAd[SA[c]];
                if (!(x >= v))
                {
                    break;
                }
                if (x == v)
                {
                    SA.Swap(c, d);
                    d -= 1;
                }
            }
        }

        // ALEX
        while (b < c)
        {
            SA.Swap(b, c);
            // SIMON
            while (true)
            {
                b += 1;
                if (!(b < c))
                {
                    break;
                }
                x = ISAd[SA[b]];
                if (!(x <= v))
                {
                    break;
                }
                if (x == v)
                {
                    SA.Swap(b, a);
                    a += 1;
                }
            }

            // GREGORY
            while (true)
            {
                c -= 1;
                if (!(b < c))
                {
                    break;
                }
                x = ISAd[SA[c]];
                if (!(x >= v))
                {
                    break;
                }
                if (x == v)
                {
                    SA.Swap(c, d);
                    d -= 1;
                }
            }
        } // end ALEX

        if (a <= d)
        {
            c = b - 1;

            s = (a - first);
            t = (b - a);
            if (s > t)
            {
                s = t;
            }

            // GENEVIEVE
            e = first;
            f = b - s;
            while (0 < s)
            {
                SA.Swap(e, f);
                s -= 1;
                e += 1;
                f += 1;
            }
            s = (d - c);
            t = (last - d - 1);
            if (s > t)
            {
                s = t;
            }

            // MARISSA
            e = b;
            f = last - s;
            while (0 < s)
            {
                SA.Swap(e, f);
                s -= 1;
                e += 1;
                f += 1;
            }
            first += (b - a);
            last -= (d - c);
        }
        pa = first;
        pb = last;
    }
}