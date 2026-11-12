using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

// Extra options (everything else is passed to BenchmarkDotNet):
//   --tfms net8.0,net10.0   runtimes to benchmark (default: the runtime this process runs on)
//   --netstandard           additionally benchmark the netstandard2.1 build of the library on each runtime
//   --quick                 fewer iterations, for exploratory runs only
//   --aot                   additionally benchmark each runtime compiled with NativeAOT (no tiered JIT / dynamic PGO)
//   --aot-isa <set>         with --aot: also a NativeAOT job compiled for an instruction set, e.g. x86-64-v3 (IlcInstructionSet)
var tfms = new List<string>();
bool netstandard = false, quick = false, aot = false;
string? aotIsa = null;
var bdnArgs = new List<string>();
for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--tfms": tfms.AddRange(args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries)); break;
        case "--netstandard": netstandard = true; break;
        case "--quick": quick = true; break;
        case "--aot": aot = true; break;
        case "--aot-isa": aotIsa = args[++i]; break;
        default: bdnArgs.Add(args[i]); break;
    }
}

var config = DefaultConfig.Instance.WithOptions(ConfigOptions.JoinSummary);
if (tfms.Count > 0 || netstandard || quick || aot)
{
    config = config.HideColumns("Arguments");
    if (tfms.Count == 0)
    {
        tfms.Add($"net{Environment.Version.Major}.0");
    }

    foreach (var tfm in tfms)
    {
        var name = ".NET " + tfm[3..];
        Runtime runtime = tfm switch
        {
            "net8.0" => CoreRuntime.Core80,
            "net9.0" => CoreRuntime.Core90,
            "net10.0" => CoreRuntime.Core10_0,
            "net11.0" => CoreRuntime.Core11_0,
            _ => throw new ArgumentException($"Unsupported TFM {tfm}"),
        };
        var baseJob = (quick ? Job.Default.WithWarmupCount(4).WithIterationCount(12).WithLaunchCount(1) : Job.Default)
            .WithRuntime(runtime);
        config = config.AddJob(baseJob.WithId(name));
        if (aot)
        {
            Runtime aotRuntime = tfm switch
            {
                "net8.0" => NativeAotRuntime.Net80,
                "net9.0" => NativeAotRuntime.Net90,
                "net10.0" => NativeAotRuntime.Net10_0,
                "net11.0" => NativeAotRuntime.Net11_0,
                _ => throw new ArgumentException($"Unsupported TFM {tfm}"),
            };

            // NativeAOT links with clang by default; fall back to gcc where clang is not installed.
            var linker = !OperatingSystem.IsWindows() && !File.Exists("/usr/bin/clang") && File.Exists("/usr/bin/gcc")
                ? new[] { new MsBuildArgument("/p:CppCompilerAndLinker=gcc") }
                : [];
            config = config.AddJob(baseJob.WithRuntime(aotRuntime).WithArguments(linker).WithId(name + " NativeAOT"));
            if (aotIsa is not null)
            {
                config = config.AddJob(baseJob.WithRuntime(aotRuntime)
                    .WithArguments([.. linker, new MsBuildArgument($"/p:IlcInstructionSet={aotIsa}")])
                    .WithId($"{name} NativeAOT {aotIsa}"));
            }
        }
        if (netstandard)
        {
            config = config.AddJob(baseJob
                .WithArguments([new MsBuildArgument("/p:LibraryTargetFramework=netstandard2.1")])
                .WithId(name + " (ns2.1 lib)"));
        }
    }
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run([.. bdnArgs], config);
