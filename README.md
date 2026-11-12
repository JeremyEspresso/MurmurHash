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

`MurmurHash3.Hash32` on each runtime (mean, lower is better):

| Input | .NET 8.0 | .NET 9.0 | .NET 10.0 | .NET 11.0 |
|---|---:|---:|---:|---:|
| 3 B | 1.34 ns | 1.34 ns | 1.32 ns | 1.31 ns |
| 16 B | 2.65 ns | 2.63 ns | 2.61 ns | 2.59 ns |
| 40 B | 6.06 ns | 6.10 ns | 6.07 ns | 6.00 ns |
| 128 B | 22.22 ns | 21.90 ns | 21.95 ns | 22.03 ns |
| 1024 B | 235 ns | 239 ns | 239 ns | 238 ns |
| 16384 B | 3,887 ns | 3,914 ns | 3,829 ns | 3,861 ns |

Compared with other implementations on .NET 8.0 (time relative to this library, lower is better):

| Input | This library | [MurmurHash.Net](https://github.com/odinmillion/MurmurHash.Net) 0.0.2 | [HashDepot](https://github.com/ssg/HashDepot) 3.2.0 |
|---|---:|---:|---:|
| 3 B | 1.34 ns | 2.02x | 11.61x |
| 16 B | 2.65 ns | 1.25x | 5.33x |
| 40 B | 6.06 ns | 1.22x | 3.54x |
| 128 B | 22.22 ns | 1.13x | 1.92x |
| 1024 B | 235 ns | 1.02x | 1.10x |
| 16384 B | 3,887 ns | 1.01x | 1.01x |

Compared with other implementations on .NET 10.0 (time relative to this library, lower is better):

| Input | This library | [MurmurHash.Net](https://github.com/odinmillion/MurmurHash.Net) 0.0.2 | [HashDepot](https://github.com/ssg/HashDepot) 3.2.0 |
|---|---:|---:|---:|
| 3 B | 1.32 ns | 1.67x | 6.20x |
| 16 B | 2.61 ns | 1.35x | 1.34x |
| 40 B | 6.07 ns | 1.20x | 1.24x |
| 128 B | 21.95 ns | 1.11x | 1.13x |
| 1024 B | 239 ns | 1.02x | 1.02x |
| 16384 B | 3,829 ns | 1.01x | 1.00x |

None of the implementations allocate. All three return identical, reference-correct hashes.
From about 1 KB the implementations converge: MurmurHash3_x86_32 is a strictly serial chain (~4 cycles per 4 bytes), which caps throughput at ~4.3 bytes/ns on this machine regardless of implementation.

```ini
BenchmarkDotNet v0.16.0-preview.2, Linux Fedora Linux 45 (Kinoite), VM
13th Gen Intel Core i9-13900K, 26 vCPUs, AVX2
.NET 8.0.34, 9.0.23, 10.0.15, 11.0.0 (X64 RyuJIT x86-64-v3)
```
Full results: [benchmarks/results-2026-11.md](benchmarks/results-2026-11.md). Run them yourself with
`dotnet run -c Release --project benchmarks/MurmurHash.Benchmarks -f net10.0 -- --filter "*HashBenchmarks*" --tfms net8.0,net10.0`.

---
