using BenchmarkDotNet.Attributes;
using System.Text;

namespace MurmurHash.Benchmarks;

[MemoryDiagnoser]
public class HashBenchmarks
{
    [Params("key", "a$6ajXViSAfFw5pR2kkz3Q28YGrDx$jeaLJ5HFPe", "!zhgt#HVY#tV%kPPZ$LXYEo@EqyKjqRJPzUb3*hhASWpdyZAF3!t$V96j9Eb9ivzMH2w4jvuyHaXRxd&YbHz*W8yZGJ#CXjXfqMzNGgf@YMfh*RdZpRXtPQ3mV$N9N!%")]
    public string StringInput { get; set; }

    public byte[] Input { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Input = Encoding.UTF8.GetBytes(StringInput);
    }

    // https://github.com/odinmillion/MurmurHash.Net
    [Benchmark]
    public uint OdinMillion_MurMurHashDotNet()
    {
        ReadOnlySpan<byte> arr = Input.AsSpan();
        return MurmurHash.Net.MurmurHash3.Hash32(arr, 69);
    }


    [Benchmark]
    public uint MurMurHash3()
    {
        ReadOnlySpan<byte> arr = Input.AsSpan();
        return MurmurHash3.Hash32(ref arr, 69);
    }
}