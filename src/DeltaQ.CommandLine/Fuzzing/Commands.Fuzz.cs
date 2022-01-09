using DeltaQ.CommandLine.Fuzzing;
using DeltaQ.SuffixSorting.LibDivSufSort;
using DeltaQ.SuffixSorting.SAIS;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Toolkit.HighPerformance.Buffers;
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

                var sais = new GoSAIS();
                using var ownedSA = sais.Sort(T);
                var SA = ownedSA.Span;

                Verify(T, SA);
            });
            return 0;
        });
    };
}