using System.Buffers.Binary;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using Data.HashFunction.MurmurHash;
using HashifyNet;

namespace MurmurHash.Comparison;

/// <summary>How the data reaches the hash: one call, many small calls, or a stream.</summary>
public sealed record Scenario(string Name, int Length, int Chunk, bool Stream)
{
    public static IEnumerable<Scenario> All =>
    [
        new("16 B", 16, 0, false),
        new("1 KB", 1024, 0, false),
        new("16 KB", 16384, 0, false),
        new("16 KB in 7 B pieces", 16384, 7, false),
        new("16 KB in 1500 B pieces", 16384, 1500, false),
        new("1 MB stream", 1 << 20, 0, true),
    ];

    public override string ToString() => Name;
}

/// <summary>
/// Incremental MurmurHash3 through each library's incremental API, and streams through each library's stream API:
/// <list type="bullet">
/// <item>this library: MurmurHash32/MurmurHash128 (NonCryptographicHashAlgorithm.Append);</item>
/// <item>murmurhash: HashAlgorithm.TransformBlock, the API its README advertises;</item>
/// <item>Data.HashFunction 3.1.2: block transformer, TransformBytes(ArraySegment) - its
/// TransformBytes(byte[], int, int) ignores offset and count;</item>
/// <item>HashifyNET 7.1.1: block transformer, TransformBytes(ReadOnlySpan) - its TransformBytes(byte[], int, int)
/// also ignores offset and count.</item>
/// </list>
/// Each library is checked against the reference hash in its own setup, so a library that returns a wrong hash for a
/// scenario fails that benchmark (shown as NA) instead of being timed.
/// </summary>
[MemoryDiagnoser]
public abstract class IncrementalComparisonBase
{
    private byte[] _input = [];
    private MemoryStream _stream = new();
    private IMurmurHash3 _dataHashFunction = null!;
    private HashifyNet.Algorithms.MurmurHash.IMurmurHash3 _hashify = null!;

    [ParamsSource(nameof(Scenarios))]
    public Scenario Scenario { get; set; } = Scenario.All.First();

    public static IEnumerable<Scenario> Scenarios => Scenario.All;

    protected abstract int HashBits { get; }

    protected abstract byte[] Expected(byte[] input);

    protected abstract byte[] HashWithThisLibrary();

    protected abstract HashAlgorithm Murmurhash { get; }

    protected abstract HashAlgorithm MurmurhashUnsafe { get; }

    private int Chunk => Scenario.Chunk == 0 ? _input.Length : Scenario.Chunk;

    private void Initialize()
    {
        _input = new byte[Scenario.Length];
        new Random(42).NextBytes(_input);
        _stream = new MemoryStream(_input, writable: false);
        _dataHashFunction = MurmurHash3Factory.Instance.Create(new MurmurHash3Config { HashSizeInBits = HashBits, Seed = 69 });
        _hashify = HashFactory<HashifyNet.Algorithms.MurmurHash.IMurmurHash3>.Create(
            new HashifyNet.Algorithms.MurmurHash.MurmurHash3Config { HashSizeInBits = HashBits, Seed = 69 });
    }

    private void Verify(Func<byte[]> hash, string library)
    {
        Initialize();
        if (!Expected(_input).AsSpan().SequenceEqual(hash()))
        {
            throw new InvalidOperationException($"{library} returns a wrong hash for '{Scenario}'.");
        }
    }

    [GlobalSetup(Target = nameof(ThisLibrary))]
    public void SetupThisLibrary() => Verify(ThisLibrary, "This library");

    [GlobalSetup(Target = nameof(Murmurhash_))]
    public void SetupMurmurhash() => Verify(Murmurhash_, "murmurhash");

    [GlobalSetup(Target = nameof(MurmurhashUnsafe_))]
    public void SetupMurmurhashUnsafe() => Verify(MurmurhashUnsafe_, "murmurhash (unsafe)");

    [GlobalSetup(Target = nameof(DataHashFunction))]
    public void SetupDataHashFunction() => Verify(DataHashFunction, "Data.HashFunction");

    [GlobalSetup(Target = nameof(HashifyNet_))]
    public void SetupHashifyNet() => Verify(HashifyNet_, "HashifyNET");

    [Benchmark(Baseline = true)]
    public byte[] ThisLibrary() => HashWithThisLibrary();

    [Benchmark(Description = "murmurhash")]
    public byte[] Murmurhash_() => MurmurhashHash(Murmurhash);

    [Benchmark(Description = "murmurhash (unsafe)")]
    public byte[] MurmurhashUnsafe_() => MurmurhashHash(MurmurhashUnsafe);

    [Benchmark(Description = "Data.HashFunction")]
    public byte[] DataHashFunction()
    {
        if (Scenario.Stream)
        {
            _stream.Position = 0;
            return _dataHashFunction.ComputeHash(_stream).Hash;
        }

        var transformer = _dataHashFunction.CreateBlockTransformer();
        for (int i = 0; i < _input.Length; i += Chunk)
        {
            transformer.TransformBytes(new ArraySegment<byte>(_input, i, Math.Min(Chunk, _input.Length - i)));
        }

        return transformer.FinalizeHashValue().Hash;
    }

    [Benchmark(Description = "HashifyNET")]
    public byte[] HashifyNet_()
    {
        if (Scenario.Stream)
        {
            _stream.Position = 0;
            return _hashify.ComputeHash(_stream).AsByteArray();
        }

        var transformer = _hashify.CreateBlockTransformer();
        for (int i = 0; i < _input.Length; i += Chunk)
        {
            transformer.TransformBytes(new ReadOnlySpan<byte>(_input, i, Math.Min(Chunk, _input.Length - i)));
        }

        return transformer.FinalizeHashValue().AsByteArray();
    }

    /// <summary>Feeds the input to this library's incremental class the way the scenario says.</summary>
    protected void Feed(System.IO.Hashing.NonCryptographicHashAlgorithm hash)
    {
        hash.Reset();
        if (Scenario.Stream)
        {
            _stream.Position = 0;
            hash.Append(_stream);
            return;
        }

        for (int i = 0; i < _input.Length; i += Chunk)
        {
            hash.Append(_input.AsSpan(i, Math.Min(Chunk, _input.Length - i)));
        }
    }

    private byte[] MurmurhashHash(HashAlgorithm hash)
    {
        if (Scenario.Stream)
        {
            _stream.Position = 0;
            return hash.ComputeHash(_stream);
        }

        hash.Initialize();
        for (int i = 0; i < _input.Length; i += Chunk)
        {
            hash.TransformBlock(_input, i, Math.Min(Chunk, _input.Length - i), null, 0);
        }

        hash.TransformFinalBlock([], 0, 0);
        return hash.Hash!;
    }
}

public class Incremental32Comparison : IncrementalComparisonBase
{
    private readonly MurmurHash32 _thisLibrary = new(69);
    private readonly HashAlgorithm _murmurhash = Murmur.MurmurHash.Create32(69, managed: true);
    private readonly HashAlgorithm _murmurhashUnsafe = Murmur.MurmurHash.Create32(69, managed: false);
    private readonly byte[] _hash = new byte[4];

    protected override int HashBits => 32;

    protected override HashAlgorithm Murmurhash => _murmurhash;

    protected override HashAlgorithm MurmurhashUnsafe => _murmurhashUnsafe;

    protected override byte[] Expected(byte[] input)
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, MurmurHash3.Hash32(input, 69));
        return bytes;
    }

    // Writes into a reused array, so the benchmark measures hashing rather than allocating a result.
    protected override byte[] HashWithThisLibrary()
    {
        Feed(_thisLibrary);
        BinaryPrimitives.WriteUInt32LittleEndian(_hash, _thisLibrary.GetCurrentHashAsUInt32());
        return _hash;
    }
}

public class Incremental128Comparison : IncrementalComparisonBase
{
    private readonly MurmurHash128 _thisLibrary = new(69);
    private readonly HashAlgorithm _murmurhash = Murmur.MurmurHash.Create128(69, managed: true, preference: Murmur.AlgorithmPreference.X64);
    private readonly HashAlgorithm _murmurhashUnsafe = Murmur.MurmurHash.Create128(69, managed: false, preference: Murmur.AlgorithmPreference.X64);
    private readonly byte[] _hash = new byte[16];

    protected override int HashBits => 128;

    protected override HashAlgorithm Murmurhash => _murmurhash;

    protected override HashAlgorithm MurmurhashUnsafe => _murmurhashUnsafe;

    protected override byte[] Expected(byte[] input)
    {
        var bytes = new byte[16];
        MurmurHash3.Hash128(input, bytes, 69);
        return bytes;
    }

    protected override byte[] HashWithThisLibrary()
    {
        Feed(_thisLibrary);
        BinaryPrimitives.WriteUInt128LittleEndian(_hash, _thisLibrary.GetCurrentHashAsUInt128());
        return _hash;
    }
}
