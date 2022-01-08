using DeltaQ.SuffixSorting;
using DeltaQ.SuffixSorting.SAIS;

namespace DeltaQ.Tests;

public class SAISTests : SAISTester
{
    protected override ISuffixSort Impl => new SAIS();
}
