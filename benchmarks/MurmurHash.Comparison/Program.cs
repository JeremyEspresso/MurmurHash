using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

// Data.HashFunction 3.1.2's published assemblies are Debug builds; they are measured as shipped, so BenchmarkDotNet's
// check for non-optimized dependencies is turned off.
var config = DefaultConfig.Instance
    .WithOptions(ConfigOptions.JoinSummary | ConfigOptions.DisableOptimizationsValidator)
    .AddJob(Job.Default.WithRuntime(CoreRuntime.Core80).WithId(".NET 8.0"))
    .AddJob(Job.Default.WithRuntime(CoreRuntime.Core10_0).WithId(".NET 10.0"));
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
