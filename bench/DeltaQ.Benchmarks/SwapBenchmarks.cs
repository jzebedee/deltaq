using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using System.Runtime.CompilerServices;

namespace DeltaQ.Benchmarks
{
    //[HardwareCounters(HardwareCounter.BranchInstructions, HardwareCounter.BranchMispredictions)]
    [SimpleJob(RunStrategy.Throughput)]
    public class SwapBenchmarks
    {
        [Benchmark]
        public void SwapTupleByte()
        {
            Span<byte> bytes = stackalloc byte[] { 0, 10, 15, 9, 12 };
            Swap(bytes, 0, 4);
        }

        [Benchmark(Baseline = true)]
        public void SwapTempByte()
        {
            Span<byte> bytes = stackalloc byte[] { 0, 10, 15, 9, 12 };
            Swap(ref bytes[0], ref bytes[4]);
        }

        [Benchmark]
        public void SwapTupleInt()
        {
            Span<int> ints = stackalloc int[] { 0, 10, 15, 9, 12 };
            Swap(ints, 0, 4);
        }

        [Benchmark]
        public void SwapTempInt()
        {
            Span<int> ints = stackalloc int[] { 0, 10, 15, 9, 12 };
            Swap(ref ints[0], ref ints[4]);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap<T>(Span<T> span, int i, int j)
            => (span[j], span[i]) = (span[i], span[j]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp;
            temp = lhs;
            lhs = rhs;
            rhs = temp;
        }
    }
}
