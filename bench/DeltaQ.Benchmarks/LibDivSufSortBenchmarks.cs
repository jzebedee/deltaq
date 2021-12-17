using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using DeltaQ.SuffixSorting.LibDivSufSort;

namespace DeltaQ.Benchmarks
{
    [SimpleJob(RunStrategy.Throughput)]
    public class LibDivSufSortBenchmarks
    {
        private LibDivSufSort ldss = new LibDivSufSort();

        private static readonly byte[][] _assets = Directory.EnumerateFiles("./assets/").Select(File.ReadAllBytes).ToArray();

    }
}
