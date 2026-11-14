using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MurmurHash;

// Building blocks for the incremental hashes (MurmurHash32, MurmurHash128). The one-shot methods keep their own
// fully inlined copies of the block loops, so this file cannot change their performance.
public static partial class MurmurHash3
{
    /// <summary>
    /// Reads 0-15 bytes as the little-endian value they form (bytes 0-7 in <paramref name="low"/>, 8-15 in
    /// <paramref name="high"/>), using overlapping loads that stay inside <c>[0, count)</c>. The incremental hashes
    /// keep partial blocks in registers this way: writing pieces to a buffer and reading the buffer back as a wider
    /// value stalls on store forwarding (measured about 2x slower for 7-byte appends).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ReadPartialBlock(ref byte p, int count, out ulong low, out ulong high)
    {
        high = 0;
        if (count > 8)
        {
            low = ReadUInt64LittleEndian(ref p);
            high = ReadUInt64LittleEndian(ref Unsafe.Add(ref p, count - 8)) >> ((16 - count) * 8);
        }
        else if (count >= 4)
        {
            low = ReadUInt32LittleEndian(ref p)
                | ((ulong)ReadUInt32LittleEndian(ref Unsafe.Add(ref p, count - 4)) << ((count - 4) * 8));
        }
        else
        {
            low = ReadUpTo3(ref p, count);
        }
    }

    /// <summary>Reads 0-3 bytes as the little-endian value they form.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ReadUpTo3(ref byte p, int count)
    {
        uint value = 0;
        if (count > 0)
        {
            value = p;
            if (count > 1)
            {
                value |= (uint)Unsafe.Add(ref p, 1) << 8;
            }

            if (count > 2)
            {
                value |= (uint)Unsafe.Add(ref p, 2) << 16;
            }
        }

        return value;
    }

    /// <summary>Mixes whole 4-byte blocks into <paramref name="h"/>. <paramref name="blocks"/> length must be a multiple of 4.</summary>
    internal static uint Hash32Blocks(uint h, ReadOnlySpan<byte> blocks) =>
        CpuInfo.HasFastComplexLea
            ? Hash32Blocks<FusedLeaStep>(h, blocks)
            : Hash32Blocks<MultiplyStep>(h, blocks);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint Hash32Blocks<TStep>(uint h, ReadOnlySpan<byte> blocks)
        where TStep : struct, IStep
    {
        if (blocks.Length >= 4)
        {
            ref byte p = ref MemoryMarshal.GetReference(blocks);
            ref byte blockEnd = ref Unsafe.Add(ref p, blocks.Length & ~3);
            do
            {
                h ^= MixK(ReadUInt32LittleEndian(ref p));
                h = default(TStep).TimesFivePlusConstant(RotateLeft(h, 13));
                p = ref Unsafe.Add(ref p, 4);
            }
            while (Unsafe.IsAddressLessThan(ref p, ref blockEnd));
        }

        return h;
    }

    /// <summary>Mixes one 4-byte block, given as a little-endian value, into <paramref name="h"/>.</summary>
    internal static uint Hash32Block(uint h, uint k) =>
        default(MultiplyStep).TimesFivePlusConstant(RotateLeft(h ^ MixK(k), 13));

    /// <summary>
    /// Mixes in the 0-3 byte tail (little-endian in <paramref name="tail"/>, zero if absent) and the total length.
    /// The reference implementation mixes the length as a 32-bit value.
    /// </summary>
    internal static uint Hash32Finish(uint h, uint tail, int tailLength, ulong totalLength)
    {
        if (tailLength != 0)
        {
            h ^= MixK(tail);
        }

        return FMix(h ^ (uint)totalLength);
    }

    /// <summary>Mixes whole 16-byte blocks into the state. <paramref name="blocks"/> length must be a multiple of 16.</summary>
    internal static void Hash128Blocks(ref ulong h1, ref ulong h2, ReadOnlySpan<byte> blocks)
    {
        if (CpuInfo.HasFastComplexLea)
        {
            Hash128Blocks<FusedLeaStep>(ref h1, ref h2, blocks);
        }
        else
        {
            Hash128Blocks<MultiplyStep>(ref h1, ref h2, blocks);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Hash128Blocks<TStep>(ref ulong h1Ref, ref ulong h2Ref, ReadOnlySpan<byte> blocks)
        where TStep : struct, IStep
    {
        if (blocks.Length < 16)
        {
            return;
        }

        // Locals rather than the refs inside the loop, so the state stays in registers.
        ulong h1 = h1Ref;
        ulong h2 = h2Ref;
        ref byte p = ref MemoryMarshal.GetReference(blocks);
        ref byte blockEnd = ref Unsafe.Add(ref p, blocks.Length & ~15);
        do
        {
            h1 ^= MixK1(ReadUInt64LittleEndian(ref p));
            h1 = default(TStep).TimesFivePlusConstant(RotateLeft(h1, 27) + h2, 0x52dce729);

            h2 ^= MixK2(ReadUInt64LittleEndian(ref Unsafe.Add(ref p, 8)));
            h2 = default(TStep).TimesFivePlusConstant(RotateLeft(h2, 31) + h1, 0x38495ab5);

            p = ref Unsafe.Add(ref p, 16);
        }
        while (Unsafe.IsAddressLessThan(ref p, ref blockEnd));

        h1Ref = h1;
        h2Ref = h2;
    }

    /// <summary>Mixes one 16-byte block, given as two little-endian values, into the state.</summary>
    internal static void Hash128Block(ref ulong h1, ref ulong h2, ulong k1, ulong k2)
    {
        h1 ^= MixK1(k1);
        h1 = default(MultiplyStep).TimesFivePlusConstant(RotateLeft(h1, 27) + h2, 0x52dce729);
        h2 ^= MixK2(k2);
        h2 = default(MultiplyStep).TimesFivePlusConstant(RotateLeft(h2, 31) + h1, 0x38495ab5);
    }

    /// <summary>
    /// Mixes in the 0-15 byte tail (bytes 0-7 little-endian in <paramref name="t1"/>, bytes 8-15 in
    /// <paramref name="t2"/>) and the total length, and finalizes.
    /// </summary>
    internal static void Hash128Finish(
        ulong h1, ulong h2, ulong t1, ulong t2, int tailLength, ulong totalLength, out ulong h1Out, out ulong h2Out)
    {
        if (tailLength > 8)
        {
            h2 ^= MixK2(t2);
        }

        if (tailLength != 0)
        {
            h1 ^= MixK1(t1);
        }

        h1 ^= totalLength;
        h2 ^= totalLength;
        h1 += h2;
        h2 += h1;
        h1 = FMix(h1);
        h2 = FMix(h2);
        h1 += h2;
        h2 += h1;

        h1Out = h1;
        h2Out = h2;
    }
}
