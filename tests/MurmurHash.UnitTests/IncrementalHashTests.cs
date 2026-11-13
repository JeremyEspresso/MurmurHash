using System.Buffers.Binary;
using System.IO.Hashing;
using System.Text;

namespace MurmurHash.UnitTests;

public class IncrementalHashTests
{
    // Every test runs against both algorithms. Expected values come from the reference vectors or the one-shot API,
    // which the reference vectors already cover.
    public static TheoryData<string> Algorithms => new() { "32", "128" };

    private static NonCryptographicHashAlgorithm Create(string algorithm, uint seed) =>
        algorithm == "32" ? new MurmurHash32(seed) : new MurmurHash128(seed);

    private static byte[] Expected(string algorithm, TestVectors.Vector v)
    {
        var bytes = new byte[algorithm == "32" ? 4 : 16];
        if (algorithm == "32")
        {
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, v.Hash32);
        }
        else
        {
            BinaryPrimitives.WriteUInt64LittleEndian(bytes, v.Hash128Low);
            BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(8), v.Hash128High);
        }

        return bytes;
    }

    private static byte[] OneShot(string algorithm, ReadOnlySpan<byte> data, uint seed)
    {
        var bytes = new byte[algorithm == "32" ? 4 : 16];
        if (algorithm == "32")
        {
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, MurmurHash3.Hash32(data, seed));
        }
        else
        {
            MurmurHash3.Hash128(data, bytes, seed);
        }

        return bytes;
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void SingleAppend_MatchesReferenceVectors(string algorithm)
    {
        foreach (var v in TestVectors.All)
        {
            var hash = Create(algorithm, v.Seed);
            hash.Append(TestVectors.Data.AsSpan(0, v.Length));
            Assert.True(Expected(algorithm, v).AsSpan().SequenceEqual(hash.GetCurrentHash()), $"length={v.Length} seed={v.Seed}");
        }
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void TwoAppends_EverySplitPoint_MatchesReferenceVectors(string algorithm)
    {
        foreach (var v in TestVectors.All)
        {
            var data = TestVectors.Data.AsSpan(0, v.Length);
            var expected = Expected(algorithm, v);
            for (int split = 0; split <= v.Length; split++)
            {
                var hash = Create(algorithm, v.Seed);
                hash.Append(data.Slice(0, split));
                hash.Append(data.Slice(split));
                Assert.True(expected.AsSpan().SequenceEqual(hash.GetCurrentHash()), $"length={v.Length} seed={v.Seed} split={split}");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void FixedChunkSizes_MatchReferenceVectors(string algorithm)
    {
        foreach (var v in TestVectors.All)
        {
            var data = TestVectors.Data.AsSpan(0, v.Length);
            var expected = Expected(algorithm, v);
            for (int chunk = 1; chunk <= 33; chunk++)
            {
                var hash = Create(algorithm, v.Seed);
                for (int i = 0; i < data.Length; i += chunk)
                {
                    hash.Append(data.Slice(i, Math.Min(chunk, data.Length - i)));
                }

                Assert.True(expected.AsSpan().SequenceEqual(hash.GetCurrentHash()), $"length={v.Length} seed={v.Seed} chunk={chunk}");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void RandomSplits_LongInputs_MatchOneShot(string algorithm)
    {
        var random = new Random(1234);
        var data = new byte[70_000];
        random.NextBytes(data);
        for (int round = 0; round < 200; round++)
        {
            int length = random.Next(data.Length + 1);
            uint seed = (uint)random.Next();
            var hash = Create(algorithm, seed);
            int position = 0;
            while (position < length)
            {
                // Mostly small pieces with occasional large ones, including empty appends.
                int piece = random.Next(4) == 0 ? random.Next(5000) : random.Next(40);
                piece = Math.Min(piece, length - position);
                hash.Append(data.AsSpan(position, piece));
                position += piece;
            }

            Assert.True(OneShot(algorithm, data.AsSpan(0, length), seed).AsSpan().SequenceEqual(hash.GetCurrentHash()), $"round={round} length={length}");
        }
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void GetCurrentHash_DoesNotChangeState(string algorithm)
    {
        var data = TestVectors.Data.AsSpan(0, 100);
        var hash = Create(algorithm, 69);
        for (int i = 0; i < data.Length; i++)
        {
            hash.Append(data.Slice(i, 1));
            Assert.True(OneShot(algorithm, data.Slice(0, i + 1), 69).AsSpan().SequenceEqual(hash.GetCurrentHash()), $"after {i + 1} bytes");
        }
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void GetHashAndReset_ReturnsHashAndRestartsWithSameSeed(string algorithm)
    {
        var hash = Create(algorithm, 420);
        hash.Append("Hello"u8);
        hash.Append(" World!"u8);

        Assert.Equal(OneShot(algorithm, "Hello World!"u8, 420), hash.GetHashAndReset());
        Assert.Equal(OneShot(algorithm, [], 420), hash.GetCurrentHash());

        hash.Append("abc"u8);
        Assert.Equal(OneShot(algorithm, "abc"u8, 420), hash.GetCurrentHash());
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void Reset_RestartsWithSameSeed(string algorithm)
    {
        // 31 bytes leave a partial block pending (3 bytes for 32-bit, 15 for 128-bit). The 5 bytes appended
        // afterwards leave a shorter partial block (1 and 5 bytes), so any leftover bytes would change the result.
        var hash = Create(algorithm, 7);
        hash.Append(TestVectors.Data.AsSpan(0, 31));

        hash.Reset();
        hash.Append("Hello"u8);

        Assert.Equal(OneShot(algorithm, "Hello"u8, 7), hash.GetCurrentHash());
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void AppendStream_MatchesOneShot(string algorithm)
    {
        // Larger than the base class's 4 KB copy buffer, and not a multiple of the block size.
        var data = new byte[10_007];
        new Random(5).NextBytes(data);
        var hash = Create(algorithm, 3);

        hash.Append(new MemoryStream(data));

        Assert.Equal(OneShot(algorithm, data, 3), hash.GetCurrentHash());
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public async Task AppendAsyncStream_MatchesOneShot(string algorithm)
    {
        var data = new byte[10_007];
        new Random(6).NextBytes(data);
        var hash = Create(algorithm, 3);

        await hash.AppendAsync(new MemoryStream(data), TestContext.Current.CancellationToken);

        Assert.Equal(OneShot(algorithm, data, 3), hash.GetCurrentHash());
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void Append_PoisonedSurroundings_MatchesReferenceVectors(string algorithm)
    {
        foreach (var offset in new[] { 1, 3, 7 })
        {
            foreach (var v in TestVectors.All)
            {
                var data = TestVectors.Data.AsSpan(0, v.Length);
                int split = v.Length / 3;
                var hash = Create(algorithm, v.Seed);
                hash.Append(TestVectors.Poisoned(data.Slice(0, split), offset));
                hash.Append(TestVectors.Poisoned(data.Slice(split), offset));
                Assert.True(Expected(algorithm, v).AsSpan().SequenceEqual(hash.GetCurrentHash()), $"length={v.Length} seed={v.Seed} offset={offset}");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void Append_AtGuardPage_DoesNotReadOutsideInput(string algorithm)
    {
        Assert.SkipUnless(GuardedMemory.IsSupported, "Guard pages are only implemented on Linux.");
        foreach (var guardBefore in new[] { false, true })
        {
            foreach (var v in TestVectors.All)
            {
                var data = TestVectors.Data.AsSpan(0, v.Length);
                var expected = Expected(algorithm, v);
                int split = v.Length / 3;
                var hash = Create(algorithm, v.Seed);

                // Both pieces sit directly against a guard page, so any read outside them crashes.
                GuardedMemory.Run(data.Slice(0, split), piece => hash.Append(piece), guardBefore);
                GuardedMemory.Run(data.Slice(split), piece => hash.Append(piece), guardBefore);

                Assert.True(expected.AsSpan().SequenceEqual(hash.GetCurrentHash()), $"length={v.Length} seed={v.Seed}");
            }
        }
    }

    // The classes use whichever block loop this CPU picks; both are checked here on every machine.
    [Fact]
    public void BlockLoops_BothCodePaths_MatchReferenceVectors()
    {
        foreach (var v in TestVectors.All)
        {
            var data = TestVectors.Data.AsSpan(0, v.Length);

            int blocks32 = v.Length & ~3;
            uint tail32 = 0;
            for (int i = blocks32; i < v.Length; i++)
            {
                tail32 |= (uint)data[i] << ((i - blocks32) * 8);
            }

            foreach (var h in new[]
            {
                MurmurHash3.Hash32Blocks<MurmurHash3.MultiplyStep>(v.Seed, data.Slice(0, blocks32)),
                MurmurHash3.Hash32Blocks<MurmurHash3.FusedLeaStep>(v.Seed, data.Slice(0, blocks32)),
            })
            {
                Assert.True(v.Hash32 == MurmurHash3.Hash32Finish(h, tail32, v.Length - blocks32, (ulong)v.Length), $"32 length={v.Length} seed={v.Seed}");
            }

            int blocks128 = v.Length & ~15;
            ulong t1 = 0;
            ulong t2 = 0;
            for (int i = blocks128; i < v.Length; i++)
            {
                int j = i - blocks128;
                if (j < 8)
                {
                    t1 |= (ulong)data[i] << (j * 8);
                }
                else
                {
                    t2 |= (ulong)data[i] << ((j - 8) * 8);
                }
            }

            for (int path = 0; path < 2; path++)
            {
                ulong h1 = v.Seed;
                ulong h2 = v.Seed;
                if (path == 0)
                {
                    MurmurHash3.Hash128Blocks<MurmurHash3.MultiplyStep>(ref h1, ref h2, data.Slice(0, blocks128));
                }
                else
                {
                    MurmurHash3.Hash128Blocks<MurmurHash3.FusedLeaStep>(ref h1, ref h2, data.Slice(0, blocks128));
                }

                MurmurHash3.Hash128Finish(h1, h2, t1, t2, v.Length - blocks128, (ulong)v.Length, out var r1, out var r2);
                Assert.True(v.Hash128Low == r1 && v.Hash128High == r2, $"128 path={path} length={v.Length} seed={v.Seed}");
            }
        }
    }

    [Fact]
    public void MurmurHash32_GetCurrentHashAsUInt32_MatchesHash32()
    {
        foreach (var v in TestVectors.All)
        {
            var hash = new MurmurHash32(v.Seed);
            hash.Append(TestVectors.Data.AsSpan(0, v.Length));
            Assert.True(v.Hash32 == hash.GetCurrentHashAsUInt32(), $"length={v.Length} seed={v.Seed}");
        }
    }

    [Fact]
    public void MurmurHash32_Bytes_MatchPythonMmh3Digest()
    {
        // mmh3.mmh3_32_digest(b"Hello World!", 420).hex()
        var hash = new MurmurHash32(420);
        hash.Append(Encoding.UTF8.GetBytes("Hello World!"));

        Assert.Equal("7d24865b", Convert.ToHexString(hash.GetCurrentHash()).ToLowerInvariant());
        Assert.Equal(4, hash.HashLengthInBytes);
    }

    [Fact]
    public void MurmurHash128_Bytes_MatchPythonMmh3HashBytes()
    {
        // mmh3.hash_bytes(b"Hello World!", 420).hex()
        var hash = new MurmurHash128(420);
        hash.Append(Encoding.UTF8.GetBytes("Hello World!"));

        Assert.Equal("18222523c0a9220dc7d473610d0ef99e", Convert.ToHexString(hash.GetCurrentHash()).ToLowerInvariant());
        Assert.Equal(16, hash.HashLengthInBytes);
    }

    [Fact]
    public void MurmurHash128_GetCurrentHashAsUInt64_MatchesHash64()
    {
        foreach (var v in TestVectors.All)
        {
            var hash = new MurmurHash128(v.Seed);
            hash.Append(TestVectors.Data.AsSpan(0, v.Length));
            Assert.True(v.Hash128Low == hash.GetCurrentHashAsUInt64(), $"length={v.Length} seed={v.Seed}");
        }
    }

#if !NETSTANDARD_LIBRARY
    [Fact]
    public void MurmurHash128_GetCurrentHashAsUInt128_MatchesHash128()
    {
        foreach (var v in TestVectors.All)
        {
            var hash = new MurmurHash128(v.Seed);
            hash.Append(TestVectors.Data.AsSpan(0, v.Length));
            Assert.True(new UInt128(v.Hash128High, v.Hash128Low) == hash.GetCurrentHashAsUInt128(), $"length={v.Length} seed={v.Seed}");
        }
    }
#endif

    [Fact]
    public void DefaultConstructors_UseSeedZero()
    {
        var h32 = new MurmurHash32();
        var h128 = new MurmurHash128();
        h32.Append("abc"u8);
        h128.Append("abc"u8);

        Assert.Equal(MurmurHash3.Hash32("abc"u8), h32.GetCurrentHashAsUInt32());
        Assert.Equal(MurmurHash3.Hash64("abc"u8), h128.GetCurrentHashAsUInt64());
    }
}
