# MurmurHash
A super fast implementation of the MurmurHash3 algorithm with zero heap allocations.

The initial implementation was based on [https://github.com/odinmillion/MurmurHash.Net](https://github.com/odinmillion/MurmurHash.Net). 
Except for the *magic* constants there is not much left of the original implementation.


## NuGet
Get MurmurHash on NuGet: [https://www.nuget.org/packages/JeremyEspresso.MurmurHash](https://www.nuget.org/packages/JeremyEspresso.MurmurHash)

---
## API Usage

```cs
using MurmurHash;

string input = "Hello World!";
const uint seed = 420;

ReadOnlySpan<byte> inputSpan = Encoding.UTF8.GetBytes(input).AsSpan();

uint hash = MurmurHash3.Hash32(ref inputSpan, seed);

Console.WriteLine(hash);
```

Output:
```
1535517821
```
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