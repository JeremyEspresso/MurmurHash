// UInt128 results need the net8.0 build of the library.
#if !NETSTANDARD_LIBRARY
using System.IO.Hashing;
using BenchmarkDotNet.Attributes;

namespace MurmurHash.Benchmarks;

/// <summary>
/// The incremental classes (one Append of the whole input, then the hash) against the one-shot API.
/// XxHash32/XxHash128 from System.IO.Hashing, used the same way, are included for context.
/// </summary>
[MemoryDiagnoser]
public class IncrementalBenchmarks
{
    private byte[] _input = [];
    private readonly MurmurHash32 _murmur32 = new(69);
    private readonly MurmurHash128 _murmur128 = new(69);
    private readonly XxHash32 _xx32 = new(69);
    private readonly XxHash128 _xx128 = new(69);

    [Params(16, 128, 1024, 16384)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _input = new byte[Length];
        new Random(42).NextBytes(_input);
        if (Incremental32() != MurmurHash3.Hash32(_input, 69) || Incremental128() != MurmurHash3.Hash128(_input, 69))
        {
            throw new InvalidOperationException("Incremental and one-shot results differ.");
        }
    }

    [Benchmark]
    public uint OneShot32() => MurmurHash3.Hash32(_input, 69);

    [Benchmark]
    public uint Incremental32()
    {
        _murmur32.Reset();
        _murmur32.Append(_input);
        return _murmur32.GetCurrentHashAsUInt32();
    }

    [Benchmark]
    public uint XxHash32()
    {
        _xx32.Reset();
        _xx32.Append(_input);
        return _xx32.GetCurrentHashAsUInt32();
    }

    [Benchmark]
    public UInt128 OneShot128() => MurmurHash3.Hash128(_input, 69);

    [Benchmark]
    public UInt128 Incremental128()
    {
        _murmur128.Reset();
        _murmur128.Append(_input);
        return _murmur128.GetCurrentHashAsUInt128();
    }

    [Benchmark]
    public UInt128 XxHash128()
    {
        _xx128.Reset();
        _xx128.Append(_input);
        return _xx128.GetCurrentHashAsUInt128();
    }
}

/// <summary>
/// 16 KB appended in chunks, the worst case for the partial-block handling: 7 bytes (never block-aligned),
/// 64 bytes (aligned) and 1500 bytes (a network packet; not a multiple of 16).
/// </summary>
public class ChunkedAppendBenchmarks
{
    private byte[] _input = [];
    private readonly MurmurHash32 _murmur32 = new(69);
    private readonly MurmurHash128 _murmur128 = new(69);
    private readonly XxHash32 _xx32 = new(69);
    private readonly XxHash128 _xx128 = new(69);

    [Params(7, 64, 1500)]
    public int Chunk { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _input = new byte[16384];
        new Random(42).NextBytes(_input);
        if (Incremental32() != MurmurHash3.Hash32(_input, 69) || Incremental128() != MurmurHash3.Hash128(_input, 69))
        {
            throw new InvalidOperationException("Chunked and one-shot results differ.");
        }
    }

    [Benchmark]
    public uint Incremental32()
    {
        _murmur32.Reset();
        Append(_murmur32);
        return _murmur32.GetCurrentHashAsUInt32();
    }

    [Benchmark]
    public uint XxHash32()
    {
        _xx32.Reset();
        Append(_xx32);
        return _xx32.GetCurrentHashAsUInt32();
    }

    [Benchmark]
    public UInt128 Incremental128()
    {
        _murmur128.Reset();
        Append(_murmur128);
        return _murmur128.GetCurrentHashAsUInt128();
    }

    [Benchmark]
    public UInt128 XxHash128()
    {
        _xx128.Reset();
        Append(_xx128);
        return _xx128.GetCurrentHashAsUInt128();
    }

    private void Append(NonCryptographicHashAlgorithm hash)
    {
        ReadOnlySpan<byte> input = _input;
        for (int i = 0; i < input.Length; i += Chunk)
        {
            hash.Append(input.Slice(i, Math.Min(Chunk, input.Length - i)));
        }
    }
}
#endif
