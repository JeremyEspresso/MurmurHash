using System.Text;

namespace MurmurHash.UnitTests;

public class MurmurHash3Tests
{
    [InlineData("key", 293, 2495785535)]
    [InlineData("Hello World!", 420, 1535517821)]
    [InlineData("a$6ajXViSAfFw5pR2kkz3Q28YGrDx$jeaLJ5HFPe", 69, 3131871211)]
    [InlineData("!zhgt#HVY#tV%kPPZ$LXYEo@EqyKjqRJPzUb3*hhASWpdyZAF3!t$V96j9Eb9ivzMH2w4jvuyHaXRxd&YbHz*W8yZGJ#CXjXfqMzNGgf@YMfh*RdZpRXtPQ3mV$N9N!%", 23485, 4240136436)]
    [Theory]
    public void MurMurHash3_Hash_SameAsKnownOutputForString(string input, uint seed, uint expected)
    {
        ReadOnlySpan<byte> inputSpan = Encoding.UTF8.GetBytes(input).AsSpan();

        var actual = MurmurHash3.Hash32(ref inputSpan, seed);

        Assert.Equal(expected, actual);
    }
}