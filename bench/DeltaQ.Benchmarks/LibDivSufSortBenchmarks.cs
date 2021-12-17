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

        [Benchmark(Baseline = true)]
        public void ss_compare_old()
        {
            SsSort.new_ss_compare_feature_flag = false;
            foreach (var asset in _assets)
            {
                ldss.Sort(asset).Dispose();
            }
        }

        [Benchmark]
        public void ss_compare_new()
        {
            SsSort.new_ss_compare_feature_flag = true;
            foreach (var asset in _assets)
            {
                ldss.Sort(asset).Dispose();
            }
        }
    }
}
