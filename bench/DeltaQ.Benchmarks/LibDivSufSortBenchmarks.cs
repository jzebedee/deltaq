using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using DeltaQ.SuffixSorting.LibDivSufSort;

namespace DeltaQ.Benchmarks
{
    [SimpleJob(RunStrategy.Throughput)]
    public class LibDivSufSortBenchmarks
    {
        private static readonly byte[][] _assets = Directory.EnumerateFiles("./assets/").Select(File.ReadAllBytes).ToArray();

        [Benchmark(Baseline = true)]
        public void ldss()
        {
            //SsSort.new_ss_pivot_feature_flag = false;

            var ldss = new LibDivSufSort();
            foreach (var asset in _assets)
            {
                ldss.Sort(asset).Dispose();
            }
        }

        //[Benchmark]
        //public void ss_pivot_new()
        //{
        //    SsSort.new_ss_pivot_feature_flag = true;

        //    var ldss = new LibDivSufSort();
        //    foreach (var asset in _assets)
        //    {
        //        ldss.Sort(asset).Dispose();
        //    }
        //}
    }
}
