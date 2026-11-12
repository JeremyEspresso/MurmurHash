# MurmurHash
A super fast implementation of the MurmurHash3 algorithm with zero heap allocations.

The initial implementation was based on [https://github.com/odinmillion/MurmurHash.Net](https://github.com/odinmillion/MurmurHash.Net). 
Except for the *magic* constants there is not much left of the original implementation.


## NuGet
Get MurmurHash on NuGet: [https://www.nuget.org/packages/JeremyEspresso.MurmurHash](https://www.nuget.org/packages/JeremyEspresso.MurmurHash)

---
## Supported platforms
| Target | Covers |
|---|---|
| `net8.0` | .NET 8, 9, 10, 11+ |
| `netstandard2.1` | .NET Core 3.x, Mono 6.4+, Xamarin, Unity 2021.2+ |
| `netstandard2.0` | .NET Framework 4.6.2+, older Mono/Unity |

The output is identical to the reference implementation ([SMHasher `MurmurHash3_x86_32`](https://github.com/aappleby/smhasher/blob/master/src/MurmurHash3.cpp)) on every platform and endianness.
The test suite checks 1505 reference vectors (lengths 0-300, 5 seeds) generated with the reference C code and cross-checked with the Python [`mmh3`](https://pypi.org/project/mmh3/) package.

---
## API Usage

```cs
using System.Text;
using MurmurHash;

string input = "Hello World!";
const uint seed = 420;

uint hash = MurmurHash3.Hash32(Encoding.UTF8.GetBytes(input), seed);
// or, without allocating: MurmurHash3.Hash32("Hello World!"u8, seed);

Console.WriteLine(hash);
```

Output:
```
1535517821
```

The `Hash32(ref ReadOnlySpan<byte>, uint)` overload from 0.0.x is still available for backwards compatibility.

---
## Benchmarks
```ini
BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.19045.2364)
Intel Core i7-9700K CPU 3.60GHz (Coffee Lake), 1 CPU, 8 logical and 8 physical cores
.NET SDK=7.0.101
  [Host]     : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
```
|                       Method |          StringInput |      Mean |     Error |    StdDev | Allocated |
|----------------------------- |--------------------- |----------:|----------:|----------:|----------:|
| **MurMurHash3** |  **!zhg(...)9N!% [128]** | **28.540 ns** | **0.6031 ns** | **0.9909 ns** | **-** |
| OdinMillion_MurMurHashDotNet |  !zhg(...)9N!% [128] | 39.308 ns | 0.8142 ns | 1.1934 ns | - |
| **MurMurHash3** | **a$6aj(...)5HFPe [40]** |  **9.142 ns** | **0.2075 ns** | **0.2625 ns** | **-** |
| OdinMillion_MurMurHashDotNet | a$6aj(...)5HFPe [40] | 12.032 ns | 0.2711 ns | 0.3620 ns | - |
| **MurMurHash3** | **key** |  **1.801 ns** | **0.0643 ns** | **0.0858 ns** | **-** |
| OdinMillion_MurMurHashDotNet | key | 4.415 ns | 0.1052 ns | 0.1542 ns | - |

---