using System.Buffers.Binary;
using static System.Numerics.BitOperations;

namespace MurmurHash.Benchmarks.Baselines;

/// <summary>
/// A direct port of SMHasher's <c>MurmurHash3_x64_128</c> (indexed block loop, fall-through switch for the tail),
/// used as the benchmark baseline for the library's Hash128.
/// </summary>
public static class ReferenceMurmurHash3x64
{
    private const ulong C1 = 0x87c37b91114253d5;
    private const ulong C2 = 0x4cf5ad432745937f;

    public static UInt128 Hash128(ReadOnlySpan<byte> data, uint seed)
    {
        int nblocks = data.Length / 16;
        ulong h1 = seed;
        ulong h2 = seed;

        for (int i = 0; i < nblocks; i++)
        {
            ulong k1 = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(i * 16));
            ulong k2 = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(i * 16 + 8));

            k1 *= C1; k1 = RotateLeft(k1, 31); k1 *= C2; h1 ^= k1;
            h1 = RotateLeft(h1, 27); h1 += h2; h1 = h1 * 5 + 0x52dce729;

            k2 *= C2; k2 = RotateLeft(k2, 33); k2 *= C1; h2 ^= k2;
            h2 = RotateLeft(h2, 31); h2 += h1; h2 = h2 * 5 + 0x38495ab5;
        }

        var tail = data.Slice(nblocks * 16);
        ulong t1 = 0;
        ulong t2 = 0;
        switch (data.Length & 15)
        {
            case 15: t2 ^= (ulong)tail[14] << 48; goto case 14;
            case 14: t2 ^= (ulong)tail[13] << 40; goto case 13;
            case 13: t2 ^= (ulong)tail[12] << 32; goto case 12;
            case 12: t2 ^= (ulong)tail[11] << 24; goto case 11;
            case 11: t2 ^= (ulong)tail[10] << 16; goto case 10;
            case 10: t2 ^= (ulong)tail[9] << 8; goto case 9;
            case 9:
                t2 ^= tail[8];
                t2 *= C2; t2 = RotateLeft(t2, 33); t2 *= C1; h2 ^= t2;
                goto case 8;
            case 8: t1 ^= (ulong)tail[7] << 56; goto case 7;
            case 7: t1 ^= (ulong)tail[6] << 48; goto case 6;
            case 6: t1 ^= (ulong)tail[5] << 40; goto case 5;
            case 5: t1 ^= (ulong)tail[4] << 32; goto case 4;
            case 4: t1 ^= (ulong)tail[3] << 24; goto case 3;
            case 3: t1 ^= (ulong)tail[2] << 16; goto case 2;
            case 2: t1 ^= (ulong)tail[1] << 8; goto case 1;
            case 1:
                t1 ^= tail[0];
                t1 *= C1; t1 = RotateLeft(t1, 31); t1 *= C2; h1 ^= t1;
                break;
        }

        h1 ^= (ulong)data.Length;
        h2 ^= (ulong)data.Length;
        h1 += h2;
        h2 += h1;
        h1 = FMix(h1);
        h2 = FMix(h2);
        h1 += h2;
        h2 += h1;
        return new UInt128(h2, h1);
    }

    private static ulong FMix(ulong k)
    {
        k ^= k >> 33;
        k *= 0xff51afd7ed558ccd;
        k ^= k >> 33;
        k *= 0xc4ceb9fe1a85ec53;
        return k ^ (k >> 33);
    }
}
