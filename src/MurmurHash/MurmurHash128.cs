using System.Buffers.Binary;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MurmurHash;

/// <summary>
/// Computes MurmurHash3 (x64, 128-bit) incrementally, for data that arrives in pieces or from a <see cref="Stream"/>.
/// The result is the same as <see cref="MurmurHash3.Hash128(ReadOnlySpan{byte}, Span{byte}, uint)"/> over all
/// appended data, however it is split.
/// </summary>
/// <remarks>
/// <para>The hash bytes (<see cref="NonCryptographicHashAlgorithm.GetCurrentHash()"/>) are in the reference
/// implementation's byte order: <c>h1</c> then <c>h2</c>, each little-endian, the same bytes as Python's
/// <c>mmh3.hash_bytes</c>.</para>
/// <para>Instances are not thread-safe.</para>
/// </remarks>
public sealed class MurmurHash128 : NonCryptographicHashAlgorithm
{
    private readonly uint _seed;
    private ulong _h1;
    private ulong _h2;
    private ulong _length;

    // The 0-15 bytes not yet mixed in, as the little-endian value they form: bytes 0-7 in _t1, bytes 8-15 in _t2.
    private ulong _t1;
    private ulong _t2;
    private int _tailLength;

    /// <summary>Initializes a new instance with a seed of 0.</summary>
    public MurmurHash128()
        : this(0)
    {
    }

    /// <summary>Initializes a new instance with the specified seed.</summary>
    /// <param name="seed">The seed for this algorithm.</param>
    public MurmurHash128(uint seed)
        : base(MurmurHash3.Hash128SizeInBytes)
    {
        _seed = seed;
        _h1 = seed;
        _h2 = seed;
    }

    /// <inheritdoc/>
    public override void Append(ReadOnlySpan<byte> source)
    {
        if (source.IsEmpty)
        {
            return;
        }

        _length += (ulong)source.Length;

        ref byte p = ref MemoryMarshal.GetReference(source);
        int remaining = source.Length;

        if (_tailLength != 0)
        {
            int count = Math.Min(16 - _tailLength, remaining);
            MurmurHash3.ReadPartialBlock(ref p, count, out ulong low, out ulong high);

            // Shift the piece's 128-bit value left by _tailLength bytes (8-120 bits) and merge it into (_t1, _t2).
            // The result never exceeds 16 bytes, so nothing shifted out is lost.
            int shift = _tailLength * 8;
            if (shift < 64)
            {
                _t1 |= low << shift;
                _t2 |= (high << shift) | (low >> (64 - shift));
            }
            else
            {
                _t2 |= low << (shift - 64);
            }

            _tailLength += count;
            if (_tailLength < 16)
            {
                return;
            }

            MurmurHash3.Hash128Block(ref _h1, ref _h2, _t1, _t2);
            _t1 = 0;
            _t2 = 0;
            _tailLength = 0;
            p = ref Unsafe.Add(ref p, count);
            remaining -= count;
        }

        int wholeBlocks = remaining & ~15;
        if (wholeBlocks != 0)
        {
            MurmurHash3.Hash128Blocks(ref _h1, ref _h2, source.Slice(source.Length - remaining, wholeBlocks));
            p = ref Unsafe.Add(ref p, wholeBlocks);
        }

        // _tailLength, _t1 and _t2 are 0 here.
        if (remaining != wholeBlocks)
        {
            _tailLength = remaining - wholeBlocks;
            MurmurHash3.ReadPartialBlock(ref p, _tailLength, out _t1, out _t2);
        }
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        _h1 = _seed;
        _h2 = _seed;
        _length = 0;
        _t1 = 0;
        _t2 = 0;
        _tailLength = 0;
    }

    /// <summary>Gets the first 64 bits of the hash of the data appended so far, without changing the state.</summary>
    /// <returns>The same value as <see cref="MurmurHash3.Hash64(ReadOnlySpan{byte}, uint)"/> over the data appended so far.</returns>
    public ulong GetCurrentHashAsUInt64()
    {
        GetCurrentHash(out ulong h1, out _);
        return h1;
    }

#if NET8_0_OR_GREATER
    /// <summary>Gets the hash of the data appended so far, without changing the state.</summary>
    /// <returns>The same value as <see cref="MurmurHash3.Hash128(ReadOnlySpan{byte}, uint)"/> over the data appended so far.</returns>
    public UInt128 GetCurrentHashAsUInt128()
    {
        GetCurrentHash(out ulong h1, out ulong h2);
        return new UInt128(h2, h1);
    }
#endif

    /// <inheritdoc/>
    protected override void GetCurrentHashCore(Span<byte> destination)
    {
        GetCurrentHash(out ulong h1, out ulong h2);
        BinaryPrimitives.WriteUInt64LittleEndian(destination, h1);
        BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(8), h2);
    }

    private void GetCurrentHash(out ulong h1, out ulong h2) =>
        MurmurHash3.Hash128Finish(_h1, _h2, _t1, _t2, _tailLength, _length, out h1, out h2);
}
