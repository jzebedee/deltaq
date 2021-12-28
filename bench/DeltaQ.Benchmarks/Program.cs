using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Running;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;

IConfig? config = null;
if (args.Any() && args[0] == "etl")
{
    var bdnDefaults = (ClrTraceEventParser.Keywords)167993uL;
    var keywords = bdnDefaults | ClrTraceEventParser.Keywords.GCSampledObjectAllocationHigh | ClrTraceEventParser.Keywords.Threading;
    var providers = new (Guid, TraceEventLevel, ulong, TraceEventProviderOptions?)[]
    {
    (ClrTraceEventParser.ProviderGuid, TraceEventLevel.Verbose, (ulong)keywords, new TraceEventProviderOptions
    {
        StacksEnabled = true,
    }),
    (new Guid("0866B2B8-5CEF-5DB9-2612-0C0FFD814A44"), TraceEventLevel.Informational, ulong.MaxValue, null)
    };
    var profilerConfig = new EtwProfilerConfig(providers: providers);
    var profiler = new EtwProfiler(profilerConfig);
    config = DefaultConfig.Instance.AddDiagnoser(profiler);
}

BenchmarkSwitcher.FromAssemblies(new[] { typeof(Program).Assembly }).Run(args, config);