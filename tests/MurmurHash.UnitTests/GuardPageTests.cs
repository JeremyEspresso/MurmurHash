namespace MurmurHash.UnitTests;

public class GuardPageTests
{
    // The original implementation crashed this test with an AccessViolationException (it read past the span).
    [Fact]
    public void Hash32_EndsAtGuardPage_DoesNotReadPastEnd()
    {
        Assert.SkipUnless(GuardedMemory.IsSupported, "Guard pages are only implemented on Linux.");
        foreach (var v in TestVectors.All)
        {
            GuardedMemory.Run(TestVectors.Data.AsSpan(0, v.Length), data => Assert.Equal(v.Hash32, MurmurHash3.Hash32(data, v.Seed)));
        }
    }
}
