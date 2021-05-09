using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sauchar_t = System.Byte;
using saint_t = System.Int32;
using saidx_t = System.Int32;
using System.Diagnostics;

namespace DeltaQ.SuffixSorting.LibDivSufSort
{
    class Utils
    {
        private const int ALPHABET_SIZE = sizeof(byte) + 1;

        /* Binary search for inverse bwt. */
        static saidx_t binarysearch_lower(ReadOnlySpan<saidx_t> A, saidx_t size, saidx_t value)
        {
            saidx_t half, i;
            for (i = 0, half = size >> 1;
                0 < size;
                size = half, half >>= 1)
            {
                if (A[i + half] < value)
                {
                    i += half + 1;
                    half -= (size & 1) ^ 1;
                }
            }
            return i;
        }


        /*- Functions -*/

        /* Burrows-Wheeler transform. */
        saint_t
        bw_transform(ReadOnlySpan<sauchar_t> T, Span<sauchar_t> U, Span<saidx_t> SA,
                     saidx_t n, ref saidx_t idx)
        {
            Span<saidx_t> A;
            saint_t c;

            /* Check arguments. */
            if ((T == null) || (U == null) || (n < 0) || (idx == null)) { return -1; }
            if (n <= 1)
            {
                if (n == 1) { U[0] = T[0]; }
                idx = n;
                return 0;
            }

            if ((A = SA) == null)
            {
                saidx_t i = divbwt(T, U, null, n);
                if (0 <= i) { idx = i; i = 0; }
                return (saint_t)i;
            }

            /* BW transform. */
            if (T == U)
            {
                saidx_t i, j, p, t = n;
                for (i = 0, j = 0; i < n; ++i)
                {
                    p = t - 1;
                    t = A[i];
                    if (0 <= p)
                    {
                        c = T[j];
                        U[j] = (j <= p) ? T[p] : (sauchar_t)A[p];
                        A[j] = c;
                        j++;
                    }
                    else
                    {
                        idx = i;
                    }
                }
                p = t - 1;
                if (0 <= p)
                {
                    c = T[j];
                    U[j] = (j <= p) ? T[p] : (sauchar_t)A[p];
                    A[j] = c;
                }
                else
                {
                    idx = i;
                }
            }
            else
            {
                saidx_t i;
                U[0] = T[n - 1];
                for (i = 0; A[i] != 0; ++i) { U[i + 1] = T[A[i] - 1]; }
                idx = i + 1;
                for (++i; i < n; ++i) { U[i] = T[A[i] - 1]; }
            }

            return 0;
        }

        /* Inverse Burrows-Wheeler transform. */
        saint_t
        inverse_bw_transform(ReadOnlySpan<sauchar_t> T, Span<sauchar_t> U, Span<saidx_t> A,
                             saidx_t n, saidx_t idx)
        {
            Span<saidx_t> C = new saidx_t[ALPHABET_SIZE];
            Span<sauchar_t> D = new sauchar_t[ALPHABET_SIZE];
            //saidx_t C[ALPHABET_SIZE];
            //sauchar_t D[ALPHABET_SIZE];
            Span<saidx_t> B;
            saidx_t i, p;
            saint_t c, d;

            /* Check arguments. */
            if ((T == null) || (U == null) || (n < 0) || (idx < 0) ||
               (n < idx) || ((0 < n) && (idx == 0)))
            {
                return -1;
            }
            if (n <= 1) { return 0; }

            if ((B = A) == null)
            {
                /* Allocate n*sizeof(saidx_t) bytes of memory. */
                try
                {
                    B = new saidx_t[n];// (saidx_t*)malloc((size_t)n * sizeof(saidx_t));
                    //if (B == null) { return -2; }
                }
                //TODO: fixme
                catch (Exception)
                {
                    return -2;
                }
            }

            /* Inverse BW transform. */
            for (c = 0; c < ALPHABET_SIZE; ++c) { C[c] = 0; }
            for (i = 0; i < n; ++i) { ++C[T[i]]; }
            for (c = 0, d = 0, i = 0; c < ALPHABET_SIZE; ++c)
            {
                p = C[c];
                if (0 < p)
                {
                    C[c] = i;
                    D[d++] = (sauchar_t)c;
                    i += p;
                }
            }
            for (i = 0; i < idx; ++i) { B[C[T[i]]++] = i; }
            for (; i < n; ++i) { B[C[T[i]]++] = i + 1; }
            for (c = 0; c < d; ++c) { C[c] = C[D[c]]; }
            for (i = 0, p = idx; i < n; ++i)
            {
                U[i] = D[binarysearch_lower(C, d, p)];
                p = B[p - 1];
            }

            if (A == null)
            {
                /* Deallocate memory. */
                free(B);
            }

            return 0;
        }

        /* Checks the suffix array SA of the string T. */
        saint_t
        sufcheck(ReadOnlySpan<sauchar_t> T, ReadOnlySpan<saidx_t> SA,
                 saidx_t n)
        {
            Span<saidx_t> C = new saidx_t[ALPHABET_SIZE];
            //saidx_t C[ALPHABET_SIZE];
            saidx_t i, p, q, t;
            saint_t c;

            Debug.Write("sufcheck: ");

            /* Check arguments. */
            if ((T == null) || (SA == null) || (n < 0))
            {
                Debug.WriteLine("Invalid arguments.");
                return -1;
            }
            if (n == 0)
            {
                Debug.WriteLine("Done.");
                return 0;
            }

            /* check range: [0..n-1] */
            for (i = 0; i < n; ++i)
            {
                if ((SA[i] < 0) || (n <= SA[i]))
                {
                    Debug.WriteLine("Out of the range [0,{0}]", n - 1);
                    Debug.WriteLine("SA[{0}]={1}", i, SA[i]);
                    return -2;
                }
            }

            /* check first characters. */
            for (i = 1; i < n; ++i)
            {
                if (T[SA[i - 1]] > T[SA[i]])
                {
                    Debug.WriteLine("Suffixes in wrong order.");
                    Debug.WriteLine("   T[SA[{0}]={1}]={2}", i - 1, SA[i - 1], T[SA[i - 1]]);
                    Debug.WriteLine(" > T[SA[{0}]={1}]={2}", i, SA[i], T[SA[i]]);
                    return -3;
                }
            }

            /* check suffixes. */
            for (i = 0; i < ALPHABET_SIZE; ++i) { C[i] = 0; }
            for (i = 0; i < n; ++i) { ++C[T[i]]; }
            for (i = 0, p = 0; i < ALPHABET_SIZE; ++i)
            {
                t = C[i];
                C[i] = p;
                p += t;
            }

            q = C[T[n - 1]];
            C[T[n - 1]] += 1;
            for (i = 0; i < n; ++i)
            {
                p = SA[i];
                if (0 < p)
                {
                    c = T[--p];
                    t = C[c];
                }
                else
                {
                    c = T[p = n - 1];
                    t = q;
                }
                if ((t < 0) || (p != SA[t]))
                {
                    Debug.WriteLine("Suffix in wrong position.");
                    Debug.WriteLine("  SA[{0}]={1} or", t, 0 <= t ? SA[t] : -1);
                    Debug.WriteLine("  SA[{0}]={1}", i, SA[i]);
                    return -4;
                }
                if (t != q)
                {
                    ++C[c];
                    if ((n <= C[c]) || (T[SA[C[c]]] != c)) { C[c] = -1; }
                }
            }

            Debug.WriteLine("Done.");
            return 0;
        }


        static
        int
        _compare(ReadOnlySpan<sauchar_t> T, saidx_t Tsize,
         ReadOnlySpan<sauchar_t> P, saidx_t Psize,
         saidx_t suf, ref saidx_t match)
        {
            saidx_t i, j;
            saint_t r;
            for (i = suf + match, j = match, r = 0;
                (i < Tsize) && (j < Psize) && ((r = T[i] - P[j]) == 0); ++i, ++j) { }
            match = j;
            //TODO: checkme
            return (r == 0) ? (j != Psize ? -1 : 0) : r;
        }

        /* Search for the pattern P in the string T. */
        saidx_t
        sa_search(ReadOnlySpan<sauchar_t> T, saidx_t Tsize,
                  ReadOnlySpan<sauchar_t> P, saidx_t Psize,
                  ReadOnlySpan<saidx_t> SA, saidx_t SAsize,
                  ref saidx_t idx)
        {
            saidx_t size, lsize, rsize, half;
            saidx_t match, lmatch, rmatch;
            saidx_t llmatch, lrmatch, rlmatch, rrmatch;
            saidx_t i, j, k;
            saint_t r;

            if (idx != null) { idx = -1; }
            if ((T == null) || (P == null) || (SA == null) ||
               (Tsize < 0) || (Psize < 0) || (SAsize < 0)) { return -1; }
            if ((Tsize == 0) || (SAsize == 0)) { return 0; }
            if (Psize == 0) { if (idx != null) { idx = 0; } return SAsize; }

            for (i = j = k = 0, lmatch = rmatch = 0, size = SAsize, half = size >> 1;
                0 < size;
                size = half, half >>= 1)
            {
                match = Math.Min(lmatch, rmatch);
                r = _compare(T, Tsize, P, Psize, SA[i + half], ref match);
                if (r < 0)
                {
                    i += half + 1;
                    half -= (size & 1) ^ 1;
                    lmatch = match;
                }
                else if (r > 0)
                {
                    rmatch = match;
                }
                else
                {
                    lsize = half;
                    j = i;
                    rsize = size - half - 1;
                    k = i + half + 1;

                    /* left part */
                    for (llmatch = lmatch, lrmatch = match, half = lsize >> 1;
                        0 < lsize;
                        lsize = half, half >>= 1)
                    {
                        lmatch = Math.Min(llmatch, lrmatch);
                        r = _compare(T, Tsize, P, Psize, SA[j + half], ref lmatch);
                        if (r < 0)
                        {
                            j += half + 1;
                            half -= (lsize & 1) ^ 1;
                            llmatch = lmatch;
                        }
                        else
                        {
                            lrmatch = lmatch;
                        }
                    }

                    /* right part */
                    for (rlmatch = match, rrmatch = rmatch, half = rsize >> 1;
                        0 < rsize;
                        rsize = half, half >>= 1)
                    {
                        rmatch = Math.Min(rlmatch, rrmatch);
                        r = _compare(T, Tsize, P, Psize, SA[k + half], ref rmatch);
                        if (r <= 0)
                        {
                            k += half + 1;
                            half -= (rsize & 1) ^ 1;
                            rlmatch = rmatch;
                        }
                        else
                        {
                            rrmatch = rmatch;
                        }
                    }

                    break;
                }
            }

            if (idx != null) { idx = (0 < (k - j)) ? j : i; }
            return k - j;
        }

        /* Search for the character c in the string T. */
        saidx_t
        sa_simplesearch(ReadOnlySpan<sauchar_t> T, saidx_t Tsize,
                        ReadOnlySpan<saidx_t> SA, saidx_t SAsize,
                        saint_t c, ref saidx_t idx)
        {
            saidx_t size, lsize, rsize, half;
            saidx_t i, j, k, p;
            saint_t r;

            if (idx != null) { idx = -1; }
            if ((T == null) || (SA == null) || (Tsize < 0) || (SAsize < 0)) { return -1; }
            if ((Tsize == 0) || (SAsize == 0)) { return 0; }

            for (i = j = k = 0, size = SAsize, half = size >> 1;
                0 < size;
                size = half, half >>= 1)
            {
                p = SA[i + half];
                r = (p < Tsize) ? T[p] - c : -1;
                if (r < 0)
                {
                    i += half + 1;
                    half -= (size & 1) ^ 1;
                }
                else if (r == 0)
                {
                    lsize = half;
                    j = i;
                    rsize = size - half - 1;
                    k = i + half + 1;

                    /* left part */
                    for (half = lsize >> 1;
                        0 < lsize;
                        lsize = half, half >>= 1)
                    {
                        p = SA[j + half];
                        r = (p < Tsize) ? T[p] - c : -1;
                        if (r < 0)
                        {
                            j += half + 1;
                            half -= (lsize & 1) ^ 1;
                        }
                    }

                    /* right part */
                    for (half = rsize >> 1;
                        0 < rsize;
                        rsize = half, half >>= 1)
                    {
                        p = SA[k + half];
                        r = (p < Tsize) ? T[p] - c : -1;
                        if (r <= 0)
                        {
                            k += half + 1;
                            half -= (rsize & 1) ^ 1;
                        }
                    }

                    break;
                }
            }

            if (idx != null) { idx = (0 < (k - j)) ? j : i; }
            return k - j;
        }

    }
}
