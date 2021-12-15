using System;
using Microsoft.Extensions.CommandLineUtils;

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

app.Command("diff", command =>
{
    command.Description = "Diff two files";
    command.HelpOption(HelpOptions);

    var oldFileArg = command.Argument("[oldfile]", "");
    var newFileArg = command.Argument("[newfile]", "");
    var deltaFileArg = command.Argument("[deltafile]", "");

    command.OnExecute(() =>
    {
        var oldFile = oldFileArg.Value;
        var newFile = newFileArg.Value;
        var deltaFile = deltaFileArg.Value;
        Console.WriteLine($"Diff: old:{oldFile} new:{newFile} delta:{deltaFile}");
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