﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sauchar_t = System.Byte;
using saint_t = System.Int32;
using saidx_t = System.Int32;
using System.Runtime.CompilerServices;

namespace DeltaQ.SuffixSorting.LibDivSufSort
{
    public partial class LibDivSufSort
    {
        //# define SS_BLOCKSIZE (1024)
        private const int SS_BLOCKSIZE = 1024;
        //# define SS_INSERTIONSORT_THRESHOLD (8)
        private const int SS_INSERTIONSORT_THRESHOLD = 8;

        private static readonly saint_t[] lg_table_array = new[] {
 -1,0,1,1,2,2,2,2,3,3,3,3,3,3,3,3,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,
  5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,
  6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,
  6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,
  7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
  7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
  7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
  7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7
};
        internal static ReadOnlySpan<int> lg_table => lg_table_array;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static /*INLINE*/ saint_t ss_ilg(saidx_t n)
        {
            return (n & 0xff00) != 0 ?
                    8 + lg_table[(n >> 8) & 0xff] :
                    0 + lg_table[(n >> 0) & 0xff];
        }

        private static readonly saint_t[] sqq_table_array = new[] {
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
        private static ReadOnlySpan<saint_t> sqq_table => sqq_table_array;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static /*INLINE*/ saidx_t ss_isqrt(saidx_t x)
        {
            saidx_t y, e;

            if (x >= (SS_BLOCKSIZE * SS_BLOCKSIZE)) { return SS_BLOCKSIZE; }
            e = (x & 0xffff0000) != 0 ?
                  ((x & 0xff000000) != 0 ?
                    24 + lg_table[(x >> 24) & 0xff] :
                    16 + lg_table[(x >> 16) & 0xff]) :
                  ((x & 0x0000ff00) != 0 ?
                     8 + lg_table[(x >> 8) & 0xff] :
                     0 + lg_table[(x >> 0) & 0xff]);

            if (e >= 16)
            {
                y = sqq_table[x >> ((e - 6) - (e & 1))] << ((e >> 1) - 7);
                if (e >= 24) { y = (y + 1 + x / y) >> 1; }
                y = (y + 1 + x / y) >> 1;
            }
            else if (e >= 8)
            {
                y = (sqq_table[x >> ((e - 6) - (e & 1))] >> (7 - (e >> 1))) + 1;
            }
            else
            {
                return sqq_table[x] >> 4;
            }

            return (x < (y * y)) ? y - 1 : y;
        }

        /*---------------------------------------------------------------------------*/

        /* Compares two suffixes. */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static /*INLINE*/ saint_t ss_compare(ReadOnlySpan<sauchar_t> T,
           ReadOnlySpan<saidx_t> p1, ReadOnlySpan<saidx_t> p2,
           saidx_t depth)
        {
            ReadOnlySpan<sauchar_t> U1, *U2, *U1n, *U2n;

            for (U1 = T + depth + *p1,
                U2 = T + depth + *p2,
                U1n = T + *(p1 + 1) + 2,
                U2n = T + *(p2 + 1) + 2;
                (U1 < U1n) && (U2 < U2n) && (*U1 == *U2);
                ++U1, ++U2)
            {
            }

            return U1 < U1n ?
                  (U2 < U2n ? *U1 - *U2 : 1) :
                  (U2 < U2n ? -1 : 0);
        }


        /*---------------------------------------------------------------------------*/

        /* Insertionsort for small size groups */
        static void ss_insertionsort(ReadOnlySpan<sauchar_t> T, ReadOnlySpan<saidx_t> PA,
                         saidx_t* first, saidx_t* last, saidx_t depth)
        {
            saidx_t* i, *j;
            saidx_t t;
            saint_t r;

            for (i = last - 2; first <= i; --i)
            {
                for (t = *i, j = i + 1; 0 < (r = ss_compare(T, PA + t, PA + *j, depth));)
                {
                    do { *(j - 1) = *j; } while ((++j < last) && (*j < 0));
                    if (last <= j) { break; }
                }
                if (r == 0) { *j = ~*j; }
                *(j - 1) = t;
            }
        }

        /*---------------------------------------------------------------------------*/

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static /*INLINE*/ void ss_fixdown(ReadOnlySpan<sauchar_t> Td, ReadOnlySpan<saidx_t> PA,
           saidx_t* SA, saidx_t i, saidx_t size)
        {
            saidx_t j, k;
            saidx_t v;
            saint_t c, d, e;

            for (v = SA[i], c = Td[PA[v]]; (j = 2 * i + 1) < size; SA[i] = SA[k], i = k)
            {
                d = Td[PA[SA[k = j++]]];
                if (d < (e = Td[PA[SA[j]]])) { k = j; d = e; }
                if (d <= c) { break; }
            }
            SA[i] = v;
        }

        /* Simple top-down heapsort. */
        static void ss_heapsort(ReadOnlySpan<sauchar_t> Td, ReadOnlySpan<saidx_t> PA, saidx_t* SA, saidx_t size)
        {
            saidx_t i, m;
            saidx_t t;

            m = size;
            if ((size % 2) == 0)
            {
                m--;
                if (Td[PA[SA[m / 2]]] < Td[PA[SA[m]]]) { SWAP(SA[m], SA[m / 2]); }
            }

            for (i = m / 2 - 1; 0 <= i; --i) { ss_fixdown(Td, PA, SA, i, m); }
            if ((size % 2) == 0) { SWAP(SA[0], SA[m]); ss_fixdown(Td, PA, SA, 0, m); }
            for (i = m - 1; 0 < i; --i)
            {
                t = SA[0], SA[0] = SA[i];
                ss_fixdown(Td, PA, SA, 0, i);
                SA[i] = t;
            }
        }


        /*---------------------------------------------------------------------------*/

        /* Returns the median of three elements. */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static /*INLINE*/ saidx_t* ss_median3(ReadOnlySpan<sauchar_t> Td, ReadOnlySpan<saidx_t> PA,
           saidx_t* v1, saidx_t* v2, saidx_t* v3)
        {
            saidx_t* t;
            if (Td[PA[*v1]] > Td[PA[*v2]]) { SWAP(v1, v2); }
            if (Td[PA[*v2]] > Td[PA[*v3]])
            {
                if (Td[PA[*v1]] > Td[PA[*v3]]) { return v1; }
                else { return v3; }
            }
            return v2;
        }

        /* Returns the median of five elements. */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static /*INLINE*/ saidx_t* ss_median5(ReadOnlySpan<sauchar_t> Td, ReadOnlySpan<saidx_t> PA,
           saidx_t* v1, saidx_t* v2, saidx_t* v3, saidx_t* v4, saidx_t* v5)
        {
            saidx_t* t;
            if (Td[PA[*v2]] > Td[PA[*v3]]) { SWAP(v2, v3); }
            if (Td[PA[*v4]] > Td[PA[*v5]]) { SWAP(v4, v5); }
            if (Td[PA[*v2]] > Td[PA[*v4]]) { SWAP(v2, v4); SWAP(v3, v5); }
            if (Td[PA[*v1]] > Td[PA[*v3]]) { SWAP(v1, v3); }
            if (Td[PA[*v1]] > Td[PA[*v4]]) { SWAP(v1, v4); SWAP(v3, v5); }
            if (Td[PA[*v3]] > Td[PA[*v4]]) { return v4; }
            return v3;
        }

        /* Returns the pivot element. */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static /*INLINE*/ saidx_t* ss_pivot(ReadOnlySpan<sauchar_t> Td, ReadOnlySpan<saidx_t> PA, saidx_t* first, saidx_t* last)
        {
            saidx_t* middle;
            saidx_t t;

            t = last - first;
            middle = first + t / 2;

            if (t <= 512)
            {
                if (t <= 32)
                {
                    return ss_median3(Td, PA, first, middle, last - 1);
                }
                else
                {
                    t >>= 2;
                    return ss_median5(Td, PA, first, first + t, middle, last - 1 - t, last - 1);
                }
            }
            t >>= 3;
            first = ss_median3(Td, PA, first, first + t, first + (t << 1));
            middle = ss_median3(Td, PA, middle - t, middle, middle + t);
            last = ss_median3(Td, PA, last - 1 - (t << 1), last - 1 - t, last - 1);
            return ss_median3(Td, PA, first, middle, last);
        }


        /*---------------------------------------------------------------------------*/

        /* Binary partition for substrings. */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static /*INLINE*/ saidx_t* ss_partition(ReadOnlySpan<saidx_t> PA,
                    saidx_t* first, saidx_t* last, saidx_t depth)
        {
            saidx_t* a, *b;
            saidx_t t;
            for (a = first - 1, b = last; ;)
            {
                for (; (++a < b) && ((PA[*a] + depth) >= (PA[*a + 1] + 1));) { *a = ~*a; }
                for (; (a < --b) && ((PA[*b] + depth) < (PA[*b + 1] + 1));) { }
                if (b <= a) { break; }
                t = ~*b;
                *b = *a;
                *a = t;
            }
            if (first < a) { *first = ~*first; }
            return a;
        }

        //#define STACK_SIZE SS_MISORT_STACKSIZE
        //#define SS_MISORT_STACKSIZE (16)
        private const int STACK_SIZE = 16;

        private struct stack
        {
            ref saidx_t a;
            ref saidx_t b;
            saidx_t c;
            saint_t d;
        }

        /* Multikey introsort for medium size groups. */
        static void ss_mintrosort(ReadOnlySpan<sauchar_t> T, ReadOnlySpan<saidx_t> PA,
                      ref saidx_t first, ref saidx_t last,
                      saidx_t depth)
        {
            //struct { saidx_t* a, * b, c; saint_t d; } stack[STACK_SIZE];
            Span<stack> stack = stackalloc stack[STACK_SIZE];

            ReadOnlySpan<sauchar_t> Td;
            ref saidx_t a, b, c, d, e, f;
            saidx_t s, t;
            saint_t ssize;
            saint_t limit;
            saint_t v, x = 0;

            for (ssize = 0, limit = ss_ilg(last - first); ;)
            {

                if ((last - first) <= SS_INSERTIONSORT_THRESHOLD)
                {
                    if (1 < (last - first)) { ss_insertionsort(T, PA, first, last, depth); }
                    STACK_POP(first, last, depth, limit);
                    continue;
                }

                Td = T + depth;
                if (limit-- == 0) { ss_heapsort(Td, PA, first, last - first); }
                if (limit < 0)
                {
                    for (a = first + 1, v = Td[PA[ref first]]; a < last; ++a)
                    {
                        if ((x = Td[PA[ref a]]) != v)
                        {
                            if (1 < (a - first)) { break; }
                            v = x;
                            first = a;
                        }
                    }
                    if (Td[PA[ref first] - 1] < v)
                    {
                        first = ss_partition(PA, first, a, depth);
                    }
                    if ((a - first) <= (last - a))
                    {
                        if (1 < (a - first))
                        {
                            STACK_PUSH(a, last, depth, -1);
                            last = a, depth += 1, limit = ss_ilg(a - first);
                        }
                        else
                        {
                            first = a, limit = -1;
                        }
                    }
                    else
                    {
                        if (1 < (last - a))
                        {
                            STACK_PUSH(first, a, depth + 1, ss_ilg(a - first));
                            first = a, limit = -1;
                        }
                        else
                        {
                            last = a, depth += 1, limit = ss_ilg(a - first);
                        }
                    }
                    continue;
                }

                /* choose pivot */
                a = ss_pivot(Td, PA, first, last);
                v = Td[PA[ref a]];
                SWAP(ref first, ref a);

                /* partition */
                for (b = first; (++b < last) && ((x = Td[PA[*b]]) == v);) { }
                if (((a = b) < last) && (x < v))
                {
                    for (; (++b < last) && ((x = Td[PA[*b]]) <= v);)
                    {
                        if (x == v) { SWAP(*b, *a); ++a; }
                    }
                }
                for (c = last; (b < --c) && ((x = Td[PA[*c]]) == v);) { }
                if ((b < (d = c)) && (x > v))
                {
                    for (; (b < --c) && ((x = Td[PA[*c]]) >= v);)
                    {
                        if (x == v) { SWAP(*c, *d); --d; }
                    }
                }
                for (; b < c;)
                {
                    SWAP(*b, *c);
                    for (; (++b < c) && ((x = Td[PA[*b]]) <= v);)
                    {
                        if (x == v) { SWAP(*b, *a); ++a; }
                    }
                    for (; (b < --c) && ((x = Td[PA[*c]]) >= v);)
                    {
                        if (x == v) { SWAP(*c, *d); --d; }
                    }
                }

                if (a <= d)
                {
                    c = b - 1;

                    if ((s = a - first) > (t = b - a)) { s = t; }
                    for (e = first, f = b - s; 0 < s; --s, ++e, ++f) { SWAP(*e, *f); }
                    if ((s = d - c) > (t = last - d - 1)) { s = t; }
                    for (e = b, f = last - s; 0 < s; --s, ++e, ++f) { SWAP(*e, *f); }

                    a = first + (b - a), c = last - (d - c);
                    b = (v <= Td[PA[*a] - 1]) ? a : ss_partition(PA, a, c, depth);

                    if ((a - first) <= (last - c))
                    {
                        if ((last - c) <= (c - b))
                        {
                            STACK_PUSH(b, c, depth + 1, ss_ilg(c - b));
                            STACK_PUSH(c, last, depth, limit);
                            last = a;
                        }
                        else if ((a - first) <= (c - b))
                        {
                            STACK_PUSH(c, last, depth, limit);
                            STACK_PUSH(b, c, depth + 1, ss_ilg(c - b));
                            last = a;
                        }
                        else
                        {
                            STACK_PUSH(c, last, depth, limit);
                            STACK_PUSH(first, a, depth, limit);
                            first = b, last = c, depth += 1, limit = ss_ilg(c - b);
                        }
                    }
                    else
                    {
                        if ((a - first) <= (c - b))
                        {
                            STACK_PUSH(b, c, depth + 1, ss_ilg(c - b));
                            STACK_PUSH(first, a, depth, limit);
                            first = c;
                        }
                        else if ((last - c) <= (c - b))
                        {
                            STACK_PUSH(first, a, depth, limit);
                            STACK_PUSH(b, c, depth + 1, ss_ilg(c - b));
                            first = c;
                        }
                        else
                        {
                            STACK_PUSH(first, a, depth, limit);
                            STACK_PUSH(c, last, depth, limit);
                            first = b, last = c, depth += 1, limit = ss_ilg(c - b);
                        }
                    }
                }
                else
                {
                    limit += 1;
                    if (Td[PA[*first] - 1] < v)
                    {
                        first = ss_partition(PA, first, last, depth);
                        limit = ss_ilg(last - first);
                    }
                    depth += 1;
                }
            }
        }

        /*---------------------------------------------------------------------------*/

#if SS_BLOCKSIZE != 0

static INLINE
void
ss_blockswap(saidx_t *a, saidx_t *b, saidx_t n) {
  saidx_t t;
  for(; 0 < n; --n, ++a, ++b) {
    t = *a, *a = *b, *b = t;
  }
}

static INLINE
void
ss_rotate(saidx_t *first, saidx_t *middle, saidx_t *last) {
  saidx_t *a, *b, t;
  saidx_t l, r;
  l = middle - first, r = last - middle;
  for(; (0 < l) && (0 < r);) {
    if(l == r) { ss_blockswap(first, middle, l); break; }
    if(l < r) {
      a = last - 1, b = middle - 1;
      t = *a;
      do {
        *a-- = *b, *b-- = *a;
        if(b < first) {
          *a = t;
          last = a;
          if((r -= l + 1) <= l) { break; }
          a -= 1, b = middle - 1;
          t = *a;
        }
      } while(1);
    } else {
      a = first, b = middle;
      t = *a;
      do {
        *a++ = *b, *b++ = *a;
        if(last <= b) {
          *a = t;
          first = a + 1;
          if((l -= r + 1) <= r) { break; }
          a += 1, b = middle;
          t = *a;
        }
      } while(1);
    }
  }
}


/*---------------------------------------------------------------------------*/

static
void
ss_inplacemerge(const sauchar_t *T, const saidx_t *PA,
                saidx_t *first, saidx_t *middle, saidx_t *last,
                saidx_t depth) {
  const saidx_t *p;
  saidx_t *a, *b;
  saidx_t len, half;
  saint_t q, r;
  saint_t x;

  for(;;) {
    if(*(last - 1) < 0) { x = 1; p = PA + ~*(last - 1); }
    else                { x = 0; p = PA +  *(last - 1); }
    for(a = first, len = middle - first, half = len >> 1, r = -1;
        0 < len;
        len = half, half >>= 1) {
      b = a + half;
      q = ss_compare(T, PA + ((0 <= *b) ? *b : ~*b), p, depth);
      if(q < 0) {
        a = b + 1;
        half -= (len & 1) ^ 1;
      } else {
        r = q;
      }
    }
    if(a < middle) {
      if(r == 0) { *a = ~*a; }
      ss_rotate(a, middle, last);
      last -= middle - a;
      middle = a;
      if(first == middle) { break; }
    }
    --last;
    if(x != 0) { while(*--last < 0) { } }
    if(middle == last) { break; }
  }
}


/*---------------------------------------------------------------------------*/

/* Merge-forward with internal buffer. */
static
void
ss_mergeforward(const sauchar_t *T, const saidx_t *PA,
                saidx_t *first, saidx_t *middle, saidx_t *last,
                saidx_t *buf, saidx_t depth) {
  saidx_t *a, *b, *c, *bufend;
  saidx_t t;
  saint_t r;

  bufend = buf + (middle - first) - 1;
  ss_blockswap(buf, first, middle - first);

  for(t = *(a = first), b = buf, c = middle;;) {
    r = ss_compare(T, PA + *b, PA + *c, depth);
    if(r < 0) {
      do {
        *a++ = *b;
        if(bufend <= b) { *bufend = t; return; }
        *b++ = *a;
      } while(*b < 0);
    } else if(r > 0) {
      do {
        *a++ = *c, *c++ = *a;
        if(last <= c) {
          while(b < bufend) { *a++ = *b, *b++ = *a; }
          *a = *b, *b = t;
          return;
        }
      } while(*c < 0);
    } else {
      *c = ~*c;
      do {
        *a++ = *b;
        if(bufend <= b) { *bufend = t; return; }
        *b++ = *a;
      } while(*b < 0);

      do {
        *a++ = *c, *c++ = *a;
        if(last <= c) {
          while(b < bufend) { *a++ = *b, *b++ = *a; }
          *a = *b, *b = t;
          return;
        }
      } while(*c < 0);
    }
  }
}

/* Merge-backward with internal buffer. */
static
void
ss_mergebackward(const sauchar_t *T, const saidx_t *PA,
                 saidx_t *first, saidx_t *middle, saidx_t *last,
                 saidx_t *buf, saidx_t depth) {
  const saidx_t *p1, *p2;
  saidx_t *a, *b, *c, *bufend;
  saidx_t t;
  saint_t r;
  saint_t x;

  bufend = buf + (last - middle) - 1;
  ss_blockswap(buf, middle, last - middle);

  x = 0;
  if(*bufend < 0)       { p1 = PA + ~*bufend; x |= 1; }
  else                  { p1 = PA +  *bufend; }
  if(*(middle - 1) < 0) { p2 = PA + ~*(middle - 1); x |= 2; }
  else                  { p2 = PA +  *(middle - 1); }
  for(t = *(a = last - 1), b = bufend, c = middle - 1;;) {
    r = ss_compare(T, p1, p2, depth);
    if(0 < r) {
      if(x & 1) { do { *a-- = *b, *b-- = *a; } while(*b < 0); x ^= 1; }
      *a-- = *b;
      if(b <= buf) { *buf = t; break; }
      *b-- = *a;
      if(*b < 0) { p1 = PA + ~*b; x |= 1; }
      else       { p1 = PA +  *b; }
    } else if(r < 0) {
      if(x & 2) { do { *a-- = *c, *c-- = *a; } while(*c < 0); x ^= 2; }
      *a-- = *c, *c-- = *a;
      if(c < first) {
        while(buf < b) { *a-- = *b, *b-- = *a; }
        *a = *b, *b = t;
        break;
      }
      if(*c < 0) { p2 = PA + ~*c; x |= 2; }
      else       { p2 = PA +  *c; }
    } else {
      if(x & 1) { do { *a-- = *b, *b-- = *a; } while(*b < 0); x ^= 1; }
      *a-- = ~*b;
      if(b <= buf) { *buf = t; break; }
      *b-- = *a;
      if(x & 2) { do { *a-- = *c, *c-- = *a; } while(*c < 0); x ^= 2; }
      *a-- = *c, *c-- = *a;
      if(c < first) {
        while(buf < b) { *a-- = *b, *b-- = *a; }
        *a = *b, *b = t;
        break;
      }
      if(*b < 0) { p1 = PA + ~*b; x |= 1; }
      else       { p1 = PA +  *b; }
      if(*c < 0) { p2 = PA + ~*c; x |= 2; }
      else       { p2 = PA +  *c; }
    }
  }
}

/* D&C based merge. */
static
void
ss_swapmerge(const sauchar_t *T, const saidx_t *PA,
             saidx_t *first, saidx_t *middle, saidx_t *last,
             saidx_t *buf, saidx_t bufsize, saidx_t depth) {
#define STACK_SIZE SS_SMERGE_STACKSIZE
#define GETIDX(a) ((0 <= (a)) ? (a) : (~(a)))
#define MERGE_CHECK(a, b, c)\
  do {\
    if(((c) & 1) ||\
       (((c) & 2) && (ss_compare(T, PA + GETIDX(*((a) - 1)), PA + *(a), depth) == 0))) {\
      *(a) = ~*(a);\
    }\
    if(((c) & 4) && ((ss_compare(T, PA + GETIDX(*((b) - 1)), PA + *(b), depth) == 0))) {\
      *(b) = ~*(b);\
    }\
  } while(0)
  struct { saidx_t *a, *b, *c; saint_t d; } stack[STACK_SIZE];
  saidx_t *l, *r, *lm, *rm;
  saidx_t m, len, half;
  saint_t ssize;
  saint_t check, next;

  for(check = 0, ssize = 0;;) {
    if((last - middle) <= bufsize) {
      if((first < middle) && (middle < last)) {
        ss_mergebackward(T, PA, first, middle, last, buf, depth);
      }
      MERGE_CHECK(first, last, check);
      STACK_POP(first, middle, last, check);
      continue;
    }

    if((middle - first) <= bufsize) {
      if(first < middle) {
        ss_mergeforward(T, PA, first, middle, last, buf, depth);
      }
      MERGE_CHECK(first, last, check);
      STACK_POP(first, middle, last, check);
      continue;
    }

    for(m = 0, len = MIN(middle - first, last - middle), half = len >> 1;
        0 < len;
        len = half, half >>= 1) {
      if(ss_compare(T, PA + GETIDX(*(middle + m + half)),
                       PA + GETIDX(*(middle - m - half - 1)), depth) < 0) {
        m += half + 1;
        half -= (len & 1) ^ 1;
      }
    }

    if(0 < m) {
      lm = middle - m, rm = middle + m;
      ss_blockswap(lm, middle, m);
      l = r = middle, next = 0;
      if(rm < last) {
        if(*rm < 0) {
          *rm = ~*rm;
          if(first < lm) { for(; *--l < 0;) { } next |= 4; }
          next |= 1;
        } else if(first < lm) {
          for(; *r < 0; ++r) { }
          next |= 2;
        }
      }

      if((l - first) <= (last - r)) {
        STACK_PUSH(r, rm, last, (next & 3) | (check & 4));
        middle = lm, last = l, check = (check & 3) | (next & 4);
      } else {
        if((next & 2) && (r == middle)) { next ^= 6; }
        STACK_PUSH(first, lm, l, (check & 3) | (next & 4));
        first = r, middle = rm, check = (next & 3) | (check & 4);
      }
    } else {
      if(ss_compare(T, PA + GETIDX(*(middle - 1)), PA + *middle, depth) == 0) {
        *middle = ~*middle;
      }
      MERGE_CHECK(first, last, check);
      STACK_POP(first, middle, last, check);
    }
  }
#undef STACK_SIZE
}

#endif /* SS_BLOCKSIZE != 0 */


        /*---------------------------------------------------------------------------*/

        /*- Function -*/

        /* Substring sort */
        void sssort(ReadOnlySpan<sauchar_t> T, ReadOnlySpan<saidx_t> PA,
               saidx_t first, saidx_t last,
               Span<saidx_t> buf, saidx_t bufsize,
               saidx_t depth, saidx_t n, bool lastsuffix)
        {
            ref saidx_t a;
            ref saidx_t b, middle, curbuf;
            saidx_t j, k, curbufsize, limit;
            saidx_t i;

            if (lastsuffix) { ++first; }

            if ((bufsize < SS_BLOCKSIZE) &&
                (bufsize < (last - first)) &&
                (bufsize < (limit = ss_isqrt(last - first))))
            {
                if (SS_BLOCKSIZE < limit) { limit = SS_BLOCKSIZE; }
                buf = middle = last - limit, bufsize = limit;
            }
            else
            {
                middle = last, limit = 0;
            }
            for (a = first, i = 0; SS_BLOCKSIZE < (middle - a); a += SS_BLOCKSIZE, ++i)
            {
                ss_mintrosort(T, PA, a, a + SS_BLOCKSIZE, depth);
                curbufsize = last - (a + SS_BLOCKSIZE);
                curbuf = a + SS_BLOCKSIZE;
                if (curbufsize <= bufsize) { curbufsize = bufsize, curbuf = buf; }
                for (b = a, k = SS_BLOCKSIZE, j = i; j & 1; b -= k, k <<= 1, j >>= 1)
                {
                    ss_swapmerge(T, PA, b - k, b, b + k, curbuf, curbufsize, depth);
                }
            }
            ss_mintrosort(T, PA, a, middle, depth);
            for (k = SS_BLOCKSIZE; i != 0; k <<= 1, i >>= 1)
            {
                if (i & 1)
                {
                    ss_swapmerge(T, PA, a - k, a, middle, buf, bufsize, depth);
                    a -= k;
                }
            }
            if (limit != 0)
            {
                ss_mintrosort(T, PA, middle, last, depth);
                ss_inplacemerge(T, PA, first, middle, last, depth);
            }

            if (lastsuffix)
            {
                /* Insert last type B* suffix. */
                Span<saidx_t> PAi = stackalloc saidx_t[2];
                PAi[0] = PA[*(first - 1)];
                PAi[1] = n - 2;
                for (a = first, i = *(first - 1);
                    (a < last) && ((*a < 0) || (0 < ss_compare(T, &(PAi[0]), PA + *a, depth)));
                    ++a)
                {
                    *(a - 1) = *a;
                }
                *(a - 1) = i;
            }
        }

    }
}