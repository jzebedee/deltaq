using System;
using System.IO;
using System.Text;
using DeltaQ.SuffixSorting;
using DeltaQ.SuffixSorting.LibDivSufSort;
using DeltaQ.SuffixSorting.SAIS;
using Microsoft.Extensions.CommandLineUtils;
using SharpFuzz;

static void Verify(ReadOnlySpan<byte> input, ReadOnlySpan<int> sa)
{
    //ref byte suff(int index) => ref input[sa[index]];
    for (int i = 0; i < input.Length - 1; i++)
    {
        //if(!(suff(i) < suff(i + 1)))
        var cur = input[sa[i]..];
        var next = input[sa[i + 1]..];
        var cmp = cur.SequenceCompareTo(next);
        if (!(cmp < 0))
        //if (!(cur < next))
        {
            var ex = new InvalidOperationException("Input was unsorted");
            ex.Data["i"] = i;
            ex.Data["j"] = i + 1;
            throw ex;
        }
    }

    var result = DeltaQ.Tests.LDSSChecker.Check(input, sa, true);
    if (result != DeltaQ.Tests.LDSSChecker.ResultCode.Done)
    {
        throw new InvalidOperationException($"Input failed with result code {result}");
    }
}

const string HelpOptions = "-?|-h|--help";

// Description of the application
var app = new CommandLineApplication()
{
    Name = "dq",
    FullName = "DeltaQ",
    Description = "DeltaQ binary diff and patch tool"
};

app.HelpOption(HelpOptions);
app.VersionOption("--version", "0.1.0");

//No args
app.OnExecute(() =>
{
    app.ShowRootCommandFullNameAndVersion();
    app.ShowHint();
    return 0;
});

app.Command("fuzz", command =>
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
});

static ISuffixSort GetDefaultSort() => new LibDivSufSort();
app.Command("diff", command =>
{
    command.Description = "Diff two files";
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
        DeltaQ.BsDiff.Diff.Create(File.ReadAllBytes(oldFile), File.ReadAllBytes(newFile), File.Create(deltaFile), sort);
        Console.WriteLine($"Diff [sort:{sort.GetType()}]: old:{oldFile} new:{newFile} delta:{deltaFile}");
        return 0;
    });
});

try
{
    return app.Execute(args);
}
catch (CommandParsingException ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine();
    app.ShowHelp();
    return -1;
}