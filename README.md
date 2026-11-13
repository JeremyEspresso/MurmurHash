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

The output is identical to the reference implementation ([SMHasher `MurmurHash3_x86_32` and `MurmurHash3_x64_128`](https://github.com/aappleby/smhasher/blob/master/src/MurmurHash3.cpp)) on every platform and endianness.
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

### 128-bit and 64-bit hashes

`Hash128` and `Hash64` implement MurmurHash3 x64 128-bit, the variant to use when you need more than 32 bits.
It is also faster than `Hash32` for inputs of 40 bytes and larger (about 2x at 128 bytes and 2.4x for large inputs), because it processes 16 bytes per step.

```cs
ReadOnlySpan<byte> data = "Hello World!"u8;

UInt128 hash128 = MurmurHash3.Hash128(data, 420);    // .NET 8 and later
Console.WriteLine(hash128.ToString("x32"));         // 9ef90e0d6173d4c70d22a9c023252218

Span<byte> bytes = stackalloc byte[MurmurHash3.Hash128SizeInBytes];
MurmurHash3.Hash128(data, bytes, 420);              // all platforms
Console.WriteLine(Convert.ToHexString(bytes));      // 18222523C0A9220DC7D473610D0EF99E

ulong hash64 = MurmurHash3.Hash64(data, 420);       // the first 64 bits of the 128-bit hash
Console.WriteLine(hash64);                          // 946505514389676568
```

- The `destination` overload writes the 16 bytes in the reference implementation's byte order (the same bytes as
  SMHasher's output and Python's `mmh3.hash_bytes`).
- In the `UInt128`, the reference implementation's `h1` is the lower 64 bits and `h2` the upper 64 bits, so on
  little-endian machines its in-memory layout equals the `destination` bytes.
- `Hash64` returns `h1`, the first 8 bytes of the 128-bit hash: the same value as Python's
  `mmh3.hash64(data, seed, signed=False)[0]`.

### Streams and data in pieces

`MurmurHash32` and `MurmurHash128` hash data that arrives in pieces, or from a `Stream`. They derive from
[`System.IO.Hashing.NonCryptographicHashAlgorithm`](https://learn.microsoft.com/dotnet/api/system.io.hashing.noncryptographichashalgorithm),
like `XxHash32` and `Crc32`, so they work wherever that type is accepted. The result is the same as the one-shot
methods over all appended data, however it is split.

```cs
var hasher = new MurmurHash128(seed: 420);
hasher.Append("Hello "u8);
hasher.Append("World!"u8);
Console.WriteLine(hasher.GetCurrentHashAsUInt128().ToString("x32")); // 9ef90e0d6173d4c70d22a9c023252218

using var file = File.OpenRead("data.bin");
var fileHasher = new MurmurHash32();
fileHasher.Append(file);                    // or await fileHasher.AppendAsync(file)
uint fileHash = fileHasher.GetCurrentHashAsUInt32();
```

- `GetCurrentHash()` returns the bytes in the reference implementation's byte order (Python's `mmh3.mmh3_32_digest` and
  `mmh3.hash_bytes`). `GetCurrentHashAsUInt32()`, `GetCurrentHashAsUInt64()` (the same value as `Hash64`) and
  `GetCurrentHashAsUInt128()` (.NET 8 and later) return the value without allocating.
- `GetHashAndReset()` and `Reset()` start over with the same seed. Instances are not thread-safe.
- These classes need the [System.IO.Hashing](https://www.nuget.org/packages/System.IO.Hashing) package, which is
  this library's only dependency on .NET 8 and later.

---
## Benchmarks

### Hash32

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

### Hash128

`MurmurHash3.Hash128` on each runtime (mean, lower is better):

| Input | .NET 8.0 | .NET 10.0 | .NET 11.0 |
|---|---:|---:|---:|
| 3 B | 2.43 ns | 2.41 ns | 2.42 ns |
| 16 B | 2.74 ns | 2.73 ns | 2.69 ns |
| 40 B | 4.27 ns | 4.26 ns | 4.17 ns |
| 128 B | 10.47 ns | 10.44 ns | 10.33 ns |
| 1024 B | 84.27 ns | 86.33 ns | 83.38 ns |
| 16384 B | 1,341 ns | 1,340 ns | 1,342 ns |

Compared with a direct port of the reference implementation and with HashDepot (time relative to this library, lower is better):

| Input | This library (.NET 8.0) | Reference port | [HashDepot](https://github.com/ssg/HashDepot) 3.2.0 | This library (.NET 10.0) | Reference port | HashDepot 3.2.0 |
|---|---:|---:|---:|---:|---:|---:|
| 3 B | 2.43 ns | 1.44x | 8.32x | 2.41 ns | 1.34x | 7.90x |
| 16 B | 2.74 ns | 1.50x | 6.12x | 2.73 ns | 1.36x | 2.65x |
| 40 B | 4.27 ns | 1.60x | 4.33x | 4.26 ns | 1.58x | 3.25x |
| 128 B | 10.47 ns | 1.45x | 2.81x | 10.44 ns | 1.43x | 1.69x |
| 1024 B | 84.27 ns | 1.29x | 1.42x | 86.33 ns | 1.23x | 1.28x |
| 16384 B | 1,341 ns | 1.23x | 1.24x | 1,340 ns | 1.23x | 1.24x |

For short inputs of mixed lengths (0-32 bytes), where the tail handling cannot be predicted, the reference port takes
2.0-2.3x as long. This library does not allocate; HashDepot allocates a 40-byte array per call.

`Hash128` uses the same CPU-specific code path as `Hash32` on Intel Ice Lake and newer. The general code path is
6-7% slower for 16-40 byte inputs and 20-23% slower for inputs of 128 bytes and larger.

### Streams and data in pieces

`MurmurHash32` and `MurmurHash128` against the one-shot methods (.NET 10.0, mean, lower is better):

| Input | `Hash32` | `MurmurHash32` | `Hash128` | `MurmurHash128` |
|---|---:|---:|---:|---:|
| 16 B, one `Append` | 2.37 ns | 4.10 ns | 2.69 ns | 5.44 ns |
| 128 B, one `Append` | 20.42 ns | 22.40 ns | 10.35 ns | 12.97 ns |
| 16 KB, one `Append` | 3,186 ns | 3,266 ns | 1,318 ns | 1,344 ns |
| 16 KB in 7-byte pieces | | 6,806 ns | | 5,707 ns |

For inputs of 1 KB and more, appending costs the same as the one-shot methods (within 1-4%).

Compared with the other packages that hash MurmurHash3 incrementally, each through its own incremental or stream API
(.NET 10.0, time relative to this library, lower is better):

| Input | This library | [murmurhash](https://github.com/darrenkopp/murmurhash-net) 1.0.3 (unsafe) | [Data.HashFunction](https://github.com/deskasoft/Data.HashFunction) 3.1.2 | [HashifyNET](https://github.com/Deskasoft/HashifyNET) 7.1.1 |
|---|---:|---:|---:|---:|
| **32-bit** | | | | |
| 16 B | 4.90 ns | 13.00x | 15.61x | 11.96x |
| 16 KB | 3,031 ns | 1.26x | 4.27x | 1.91x |
| 16 KB in 7-byte pieces | 6,827 ns | wrong hash | 15.92x | 2.10x |
| 1 MB stream | 204 us | 1.22x | 4.14x | 1.85x |
| **128-bit** | | | | |
| 16 B | 5.08 ns | 12.83x | 20.05x | 15.27x |
| 16 KB | 1,274 ns | 1.27x | 5.30x | 3.09x |
| 16 KB in 7-byte pieces | 5,890 ns | wrong hash | wrong hash | 1.96x |
| 16 KB in 1500-byte pieces | 1,300 ns | wrong hash | 6.13x | 3.06x |
| 1 MB stream | 90.8 us | 1.20x | 5.52x | 2.73x |

This library allocates nothing; the others allocate 64 B to 4.5 KB per hash (Data.HashFunction 110 KB for the
7-byte pieces). "Wrong hash" means the package returns a different result than MurmurHash3 over the same bytes:
murmurhash treats the end of every piece as the end of the input, and Data.HashFunction mishandles partial blocks
between pieces. All other results were checked against the reference output before timing. These timings include
writing the hash to a byte array, as the other packages do. murmurhash's managed variant, .NET 8.0 and
System.IO.Hashing's xxHash (a different algorithm) are in the full results.

```ini
BenchmarkDotNet v0.16.0-preview.2, Linux Fedora Linux 45 (Kinoite), VM
13th Gen Intel Core i9-13900K, 26 vCPUs, AVX2
.NET 8.0.34, 9.0.23, 10.0.15, 11.0.0 (X64 RyuJIT x86-64-v3)
```
Full results: [benchmarks/results-2026-11.md](benchmarks/results-2026-11.md) (`Hash32`) and
[benchmarks/results-hash128-2026-11.md](benchmarks/results-hash128-2026-11.md) (`Hash128`) and
[benchmarks/results-incremental-2026-11.md](benchmarks/results-incremental-2026-11.md) (incremental classes) and
[benchmarks/results-comparison-2026-11.md](benchmarks/results-comparison-2026-11.md) (other MurmurHash3 packages). Run them yourself with
`dotnet run -c Release --project benchmarks/MurmurHash.Benchmarks -f net10.0 -- --filter "*HashBenchmarks*" "*Hash128Benchmarks*" "*IncrementalBenchmarks*" "*ChunkedAppendBenchmarks*" --tfms net8.0,net10.0`
and `dotnet run -c Release --project benchmarks/MurmurHash.Comparison -f net10.0 -- --filter "*Comparison*"`.

---
