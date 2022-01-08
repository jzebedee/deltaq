using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using DeltaQ.SuffixSorting;
using DeltaQ.SuffixSorting.LibDivSufSort;
using DeltaQ.SuffixSorting.SAIS;

namespace DeltaQ.Benchmarks
{
    [MemoryDiagnoser]
    [HardwareCounters(HardwareCounter.BranchInstructions, HardwareCounter.BranchMispredictions)]
    public class SuffixSortingBenchmarks
    {
        private static byte[] GetOwnedRandomBuffer(int size)
        {
            var rand = new Random(63 * 13 * 63 * 13);
            var buf = new byte[size];
            rand.NextBytes(buf);
            return buf;
        }

        private const string AbsoluteAssetsPath = @"assets/";
        public static IEnumerable<object[]> Assets { get; } = Directory.EnumerateFiles(AbsoluteAssetsPath)
            .Select(file => new { filename = Path.GetFileName(file), contents = File.ReadAllBytes(file) })
            .Select(a => new object[] { a.filename, a.contents })
            .ToArray();

        public static IEnumerable<int> Sizes
        {
            get
            {
                yield return 0;
                yield return 1;
                yield return 2;
                yield return 4;
                yield return 8;
                yield return 16;
                yield return 32;
                yield return 64;
                yield return 128;
                yield return 256;
                yield return 512;
                yield return 1024;
                yield return 2048;
                yield return 4096;
                yield return 8192;
                yield return 16384;
                yield return 32768;
                for (int i = 64; i <= 1024; i += 64)
                {
                    yield return i * 1024;
                }
            }
        }
        public static IEnumerable<byte[]> Randoms { get; } = Sizes
            .Select(i => new { size = i, asset = GetOwnedRandomBuffer(i) })
            .Select(a => new object[] { i.ToString(), a.asset })
            .ToArray();

        private static readonly ISuffixSort GoSAIS = new GoSAIS();
        private static readonly ISuffixSort SAIS = new SAIS();
        private static readonly ISuffixSort LDSS = new LibDivSufSort();

        [ArgumentsSource(nameof(Randoms))]
        [Benchmark(Baseline = true)]
        public void sais(string name, byte[] asset) => SAIS.Sort(asset).Dispose();

        [ArgumentsSource(nameof(Randoms))]
        [Benchmark]
        public void go_sais(string name, byte[] asset) => GoSAIS.Sort(asset).Dispose();

        [ArgumentsSource(nameof(Randoms))]
        [Benchmark]
        public void ldss(string name, byte[] asset) => LDSS.Sort(asset).Dispose();

    }
}
