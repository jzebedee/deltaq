using System.Diagnostics;
using System.IO;

namespace DeltaQ.Tests;

internal static class Crosscheck
{
    [Conditional("CROSSCHECK")]
    internal static void SetupCrosscheckListeners()
    {
        const string crosscheckDir = "crosscheck/";
        const string crosscheckFilename = crosscheckDir + "csharp";
        try
        {
            Directory.CreateDirectory(crosscheckDir);
            File.Create(crosscheckFilename).Dispose();
        }
        catch (IOException) { }

        if (Trace.Listeners[0] is DefaultTraceListener dtl)
        {
            dtl!.LogFileName = "crosscheck/csharp";
        }
        else
        {
            var lflistener = new TextWriterTraceListener(crosscheckFilename);
            Trace.Listeners.Add(lflistener);
        }
    }

    [Conditional("CROSSCHECK")]
    internal static void FinalizeCrosscheck()
    {
        Trace.Flush();
    }
}
