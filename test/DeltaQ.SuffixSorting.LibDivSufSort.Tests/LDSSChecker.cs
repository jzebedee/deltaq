using CommunityToolkit.HighPerformance.Buffers;
using System;
using System.Diagnostics;

namespace DeltaQ.Tests
{
    internal static class LDSSChecker
    {
        private const int ALPHABET_SIZE = byte.MaxValue + 1;

        internal enum ResultCode
        {
            Done = 0,
            BadArguments = -1,
            OutOfRange = -2,
            WrongOrder = -3,
            WrongPosition = -4,
        }

        /// <summary>
        /// Checks the suffix array SA of the string T.
        /// </summary>
        public static ResultCode Check(ReadOnlySpan<byte> T, ReadOnlySpan<int> SA, bool verbose)
        {
            if (verbose) { Trace.TraceInformation("sufcheck: "); }

            // Check arguments. 
            if (T.Length != SA.Length)
            {
                if (verbose) { Trace.TraceError("Invalid arguments.\n"); }
                return ResultCode.BadArguments;
            }

            if (T.IsEmpty)
            {
                if (verbose) { Trace.TraceInformation("Done.\n"); }
                return ResultCode.Done;
            }

            int i, p, q, t;
            int c;

            // check range: [0..n-1]
            int n = T.Length;
            for (i = 0; i < n; ++i)
            {
                if ((SA[i] < 0) || (n <= SA[i]))
                {
                    if (verbose)
                    {
                        Trace.TraceError($"Out of the range [0,{n - 1}].\n");
                        Trace.TraceError($"  SA[{i}]={SA[i]}\n");
                    }
                    return ResultCode.OutOfRange;
                }
            }

            // check first characters.
            for (i = 1; i < n; ++i)
            {
                if (T[SA[i - 1]] > T[SA[i]])
                {
                    if (verbose)
                    {
                        Trace.TraceError("Suffixes in wrong order.\n");
                        Trace.TraceError($"  T[SA[{i - 1}]={SA[i - 1]}]={T[SA[i - 1]]}");
                        Trace.TraceError($" > T[SA[{i}]={SA[i]}]={T[SA[i]]}\n");
                    }
                    return ResultCode.WrongOrder;
                }
            }

            // check suffixes.
            using var cOwner = SpanOwner<int>.Allocate(ALPHABET_SIZE, AllocationMode.Clear);
            Span<int> C = cOwner.Span;

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
                    if (verbose)
                    {
                        Trace.TraceError("Suffix in wrong position.\n");
                        Trace.TraceError($"  SA[{t}]={((0 <= t) ? SA[t] : -1)} or\n");
                        Trace.TraceError($"  SA[{i}]={SA[i]}\n");
                    }
                    return ResultCode.WrongPosition;
                }
                if (t != q)
                {
                    ++C[c];
                    if ((n <= C[c]) || (T[SA[C[c]]] != c)) { C[c] = -1; }
                }
            }

            if (verbose) { Trace.TraceInformation("Done.\n"); }
            return ResultCode.Done;
        }
    }
}
