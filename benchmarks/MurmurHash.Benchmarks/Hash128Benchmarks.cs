// The UInt128 overload only exists in the net8.0 build of the library.
#if !NETSTANDARD_LIBRARY
using BenchmarkDotNet.Attributes;
using MurmurHash.Benchmarks.Baselines;

namespace MurmurHash.Benchmarks;

/// <summary>The public 128-bit API against a direct port of the reference implementation and HashDepot.</summary>
[MemoryDiagnoser]
public class Hash128Benchmarks
{
    private byte[] _input = [];
    private readonly byte[] _destination = new byte[16];

    [Params(3, 7, 15, 16, 40, 128, 1024, 16384)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _input = new byte[Length];
        new Random(42).NextBytes(_input);
        if (ReferenceMurmurHash3x64.Hash128(_input, 69) != MurmurHash3.Hash128(_input, 69))
        {
            throw new InvalidOperationException("Reference port and library disagree.");
        }
    }

    /// <summary>A direct port of SMHasher's MurmurHash3_x64_128.</summary>
    [Benchmark(Baseline = true)]
    public UInt128 Reference() => ReferenceMurmurHash3x64.Hash128(_input, 69);

    /// <summary>The public API: the fused-LEA code path on Ice Lake and newer Intel CPUs, the general path elsewhere.</summary>
    [Benchmark]
    public UInt128 Hash128() => MurmurHash3.Hash128(_input, 69);

    /// <summary>The general code path, which CPUs without a fast 3-component LEA use.</summary>
    [Benchmark]
    public UInt128 Hash128_GeneralPath()
    {
        MurmurHash3.Hash128Core<MurmurHash3.MultiplyStep>(_input, 69, out ulong h1, out ulong h2);
        return new UInt128(h2, h1);
    }

    [Benchmark]
    public void Hash128_Destination() => MurmurHash3.Hash128(_input, _destination, 69);

    [Benchmark]
    public ulong Hash64() => MurmurHash3.Hash64(_input, 69);

    // https://github.com/ssg/HashDepot
    [Benchmark]
    public byte[] HashDepot_Hash128() => HashDepot.MurmurHash3.Hash128(_input, 69);
}

/// <summary>
/// 1024 inputs of random lengths, so the tail's branches cannot be predicted from one call to the next.
/// Reported per input.
/// </summary>
public class Hash128MixedLengthBenchmarks
{
    private const int Count = 1024;
    private byte[] _data = [];
    private int[] _offsets = [];
    private int[] _lengths = [];

    [Params(32, 256)]
    public int MaxLength { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        _data = new byte[Count * MaxLength];
        random.NextBytes(_data);
        _offsets = new int[Count];
        _lengths = new int[Count];
        for (int i = 0; i < Count; i++)
        {
            _lengths[i] = random.Next(MaxLength + 1);
            _offsets[i] = i * MaxLength;
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public UInt128 Reference()
    {
        UInt128 acc = 0;
        for (int i = 0; i < Count; i++)
        {
            acc += ReferenceMurmurHash3x64.Hash128(_data.AsSpan(_offsets[i], _lengths[i]), 69);
        }

        return acc;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public UInt128 Hash128()
    {
        UInt128 acc = 0;
        for (int i = 0; i < Count; i++)
        {
            acc += MurmurHash3.Hash128(_data.AsSpan(_offsets[i], _lengths[i]), 69);
        }

        return acc;
    }
}
#endif
