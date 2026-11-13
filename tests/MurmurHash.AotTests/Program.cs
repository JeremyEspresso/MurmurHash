using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.CompilerServices;
using MurmurHash;

// Runs the reference vectors through the public API in a NativeAOT (or trimmed) app. Exit code 0 = all passed.
var data = new byte[4096];
for (int i = 0; i < data.Length; i++)
{
    data[i] = unchecked((byte)(i * 31 + 7));
}

using var stream = typeof(MurmurHash3).Assembly.GetManifestResourceStream("vectors.txt")
    ?? System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("vectors.txt")
    ?? throw new InvalidOperationException("vectors.txt resource missing");
using var reader = new StreamReader(stream);

Span<byte> destination = stackalloc byte[MurmurHash3.Hash128SizeInBytes];
int checks = 0;
int failures = 0;
while (reader.ReadLine() is { } line)
{
    if (line.Length == 0 || line[0] == '#')
    {
        continue;
    }

    var p = line.Split(' ');
    int length = int.Parse(p[0], CultureInfo.InvariantCulture);
    uint seed = uint.Parse(p[1], CultureInfo.InvariantCulture);
    uint expected = uint.Parse(p[2], CultureInfo.InvariantCulture);
    ulong expectedLow = ulong.Parse(p[3].AsSpan(0, 16), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    ulong expectedHigh = ulong.Parse(p[3].AsSpan(16, 16), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    foreach (int offset in (ReadOnlySpan<int>)[0, 1, 3])
    {
        var buffer = new byte[length + offset + 16];
        buffer.AsSpan().Fill(0xA5);
        data.AsSpan(0, length).CopyTo(buffer.AsSpan(offset));
        ReadOnlySpan<byte> input = buffer.AsSpan(offset, length);

        Check(MurmurHash3.Hash32(input, seed), expected, length, seed, "Hash32");
        Check(MurmurHash3.Hash32(ref input, seed), expected, length, seed, "Hash32(ref)");
        Check(MurmurHash3.Hash64(input, seed), expectedLow, length, seed, "Hash64");
        Check(MurmurHash3.Hash128(input, seed), new UInt128(expectedHigh, expectedLow), length, seed, "Hash128");

        MurmurHash3.Hash128(input, destination, seed);
        Check(BinaryPrimitives.ReadUInt64LittleEndian(destination), expectedLow, length, seed, "Hash128(destination) low");
        Check(BinaryPrimitives.ReadUInt64LittleEndian(destination[8..]), expectedHigh, length, seed, "Hash128(destination) high");
    }
}

Check(MurmurHash3.Hash32("Hello World!"u8, 420), 1535517821u, 12, 420, "README sample");

Console.WriteLine($"{(RuntimeFeature.IsDynamicCodeSupported ? "JIT" : "NativeAOT")}: {checks - failures}/{checks} checks passed");
return failures == 0 && checks > 27000 ? 0 : 1;

void Check<T>(T actual, T expected, int length, uint seed, string api)
    where T : IEquatable<T>
{
    checks++;
    if (!actual.Equals(expected))
    {
        failures++;
        Console.WriteLine($"FAIL {api} length={length} seed={seed} expected={expected} actual={actual}");
    }
}
