using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace MurmurHash.UnitTests;

/// <summary>
/// Hash32Batch must write exactly what a loop of <see cref="MurmurHash3.Hash32(ReadOnlySpan{byte}, uint)"/> writes.
/// "Public" runs the public method (the AVX2 path on CPUs that have it); "Scalar" runs the fallback directly, so
/// both paths are tested on every machine.
/// </summary>
public class Hash32BatchTests
{
    private delegate void Batch(ReadOnlySpan<byte> keys, int keyLength, Span<uint> destination, uint seed);

    private static readonly uint[] Seeds = [0, 1, 69, 0x9747b28c, 0xFFFFFFFF];

    public static TheoryData<string> Paths => new() { "Public", "Scalar" };

    private static Batch Get(string path) => path switch
    {
        "Public" => MurmurHash3.Hash32Batch,
        "Scalar" => (keys, keyLength, destination, seed) =>
            MurmurHash3.Hash32BatchScalar(keys, keyLength, destination, 0, keys.Length / keyLength, seed),
        _ => throw new ArgumentException(path),
    };

    private static uint[] Expected(ReadOnlySpan<byte> keys, int keyLength, int count, uint seed)
    {
        var expected = new uint[count];
        for (int i = 0; i < count; i++)
        {
            expected[i] = MurmurHash3.Hash32(keys.Slice(i * keyLength, keyLength), seed);
        }

        return expected;
    }

    [Theory]
    [MemberData(nameof(Paths))]
    public void ReferenceVectors(string path)
    {
        var batch = Get(path);
        foreach (var v in TestVectors.All.Where(v => v.Length > 0))
        {
            foreach (var count in new[] { 1, 8, 19 })
            {
                var keys = new byte[v.Length * count];
                for (int i = 0; i < count; i++)
                {
                    TestVectors.Data.AsSpan(0, v.Length).CopyTo(keys.AsSpan(i * v.Length));
                }

                var actual = new uint[count];
                batch(keys, v.Length, actual, v.Seed);
                Assert.True(actual.All(h => h == v.Hash32), $"{path} length={v.Length} seed={v.Seed} count={count}");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Paths))]
    public void RandomKeys_AllLengths_UnalignedAndPoisoned(string path)
    {
        var batch = Get(path);
        var random = new Random(12345);
        foreach (var keyLength in Enumerable.Range(1, 300))
        {
            foreach (var count in new[] { 0, 1, 3, 4, 7, 8, 9, 15, 16, 17, 33 })
            {
                foreach (var seed in Seeds)
                {
                    int offset = random.Next(0, 8);
                    var data = new byte[keyLength * count];
                    random.NextBytes(data);
                    var keys = TestVectors.Poisoned(data, offset, (byte)random.Next(256));

                    // One element more than needed: it must not be written.
                    var actual = new uint[count + 1];
                    actual[count] = 0xDEADBEEF;
                    batch(keys, keyLength, actual, seed);
                    var expected = Expected(data, keyLength, count, seed);
                    Assert.True(expected.AsSpan().SequenceEqual(actual.AsSpan(0, count)), $"{path} keyLength={keyLength} count={count} seed={seed} offset={offset}");
                    Assert.Equal(0xDEADBEEF, actual[count]);
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(Paths))]
    public void KeysAtGuardPage_DoesNotReadOutside(string path)
    {
        Assert.SkipUnless(GuardedMemory.IsSupported, "Guard pages are only implemented on Linux.");
        var batch = Get(path);
        var random = new Random(777);
        foreach (var keyLength in Enumerable.Range(1, 300))
        {
            foreach (var count in new[] { 1, 8, 9, 16, 17, 24 })
            {
                var data = new byte[keyLength * count];
                random.NextBytes(data);
                var expected = Expected(data, keyLength, count, 42);
                foreach (var guardBefore in new[] { false, true })
                {
                    GuardedMemory.Run(data, keys =>
                    {
                        var actual = new uint[count];
                        batch(keys, keyLength, actual, 42);
                        Assert.True(expected.AsSpan().SequenceEqual(actual), $"{path} keyLength={keyLength} count={count} guardBefore={guardBefore}");
                    }, guardBefore);
                }
            }
        }
    }

    [Fact]
    public void UInt32AndUInt64Keys_MatchHash32OfTheirLittleEndianBytes()
    {
        var random = new Random(99);
        foreach (var count in new[] { 0, 1, 7, 8, 9, 17, 1000 })
        {
            foreach (var seed in Seeds)
            {
                var u32 = new uint[count];
                var u64 = new ulong[count];
                random.NextBytes(MemoryMarshal.AsBytes(u32.AsSpan()));
                random.NextBytes(MemoryMarshal.AsBytes(u64.AsSpan()));

                // Little-endian bytes of each value, so the expected hashes are right on big-endian machines too.
                var bytes32 = new byte[count * 4];
                var bytes64 = new byte[count * 8];
                for (int i = 0; i < count; i++)
                {
                    BinaryPrimitives.WriteUInt32LittleEndian(bytes32.AsSpan(i * 4), u32[i]);
                    BinaryPrimitives.WriteUInt64LittleEndian(bytes64.AsSpan(i * 8), u64[i]);
                }

                var actual = new uint[count + 1];
                actual[count] = 0xDEADBEEF;
                MurmurHash3.Hash32Batch(u32, actual, seed);
                Assert.Equal(Expected(bytes32, 4, count, seed), actual.Take(count));
                Assert.Equal(0xDEADBEEF, actual[count]);

                MurmurHash3.Hash32Batch(u64, actual, seed);
                Assert.Equal(Expected(bytes64, 8, count, seed), actual.Take(count));
                Assert.Equal(0xDEADBEEF, actual[count]);

                // The fallback directly, so it is tested on machines with AVX2 too.
                Array.Clear(actual, 0, count);
                MurmurHash3.Hash32BatchScalar(u32, actual, 0, seed);
                Assert.Equal(Expected(bytes32, 4, count, seed), actual.Take(count));
                MurmurHash3.Hash32BatchScalar(u64, actual, 0, seed);
                Assert.Equal(Expected(bytes64, 8, count, seed), actual.Take(count));
                Assert.Equal(0xDEADBEEF, actual[count]);
            }
        }
    }

    [Fact]
    public void UInt32Keys_InPlace()
    {
        var keys = new uint[1003];
        new Random(5).NextBytes(MemoryMarshal.AsBytes(keys.AsSpan()));
        var expected = new uint[keys.Length];
        MurmurHash3.Hash32Batch(keys, expected, 7);

        MurmurHash3.Hash32Batch(keys, keys, 7);

        Assert.Equal(expected, keys);
    }

    [Fact]
    public void DefaultSeed_IsZero()
    {
        var keys = new byte[16 * 9];
        new Random(3).NextBytes(keys);
        var withDefault = new uint[9];
        var withZero = new uint[9];
        MurmurHash3.Hash32Batch(keys, 16, withDefault);
        MurmurHash3.Hash32Batch(keys, 16, withZero, 0);
        Assert.Equal(withZero, withDefault);
    }

    [Fact]
    public void EmptyInput_WritesNothing()
    {
        MurmurHash3.Hash32Batch(ReadOnlySpan<byte>.Empty, 16, Span<uint>.Empty);
        MurmurHash3.Hash32Batch(ReadOnlySpan<uint>.Empty, Span<uint>.Empty);
        MurmurHash3.Hash32Batch(ReadOnlySpan<ulong>.Empty, Span<uint>.Empty);
    }

    [Fact]
    public void InvalidArguments_Throw()
    {
        var keys = new byte[64];
        Assert.Throws<ArgumentOutOfRangeException>("keyLength", () => MurmurHash3.Hash32Batch(keys, 0, new uint[64]));
        Assert.Throws<ArgumentOutOfRangeException>("keyLength", () => MurmurHash3.Hash32Batch(keys, -4, new uint[64]));
        Assert.Throws<ArgumentException>("keys", () => MurmurHash3.Hash32Batch(keys, 3, new uint[64]));
        Assert.Throws<ArgumentException>("destination", () => MurmurHash3.Hash32Batch(keys, 4, new uint[15]));
        Assert.Throws<ArgumentException>("destination", () => MurmurHash3.Hash32Batch(new uint[9], new uint[8]));
        Assert.Throws<ArgumentException>("destination", () => MurmurHash3.Hash32Batch(new ulong[9], new uint[8]));
    }
}
