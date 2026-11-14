using System.Buffers.Binary;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MurmurHash;

/// <summary>
/// Computes MurmurHash3 (x86, 32-bit) incrementally, for data that arrives in pieces or from a <see cref="Stream"/>.
/// The result is the same as <see cref="MurmurHash3.Hash32(ReadOnlySpan{byte}, uint)"/> over all appended data,
/// however it is split.
/// </summary>
/// <remarks>
/// <para>The hash bytes (<see cref="NonCryptographicHashAlgorithm.GetCurrentHash()"/>) are in the reference
/// implementation's byte order: the 32-bit value in little-endian, the same bytes as Python's
/// <c>mmh3.mmh3_32_digest</c>.</para>
/// <para>Instances are not thread-safe.</para>
/// </remarks>
public sealed class MurmurHash32 : NonCryptographicHashAlgorithm
{
    private const int HashSizeInBytes = 4;

    private readonly uint _seed;
    private uint _h;
    private ulong _length;

    // The 0-3 bytes not yet mixed in, as the little-endian value they form.
    private uint _tail;
    private int _tailLength;

    /// <summary>Initializes a new instance with a seed of 0.</summary>
    public MurmurHash32()
        : this(0)
    {
    }

    /// <summary>Initializes a new instance with the specified seed.</summary>
    /// <param name="seed">The seed for this algorithm.</param>
    public MurmurHash32(uint seed)
        : base(HashSizeInBytes)
    {
        _seed = seed;
        _h = seed;
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
            int count = Math.Min(4 - _tailLength, remaining);
            _tail |= MurmurHash3.ReadUpTo3(ref p, count) << (_tailLength * 8);
            _tailLength += count;
            if (_tailLength < 4)
            {
                return;
            }

            _h = MurmurHash3.Hash32Block(_h, _tail);
            _tail = 0;
            _tailLength = 0;
            p = ref Unsafe.Add(ref p, count);
            remaining -= count;
        }

        int wholeBlocks = remaining & ~3;
        if (wholeBlocks != 0)
        {
            _h = MurmurHash3.Hash32Blocks(_h, source.Slice(source.Length - remaining, wholeBlocks));
            p = ref Unsafe.Add(ref p, wholeBlocks);
        }

        // _tailLength and _tail are 0 here.
        if (remaining != wholeBlocks)
        {
            _tailLength = remaining - wholeBlocks;
            _tail = MurmurHash3.ReadUpTo3(ref p, _tailLength);
        }
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        _h = _seed;
        _length = 0;
        _tail = 0;
        _tailLength = 0;
    }

    /// <summary>Gets the hash of the data appended so far, without changing the state.</summary>
    /// <returns>The 32-bit MurmurHash3 of the data appended so far.</returns>
    public uint GetCurrentHashAsUInt32() => MurmurHash3.Hash32Finish(_h, _tail, _tailLength, _length);

    /// <inheritdoc/>
    protected override void GetCurrentHashCore(Span<byte> destination) =>
        BinaryPrimitives.WriteUInt32LittleEndian(destination, GetCurrentHashAsUInt32());
}
