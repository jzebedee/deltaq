using Microsoft.Toolkit.HighPerformance.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaQ.SuffixSorting.LibDivSufSort
{
    public partial class DivSufSort
    {
        private const int ALPHABET_SIZE = sizeof(byte) + 1;
        private const int BUCKET_A_SIZE = ALPHABET_SIZE;
        private const int BUCKET_B_SIZE = ALPHABET_SIZE * ALPHABET_SIZE;

        public ref struct SortTypeBstarResult
        {
            public Span<int> A;
            public Span<int> B;
            public int m;
        }

        public ref struct BBucket
        {
            public readonly Span<int> B;
            public BBucket(Span<int> B)
            {
                this.B = B;
            }

            public ref int this[(int c0, int c1) index] => ref B[(index.c1 << 8) | index.c0];
        }

        //fn sort_typeBstar(T: &Text, SA: &mut SuffixArray) -> SortTypeBstarResult {
        public SortTypeBstarResult sort_typeBstar(in ReadOnlySpan<byte> T, Span<byte> SA)
        {
            var n = T.Length;

            using var owner_A = SpanOwner<int>.Allocate(BUCKET_A_SIZE);
            using var owner_B = SpanOwner<int>.Allocate(BUCKET_B_SIZE);

            Span<int> A = owner_A.Span;
            BBucket B = new(owner_B.Span);

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
                    B.bstar()[(c0, c1)] += 1;

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
                        B[(c0, c1)] += 1;

                        // iter
                        i -= 1;
                        c1 = c0;
                    }
                }
            }
            m = n - m;

            //    // Note: A type B* suffix is lexicographically smaller than a type B suffix
            //    // that beings with the same first two characters.

            //    // Calculate the index of start/end point of each bucket.
            //    {
            //        i = 0;
            //        j = 0;
            //        for c0 in 0..(ALPHABET_SIZE as Idx) {
            //            // body
            //            t = i + A[c0];
            //            A[c0] = i + j; // start point
            //            i = t + B.b()[(c0, c0)];

            //            for c1 in (c0 + 1)..(ALPHABET_SIZE as Idx) {
            //                j += B.bstar()[(c0, c1)];
            //                B.bstar()[(c0, c1)] = j; // end point
            //                i += B.b()[(c0, c1)];
            //            }
            //        }
            //    }

            //    if (0 < m) {
            //        // Sort the type B* suffixes by their first two characters
            //        let PAb = SAPtr(n - m);
            //        let ISAb = SAPtr(m);

            //        for i in (0..=(m - 2)).rev() {
            //            t = SA[PAb + i];
            //            c0 = T.get(t);
            //            c1 = T.get(t + 1);
            //            B.bstar()[(c0, c1)] -= 1;
            //            SA[B.bstar()[(c0, c1)]] = i;
            //        }
            //        t = SA[PAb + m - 1];
            //        c0 = T.get(t);
            //        c1 = T.get(t + 1);
            //        B.bstar()[(c0, c1)] -= 1;
            //        SA[B.bstar()[(c0, c1)]] = m - 1;

            //        // Sort the type B* substrings using sssort.
            //        let buf = SAPtr(m);
            //        let bufsize = n - (2 * m);

            //        // init (outer)
            //        c0 = ALPHABET_SIZE as Idx - 2;
            //        j = m;
            //        while 0 < j {
            //            // init (inner)
            //            c1 = ALPHABET_SIZE as Idx - 1;
            //            while c0 < c1 {
            //                // body (inner)
            //                i = B.bstar()[(c0, c1)];

            //                if (1 < (j - i)) {
            //                    SA_dump!(&SA.range(i..j), "sssort(A)");
            //                    sssort::sssort(
            //                        T,
            //                        SA,
            //                        PAb,
            //                        SAPtr(i),
            //                        SAPtr(j),
            //                        buf,
            //                        bufsize,
            //                        2,
            //                        n,
            //                        SA[i] == (m - 1),
            //                    );
            //                    SA_dump!(&SA.range(i..j), "sssort(B)");
            //                }

            //                // iter (inner)
            //                j = i;
            //                c1 -= 1;
            //            }

            //            // iter (outer)
            //            c0 -= 1;
            //        }

            //        // Compute ranks of type B* substrings
            //        i = m - 1;
            //        while 0 <= i {
            //            if (0 <= SA[i]) {
            //                j = i;
            //                loop {
            //                    {
            //                        let SAi = SA[i];
            //                        SA[ISAb + SAi] = i;
            //                    }

            //                    i -= 1;
            //                    if !((0 <= i) && (0 <= SA[i])) {
            //                        break;
            //                    }
            //                }

            //                SA[i + 1] = i - j;
            //                if (i <= 0) {
            //                    break;
            //                }
            //            }
            //            j = i;
            //            loop {
            //                SA[i] = !SA[i];
            //                {
            //                    let idx = ISAb + SA[i];
            //                    SA[idx] = j;
            //                }

            //                i -= 1;
            //                if !(SA[i] < 0) {
            //                    break;
            //                }
            //            }
            //            {
            //                let idx = ISAb + SA[i];
            //                SA[idx] = j;
            //            }

            //            i -= 1;
            //        }

            //        // Construct the inverse suffix array of type B* suffixes using trsort.
            //        trsort::trsort(ISAb, SA, m, 1);

            //        // Set the sorted order of type B* suffixes
            //        {
            //            // init
            //            i = n - 1;
            //            j = m;
            //            c0 = T.get(n - 1);
            //            while 0 <= i {
            //                // init
            //                i -= 1;
            //                c1 = c0;

            //                loop {
            //                    // cond
            //                    if !(0 <= i) {
            //                        break;
            //                    }
            //                    c0 = T.get(i);
            //                    if !(c0 >= c1) {
            //                        break;
            //                    }

            //                    // body (empty)

            //                    // iter
            //                    i -= 1;
            //                    c1 = c0;
            //                }

            //                if 0 <= i {
            //                    t = i;

            //                    // init
            //                    i -= 1;
            //                    c1 = c0;

            //                    loop {
            //                        // cond
            //                        if !(0 <= i) {
            //                            break;
            //                        }
            //                        c0 = T.get(i);
            //                        if !(c0 <= c1) {
            //                            break;
            //                        }

            //                        // body (empty)

            //                        // iter
            //                        i -= 1;
            //                        c1 = c0;
            //                    }

            //                    j -= 1;
            //                    {
            //                        let pos = SA[ISAb + j];
            //                        SA[pos] = if (t == 0) || (1 < (t - i)) { t } else { !t };
            //                    }
            //                }
            //            }
            //        } // End: Set the sorted order of type B* suffixes

            //        // Calculate the index of start/end point of each bucket
            //        {
            //            B.b()[(ALPHABET_SIZE as Idx - 1, ALPHABET_SIZE as Idx - 1)] = n; // end point

            //            // init
            //            c0 = ALPHABET_SIZE as Idx - 2;
            //            k = m - 1;

            //            while 0 <= c0 {
            //                i = A[c0 + 1] - 1;

            //                // init
            //                c1 = ALPHABET_SIZE as Idx - 1;
            //                while c0 < c1 {
            //                    t = i - B.b()[(c0, c1)];
            //                    B.b()[(c0, c1)] = i; // end point

            //                    // Move all type B* suffixes to the correct position
            //                    {
            //                        // init
            //                        i = t;
            //                        j = B.bstar()[(c0, c1)];

            //                        while j <= k {
            //                            SA[i] = SA[k];

            //                            // iter
            //                            i -= 1;
            //                            k -= 1;
            //                        }
            //                    } // End: Move all type B* suffixes to the correct position

            //                    // iter
            //                    c1 -= 1;
            //                }
            //                B.bstar()[(c0, c0 + 1)] = i - B.b()[(c0, c0)] + 1;
            //                B.b()[(c0, c0)] = i; // end point

            //                // iter
            //                c0 -= 1;
            //            }
            //        } // End: Calculate the index of start/end point of each bucket
            //    }

            //    SortTypeBstarResult { A, B, m }
        }
        //}

    }
}
