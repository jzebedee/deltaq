using Humanizer;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace DeltaQ.CommandLine;
using static Defaults;

internal static partial class Commands
{
    public static Action<CommandLineApplication> BsPatchCommand { get; } = command =>
    {
        command.Description = "Apply a BSDIFF-compatible delta (patch) to an original file and generate an output file";
        command.HelpOption(HelpOptions);

        var oldFileArg = command.Argument("[oldfile]", "Original file (input)");
        var deltaFileArg = command.Argument("[deltafile]", "Delta file (input)");
        var newFileArg = command.Argument("[newfile]", "New file (output)");

        command.OnExecute(() =>
        {
            var oldFile = oldFileArg.Value;
            var newFile = newFileArg.Value;
            var deltaFile = deltaFileArg.Value;
            Console.WriteLine("Applying BsDiff delta between");
            Console.WriteLine($@"Old file:   ""{oldFile}""");
            Console.WriteLine($@"Delta file: ""{deltaFile}""");
            Console.WriteLine();
            try
            {
                Stopwatch sw;
                {
                    using var fsInput = File.OpenRead(oldFile);
                    using var fsDelta = MemoryMappedFile.CreateFromFile(deltaFile, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
                    using var fsOutput = File.Create(newFile);
                    sw = Stopwatch.StartNew();
                    BsDiff.Patch.Apply(fsInput, OpenPatch, fsOutput);
                    sw.Stop();

                    Stream OpenPatch(long offset, long length) => fsDelta.CreateViewStream(offset, length, MemoryMappedFileAccess.Read);
                }

                Console.WriteLine($"Finished in {sw.Elapsed.Humanize()} [{sw.Elapsed}]");
                Console.WriteLine($@"New file: ""{newFile}""");
            }
            catch
            {
                Console.Error.WriteLine("Failed to apply delta");
                throw;
            }
            return 0;
        });
    };
}