using BenchmarkDotNet.Attributes;
using Idx = System.Int32;
using BenchmarkDotNet.Engines;

namespace DeltaQ.Benchmarks
{
    //[HardwareCounters(HardwareCounter.BranchInstructions, HardwareCounter.BranchMispredictions)]
    [SimpleJob(RunStrategy.Throughput)]
    public class BsSpanWriteBenchmarks
    {
        public static IEnumerable<long> Numbers
        {
            get
            {
                yield return long.MinValue;
                yield return long.MinValue / 2;
                yield return long.MinValue / 3;
                yield return 0;
                yield return long.MaxValue;
                yield return long.MaxValue / 2;
                yield return long.MaxValue / 3;
            }
        }

        [ArgumentsSource(nameof(Numbers))]
        [Benchmark(Baseline = true)]
        public void SpanWrite(long y)
        {
            Span<byte> span = stackalloc byte[sizeof(long)];
            WritePackedLong(span, y);
        }

        [ArgumentsSource(nameof(Numbers))]
        [Benchmark]
        public void SpanWriteStartHigh(long y)
        {
            Span<byte> span = stackalloc byte[sizeof(long)];
            WritePackedLongStartHigh(span, y);
        }

        public static void WritePackedLong(Span<byte> span, long y)
        {
            if (y < 0)
            {
                y = -y;

                span[0] = (byte)y;
                span[1] = (byte)(y >>= 8);
                span[2] = (byte)(y >>= 8);
                span[3] = (byte)(y >>= 8);
                span[4] = (byte)(y >>= 8);
                span[5] = (byte)(y >>= 8);
                span[6] = (byte)(y >>= 8);
                span[7] = (byte)((y >> 8) | 0x80);
            }
            else
            {
                span[0] = (byte)y;
                span[1] = (byte)(y >>= 8);
                span[2] = (byte)(y >>= 8);
                span[3] = (byte)(y >>= 8);
                span[4] = (byte)(y >>= 8);
                span[5] = (byte)(y >>= 8);
                span[6] = (byte)(y >>= 8);
                span[7] = (byte)(y >> 8);
            }
        }

        public static void WritePackedLongStartHigh(Span<byte> span, long y)
        {
            if (y < 0)
            {
                y = -y;
                span[7] = (byte)((y >> 56) | 0x80);
            }
            else
            {
                span[7] = (byte)(y >> 56);
            }

            span[6] = (byte)(y >> 48);
            span[5] = (byte)(y >> 40);
            span[4] = (byte)(y >> 32);
            span[3] = (byte)(y >> 24);
            span[2] = (byte)(y >> 16);
            span[1] = (byte)(y >> 8);
            span[0] = (byte)y;
        }
    }
}
