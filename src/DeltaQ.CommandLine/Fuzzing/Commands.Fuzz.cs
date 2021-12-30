using DeltaQ.CommandLine.Fuzzing;
using DeltaQ.SuffixSorting.LibDivSufSort;
using Microsoft.Extensions.CommandLineUtils;
using SharpFuzz;
using System;
using System.IO;

namespace DeltaQ.CommandLine;
using static Defaults;
using static SuffixSortingVerifier;

internal static partial class Commands
{
    public static Action<CommandLineApplication> FuzzCommand { get; } = command =>
    {
        command.Description = "Fuzzit";
        command.HelpOption(HelpOptions);

        command.OnExecute(() =>
        {
            Fuzzer.Run((Stream s) =>
            {
                using var ms = new MemoryStream();
                s.CopyTo(ms);

                if (!ms.TryGetBuffer(out var T))
                {
                    throw new InvalidOperationException();
                }

                var ldss = new LibDivSufSort();
                using var ownedSA = ldss.Sort(T);
                Verify(T, ownedSA.Memory.Span);
            });
            return 0;
        });
    };
}