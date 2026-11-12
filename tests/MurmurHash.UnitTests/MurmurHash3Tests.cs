using System.Text;

namespace MurmurHash.UnitTests;

public class MurmurHash3Tests
{
    [Theory]
    [InlineData("", 0u, 0u)]
    [InlineData("", 1u, 0x514E28B7u)]
    [InlineData("key", 293u, 2495785535u)]
    [InlineData("Hello World!", 420u, 1535517821u)]
    [InlineData("a$6ajXViSAfFw5pR2kkz3Q28YGrDx$jeaLJ5HFPe", 69u, 3131871211u)]
    [InlineData("!zhgt#HVY#tV%kPPZ$LXYEo@EqyKjqRJPzUb3*hhASWpdyZAF3!t$V96j9Eb9ivzMH2w4jvuyHaXRxd&YbHz*W8yZGJ#CXjXfqMzNGgf@YMfh*RdZpRXtPQ3mV$N9N!%", 23485u, 4240136436u)]
    public void Hash32_KnownString_MatchesReference(string input, uint seed, uint expected)
    {
        ReadOnlySpan<byte> inputSpan = Encoding.UTF8.GetBytes(input);

        Assert.Equal(expected, MurmurHash3.Hash32(inputSpan, seed));
    }

    [Fact]
    public void Hash32_DefaultSeed_IsZero()
    {
        ReadOnlySpan<byte> input = "Hello World!"u8;

        Assert.Equal(MurmurHash3.Hash32(input, 0), MurmurHash3.Hash32(input));
    }

    [Fact]
    public void Hash32_RefOverload_MatchesValueOverload()
    {
        foreach (var v in TestVectors.All)
        {
            ReadOnlySpan<byte> input = TestVectors.Data.AsSpan(0, v.Length);
            Assert.Equal(v.Hash32, MurmurHash3.Hash32(ref input, v.Seed));
            Assert.Equal(v.Length, input.Length);
        }
    }

    [Fact]
    public void Hash32_SliceOfLargerBuffer_MatchesReference()
    {
        foreach (var v in TestVectors.All)
        {
            var actual = MurmurHash3.Hash32(TestVectors.Data.AsSpan(0, v.Length), v.Seed);
            Assert.True(v.Hash32 == actual, $"length={v.Length} seed={v.Seed} expected={v.Hash32} actual={actual}");
        }
    }

    [Theory]
    [InlineData(0, (byte)0x00)]
    [InlineData(1, (byte)0xA5)]
    [InlineData(2, (byte)0xFF)]
    [InlineData(3, (byte)0x5A)]
    [InlineData(7, (byte)0xA5)]
    public void Hash32_PoisonedSurroundings_MatchesReference(int offset, byte poison)
    {
        foreach (var v in TestVectors.All)
        {
            var input = TestVectors.Poisoned(TestVectors.Data.AsSpan(0, v.Length), offset, poison);
            var actual = MurmurHash3.Hash32(input, v.Seed);
            Assert.True(v.Hash32 == actual, $"length={v.Length} seed={v.Seed} expected={v.Hash32} actual={actual}");
        }
    }
}
