using BenchmarkDotNet.Attributes;
using System.Runtime.CompilerServices;
using Idx = System.Int32;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using System.Numerics;

namespace DeltaQ.Benchmarks;

[HardwareCounters(HardwareCounter.BranchInstructions, HardwareCounter.BranchMispredictions)]
[SimpleJob(RunStrategy.Throughput)]
public class Log2Benchmarks
{
    public static IEnumerable<int> Numbers { get; } = new[] { 0, 1, 2, 32, 51, 1024, 2000, 48000, int.MaxValue };

    [ArgumentsSource(nameof(Numbers))]
    [Benchmark(Baseline = true)]
    public void tr_ilg(int i)
    {
        Idx y = tr_ilg_core(i);
        GC.KeepAlive(y);
    }

    [ArgumentsSource(nameof(Numbers))]
    [Benchmark]
    public void Log2(int i)
    {
        Idx y = Log2_core(i);
        GC.KeepAlive(y);
    }

    [ArgumentsSource(nameof(Numbers))]
    [Benchmark]
    public void Log2BitOps(int i)
    {
        Idx y = Log2BitOps_core((uint)i);
        GC.KeepAlive(y);
    }

#if NETCOREAPP3_0_OR_GREATER
    [ArgumentsSource(nameof(Numbers))]
    [Benchmark]
    public void MathLog2(int i)
    {
        Idx y = (int)Math.Log2(i);
        GC.KeepAlive(y);
    }

    [ArgumentsSource(nameof(Numbers))]
    [Benchmark]
    public void MathFLog2(int i)
    {
        Idx y = (int)MathF.Log2(i);
        GC.KeepAlive(y);
    }

    [ArgumentsSource(nameof(Numbers))]
    [Benchmark]
    public void MathILogB(int i)
    {
        Idx y = Math.ILogB(i);
        GC.KeepAlive(y);
    }

    [ArgumentsSource(nameof(Numbers))]
    [Benchmark]
    public void MathFILogB(int i)
    {
        Idx y = MathF.ILogB(i);
        GC.KeepAlive(y);
    }
#endif

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static int tr_ilg_core(int n)
    {
        if ((n & 0xffff_0000) > 0)
        {
            if ((n & 0xff00_0000) > 0)
            {
                return 24 + lg_table[(n >> 24) & 0xff];
            }
            else
            {
                return 16 + lg_table[(n >> 16) & 0xff];
            }
        }
        else
        {
            if ((n & 0x0000_ff00) > 0)
            {
                return 8 + lg_table[(n >> 8) & 0xff];
            }
            else
            {
                return 0 + lg_table[(n >> 0) & 0xff];
            }
        }
    }
    private static readonly int[] lg_table_array = new[]
    {
     -1,0,1,1,2,2,2,2,3,3,3,3,3,3,3,3,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,
      5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,
      6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,
      6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,
      7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
      7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
      7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
      7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
    };
    private static ReadOnlySpan<int> lg_table => lg_table_array;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static int Log2_core(int v)
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
        r |= v >> 1;
        return r;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static int Log2BitOps_core(uint v) => BitOperations.Log2(v);
}
