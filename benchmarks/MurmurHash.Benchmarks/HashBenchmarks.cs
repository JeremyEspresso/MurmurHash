using BenchmarkDotNet.Attributes;
using MurmurHash.Benchmarks.Baselines;

namespace MurmurHash.Benchmarks;

[MemoryDiagnoser]
public class HashBenchmarks
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

    [Benchmark(Baseline = true)]
    public uint Original()
    {
        ReadOnlySpan<byte> span = _input;
        return OriginalMurmurHash3.Hash32(ref span, 69);
    }

    [Benchmark]
    public uint Current() => MurmurHash3.Hash32(_input, 69);

    // https://github.com/odinmillion/MurmurHash.Net
    [Benchmark]
    public uint OdinMillion() => global::MurmurHash.Net.MurmurHash3.Hash32(_input, 69);

    // https://github.com/ssg/HashDepot
    [Benchmark]
    public uint HashDepot() => global::HashDepot.MurmurHash3.Hash32(_input, 69);
}
