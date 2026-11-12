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
    public static uint Hash32(ReadOnlySpan<byte> bytes, uint seed = 0)
    {
        ref byte start = ref MemoryMarshal.GetReference(bytes);
        int length = bytes.Length;
        uint h = seed;

        nint offset = 0;
        nint blockEnd = length & ~3;
        while (offset < blockEnd)
        {
            h ^= MixK(ReadUInt32LittleEndian(ref Unsafe.Add(ref start, offset)));
            h = RotateLeft(h, 13) * 5 + 0xe6546b64;
            offset += 4;
        }

        uint k = 0;
        switch (length & 3)
        {
            case 3:
                k ^= (uint)Unsafe.Add(ref start, offset + 2) << 16;
                goto case 2;
            case 2:
                k ^= (uint)Unsafe.Add(ref start, offset + 1) << 8;
                goto case 1;
            case 1:
                k ^= Unsafe.Add(ref start, offset);
                h ^= MixK(k);
                break;
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
