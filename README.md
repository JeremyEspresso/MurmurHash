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

The library is trimming- and NativeAOT-compatible (`IsAotCompatible`): CI publishes a NativeAOT app on .NET 8, 10 and 11
with zero trim/AOT warnings and checks it against the reference vectors.

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
| 3 B | 1.37 ns | 1.37 ns | 1.38 ns | 1.38 ns |
| 16 B | 2.45 ns | 2.42 ns | 2.44 ns | 2.38 ns |
| 40 B | 5.21 ns | 5.22 ns | 5.22 ns | 5.24 ns |
| 128 B | 20.53 ns | 20.49 ns | 20.52 ns | 20.54 ns |
| 1024 B | 193 ns | 194 ns | 194 ns | 193 ns |
| 16384 B | 3,247 ns | 3,211 ns | 3,212 ns | 3,212 ns |

Compared with other implementations on .NET 8.0 (time relative to this library, lower is better):

| Input | This library | [MurmurHash.Net](https://github.com/odinmillion/MurmurHash.Net) 0.0.2 | [HashDepot](https://github.com/ssg/HashDepot) 3.2.0 |
|---|---:|---:|---:|
| 3 B | 1.37 ns | 2.03x | 11.49x |
| 16 B | 2.45 ns | 1.40x | 5.99x |
| 40 B | 5.21 ns | 1.44x | 4.16x |
| 128 B | 20.53 ns | 1.22x | 2.12x |
| 1024 B | 193 ns | 1.26x | 1.35x |
| 16384 B | 3,247 ns | 1.23x | 1.23x |

Compared with other implementations on .NET 10.0 (time relative to this library, lower is better):

| Input | This library | [MurmurHash.Net](https://github.com/odinmillion/MurmurHash.Net) 0.0.2 | [HashDepot](https://github.com/ssg/HashDepot) 3.2.0 |
|---|---:|---:|---:|
| 3 B | 1.38 ns | 1.50x | 6.18x |
| 16 B | 2.44 ns | 1.37x | 1.44x |
| 40 B | 5.22 ns | 1.44x | 1.49x |
| 128 B | 20.52 ns | 1.22x | 1.24x |
| 1024 B | 194 ns | 1.26x | 1.26x |
| 16384 B | 3,212 ns | 1.24x | 1.24x |

None of the implementations allocate, and all three return identical, reference-correct hashes.

These results are from an Intel Raptor Lake CPU, where `Hash32` uses its CPU-specific code path (Intel Ice Lake and newer). On other CPUs the general code path is used; it is about 10-19% slower for inputs of 16 bytes and larger and still matches or beats the other implementations. For large inputs, throughput is bounded by the algorithm itself: MurmurHash3_x86_32 is a strictly serial chain of operations per 4-byte block.

```ini
BenchmarkDotNet v0.16.0-preview.2, Linux Fedora Linux 45 (Kinoite), VM
13th Gen Intel Core i9-13900K, 26 vCPUs, AVX2
.NET 8.0.34, 9.0.23, 10.0.15, 11.0.0 (X64 RyuJIT x86-64-v3)
```
Full results: [benchmarks/results-2026-11.md](benchmarks/results-2026-11.md). Run them yourself with
`dotnet run -c Release --project benchmarks/MurmurHash.Benchmarks -f net10.0 -- --filter "*HashBenchmarks*" --tfms net8.0,net10.0`.

---
