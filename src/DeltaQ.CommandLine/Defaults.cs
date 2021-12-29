using DeltaQ.SuffixSorting;
using DeltaQ.SuffixSorting.LibDivSufSort;

namespace DeltaQ.CommandLine;

internal static class Defaults
{
    public const string HelpOptions = "-?|-h|--help";
    public static ISuffixSort GetDefaultSort() => new LibDivSufSort();

}
