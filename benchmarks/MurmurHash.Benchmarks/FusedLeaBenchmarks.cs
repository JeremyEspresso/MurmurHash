using BenchmarkDotNet.Attributes;

namespace MurmurHash.Benchmarks;

public class FusedLeaBenchmarks
{
    private byte[] _input = [];

    [Params(3, 16, 40, 128, 1024, 16384)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _input = new byte[Length];
        new Random(42).NextBytes(_input);
    }

    /// <summary>The previously shipped code path, called directly.</summary>
    [Benchmark(Baseline = true)]
    public uint Multiply() => MurmurHash3.Hash32Core<MurmurHash3.MultiplyStep>(_input, 69);

    /// <summary>The fused-LEA path, called directly.</summary>
    [Benchmark]
    public uint FusedLea() => MurmurHash3.Hash32Core<MurmurHash3.FusedLeaStep>(_input, 69);

    /// <summary>The public API with the CPU check: should equal FusedLea on this CPU (no per-call cost).</summary>
    [Benchmark]
    public uint PublicDispatch() => MurmurHash3.Hash32(_input, 69);
}
