using DeltaQ.SuffixSorting;
using DeltaQ.SuffixSorting.SAIS;

namespace DeltaQ.Tests;

public class GoSAISTests : SAISTester
{
    protected override ISuffixSort Impl => new GoSAIS();
}
