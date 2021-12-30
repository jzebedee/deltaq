using BenchmarkDotNet.Attributes;
using System.Runtime.CompilerServices;
using Idx = System.Int32;
using static DeltaQ.SuffixSorting.LibDivSufSort.Utils;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;

namespace DeltaQ.Benchmarks
{
    //[HardwareCounters(HardwareCounter.BranchInstructions, HardwareCounter.BranchMispredictions)]
    [SimpleJob(RunStrategy.Throughput)]
    public class Log2Benchmarks
    {
        private const int Step = 1;

        [Benchmark(Baseline = true)]
        public void tr_ilg()
        {
            Idx y = -1;
            for (int i = 0; i < int.MaxValue; i += Step)
            {
                y = tr_ilg(i);
            }
            GC.KeepAlive(y);
        }

        [Benchmark]
        public void Log2()
        {
            Idx y = -1;
            for (int i = 0; i < int.MaxValue; i += Step)
            {
                y = Log2(i);
            }
            GC.KeepAlive(y);
        }

#if NETCOREAPP3_0_OR_GREATER
        [Benchmark]
        public void MathLog2()
        {
            Idx y = -1;
            for (int i = 0; i < int.MaxValue; i += Step)
            {
                y = (int)Math.Log2(i);
            }
            GC.KeepAlive(y);
        }

        [Benchmark]
        public void MathFLog2()
        {
            Idx y = -1;
            for (int i = 0; i < int.MaxValue; i += Step)
            {
                y = (int)MathF.Log2(i);
            }
            GC.KeepAlive(y);
        }

        [Benchmark]
        public void MathILogB()
        {
            Idx y = -1;
            for (int i = 0; i < int.MaxValue; i += Step)
            {
                y = Math.ILogB(i);
            }
            GC.KeepAlive(y);
        }

        [Benchmark]
        public void MathFILogB()
        {
            Idx y = -1;
            for (int i = 0; i < int.MaxValue; i += Step)
            {
                y = MathF.ILogB(i);
            }
            GC.KeepAlive(y);
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Log2(int v)
        {
            int r = 0xFFFF - v >> 31 & 0x10;
            v >>= r;
            int shift = 0xFF - v >> 31 & 0x8;
            v >>= shift;
            r |= shift;
            shift = 0xF - v >> 31 & 0x4;
            v >>= shift;
            r |= shift;
            shift = 0x3 - v >> 31 & 0x2;
            v >>= shift;
            r |= shift;
            r |= (v >> 1);
            return r;
        }
    }
}
