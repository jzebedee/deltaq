using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using DeltaQ.SuffixSorting;
using DeltaQ.SuffixSorting.LibDivSufSort;
using DeltaQ.SuffixSorting.SAIS;

namespace DeltaQ.Benchmarks
{
    //[MemoryDiagnoser]
    [HardwareCounters(HardwareCounter.BranchInstructions, HardwareCounter.BranchMispredictions)]
    public class SuffixSortingBenchmarks
    {
        private const string AbsoluteAssetsPath = @"assets/";
        public static IEnumerable<object[]> Assets { get; } = Directory.EnumerateFiles(AbsoluteAssetsPath)
            .Select(file => new object[] { Path.GetFileName(file), File.ReadAllBytes(file) })
            .ToArray();

        private static readonly ISuffixSort LDSS = new LibDivSufSort();
        private static readonly ISuffixSort SAIS = new SAIS();

        [ArgumentsSource(nameof(Assets))]
        [Benchmark]
        public void ldss(string name, byte[] asset)
        {
            LDSS.Sort(asset).Dispose();
        }

        [ArgumentsSource(nameof(Assets))]
        [Benchmark(Baseline = true)]
        public void sais(string name, byte[] asset)
        {
            SAIS.Sort(asset).Dispose();
        }
    }
}
