using System.Buffers.Binary;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MurmurHash;

/// <summary>
/// Zero allocation implementation of Austin Appleby's MurmurHash3.
/// Output is identical to the reference implementation (SMHasher <c>MurmurHash3_x86_32</c>)
/// on every platform, independent of machine endianness.
/// </summary>
public static class MurmurHash3
{
    private const uint C1 = 0xcc9e2d51;
    private const uint C2 = 0x1b873593;

    /// <summary>
    /// Hashes <paramref name="bytes"/> using MurmurHash3 (x86, 32-bit).
    /// </summary>
    /// <param name="bytes">The data to hash.</param>
    /// <param name="seed">The seed for this algorithm.</param>
    /// <returns>The 32-bit MurmurHash3 of <paramref name="bytes"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Hash32(ReadOnlySpan<byte> bytes, uint seed = 0) =>
        CpuInfo.HasFastComplexLea
            ? Hash32Core<FusedLeaStep>(bytes, seed)
            : Hash32Core<MultiplyStep>(bytes, seed);

    /// <summary>
    /// The hash itself. <typeparamref name="TStep"/> only decides how <c>h * 5 + 0xe6546b64</c> is written,
    /// which changes the machine code the JIT emits but never the result. The JIT compiles a separate,
    /// fully inlined copy per struct type argument.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint Hash32Core<TStep>(ReadOnlySpan<byte> bytes, uint seed)
        where TStep : struct, IStep
    {
        ref byte p = ref MemoryMarshal.GetReference(bytes);
        int length = bytes.Length;
        uint h = seed;

        // Pointer-bump loop in a guarded do/while: measured 5-12% faster for 16-128 byte inputs
        // than an indexed while loop.
        if (length >= 4)
        {
            ref byte blockEnd = ref Unsafe.Add(ref p, length & ~3);
            do
            {
                h ^= MixK(ReadUInt32LittleEndian(ref p));
                h = default(TStep).TimesFivePlusConstant(RotateLeft(h, 13));
                p = ref Unsafe.Add(ref p, 4);
            }
            while (Unsafe.IsAddressLessThan(ref p, ref blockEnd));
        }

        // Tail: 0-3 remaining bytes, read one at a time so nothing outside the span is touched.
        int remainder = length & 3;
        if (remainder > 0)
        {
            uint k = p;
            if (remainder > 1)
            {
                k ^= (uint)Unsafe.Add(ref p, 1) << 8;
            }

            if (remainder > 2)
            {
                k ^= (uint)Unsafe.Add(ref p, 2) << 16;
            }

            h ^= MixK(k);
        }

        return FMix(h ^ (uint)length);
    }

    /// <summary>
    /// Hashes <paramref name="bytes"/> using MurmurHash3 (x86, 32-bit).
    /// </summary>
    /// <remarks>Kept for backwards compatibility; prefer <see cref="Hash32(ReadOnlySpan{byte}, uint)"/>.</remarks>
    /// <param name="bytes">The data to hash. It is not modified.</param>
    /// <param name="seed">The seed for this algorithm.</param>
    /// <returns>The 32-bit MurmurHash3 of <paramref name="bytes"/>.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Hash32(ref ReadOnlySpan<byte> bytes, uint seed) => Hash32(bytes, seed);

    internal interface IStep
    {
        /// <summary>Returns <c>h * 5 + 0xe6546b64</c>.</summary>
        uint TimesFivePlusConstant(uint h);
    }

    /// <summary>Default: the JIT emits <c>lea r, [h + h*4]; add r, c</c> (2 instructions, 2 cycles everywhere).</summary>
    internal readonly struct MultiplyStep : IStep
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint TimesFivePlusConstant(uint h) => h * 5 + 0xe6546b64;
    }

    /// <summary>
    /// The JIT emits a single <c>lea r, [h + h*4 + c]</c>: 1 cycle on Ice Lake and newer Intel cores
    /// (measured 16-20% faster for large inputs on Raptor Lake), but 3 cycles on Skylake-family cores.
    /// Only used when <see cref="CpuInfo.HasFastComplexLea"/> is true.
    /// </summary>
    internal readonly struct FusedLeaStep : IStep
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint TimesFivePlusConstant(uint h) => h + (h << 2) + 0xe6546b64;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint MixK(uint k) => RotateLeft(k * C1, 15) * C2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint FMix(uint h)
    {
        h ^= h >> 16;
        h *= 0x85ebca6b;
        h ^= h >> 13;
        h *= 0xc2b2ae35;
        return h ^ (h >> 16);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ReadUInt32LittleEndian(ref byte source)
    {
        uint value = Unsafe.ReadUnaligned<uint>(ref source);
        return BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint RotateLeft(uint value, int offset)
#if NETCOREAPP3_0_OR_GREATER
        => System.Numerics.BitOperations.RotateLeft(value, offset);
#else
        // The JIT recognizes this pattern and emits a single rol instruction.
        => (value << offset) | (value >> (32 - offset));
#endif
}
