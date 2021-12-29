using DeltaQ.SuffixSorting;
using DeltaQ.SuffixSorting.LibDivSufSort;
using DeltaQ.SuffixSorting.SAIS;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.IO;

namespace DeltaQ.CommandLine;
using static Defaults;

internal static partial class Commands
{
    public static Action<CommandLineApplication> DeltaCommand { get; } = command =>
    {
        command.Description = "Generate a delta (difference) between two files";
        command.HelpOption(HelpOptions);

        var oldFileArg = command.Argument("[oldfile]", "");
        var newFileArg = command.Argument("[newfile]", "");
        var deltaFileArg = command.Argument("[deltafile]", "");
        var algoArg = command.Option("-ss|--suffix-sort <LIB>", "Suffix sort library: [sais], [divsufsort]", CommandOptionType.SingleValue);

        command.OnExecute(() =>
        {
            var oldFile = oldFileArg.Value;
            var newFile = newFileArg.Value;
            var deltaFile = deltaFileArg.Value;
            ISuffixSort sort = algoArg.Value() switch
            {
                "sais" => new SAIS(),
                "divsufsort" => new LibDivSufSort(),
                _ => GetDefaultSort()
            };
            BsDiff.Diff.Create(File.ReadAllBytes(oldFile), File.ReadAllBytes(newFile), File.Create(deltaFile), sort);
            Console.WriteLine($"Diff [sort:{sort.GetType()}]: old:{oldFile} new:{newFile} delta:{deltaFile}");
            return 0;
        });
    };
}