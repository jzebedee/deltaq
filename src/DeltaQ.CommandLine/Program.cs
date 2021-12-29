using System;
using DeltaQ.CommandLine;
using Microsoft.Extensions.CommandLineUtils;
using static DeltaQ.CommandLine.Defaults;

// Description of the application
var app = new CommandLineApplication()
{
    Name = "dq",
    FullName = "DeltaQ",
    Description = "DeltaQ binary diff and patch tool"
};

app.HelpOption(HelpOptions);
app.VersionOption("--version", typeof(Program).Assembly.GetName().Version.ToString());

//No args
app.OnExecute(() =>
{
    app.ShowRootCommandFullNameAndVersion();
    app.ShowHint();
    return 0;
});

#if FUZZ
app.Command("fuzz", Commands.FuzzCommand);
#endif

app.Command("bsdiff", Commands.BsDiffCommand);
app.Command("bspatch", Commands.BsPatchCommand);

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