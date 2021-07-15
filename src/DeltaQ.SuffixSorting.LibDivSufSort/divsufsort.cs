using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sauchar_t = System.Byte;
using saint_t = System.Int32;
using saidx_t = System.Int32;
using Microsoft.Toolkit.HighPerformance.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DeltaQ.SuffixSorting.LibDivSufSort
{
    public partial class LibDivSufSort
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ref int BUCKET_A(Span<int> bucket_A, int c0) => ref bucket_A[c0];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ref int BUCKET_B(Span<int> bucket_B, int c0, int c1) => ref bucket_B[((c1) << 8) | (c0)];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ref int BUCKET_BSTAR(Span<int> bucket_B, int c0, int c1) => ref bucket_B[((c0) << 8) | (c1)];

        /* Constructs the suffix array by using the sorted order of type B* suffixes. */
        static
        void
        construct_SA(ReadOnlySpan<sauchar_t> T, Span<saidx_t> SA,
                     Span<saidx_t> bucket_A, Span<saidx_t> bucket_B,
                     saidx_t n, saidx_t m)
        {
            saidx_t s;
            saint_t c0, c1, c2;

            if (0 < m)
            {
                /* Construct the sorted order of type B suffixes by using
                   the sorted order of type B* suffixes. */
                for (c1 = ALPHABET_SIZE - 2; 0 <= c1; --c1)
                {
                    /* Scan the suffix array from right to left. */
                    c2 = -1;
                    for (ref saidx_t i = ref Unsafe.Add(ref MemoryMarshal.GetReference(SA), BUCKET_BSTAR(bucket_B, c1, c1 + 1)),
                        j = ref Unsafe.Add(ref MemoryMarshal.GetReference(SA), BUCKET_A(bucket_A, c1 + 1) - 1),
                        k = ref Unsafe.NullRef<saidx_t>();
                        i <= j;
                        --j)
                    {
                        if (0 < (s = j))
                        {
                            Debug.Assert(T[s] == c1);
                            Debug.Assert(((s + 1) < n) && (T[s] <= T[s + 1]));
                            Debug.Assert(T[s - 1] <= T[s]);
                            j = ~s;
                            c0 = T[--s];
                            if ((0 < s) && (T[s - 1] > c0)) { s = ~s; }
                            if (c0 != c2)
                            {
                                if (0 <= c2) { BUCKET_B(bucket_B, c2, c1) = k - MemoryMarshal.GetReference(SA); }
                                k = Unsafe.Add(ref MemoryMarshal.GetReference(SA), BUCKET_B(bucket_B, c2 = c0, c1));
                            }
                            Debug.Assert(k < j);
                            k = ref Unsafe.Subtract(ref k, 1);
                            k = s;
                        }
                        else
                        {
                            Debug.Assert(((s == 0) && (T[s] == c1)) || (s < 0));
                            j = ~s;
                        }
                    }
                }
            }

            /* Construct the suffix array by using
               the sorted order of type B suffixes. */
            k = SA + BUCKET_A(c2 = T[n - 1]);
            *k++ = (T[n - 2] < c2) ? ~(n - 1) : (n - 1);
            /* Scan the suffix array from left to right. */
            for (i = SA, j = SA + n; i < j; ++i)
            {
                if (0 < (s = *i))
                {
                    Debug.Assert(T[s - 1] >= T[s]);
                    c0 = T[--s];
                    if ((s == 0) || (T[s - 1] < c0)) { s = ~s; }
                    if (c0 != c2)
                    {
                        BUCKET_A(c2) = k - SA;
                        k = SA + BUCKET_A(c2 = c0);
                    }
                    Debug.Assert(i < k);
                    *k++ = s;
                }
                else
                {
                    Debug.Assert(s < 0);
                    *i = ~s;
                }
            }
        }

        /* Constructs the burrows-wheeler transformed string directly
           by using the sorted order of type B* suffixes. */
        static
        saidx_t
        construct_BWT(ReadOnlySpan<sauchar_t> T, Span<saidx_t> SA,
                      Span<saidx_t> bucket_A, Span<saidx_t> bucket_B,
                      saidx_t n, saidx_t m)
        {
            saidx_t i, j, k, orig;
            saidx_t s;
            saint_t c0, c1, c2;

            if (0 < m)
            {
                /* Construct the sorted order of type B suffixes by using
                   the sorted order of type B* suffixes. */
                for (c1 = ALPHABET_SIZE - 2; 0 <= c1; --c1)
                {
                    /* Scan the suffix array from right to left. */
                    for (i = SA + BUCKET_BSTAR(c1, c1 + 1),
                        j = SA + BUCKET_A(c1 + 1) - 1, k = null, c2 = -1;
                        i <= j;
                        --j)
                    {
                        if (0 < (s = *j))
                        {
                            Debug.Assert(T[s] == c1);
                            Debug.Assert(((s + 1) < n) && (T[s] <= T[s + 1]));
                            Debug.Assert(T[s - 1] <= T[s]);
                            c0 = T[--s];
                            *j = ~((saidx_t)c0);
                            if ((0 < s) && (T[s - 1] > c0)) { s = ~s; }
                            if (c0 != c2)
                            {
                                if (0 <= c2) { BUCKET_B(c2, c1) = k - SA; }
                                k = SA + BUCKET_B(c2 = c0, c1);
                            }
                            Debug.Assert(k < j);
                            *k-- = s;
                        }
                        else if (s != 0)
                        {
                            *j = ~s;
#if DEBUG
                        }
                        else
                        {
                            Debug.Assert(T[s] == c1);
#endif
                        }
                    }
                }
            }

            /* Construct the BWTed string by using
               the sorted order of type B suffixes. */
            k = SA + BUCKET_A(c2 = T[n - 1]);
            *k++ = (T[n - 2] < c2) ? ~((saidx_t)T[n - 2]) : (n - 1);
            /* Scan the suffix array from left to right. */
            for (i = SA, j = SA + n, orig = SA; i < j; ++i)
            {
                if (0 < (s = *i))
                {
                    Debug.Assert(T[s - 1] >= T[s]);
                    c0 = T[--s];
                    *i = c0;
                    if ((0 < s) && (T[s - 1] < c0)) { s = ~((saidx_t)T[s - 1]); }
                    if (c0 != c2)
                    {
                        BUCKET_A(c2) = k - SA;
                        k = SA + BUCKET_A(c2 = c0);
                    }
                    Debug.Assert(i < k);
                    *k++ = s;
                }
                else if (s != 0)
                {
                    *i = ~s;
                }
                else
                {
                    orig = i;
                }
            }

            return orig - SA;
        }


        /*---------------------------------------------------------------------------*/

        /*- Function -*/

        saint_t
        divsufsort(ReadOnlySpan<sauchar_t> T, Span<saidx_t> SA, saidx_t n)
        {
            saidx_t m;

            /* Check arguments. */
            if ((T == null) || (SA == null) || (n < 0)) { return -1; }
            else if (n == 0) { return 0; }
            else if (n == 1) { SA[0] = 0; return 0; }
            else if (n == 2) { /*TODO: checkme*/m = T[0] < T[1] ? 1 : 0; SA[m ^ 1] = 0; SA[m] = 1; return 0; }

            using var owner_A = SpanOwner<saidx_t>.Allocate(BUCKET_A_SIZE);
            using var owner_B = SpanOwner<saidx_t>.Allocate(BUCKET_B_SIZE);

            Span<saidx_t> bucket_A = owner_A.Span;
            Span<saidx_t> bucket_B = owner_B.Span;

            /* Suffixsort. */
            if (bucket_A == null || bucket_B == null)
            {
                return -2;
            }

            m = sort_typeBstar(T, SA, bucket_A, bucket_B, n);
            construct_SA(T, SA, bucket_A, bucket_B, n, m);
            return 0;
        }

        saidx_t
        divbwt(ReadOnlySpan<sauchar_t> T, Span<sauchar_t> U, Span<saidx_t> A, saidx_t n)
        {
            Span<saidx_t> B;
            Span<saidx_t> bucket_A, bucket_B;
            saidx_t m, pidx, i;

            /* Check arguments. */
            if ((T == null) || (U == null) || (n < 0)) { return -1; }
            else if (n <= 1) { if (n == 1) { U[0] = T[0]; } return n; }

            if ((B = A) == null) { B = new saidx_t[n + 1]; }
            bucket_A = new saidx_t[BUCKET_A_SIZE];
            bucket_B = new saidx_t[BUCKET_B_SIZE];
            
            /* Burrows-Wheeler Transform. */
            if ((B != null) && (bucket_A != null) && (bucket_B != null))
            {
                m = sort_typeBstar(T, B, bucket_A, bucket_B, n);
                pidx = construct_BWT(T, B, bucket_A, bucket_B, n, m);

                /* Copy to output string. */
                U[0] = T[n - 1];
                for (i = 0; i < pidx; ++i) { U[i + 1] = (sauchar_t)B[i]; }
                for (i += 1; i < n; ++i) { U[i] = (sauchar_t)B[i]; }
                pidx += 1;
            }
            else
            {
                pidx = -2;
            }

            //free(bucket_B);
            //free(bucket_A);
            //if (A == null) { free(B); }

            return pidx;
        }
    }
}
