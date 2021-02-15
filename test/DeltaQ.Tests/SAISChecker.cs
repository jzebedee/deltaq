using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaQ.Tests
{
    internal class SAISChecker
    {
        internal static int Check(byte[] T, int[] SA, int n, bool verbose = false)
        {
            int[] C = new int[256];
            int i, p, q, t;
            int c;

            if (verbose) { Console.Write(@"sufcheck: "); }
            if (n == 0)
            {
                if (verbose) { Console.WriteLine("Done."); }
                return 0;
            }

            /* Check arguments. */
            if ((T == null) || (SA == null) || (n < 0))
            {
                if (verbose) { Console.WriteLine("Invalid arguments."); }
                return -1;
            }

            /* check range: [0..n-1] */
            for (i = 0; i < n; ++i)
            {
                if ((SA[i] < 0) || (n <= SA[i]))
                {
                    if (verbose)
                    {
                        Console.WriteLine("Out of the range [0," + (n - 1) + "].");
                        Console.WriteLine("  SA[" + i + "]=" + SA[i]);
                    }
                    return -2;
                }
            }

            /* check first characters. */
            for (i = 1; i < n; ++i)
            {
                if (T[SA[i - 1]] > T[SA[i]])
                {
                    if (verbose)
                    {
                        Console.WriteLine("Suffixes in wrong order.");
                        Console.Write("  T[SA[" + (i - 1) + "]=" + SA[i - 1] + "]=" + T[SA[i - 1]]);
                        Console.WriteLine(" > T[SA[" + i + "]=" + SA[i] + "]=" + T[SA[i]]);
                    }
                    return -3;
                }
            }

            /* check suffixes. */
            for (i = 0; i < 256; ++i) { C[i] = 0; }
            for (i = 0; i < n; ++i) { ++C[T[i]]; }
            for (i = 0, p = 0; i < 256; ++i)
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
                    if (verbose)
                    {
                        Console.WriteLine("Suffixes in wrong position.");
                        Console.WriteLine("  SA[" + t + "]=" + ((0 <= t) ? SA[t] : -1) + " or");
                        Console.WriteLine("  SA[" + i + "]=" + SA[i]);
                    }
                    return -4;
                }
                if (t != q)
                {
                    ++C[c];
                    if ((n <= C[c]) || (T[SA[C[c]]] != c)) { C[c] = -1; }
                }
            }

            if (verbose) { Console.WriteLine("Done."); }
            return 0;
        }
    }
}
