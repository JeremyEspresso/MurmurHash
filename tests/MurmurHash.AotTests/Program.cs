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

    foreach (int offset in (ReadOnlySpan<int>)[0, 1, 3])
    {
        var buffer = new byte[length + offset + 16];
        buffer.AsSpan().Fill(0xA5);
        data.AsSpan(0, length).CopyTo(buffer.AsSpan(offset));
        ReadOnlySpan<byte> input = buffer.AsSpan(offset, length);

        Check(MurmurHash3.Hash32(input, seed), expected, length, seed, "Hash32");
        Check(MurmurHash3.Hash32(ref input, seed), expected, length, seed, "Hash32(ref)");
    }
}

Check(MurmurHash3.Hash32("Hello World!"u8, 420), 1535517821, 12, 420, "README sample");

Console.WriteLine($"{(RuntimeFeature.IsDynamicCodeSupported ? "JIT" : "NativeAOT")}: {checks - failures}/{checks} checks passed");
return failures == 0 && checks > 9000 ? 0 : 1;

void Check(uint actual, uint expected, int length, uint seed, string api)
{
    checks++;
    if (actual != expected)
    {
        failures++;
        Console.WriteLine($"FAIL {api} length={length} seed={seed} expected={expected} actual={actual}");
    }
}
