using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;

// Extra options (everything else is passed to BenchmarkDotNet):
//   --tfms net8.0,net10.0   runtimes to benchmark (default: the runtime this process runs on)
//   --netstandard           additionally benchmark the netstandard2.1 build of the library on each runtime
//   --quick                 fewer iterations, for exploratory runs only
var tfms = new List<string>();
bool netstandard = false, quick = false;
var bdnArgs = new List<string>();
for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--tfms": tfms.AddRange(args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries)); break;
        case "--netstandard": netstandard = true; break;
        case "--quick": quick = true; break;
        default: bdnArgs.Add(args[i]); break;
    }
}

var config = DefaultConfig.Instance.WithOptions(ConfigOptions.JoinSummary);
if (tfms.Count > 0 || netstandard || quick)
{
    config = config.HideColumns("Arguments");
    if (tfms.Count == 0)
    {
        tfms.Add($"net{Environment.Version.Major}.0");
    }

    foreach (var tfm in tfms)
    {
        var name = ".NET " + tfm[3..];
        var baseJob = (quick ? Job.Default.WithWarmupCount(4).WithIterationCount(12).WithLaunchCount(1) : Job.Default)
            .WithRuntime(CoreRuntime.CreateForNewVersion(tfm, name))
            .WithToolchain(CsProjCoreToolchain.From(new NetCoreAppSettings(tfm, null, name)));
        config = config.AddJob(baseJob.WithId(name));
        if (netstandard)
        {
            config = config.AddJob(baseJob
                .WithArguments([new MsBuildArgument("/p:LibraryTargetFramework=netstandard2.1")])
                .WithId(name + " (ns2.1 lib)"));
        }
    }
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run([.. bdnArgs], config);
