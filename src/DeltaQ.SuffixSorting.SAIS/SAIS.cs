﻿using CommunityToolkit.HighPerformance.Buffers;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace DeltaQ.SuffixSorting.SAIS;

/// <summary>
///     An implementation of the induced sorting based suffix array construction algorithm.
/// </summary>
public class SAIS : ISuffixSort
{
    private const int AlphabetSize = byte.MaxValue + 1;

    public MemoryOwner<int> Sort(ReadOnlySpan<byte> textBuffer)
    {
        var owner = MemoryOwner<int>.Allocate(textBuffer.Length);
        Sort(textBuffer, owner.Span);
        return owner;
    }

    IMemoryOwner<int> ISuffixSort.Sort(ReadOnlySpan<byte> textBuffer)
        => Sort(textBuffer);

    public void Sort(ReadOnlySpan<byte> textBuffer, Span<int> suffixBuffer)
    {
        if (suffixBuffer.Length != textBuffer.Length)
        {
            ThrowHelper();
        }

        if (textBuffer.Length <= 1)
        {
            if (textBuffer.Length == 1)
            {
                suffixBuffer[0] = 0;
            }
            return;
        }

        SAIS<byte>.sais_main(new TextAccessor<byte>(textBuffer), suffixBuffer, 0, textBuffer.Length, AlphabetSize);
    }

    private static void ThrowHelper() => throw new ArgumentException("Text and suffix buffers should have the same length");
}

internal static class SAIS<T> where T : unmanaged, IConvertible
{
    private const int MinBucketSize = byte.MaxValue + 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GetCounts(TextAccessor<T> T, Span<int> c, int n, int k)
    {
        c[..k].Clear();

        for (int i = 0; i < n; ++i)
        {
            c[T[i]]++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GetBuckets(ReadOnlySpan<int> c, Span<int> b, int k, bool end)
    {
        for (int i = 0, sum = 0; i < k; ++i)
        {
            sum += c[i];
            b[i] = end ? sum : sum - c[i];
        }
    }

    /// <summary>
    /// sort all type LMS suffixes
    /// </summary>
    private static void LMS_sort(TextAccessor<T> T, Span<int> sa, Span<int> c, Span<int> b, int n, int k)
    {
        int bb, i, j;
        int c0, c1;

        /* compute SAl */
        if (c == b)
        {
            GetCounts(T, c, n, k);
        }

        GetBuckets(c, b, k, false); /* find starts of buckets */

        j = n - 1;
        bb = b[c1 = T[j]];
        --j;
        sa[bb++] = T[j] < c1 ? ~j : j;
        for (i = 0; i < n; ++i)
        {
            if (0 < (j = sa[i]))
            {
                if ((c0 = T[j]) != c1)
                {
                    b[c1] = bb;
                    bb = b[c1 = c0];
                }
                --j;
                sa[bb++] = T[j] < c1 ? ~j : j;
                sa[i] = 0;
            }
            else if (j < 0)
            {
                sa[i] = ~j;
            }
        }

        /* compute SAs */
        if (c == b)
        {
            GetCounts(T, c, n, k);
        }

        GetBuckets(c, b, k, true); /* find ends of buckets */

        for (i = n - 1, bb = b[c1 = 0]; 0 <= i; --i)
        {
            if (0 < (j = sa[i]))
            {
                if ((c0 = T[j]) != c1)
                {
                    b[c1] = bb;
                    bb = b[c1 = c0];
                }
                --j;
                sa[--bb] = T[j] > c1 ? ~(j + 1) : j;
                sa[i] = 0;
            }
        }
    }

    private static int LMS_post_proc(TextAccessor<T> T, Span<int> sa, int n, int m)
    {
        int i, j, p, q;
        int qlen, name;
        int c0, c1;

        /* compact all the sorted substrings into the first m items of SA
            2*m must be not larger than n (proveable) */
        for (i = 0; (p = sa[i]) < 0; ++i)
        {
            sa[i] = ~p;
        }

        if (i < m)
        {
            for (j = i, ++i; ; ++i)
            {
                if ((p = sa[i]) < 0)
                {
                    sa[j++] = ~p;
                    sa[i] = 0;
                    if (j == m)
                    {
                        break;
                    }
                }
            }
        }

        /* store the length of all substrings */
        i = n - 1;
        j = n - 1;
        c0 = T[n - 1];
        do
        {
            c1 = c0;
        } while (0 <= --i && (c0 = T[i]) >= c1);
        for (; 0 <= i;)
        {
            do
            {
                c1 = c0;
            } while (0 <= --i && (c0 = T[i]) <= c1);
            if (0 <= i)
            {
                sa[m + (i + 1 >> 1)] = j - i;
                j = i + 1;
                do
                {
                    c1 = c0;
                } while (0 <= --i && (c0 = T[i]) >= c1);
            }
        }

        /* find the lexicographic names of all substrings */
        for (i = 0, name = 0, q = n, qlen = 0; i < m; ++i)
        {
            p = sa[i];
            int plen = sa[m + (p >> 1)];
            bool diff = true;
            if (plen == qlen && q + plen < n)
            {
                for (j = 0;
                    j < plen && T[p + j] == T[q + j];
                    ++j)
                {
                }
                if (j == plen)
                {
                    diff = false;
                }
            }
            if (diff)
            {
                ++name;
                q = p;
                qlen = plen;
            }
            sa[m + (p >> 1)] = name;
        }

        return name;
    }

    private static void InduceSA(TextAccessor<T> T, Span<int> sa, Span<int> c, Span<int> b, int n, int k)
    {
        int bb, i, j;
        int c0, c1;

        /* compute SAl */
        if (c == b)
        {
            GetCounts(T, c, n, k);
        }

        GetBuckets(c, b, k, false); /* find starts of buckets */

        j = n - 1;
        bb = b[c1 = T[j]];
        sa[bb++] = 0 < j && T[j - 1] < c1 ? ~j : j;
        for (i = 0; i < n; ++i)
        {
            j = sa[i];
            sa[i] = ~j;
            if (0 < j)
            {
                if ((c0 = T[--j]) != c1)
                {
                    b[c1] = bb;
                    bb = b[c1 = c0];
                }
                sa[bb++] = 0 < j && T[j - 1] < c1 ? ~j : j;
            }
        }

        /* compute SAs */
        if (c == b)
        {
            GetCounts(T, c, n, k);
        }

        GetBuckets(c, b, k, true); /* find ends of buckets */

        for (i = n - 1, bb = b[c1 = 0]; 0 <= i; --i)
        {
            if (0 < (j = sa[i]))
            {
                if ((c0 = T[--j]) != c1)
                {
                    b[c1] = bb;
                    bb = b[c1 = c0];
                }
                sa[--bb] = j == 0 || T[j - 1] > c1 ? ~j : j;
            }
            else
            {
                sa[i] = ~j;
            }
        }
    }

    /// <summary>
    /// find the suffix array SA of T[0..n-1] in {0..k-1}^n,
    /// using a working space (excluding T and SA) of at most 2n+O(1) for a constant alphabet
    /// </summary>
    public static void sais_main(TextAccessor<T> T, Span<int> sa, int fs, int n, int k)
    {
        Span<int> c, b;
        int i, j, bb, m;
        int name;
        int c0, c1;
        uint flags;

        if (k <= MinBucketSize)
        {
            c = new int[k];// ArrayPool<int>.Shared.Rent(k);
            if (k <= fs)
            {
                b = sa[(n + fs - k)..];
                flags = 1;
            }
            else
            {
                b = new int[k];
                flags = 3;
            }
        }
        else if (k <= fs)
        {
            c = sa[(n + fs - k)..];
            if (k <= fs - k)
            {
                b = sa[(n + fs - k * 2)..];
                flags = 0;
            }
            else if (k <= MinBucketSize * 4)
            {
                b = new int[k];
                flags = 2;
            }
            else
            {
                b = c;
                flags = 8;
            }
        }
        else
        {
            c = b = new int[k];
            flags = 4 | 8;
        }

        /* stage 1: reduce the problem by at least 1/2
           sort all the LMS-substrings */
        GetCounts(T, c, n, k);
        GetBuckets(c, b, k, true); /* find ends of buckets */

        sa[..n].Clear();

        bb = -1;
        i = n - 1;
        j = n;
        m = 0;
        c0 = T[n - 1];
        do
        {
            c1 = c0;
        } while (0 <= --i && (c0 = T[i]) >= c1);

        for (; 0 <= i;)
        {
            do
            {
                c1 = c0;
            } while (0 <= --i && (c0 = T[i]) <= c1);
            if (0 <= i)
            {
                if (0 <= bb)
                {
                    sa[bb] = j;
                }
                bb = --b[c1];
                j = i;
                ++m;
                do
                {
                    c1 = c0;
                } while (0 <= --i && (c0 = T[i]) >= c1);
            }
        }
        if (1 < m)
        {
            LMS_sort(T, sa, c, b, n, k);
            name = LMS_post_proc(T, sa, n, m);
        }
        else if (m == 1)
        {
            sa[bb] = j + 1;
            name = 1;
        }
        else
        {
            name = 0;
        }

        /* stage 2: solve the reduced problem
           recurse if names are not yet unique */
        if (name < m)
        {
            if ((flags & 4) != 0)
            {
                c = null;
                b = null;
            }
            if ((flags & 2) != 0)
            {
                b = null;
            }
            int newfs = n + fs - m * 2;
            if ((flags & (1 | 4 | 8)) == 0)
            {
                if (k + name <= newfs)
                {
                    newfs -= k;
                }
                else
                {
                    flags |= 8;
                }
            }

            for (i = m + (n >> 1) - 1, j = m * 2 + newfs - 1; m <= i; --i)
            {
                if (sa[i] != 0)
                {
                    sa[j--] = sa[i] - 1;
                }
            }

            SAIS<int>.sais_main(new TextAccessor<int>(sa[(m + newfs)..]), sa, newfs, m, name);

            i = n - 1;
            j = m * 2 - 1;
            c0 = T[n - 1];
            do
            {
                c1 = c0;
            } while (0 <= --i && (c0 = T[i]) >= c1);

            for (; 0 <= i;)
            {
                do
                {
                    c1 = c0;
                } while (0 <= --i && (c0 = T[i]) <= c1);

                if (0 <= i)
                {
                    sa[j--] = i + 1;
                    do
                    {
                        c1 = c0;
                    } while (0 <= --i && (c0 = T[i]) >= c1);
                }
            }

            for (i = 0; i < m; ++i)
            {
                sa[i] = sa[m + sa[i]];
            }
            if ((flags & 4) != 0)
            {
                c = b = new int[k];
            }
            if ((flags & 2) != 0)
            {
                b = new int[k];
            }
        }

        /* stage 3: induce the result for the original problem */
        if ((flags & 8) != 0)
        {
            GetCounts(T, c, n, k);
        }
        /* put all left-most S characters into their buckets */
        if (1 < m)
        {
            GetBuckets(c, b, k, true); /* find ends of buckets */
            i = m - 1;
            j = n;
            int p = sa[m - 1];
            c1 = T[p];
            do
            {
                int q = b[c0 = c1];

                while (q < j)
                {
                    sa[--j] = 0;
                }

                do
                {
                    sa[--j] = p;
                    if (--i < 0)
                    {
                        break;
                    }
                    p = sa[i];
                } while ((c1 = T[p]) == c0);
            } while (0 <= i);

            while (0 < j)
            {
                sa[--j] = 0;
            }
        }

        InduceSA(T, sa, c, b, n, k);
    }
}