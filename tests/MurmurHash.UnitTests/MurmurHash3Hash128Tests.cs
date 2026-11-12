using System.Buffers.Binary;
using System.Text;

namespace MurmurHash.UnitTests;

public class MurmurHash3Hash128Tests
{
    private delegate void Core(ReadOnlySpan<byte> bytes, uint seed, out ulong h1, out ulong h2);

    // Both code paths are tested on every machine, whichever one this CPU would pick.
    private static readonly Dictionary<string, Core> Cores = new()
    {
        [nameof(MurmurHash3.MultiplyStep)] = MurmurHash3.Hash128Core<MurmurHash3.MultiplyStep>,
        [nameof(MurmurHash3.FusedLeaStep)] = MurmurHash3.Hash128Core<MurmurHash3.FusedLeaStep>,
    };

    public static TheoryData<string> CoreNames => new(Cores.Keys);

    [Theory]
    [InlineData("", 0u, "00000000000000000000000000000000")]
    [InlineData("The quick brown fox jumps over the lazy dog", 0u, "6c1b07bc7bbc4be347939ac4a93c437a")]
    [InlineData("Hello World!", 420u, "18222523c0a9220dc7d473610d0ef99e")]
    public void Hash128_KnownString_MatchesReference(string input, uint seed, string expectedHex)
    {
        Span<byte> destination = stackalloc byte[16];

        MurmurHash3.Hash128(Encoding.UTF8.GetBytes(input), destination, seed);

        Assert.Equal(expectedHex, Convert.ToHexString(destination).ToLowerInvariant());
    }

    [Fact]
    public void Hash128_Destination_MatchesReferenceVectors()
    {
        Span<byte> destination = stackalloc byte[16];
        foreach (var v in TestVectors.All)
        {
            MurmurHash3.Hash128(TestVectors.Data.AsSpan(0, v.Length), destination, v.Seed);
            Assert.True(
                v.Hash128Low == BinaryPrimitives.ReadUInt64LittleEndian(destination)
                && v.Hash128High == BinaryPrimitives.ReadUInt64LittleEndian(destination[8..]),
                $"length={v.Length} seed={v.Seed}");
        }
    }

    [Fact]
    public void Hash64_MatchesFirstHalfOfReferenceVectors()
    {
        foreach (var v in TestVectors.All)
        {
            Assert.True(v.Hash128Low == MurmurHash3.Hash64(TestVectors.Data.AsSpan(0, v.Length), v.Seed), $"length={v.Length} seed={v.Seed}");
        }
    }

#if !NETSTANDARD_LIBRARY
    [Fact]
    public void Hash128_UInt128_MatchesReferenceVectors()
    {
        foreach (var v in TestVectors.All)
        {
            var actual = MurmurHash3.Hash128(TestVectors.Data.AsSpan(0, v.Length), v.Seed);
            Assert.True(new UInt128(v.Hash128High, v.Hash128Low) == actual, $"length={v.Length} seed={v.Seed}");
        }
    }

    [Fact]
    public void Hash128_UInt128_LittleEndianBytesEqualDestinationBytes()
    {
        Span<byte> fromUInt128 = stackalloc byte[16];
        Span<byte> destination = stackalloc byte[16];

        BinaryPrimitives.WriteUInt128LittleEndian(fromUInt128, MurmurHash3.Hash128("Hello World!"u8, 420));
        MurmurHash3.Hash128("Hello World!"u8, destination, 420);

        Assert.True(fromUInt128.SequenceEqual(destination));
    }
#endif

    [Theory]
    [MemberData(nameof(CoreNames))]
    public void Hash128Core_PoisonedSurroundings_MatchesReference(string core)
    {
        var hash = Cores[core];
        foreach (var offset in new[] { 0, 1, 3, 7 })
        {
            foreach (var v in TestVectors.All)
            {
                hash(TestVectors.Poisoned(TestVectors.Data.AsSpan(0, v.Length), offset), v.Seed, out var h1, out var h2);
                Assert.True(v.Hash128Low == h1 && v.Hash128High == h2, $"{core} length={v.Length} seed={v.Seed} offset={offset}");
            }
        }
    }

    [Theory]
    [MemberData(nameof(CoreNames))]
    public void Hash128Core_AtGuardPage_DoesNotReadOutsideInput(string core)
    {
        Assert.SkipUnless(GuardedMemory.IsSupported, "Guard pages are only implemented on Linux.");
        var hash = Cores[core];
        foreach (var guardBefore in new[] { false, true })
        {
            foreach (var v in TestVectors.All)
            {
                GuardedMemory.Run(
                    TestVectors.Data.AsSpan(0, v.Length),
                    data =>
                    {
                        hash(data, v.Seed, out var h1, out var h2);
                        Assert.True(v.Hash128Low == h1 && v.Hash128High == h2, $"{core} length={v.Length} seed={v.Seed}");
                    },
                    guardBefore);
            }
        }
    }

    [Fact]
    public void Hash128_PublicApi_AtGuardPage_DoesNotReadOutsideInput()
    {
        Assert.SkipUnless(GuardedMemory.IsSupported, "Guard pages are only implemented on Linux.");
        foreach (var guardBefore in new[] { false, true })
        {
            foreach (var v in TestVectors.All)
            {
                GuardedMemory.Run(TestVectors.Data.AsSpan(0, v.Length), data => Assert.Equal(v.Hash128Low, MurmurHash3.Hash64(data, v.Seed)), guardBefore);
            }
        }
    }

    [Fact]
    public void Hash128_DestinationTooShort_Throws()
    {
        var destination = new byte[15];

        var ex = Assert.Throws<ArgumentException>(() => MurmurHash3.Hash128("abc"u8, destination));
        Assert.Equal("destination", ex.ParamName);
    }

    [Fact]
    public void Hash128_LargerDestination_WritesOnlyFirst16Bytes()
    {
        var destination = Enumerable.Repeat((byte)0xEE, 20).ToArray();

        MurmurHash3.Hash128("abc"u8, destination);

        Assert.All(destination[16..], b => Assert.Equal(0xEE, b));
    }
}
