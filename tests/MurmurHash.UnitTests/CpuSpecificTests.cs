namespace MurmurHash.UnitTests;

public class CpuSpecificTests
{
    private delegate uint HashFunc(ReadOnlySpan<byte> bytes, uint seed);

    public static TheoryData<string> Steps => new() { nameof(MurmurHash3.MultiplyStep), nameof(MurmurHash3.FusedLeaStep) };

    // Both code paths are tested on every machine, whichever one this CPU would pick.
    private static HashFunc Get(string step) => step == nameof(MurmurHash3.FusedLeaStep)
        ? MurmurHash3.Hash32Core<MurmurHash3.FusedLeaStep>
        : MurmurHash3.Hash32Core<MurmurHash3.MultiplyStep>;

    [Theory]
    [MemberData(nameof(Steps))]
    public void Hash32Core_EachStep_MatchesReferenceVectors(string step)
    {
        var hash = Get(step);
        foreach (var offset in new[] { 0, 1, 3 })
        {
            foreach (var v in TestVectors.All)
            {
                var actual = hash(TestVectors.Poisoned(TestVectors.Data.AsSpan(0, v.Length), offset), v.Seed);
                Assert.True(v.Hash32 == actual, $"{step} length={v.Length} seed={v.Seed} offset={offset}");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Steps))]
    public void Hash32Core_EachStep_EndsAtGuardPage_DoesNotReadPastEnd(string step)
    {
        Assert.SkipUnless(GuardedMemory.IsSupported, "Guard pages are only implemented on Linux.");
        var hash = Get(step);
        foreach (var v in TestVectors.All)
        {
            GuardedMemory.Run(TestVectors.Data.AsSpan(0, v.Length), data => Assert.Equal(v.Hash32, hash(data, v.Seed)));
        }
    }

    [Theory]
    [InlineData(true, 6, 0xB7, true)]   // Raptor Lake (i9-13900K)
    [InlineData(true, 6, 0x97, true)]   // Alder Lake
    [InlineData(true, 6, 0x7E, true)]   // Ice Lake client
    [InlineData(true, 6, 0x8F, true)]   // Sapphire Rapids
    [InlineData(true, 6, 0x9E, false)]  // Coffee Lake (i7-9700K): 3-cycle complex LEA
    [InlineData(true, 6, 0x5E, false)]  // Skylake
    [InlineData(true, 6, 0x55, false)]  // Skylake-X / Cascade Lake
    [InlineData(true, 6, 0xBE, false)]  // Alder Lake-N (E-cores only, unmeasured)
    [InlineData(true, 0x13, 0x01, false)] // unknown future family
    [InlineData(false, 0x19, 0x61, false)] // AMD Zen 4
    public void IsFastComplexLeaCpu_Allowlist(bool isIntel, int family, int model, bool expected)
    {
        Assert.Equal(expected, CpuInfo.IsFastComplexLeaCpu(isIntel, family, model));
    }
}
