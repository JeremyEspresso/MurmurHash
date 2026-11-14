# Incremental MurmurHash3 compared with other packages (2026-11)

`MurmurHash32` / `MurmurHash128` against the other .NET packages that hash MurmurHash3 incrementally, each used through
its own incremental or stream API (`benchmarks/MurmurHash.Comparison`):

| Package | API used |
|---|---|
| This library | `NonCryptographicHashAlgorithm.Append`, `Append(Stream)`; one instance, reset per hash |
| [murmurhash](https://www.nuget.org/packages/murmurhash) 1.0.3 | `HashAlgorithm.TransformBlock`, `ComputeHash(Stream)`; one instance; managed and unsafe variants |
| [Data.HashFunction.MurmurHash](https://www.nuget.org/packages/Data.HashFunction.MurmurHash) 3.1.2 | block transformer, `TransformBytes(ArraySegment<byte>)`, `ComputeHash(Stream)` |
| [HashifyNET](https://www.nuget.org/packages/HashifyNET) 7.1.1 | block transformer, `TransformBytes(ReadOnlySpan<byte>)`, `ComputeHash(Stream)` |

Each library's output is checked against `MurmurHash3.Hash32` / `Hash128` (validated against the reference vectors)
before it is timed; a library that returns a wrong hash for a scenario is not timed and is shown as "wrong hash".
The machine was idle during the run (1-minute load average median 1.00, maximum 1.17, i.e. the benchmark alone).

## Correctness

| Package | Problem |
|---|---|
| murmurhash 1.0.3 | Every `TransformBlock` call mixes its own leftover bytes as the final tail, so the hash is wrong unless each piece is a whole number of blocks (4 bytes for 32-bit, 16 bytes for 128-bit). `ComputeHash(Stream)` is affected too when the stream returns short reads. Reported in [darrenkopp/murmurhash-net#10](https://github.com/darrenkopp/murmurhash-net/issues/10). |
| Data.HashFunction 3.1.2 | The 128-bit block transformer returns a wrong hash for 7-byte pieces (partial-block bug in `BlockTransformerBase`). `TransformBytes(byte[], int, int)` ignores the offset and count, so the benchmark uses the `ArraySegment` overload. |
| HashifyNET 7.1.1 | Correct in every scenario here. `TransformBytes(byte[], int, int)` ignores the offset and count, so the benchmark uses the span overload. |

## Results

Time relative to this library, lower is better; allocations per hash in brackets. This library allocates nothing in
any scenario.

### 32-bit, .NET 8.0

| Input | This library | murmurhash | murmurhash (unsafe) | Data.HashFunction | HashifyNET |
|---|---:|---:|---:|---:|---:|
| 16 B | 5.12 ns | 11.97x (96 B) | 11.99x (96 B) | 17.84x (144 B) | 12.99x (216 B) |
| 1 KB | 185 ns | 1.72x (96 B) | 1.54x (96 B) | 5.39x (144 B) | 2.35x (216 B) |
| 16 KB | 3,081 ns | 1.34x (96 B) | 1.24x (96 B) | 4.29x (144 B) | 1.88x (216 B) |
| 16 KB in 7 B pieces | 6,973 ns | wrong hash | wrong hash | 16.21x (112,464 B) | 2.09x (216 B) |
| 16 KB in 1500 B pieces | 3,063 ns | 1.38x (96 B) | 1.25x (96 B) | 5.04x (144 B) | 1.92x (216 B) |
| 1 MB stream | 204,282 ns | 1.34x (64 B) | 1.23x (64 B) | 4.59x (4,264 B) | 1.84x (4,336 B) |

### 32-bit, .NET 10.0

| Input | This library | murmurhash | murmurhash (unsafe) | Data.HashFunction | HashifyNET |
|---|---:|---:|---:|---:|---:|
| 16 B | 4.90 ns | 13.07x (96 B) | 13.00x (96 B) | 15.61x (144 B) | 11.96x (216 B) |
| 1 KB | 185 ns | 1.73x (96 B) | 1.55x (96 B) | 4.74x (144 B) | 2.24x (216 B) |
| 16 KB | 3,031 ns | 1.35x (96 B) | 1.26x (96 B) | 4.27x (144 B) | 1.91x (216 B) |
| 16 KB in 7 B pieces | 6,827 ns | wrong hash | wrong hash | 15.92x (112,464 B) | 2.10x (216 B) |
| 16 KB in 1500 B pieces | 3,053 ns | 1.36x (96 B) | 1.26x (96 B) | 4.59x (144 B) | 1.95x (216 B) |
| 1 MB stream | 204,101 ns | 1.34x (64 B) | 1.22x (64 B) | 4.14x (4,264 B) | 1.85x (4,336 B) |

The murmurhash 32-bit results for 1500-byte pieces are correct only because 1500 is a multiple of 4.

### 128-bit, .NET 8.0

| Input | This library | murmurhash | murmurhash (unsafe) | Data.HashFunction | HashifyNET |
|---|---:|---:|---:|---:|---:|
| 16 B | 6.53 ns | 12.56x (184 B) | 9.64x (120 B) | 21.98x (352 B) | 17.39x (440 B) |
| 1 KB | 84.66 ns | 3.70x (184 B) | 1.86x (120 B) | 8.02x (352 B) | 4.11x (440 B) |
| 16 KB | 1,254 ns | 2.99x (184 B) | 1.29x (120 B) | 6.31x (352 B) | 3.18x (440 B) |
| 16 KB in 7 B pieces | 5,954 ns | wrong hash | wrong hash | wrong hash | 2.12x (440 B) |
| 16 KB in 1500 B pieces | 1,306 ns | wrong hash | wrong hash | 7.10x (952 B) | 3.03x (440 B) |
| 1 MB stream | 90,723 ns | 2.73x (144 B) | 1.22x (80 B) | 6.31x (4,472 B) | 2.72x (4,560 B) |

### 128-bit, .NET 10.0

| Input | This library | murmurhash | murmurhash (unsafe) | Data.HashFunction | HashifyNET |
|---|---:|---:|---:|---:|---:|
| 16 B | 5.08 ns | 15.93x (184 B) | 12.83x (120 B) | 20.05x (296 B) | 15.27x (384 B) |
| 1 KB | 82.96 ns | 3.54x (184 B) | 1.94x (120 B) | 6.91x (296 B) | 3.71x (384 B) |
| 16 KB | 1,274 ns | 2.70x (184 B) | 1.27x (120 B) | 5.30x (296 B) | 3.09x (384 B) |
| 16 KB in 7 B pieces | 5,890 ns | wrong hash | wrong hash | wrong hash | 1.96x (384 B) |
| 16 KB in 1500 B pieces | 1,300 ns | wrong hash | wrong hash | 6.13x (896 B) | 3.06x (384 B) |
| 1 MB stream | 90,779 ns | 2.48x (144 B) | 1.20x (80 B) | 5.52x (4,416 B) | 2.73x (4,504 B) |

```
BenchmarkDotNet v0.16.0-preview.2, Linux Fedora Linux 45.20261112.0 (Kinoite)
13th Gen Intel Core i9-13900K 3.00GHz, 1 CPU, 26 logical and 26 physical cores
Memory: 82.4 GB Total, 70.51 GB Available
.NET SDK 11.0.100
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.34 (8.0.34, 8.0.3426.52217), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
```

Run it with `dotnet run -c Release --project benchmarks/MurmurHash.Comparison -f net10.0 -- --filter "*Comparison*"`.

## System.IO.Hashing's xxHash, for reference

`XxHash32` and `XxHash128` are a different algorithm with different output, so they are not a replacement when you need
MurmurHash3 values. They are listed because they share the `NonCryptographicHashAlgorithm` API. Measured with
`MurmurHash.Benchmarks` (see [results-incremental-2026-11.md](results-incremental-2026-11.md)), .NET 10.0:

| Input | MurmurHash32 | XxHash32 | MurmurHash128 | XxHash128 |
|---|---:|---:|---:|---:|
| 16 B, one `Append` | 4.10 ns | 4.50 ns | 5.44 ns | 7.01 ns |
| 128 B, one `Append` | 22.40 ns | 13.67 ns | 12.97 ns | 13.84 ns |
| 16 KB, one `Append` | 3,266 ns | 1,512 ns | 1,344 ns | 283 ns |
| 16 KB in 7-byte pieces | 6,806 ns | 9,361 ns | 5,707 ns | 7,546 ns |

xxHash processes independent lanes in parallel (SIMD for XXH128), which MurmurHash3's serial design does not allow, so
it is faster on large inputs.
