using BenchmarkDotNet.Attributes;
using System.Runtime.CompilerServices;
using Idx = System.Int32;
using static DeltaQ.SuffixSorting.LibDivSufSort.Utils;
using BenchmarkDotNet.Diagnosers;

namespace DeltaQ.Benchmarks
{
    [RyuJitX64Job]
    //[RyuJitX86Job]
    [HardwareCounters(HardwareCounter.BranchInstructions, HardwareCounter.BranchMispredictions)]
    public class SqrtBenchmarks
    {
        private const int Step = 1;
        //public SqrtBenchmarks()
        //{
        //    //sanity check range
        //    for (int i = 0; i < SS_BLOCKSIZE * SS_BLOCKSIZE; i++)
        //    {
        //        var sqrtFast = ss_isqrt(i);
        //        var sqrtD = (int)Math.Sqrt(i);
        //        var sqrtF = (int)MathF.Sqrt(i);
        //        if (sqrtFast != sqrtD || sqrtD != sqrtF) throw new InvalidOperationException($"{i} did not match");
        //    }
        //}

        [Benchmark(Baseline = true)]
        public void SqrtsSS()
        {
            Idx y = -1;
            for (int i = 0; i < SS_BLOCKSIZE * SS_BLOCKSIZE; i += Step)
            {
                y = ss_isqrt(i);
            }
            GC.KeepAlive(y);
        }

        [Benchmark]
        public void SqrtsMath()
        {
            Idx y = -1;
            for (int i = 0; i < SS_BLOCKSIZE * SS_BLOCKSIZE; i += Step)
            {
                y = ss_isqrt_math(i);
            }
            GC.KeepAlive(y);
        }

        [Benchmark]
        public void SqrtsMathF()
        {
            Idx y = -1;
            for (int i = 0; i < SS_BLOCKSIZE * SS_BLOCKSIZE; i += Step)
            {
                y = ss_isqrt_mathf(i);
            }
            GC.KeepAlive(y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ss_isqrt_math(int x)
        {
            if (x >= (SS_BLOCKSIZE * SS_BLOCKSIZE))
            {
                return SS_BLOCKSIZE;
            }
            return (int)Math.Sqrt(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ss_isqrt_mathf(int x)
        {
            if (x >= (SS_BLOCKSIZE * SS_BLOCKSIZE))
            {
                return SS_BLOCKSIZE;
            }
            return (int)MathF.Sqrt(x);
        }

        private const Idx SS_BLOCKSIZE = 1024;
        /// <summary>
        /// Fast sqrt, using lookup tables
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /*unchecked*/
        private static int ss_isqrt(int x)
        {
            if (x >= (SS_BLOCKSIZE * SS_BLOCKSIZE))
            {
                return SS_BLOCKSIZE;
            }

            Idx e;
            if ((x & 0xffff_0000) > 0)
            {
                if ((x & 0xff00_0000) > 0)
                {
                    e = 24 + lg_table[(x >> 24) & 0xff];
                }
                else
                {
                    e = 16 + lg_table[(x >> 16) & 0xff];
                }
            }
            else
            {
                if ((x & 0x0000_ff00) > 0)
                {
                    e = 8 + lg_table[(x >> 8) & 0xff];
                }
                else
                {
                    e = 0 + lg_table[(x >> 0) & 0xff];
                }
            };

            Idx y;
            if (e >= 16)
            {
                y = sqq_table[(x >> ((e - 6) - (e & 1)))] << ((e >> 1) - 7);
                if (e >= 24)
                {
                    y = (y + 1 + x / y) >> 1;
                }
                y = (y + 1 + x / y) >> 1;
            }
            else if (e >= 8)
            {
                y = (sqq_table[(x >> ((e - 6) - (e & 1)))] >> (7 - (e >> 1))) + 1;
            }
            else
            {
                return sqq_table[x] >> 4;
            }

            if (x < (y * y))
            {
                return y - 1;
            }
            else
            {
                return y;
            }
        }
    }
}
