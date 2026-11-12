using System.Globalization;

namespace MurmurHash.UnitTests;

/// <summary>Reference vectors from SMHasher's MurmurHash3, cross-checked with Python mmh3. See TestVectors/.</summary>
internal static class TestVectors
{
    public static readonly byte[] Data = CreateData();

    public static readonly IReadOnlyList<Vector> All = Load();

    public sealed record Vector(int Length, uint Seed, uint Hash32, ulong Hash128Low, ulong Hash128High);

    private static byte[] CreateData()
    {
        var data = new byte[4096];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = unchecked((byte)(i * 31 + 7));
        }

        return data;
    }

    private static List<Vector> Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestVectors", "vectors.txt");
        var result = new List<Vector>();
        foreach (var line in File.ReadLines(path))
        {
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var p = line.Split(' ');
            result.Add(new Vector(
                int.Parse(p[0], CultureInfo.InvariantCulture),
                uint.Parse(p[1], CultureInfo.InvariantCulture),
                uint.Parse(p[2], CultureInfo.InvariantCulture),
                ulong.Parse(p[3].AsSpan(0, 16).ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                ulong.Parse(p[3].AsSpan(16, 16).ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture)));
        }

        return result;
    }

    /// <summary>
    /// Copies the input into the middle of a larger buffer filled with garbage, so that
    /// any read outside the span changes the result.
    /// </summary>
    public static ReadOnlySpan<byte> Poisoned(ReadOnlySpan<byte> input, int offset, byte poison = 0xA5)
    {
        var buffer = new byte[input.Length + offset + 32];
        buffer.AsSpan().Fill(poison);
        input.CopyTo(buffer.AsSpan(offset));
        return buffer.AsSpan(offset, input.Length);
    }
}
