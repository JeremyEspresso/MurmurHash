#if NET8_0_OR_GREATER
using System.Runtime.Intrinsics.X86;
#endif

namespace MurmurHash;

/// <summary>
/// One-time CPU detection for micro-architecture specific code paths. The decision is stored in a
/// <c>static readonly</c> field, which the JIT treats as a constant in fully optimized code, so the
/// unused path is removed and the check costs nothing per call.
/// </summary>
internal static class CpuInfo
{
    /// <summary>AppContext switch that turns off all CPU-specific code paths.</summary>
    internal const string DisableSwitchName = "MurmurHash.DisableCpuSpecificOptimizations";

    /// <summary>
    /// True when <c>lea r, [b + i*s + disp]</c> (a 3-component LEA) has 1-cycle latency, which makes
    /// <c>h * 5 + c</c> one instruction on the hash's serial dependency chain instead of two.
    /// </summary>
    internal static readonly bool HasFastComplexLea = DetectFastComplexLea();

    private static bool DetectFastComplexLea()
    {
        if (AppContext.TryGetSwitch(DisableSwitchName, out bool disabled) && disabled)
        {
            return false;
        }

#if NET8_0_OR_GREATER
        if (!X86Base.IsSupported)
        {
            return false;
        }

        (_, int ebx, int ecx, int edx) = X86Base.CpuId(0, 0);
        bool isIntel = ebx == 0x756e6547 && edx == 0x49656e69 && ecx == 0x6c65746e; // "GenuineIntel"
        (int eax, _, _, _) = X86Base.CpuId(1, 0);
        int family = (eax >> 8) & 0xF;
        int model = (eax >> 4) & 0xF;
        if (family == 0xF)
        {
            family += (eax >> 20) & 0xFF;
        }

        if (family == 0x6 || family == 0xF)
        {
            model |= ((eax >> 16) & 0xF) << 4;
        }

        return IsFastComplexLeaCpu(isIntel, family, model);
#else
        return false;
#endif
    }

    /// <summary>
    /// Allowlist of CPUs with Sunny Cove or newer performance cores, where a 3-component LEA has 1-cycle latency.
    /// Skylake through Comet Lake (3-cycle complex LEA), AMD (2 cycles, so no gain), E-core-only parts, and
    /// anything unknown keep the default code. A missing entry only costs speed, never correctness.
    /// Model numbers: Linux <c>arch/x86/include/asm/intel-family.h</c>.
    /// </summary>
    internal static bool IsFastComplexLeaCpu(bool isIntel, int family, int model) =>
        isIntel && family == 6 && model switch
        {
            0x6A or 0x6C or 0x7D or 0x7E => true, // Ice Lake (server, client)
            0x8C or 0x8D => true,                 // Tiger Lake
            0xA7 => true,                         // Rocket Lake
            0x97 or 0x9A => true,                 // Alder Lake
            0xB7 or 0xBA or 0xBF => true,         // Raptor Lake
            0x8F => true,                         // Sapphire Rapids
            0xCF => true,                         // Emerald Rapids
            0xAD or 0xAE => true,                 // Granite Rapids
            0xAA or 0xAC => true,                 // Meteor Lake
            0xB5 or 0xC5 or 0xC6 => true,         // Arrow Lake
            0xBD => true,                         // Lunar Lake
            0xCC => true,                         // Panther Lake
            _ => false,
        };
}
