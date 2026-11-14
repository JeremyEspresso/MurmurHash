using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

namespace MurmurHash.Benchmarks;

// Hash32Batch against a loop of Hash32 calls. Every benchmark hashes Count keys per invocation; with
// OperationsPerInvoke the reported Mean is the time per key.

/// <summary>Fixed-length keys back to back in one buffer.</summary>
public class Hash32BatchBenchmarks
{
    private const int Count = 1024;
    private byte[] _keys = [];
    private readonly uint[] _destination = new uint[Count];

    // 12 has no SIMD kernel: it shows the fallback that every non-AVX2 machine uses.
    [Params(4, 8, 12, 16, 32, 64)]
    public int KeyLength { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _keys = new byte[KeyLength * Count];
        new Random(42).NextBytes(_keys);
        var expected = new uint[Count];
        Loop();
        _destination.CopyTo(expected, 0);
        foreach (var run in new Action[] { Batch, BatchFallback })
        {
            Array.Clear(_destination);
            run();
            if (!expected.AsSpan().SequenceEqual(_destination))
            {
                throw new InvalidOperationException($"{run.Method.Name} differs from Hash32 at key length {KeyLength}.");
            }
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public void Loop()
    {
        ReadOnlySpan<byte> keys = _keys;
        int keyLength = KeyLength;
        for (int i = 0; i < _destination.Length; i++)
        {
            _destination[i] = MurmurHash3.Hash32(keys.Slice(i * keyLength, keyLength), 69);
        }
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public void Batch() => MurmurHash3.Hash32Batch(_keys, KeyLength, _destination, 69);

    /// <summary>The path used without AVX2 (other CPUs, the .NET Standard builds) and for other key lengths.</summary>
    [Benchmark(OperationsPerInvoke = Count)]
    public void BatchFallback() => MurmurHash3.Hash32BatchScalar(_keys, KeyLength, _destination, 0, Count, 69);
}

/// <summary>
/// <c>uint</c> and <c>ulong</c> keys, against two scalar baselines: a loop of <see cref="MurmurHash3.Hash32(ReadOnlySpan{byte}, uint)"/>
/// on each value's bytes, and a loop hand-written for the constant key length (no length checks, no loop, no tail),
/// the fastest scalar code a caller could write without this library's CPU-specific code path.
/// </summary>
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class Hash32BatchIntegerBenchmarks
{
    private const int Count = 1024;
    private const uint Seed = 69;
    private readonly uint[] _u32 = new uint[Count];
    private readonly ulong[] _u64 = new ulong[Count];
    private readonly uint[] _destination = new uint[Count];

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        random.NextBytes(MemoryMarshal.AsBytes(_u32.AsSpan()));
        random.NextBytes(MemoryMarshal.AsBytes(_u64.AsSpan()));
        var expected = new uint[Count];
        var groups = new (Action Baseline, Action[] Others)[]
        {
            (LoopUInt32, [HandWrittenUInt32, BatchUInt32, BatchFallbackUInt32]),
            (LoopUInt64, [HandWrittenUInt64, BatchUInt64, BatchFallbackUInt64]),
        };
        foreach (var (baseline, others) in groups)
        {
            baseline();
            _destination.CopyTo(expected, 0);
            foreach (var run in others)
            {
                Array.Clear(_destination);
                run();
                if (!expected.AsSpan().SequenceEqual(_destination))
                {
                    throw new InvalidOperationException($"{run.Method.Name} differs from Hash32.");
                }
            }
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    [BenchmarkCategory("uint")]
    public void LoopUInt32()
    {
        ReadOnlySpan<byte> keys = MemoryMarshal.AsBytes(_u32.AsSpan());
        for (int i = 0; i < _destination.Length; i++)
        {
            _destination[i] = MurmurHash3.Hash32(keys.Slice(i * 4, 4), Seed);
        }
    }

    [Benchmark(OperationsPerInvoke = Count)]
    [BenchmarkCategory("uint")]
    public void HandWrittenUInt32()
    {
        uint[] keys = _u32;
        uint[] destination = _destination;
        for (int i = 0; i < keys.Length; i++)
        {
            destination[i] = HandWritten.Hash4(keys[i], Seed);
        }
    }

    [Benchmark(OperationsPerInvoke = Count)]
    [BenchmarkCategory("uint")]
    public void BatchUInt32() => MurmurHash3.Hash32Batch(_u32, _destination, Seed);

    /// <summary>The path used without AVX2.</summary>
    [Benchmark(OperationsPerInvoke = Count)]
    [BenchmarkCategory("uint")]
    public void BatchFallbackUInt32() => MurmurHash3.Hash32BatchScalar(_u32, _destination, 0, Seed);

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    [BenchmarkCategory("ulong")]
    public void LoopUInt64()
    {
        ReadOnlySpan<byte> keys = MemoryMarshal.AsBytes(_u64.AsSpan());
        for (int i = 0; i < _destination.Length; i++)
        {
            _destination[i] = MurmurHash3.Hash32(keys.Slice(i * 8, 8), Seed);
        }
    }

    [Benchmark(OperationsPerInvoke = Count)]
    [BenchmarkCategory("ulong")]
    public void HandWrittenUInt64()
    {
        ulong[] keys = _u64;
        uint[] destination = _destination;
        for (int i = 0; i < keys.Length; i++)
        {
            destination[i] = HandWritten.Hash8(keys[i], Seed);
        }
    }

    [Benchmark(OperationsPerInvoke = Count)]
    [BenchmarkCategory("ulong")]
    public void BatchUInt64() => MurmurHash3.Hash32Batch(_u64, _destination, Seed);

    /// <summary>The path used without AVX2.</summary>
    [Benchmark(OperationsPerInvoke = Count)]
    [BenchmarkCategory("ulong")]
    public void BatchFallbackUInt64() => MurmurHash3.Hash32BatchScalar(_u64, _destination, 0, Seed);

    /// <summary>MurmurHash3_x86_32 written out for a constant 4- or 8-byte key, as a caller might do it.</summary>
    private static class HandWritten
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static uint Hash4(uint key, uint seed) => FMix(Round(seed, key) ^ 4);

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static uint Hash8(ulong key, uint seed) => FMix(Round(Round(seed, (uint)key), (uint)(key >> 32)) ^ 8);

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static uint Round(uint h, uint k)
        {
            k = System.Numerics.BitOperations.RotateLeft(k * 0xcc9e2d51, 15) * 0x1b873593;
            return (System.Numerics.BitOperations.RotateLeft(h ^ k, 13) * 5) + 0xe6546b64;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static uint FMix(uint h)
        {
            h ^= h >> 16;
            h *= 0x85ebca6b;
            h ^= h >> 13;
            h *= 0xc2b2ae35;
            return h ^ (h >> 16);
        }
    }
}
