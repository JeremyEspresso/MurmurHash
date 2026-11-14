using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if NET8_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace MurmurHash;

// Hashing many independent keys at once. A single MurmurHash3_x86_32 hash is a serial chain that SIMD cannot speed
// up, but separate keys are independent: with AVX2 each of 8 vector lanes hashes its own key. Every method writes
// exactly what a loop of Hash32 calls would write.
public static partial class MurmurHash3
{
    /// <summary>
    /// Hashes each <paramref name="keyLength"/>-byte key in <paramref name="keys"/> using MurmurHash3 (x86, 32-bit)
    /// and writes the hashes to <paramref name="destination"/>: the same values as calling
    /// <see cref="Hash32(ReadOnlySpan{byte}, uint)"/> on each key, several times faster for many keys.
    /// </summary>
    /// <remarks>
    /// With AVX2, key lengths of 4, 8 and multiples of 16 bytes hash 8 keys at a time. Other lengths, other CPUs and
    /// the .NET Standard builds interleave 4 keys at a time.
    /// </remarks>
    /// <param name="keys">The keys, back to back. Its length must be a multiple of <paramref name="keyLength"/>.</param>
    /// <param name="keyLength">The length of each key in bytes.</param>
    /// <param name="destination">
    /// Receives the hash of key <c>i</c> at index <c>i</c>. It must hold at least
    /// <c>keys.Length / keyLength</c> elements and must not overlap <paramref name="keys"/>.
    /// </param>
    /// <param name="seed">The seed for this algorithm.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="keyLength"/> is zero or negative.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="keys"/> is not a whole number of keys, or <paramref name="destination"/> is too short.
    /// </exception>
    public static void Hash32Batch(ReadOnlySpan<byte> keys, int keyLength, Span<uint> destination, uint seed = 0)
    {
        if (keyLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(keyLength), keyLength, "The key length must be positive.");
        }

        int count = keys.Length / keyLength;
        if (count * keyLength != keys.Length)
        {
            throw new ArgumentException("The length of keys must be a multiple of keyLength.", nameof(keys));
        }

        if (destination.Length < count)
        {
            throw new ArgumentException("destination is shorter than the number of keys.", nameof(destination));
        }

        int done = 0;
#if NET8_0_OR_GREATER
        if (Avx2.IsSupported)
        {
            done = Hash32BatchAvx2.Hash(keys, keyLength, destination, count, seed);
        }
#endif
        Hash32BatchScalar(keys, keyLength, destination, done, count, seed);
    }

    /// <summary>
    /// Hashes the 4 little-endian bytes of each value in <paramref name="keys"/> using MurmurHash3 (x86, 32-bit) and
    /// writes the hashes to <paramref name="destination"/>: the same values as
    /// <c>Hash32Batch(MemoryMarshal.AsBytes(keys), 4, destination, seed)</c> on a little-endian machine, on every
    /// machine.
    /// </summary>
    /// <param name="keys">The keys.</param>
    /// <param name="destination">
    /// Receives the hash of <c>keys[i]</c> at index <c>i</c>. It must be at least as long as <paramref name="keys"/>.
    /// It may be the same memory as <paramref name="keys"/> (hashing in place); other overlaps are not supported.
    /// </param>
    /// <param name="seed">The seed for this algorithm.</param>
    /// <exception cref="ArgumentException"><paramref name="destination"/> is shorter than <paramref name="keys"/>.</exception>
    public static void Hash32Batch(ReadOnlySpan<uint> keys, Span<uint> destination, uint seed = 0)
    {
        if (destination.Length < keys.Length)
        {
            throw new ArgumentException("destination is shorter than keys.", nameof(destination));
        }

        int done = 0;
#if NET8_0_OR_GREATER
        if (Avx2.IsSupported)
        {
            done = Hash32BatchAvx2.Length4(MemoryMarshal.AsBytes(keys), destination, keys.Length, seed);
        }
#endif
        Hash32BatchScalar(keys, destination, done, seed);
    }

    /// <summary>
    /// Hashes the 8 little-endian bytes of each value in <paramref name="keys"/> using MurmurHash3 (x86, 32-bit) and
    /// writes the hashes to <paramref name="destination"/>: the same values as
    /// <c>Hash32Batch(MemoryMarshal.AsBytes(keys), 8, destination, seed)</c> on a little-endian machine, on every
    /// machine.
    /// </summary>
    /// <param name="keys">The keys.</param>
    /// <param name="destination">
    /// Receives the hash of <c>keys[i]</c> at index <c>i</c>. It must be at least as long as <paramref name="keys"/>
    /// and must not overlap it.
    /// </param>
    /// <param name="seed">The seed for this algorithm.</param>
    /// <exception cref="ArgumentException"><paramref name="destination"/> is shorter than <paramref name="keys"/>.</exception>
    public static void Hash32Batch(ReadOnlySpan<ulong> keys, Span<uint> destination, uint seed = 0)
    {
        if (destination.Length < keys.Length)
        {
            throw new ArgumentException("destination is shorter than keys.", nameof(destination));
        }

        int done = 0;
#if NET8_0_OR_GREATER
        if (Avx2.IsSupported)
        {
            done = Hash32BatchAvx2.Length8(MemoryMarshal.AsBytes(keys), destination, keys.Length, seed);
        }
#endif
        Hash32BatchScalar(keys, destination, done, seed);
    }

    /// <summary>Hashes <c>uint</c> keys from <paramref name="start"/> on without SIMD.</summary>
    internal static void Hash32BatchScalar(ReadOnlySpan<uint> keys, Span<uint> destination, int start, uint seed)
    {
        if (CpuInfo.HasFastComplexLea)
        {
            Hash32BatchUInt32<FusedLeaStep>(keys, destination, start, seed);
        }
        else
        {
            Hash32BatchUInt32<MultiplyStep>(keys, destination, start, seed);
        }
    }

    /// <summary>Hashes <c>ulong</c> keys from <paramref name="start"/> on without SIMD.</summary>
    internal static void Hash32BatchScalar(ReadOnlySpan<ulong> keys, Span<uint> destination, int start, uint seed)
    {
        if (CpuInfo.HasFastComplexLea)
        {
            Hash32BatchUInt64<FusedLeaStep>(keys, destination, start, seed);
        }
        else
        {
            Hash32BatchUInt64<MultiplyStep>(keys, destination, start, seed);
        }
    }

    /// <summary>Hashes keys <paramref name="start"/> to <paramref name="count"/> without SIMD.</summary>
    internal static void Hash32BatchScalar(ReadOnlySpan<byte> keys, int keyLength, Span<uint> destination, int start, int count, uint seed)
    {
        if (CpuInfo.HasFastComplexLea)
        {
            Hash32BatchScalar<FusedLeaStep>(keys, keyLength, destination, start, count, seed);
        }
        else
        {
            Hash32BatchScalar<MultiplyStep>(keys, keyLength, destination, start, count, seed);
        }
    }

    /// <summary>
    /// 4 keys at a time with their dependency chains interleaved, so the core overlaps them (measured 1.25-1.4x
    /// faster than a loop of <see cref="Hash32(ReadOnlySpan{byte}, uint)"/> calls), then the rest one by one.
    /// </summary>
    private static void Hash32BatchScalar<TStep>(ReadOnlySpan<byte> keys, int keyLength, Span<uint> destination, int start, int count, uint seed)
        where TStep : struct, IStep
    {
        ref byte first = ref MemoryMarshal.GetReference(keys);
        ref uint output = ref MemoryMarshal.GetReference(destination);
        nint stride = keyLength;
        nint blockBytes = keyLength & ~3;
        int remainder = keyLength & 3;
        uint length = (uint)keyLength;
        int i = start;
        for (; i <= count - 4; i += 4)
        {
            ref byte p0 = ref Unsafe.Add(ref first, i * stride);
            ref byte p1 = ref Unsafe.Add(ref p0, stride);
            ref byte p2 = ref Unsafe.Add(ref p1, stride);
            ref byte p3 = ref Unsafe.Add(ref p2, stride);
            uint h0 = seed, h1 = seed, h2 = seed, h3 = seed;
            for (nint o = 0; o < blockBytes; o += 4)
            {
                h0 ^= MixK(ReadUInt32LittleEndian(ref Unsafe.Add(ref p0, o)));
                h1 ^= MixK(ReadUInt32LittleEndian(ref Unsafe.Add(ref p1, o)));
                h2 ^= MixK(ReadUInt32LittleEndian(ref Unsafe.Add(ref p2, o)));
                h3 ^= MixK(ReadUInt32LittleEndian(ref Unsafe.Add(ref p3, o)));
                h0 = default(TStep).TimesFivePlusConstant(RotateLeft(h0, 13));
                h1 = default(TStep).TimesFivePlusConstant(RotateLeft(h1, 13));
                h2 = default(TStep).TimesFivePlusConstant(RotateLeft(h2, 13));
                h3 = default(TStep).TimesFivePlusConstant(RotateLeft(h3, 13));
            }

            if (remainder != 0)
            {
                h0 ^= MixK(ReadTail(ref Unsafe.Add(ref p0, blockBytes), remainder));
                h1 ^= MixK(ReadTail(ref Unsafe.Add(ref p1, blockBytes), remainder));
                h2 ^= MixK(ReadTail(ref Unsafe.Add(ref p2, blockBytes), remainder));
                h3 ^= MixK(ReadTail(ref Unsafe.Add(ref p3, blockBytes), remainder));
            }

            ref uint d = ref Unsafe.Add(ref output, i);
            d = FMix(h0 ^ length);
            Unsafe.Add(ref d, 1) = FMix(h1 ^ length);
            Unsafe.Add(ref d, 2) = FMix(h2 ^ length);
            Unsafe.Add(ref d, 3) = FMix(h3 ^ length);
        }

        for (; i < count; i++)
        {
            Unsafe.Add(ref output, i) = Hash32Core<TStep>(keys.Slice(i * keyLength, keyLength), seed);
        }
    }

    /// <summary>Reads the 1-3 byte tail of a key as the little-endian value it forms.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ReadTail(ref byte p, int remainder)
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

        return k;
    }

    /// <summary>The hash of a 4-byte key with the length known, so the whole hash inlines into the loop.</summary>
    private static void Hash32BatchUInt32<TStep>(ReadOnlySpan<uint> keys, Span<uint> destination, int start, uint seed)
        where TStep : struct, IStep
    {
        for (int i = start; i < keys.Length; i++)
        {
            uint h = default(TStep).TimesFivePlusConstant(RotateLeft(seed ^ MixK(keys[i]), 13));
            destination[i] = FMix(h ^ 4);
        }
    }

    /// <summary>The hash of an 8-byte key with the length known, so the whole hash inlines into the loop.</summary>
    private static void Hash32BatchUInt64<TStep>(ReadOnlySpan<ulong> keys, Span<uint> destination, int start, uint seed)
        where TStep : struct, IStep
    {
        for (int i = start; i < keys.Length; i++)
        {
            ulong key = keys[i];
            uint h = default(TStep).TimesFivePlusConstant(RotateLeft(seed ^ MixK((uint)key), 13));
            h = default(TStep).TimesFivePlusConstant(RotateLeft(h ^ MixK((uint)(key >> 32)), 13));
            destination[i] = FMix(h ^ 8);
        }
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// 8 keys per iteration, one per <see cref="Vector256{T}"/> lane. The keys are read with plain contiguous loads
    /// and transposed in registers so that each vector holds the same 4-byte block of 8 keys; AVX2 gathers measured
    /// 1.3-1.7x slower. Each method returns how many keys it hashed (a multiple of 8); the caller does the rest.
    /// Only called when <see cref="Avx2.IsSupported"/>, so the machine is little-endian.
    /// </summary>
    internal static class Hash32BatchAvx2
    {
        private const uint FiveConst = 0xe6546b64;

        internal static int Hash(ReadOnlySpan<byte> keys, int keyLength, Span<uint> destination, int count, uint seed) =>
            keyLength switch
            {
                4 => Length4(keys, destination, count, seed),
                8 => Length8(keys, destination, count, seed),
                16 => Length16(keys, destination, count, seed),
                _ when keyLength % 16 == 0 => Length16N(keys, keyLength, destination, count, seed),
                _ => 0,
            };

        internal static int Length4(ReadOnlySpan<byte> keys, Span<uint> destination, int count, uint seed)
        {
            ref uint source = ref Unsafe.As<byte, uint>(ref MemoryMarshal.GetReference(keys));
            ref uint output = ref MemoryMarshal.GetReference(destination);
            var h0 = Vector256.Create(seed);
            var length = Vector256.Create(4u);
            int i = 0;
            for (; i <= count - 8; i += 8)
            {
                var h = Round(h0, MixK(Vector256.LoadUnsafe(ref source, (nuint)i)));
                FMix(h ^ length).StoreUnsafe(ref output, (nuint)i);
            }

            return i;
        }

        internal static int Length8(ReadOnlySpan<byte> keys, Span<uint> destination, int count, uint seed)
        {
            ref float source = ref Unsafe.As<byte, float>(ref MemoryMarshal.GetReference(keys));
            ref uint output = ref MemoryMarshal.GetReference(destination);
            var h0 = Vector256.Create(seed);
            var length = Vector256.Create(8u);
            int i = 0;
            for (; i <= count - 8; i += 8)
            {
                var r0 = Vector256.LoadUnsafe(ref source, (nuint)(i * 2));
                var r1 = Vector256.LoadUnsafe(ref source, (nuint)((i * 2) + 8));

                // Even and odd 4-byte words of each key; lanes end up in key order 0,1,4,5,2,3,6,7, which one
                // vpermq at the end restores.
                var w0 = Avx.Shuffle(r0, r1, 0b10_00_10_00).AsUInt32();
                var w1 = Avx.Shuffle(r0, r1, 0b11_01_11_01).AsUInt32();
                var h = FMix(Round(Round(h0, MixK(w0)), MixK(w1)) ^ length);
                Avx2.Permute4x64(h.AsUInt64(), 0b11_01_10_00).AsUInt32().StoreUnsafe(ref output, (nuint)i);
            }

            return i;
        }

        internal static int Length16(ReadOnlySpan<byte> keys, Span<uint> destination, int count, uint seed)
        {
            ref uint source = ref Unsafe.As<byte, uint>(ref MemoryMarshal.GetReference(keys));
            ref uint output = ref MemoryMarshal.GetReference(destination);
            var h0 = Vector256.Create(seed);
            var length = Vector256.Create(16u);
            var restore = Vector256.Create(0u, 4, 1, 5, 2, 6, 3, 7);
            int i = 0;
            for (; i <= count - 8; i += 8)
            {
                // Each load holds two keys; after the in-lane 4x4 transpose the lanes hold keys 0,2,4,6,1,3,5,7.
                nuint b = (nuint)(i * 4);
                Transpose4x4(
                    Vector256.LoadUnsafe(ref source, b),
                    Vector256.LoadUnsafe(ref source, b + 8),
                    Vector256.LoadUnsafe(ref source, b + 16),
                    Vector256.LoadUnsafe(ref source, b + 24),
                    out var w0, out var w1, out var w2, out var w3);
                var h = FMix(Round4(h0, w0, w1, w2, w3) ^ length);
                Avx2.PermuteVar8x32(h, restore).StoreUnsafe(ref output, (nuint)i);
            }

            return i;
        }

        internal static int Length16N(ReadOnlySpan<byte> keys, int keyLength, Span<uint> destination, int count, uint seed)
        {
            ref byte source = ref MemoryMarshal.GetReference(keys);
            ref uint output = ref MemoryMarshal.GetReference(destination);
            var length = Vector256.Create((uint)keyLength);
            var restore = Vector256.Create(0u, 4, 1, 5, 2, 6, 3, 7);
            nuint l = (nuint)keyLength;
            int i = 0;
            for (; i <= count - 8; i += 8)
            {
                var h = Vector256.Create(seed);
                for (nuint c = (nuint)i * l, end = c + l; c < end; c += 16)
                {
                    // The same layout as Length16, built from 16-byte loads: load j holds keys 2j and 2j + 1.
                    var r0 = Vector256.Create(Vector128.LoadUnsafe(ref source, c), Vector128.LoadUnsafe(ref source, c + l));
                    var r1 = Vector256.Create(Vector128.LoadUnsafe(ref source, c + (2 * l)), Vector128.LoadUnsafe(ref source, c + (3 * l)));
                    var r2 = Vector256.Create(Vector128.LoadUnsafe(ref source, c + (4 * l)), Vector128.LoadUnsafe(ref source, c + (5 * l)));
                    var r3 = Vector256.Create(Vector128.LoadUnsafe(ref source, c + (6 * l)), Vector128.LoadUnsafe(ref source, c + (7 * l)));
                    Transpose4x4(r0.AsUInt32(), r1.AsUInt32(), r2.AsUInt32(), r3.AsUInt32(), out var w0, out var w1, out var w2, out var w3);
                    h = Round4(h, w0, w1, w2, w3);
                }

                Avx2.PermuteVar8x32(FMix(h ^ length), restore).StoreUnsafe(ref output, (nuint)i);
            }

            return i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Transpose4x4(Vector256<uint> r0, Vector256<uint> r1, Vector256<uint> r2, Vector256<uint> r3,
            out Vector256<uint> w0, out Vector256<uint> w1, out Vector256<uint> w2, out Vector256<uint> w3)
        {
            var t0 = Avx2.UnpackLow(r0, r1).AsUInt64();
            var t1 = Avx2.UnpackHigh(r0, r1).AsUInt64();
            var t2 = Avx2.UnpackLow(r2, r3).AsUInt64();
            var t3 = Avx2.UnpackHigh(r2, r3).AsUInt64();
            w0 = Avx2.UnpackLow(t0, t2).AsUInt32();
            w1 = Avx2.UnpackHigh(t0, t2).AsUInt32();
            w2 = Avx2.UnpackLow(t1, t3).AsUInt32();
            w3 = Avx2.UnpackHigh(t1, t3).AsUInt32();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<uint> Round4(Vector256<uint> h, Vector256<uint> w0, Vector256<uint> w1, Vector256<uint> w2, Vector256<uint> w3) =>
            Round(Round(Round(Round(h, MixK(w0)), MixK(w1)), MixK(w2)), MixK(w3));

        // Literal shift counts: .NET 8 does not fold `32 - r` after inlining and emits a slower register-count shift.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<uint> RotateLeft13(Vector256<uint> v) => Vector256.ShiftLeft(v, 13) | Vector256.ShiftRightLogical(v, 19);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<uint> RotateLeft15(Vector256<uint> v) => Vector256.ShiftLeft(v, 15) | Vector256.ShiftRightLogical(v, 17);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<uint> MixK(Vector256<uint> k) => RotateLeft15(k * Vector256.Create(C1)) * Vector256.Create(C2);

        // h * 5 as (h << 2) + h keeps the 10-cycle vpmulld off the serial chain.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<uint> Round(Vector256<uint> h, Vector256<uint> mixedK)
        {
            h = RotateLeft13(h ^ mixedK);
            return Vector256.ShiftLeft(h, 2) + h + Vector256.Create(FiveConst);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<uint> FMix(Vector256<uint> h)
        {
            h ^= Vector256.ShiftRightLogical(h, 16);
            h *= Vector256.Create(0x85ebca6bu);
            h ^= Vector256.ShiftRightLogical(h, 13);
            h *= Vector256.Create(0xc2b2ae35u);
            return h ^ Vector256.ShiftRightLogical(h, 16);
        }
    }
#endif
}
