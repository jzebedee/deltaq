using Microsoft.Toolkit.HighPerformance.Buffers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SAPtr = System.Int32;

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
                            tr_introsort(ISA, ISAd, SA, first, last, budget);
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

        private void tr_introsort(int iSA, int iSAd, Span<int> sA, int first, int last, Budget budget)
        {
            throw new NotImplementedException();
        }
    }
}
