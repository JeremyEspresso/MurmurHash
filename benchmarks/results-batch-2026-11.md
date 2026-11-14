# Hash32Batch benchmarks (2026-11)

`MurmurHash3.Hash32Batch` against a loop of `MurmurHash3.Hash32` calls, 1024 keys per invocation, time per key
(`Hash32BatchBenchmarks`, `Hash32BatchIntegerBenchmarks`). "Fallback" is the path without AVX2 (other CPUs, ARM, the
.NET Standard builds, and key lengths without a SIMD kernel), measured directly on the same machine. Each method's
output is checked against the loop in the benchmark setup. The machine was idle during the run (1-minute load average
median 1.00, maximum 1.12, i.e. the benchmark alone).

## How it works

With AVX2, 8 keys are hashed at once, one per 32-bit vector lane. The keys are read with plain contiguous loads and
transposed in registers, so that each vector holds the same 4-byte block of 8 keys; the MurmurHash3 rounds then run
lane-wise. Key lengths of 4, 8, 16 and multiples of 16 bytes have kernels. Everything else interleaves the scalar hash
of 4 keys, which lets the core overlap their dependency chains.

The design comes from a spike that compared AVX2 gathers (1.3-1.7x slower than load + transpose), two vectors per
iteration (up to 10% faster at 32-64 B, not worth the code), 128-bit vectors (1.7-2.3x over the loop, too little over
the interleaved scalar path) and variable-length keys (1.26x, no API).

## Results

| Keys | Method | .NET 8.0 | .NET 10.0 | vs loop (8.0 / 10.0) |
|---|---|---:|---:|---:|
| 4 B | Loop | 1.338 ns | 1.391 ns |  |
| 4 B | Batch | 0.272 ns | 0.274 ns | 4.92x / 5.08x |
| 4 B | BatchFallback | 0.987 ns | 0.997 ns | 1.36x / 1.40x |
| 8 B | Loop | 1.720 ns | 1.730 ns |  |
| 8 B | Batch | 0.467 ns | 0.469 ns | 3.69x / 3.69x |
| 8 B | BatchFallback | 1.343 ns | 1.335 ns | 1.28x / 1.30x |
| 12 B | Loop | 2.135 ns | 2.144 ns |  |
| 12 B | Batch | 1.691 ns | 1.705 ns | 1.26x / 1.26x |
| 12 B | BatchFallback | 1.699 ns | 1.693 ns | 1.26x / 1.27x |
| 16 B | Loop | 2.588 ns | 2.652 ns |  |
| 16 B | Batch | 0.731 ns | 0.764 ns | 3.54x / 3.47x |
| 16 B | BatchFallback | 2.060 ns | 2.057 ns | 1.26x / 1.29x |
| 32 B | Loop | 4.377 ns | 4.445 ns |  |
| 32 B | Batch | 1.477 ns | 1.493 ns | 2.96x / 2.98x |
| 32 B | BatchFallback | 3.450 ns | 3.438 ns | 1.27x / 1.29x |
| 64 B | Loop | 8.669 ns | 8.666 ns |  |
| 64 B | Batch | 2.825 ns | 2.848 ns | 3.07x / 3.04x |
| 64 B | BatchFallback | 6.229 ns | 6.240 ns | 1.39x / 1.39x |

```
BenchmarkDotNet v0.16.0-preview.2, Linux Fedora Linux 45.20261112.0 (Kinoite)
13th Gen Intel Core i9-13900K 3.00GHz, 1 CPU, 26 logical and 26 physical cores
Memory: 82.4 GB Total, 68.86 GB Available
.NET SDK 11.0.100
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.34 (8.0.34, 8.0.3426.52217), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
```

### `uint` and `ulong` keys

A second run (same machine, idle: load median 1.00, maximum 1.13) with two scalar baselines: a loop of `Hash32` on
each value's bytes, and a loop hand-written for the constant 4- or 8-byte length (no length checks, no loop, no tail;
`h * 5 + c` written plainly), the fastest scalar code a caller could write without this library's CPU-specific step.

| Keys | Method | .NET 8.0 | .NET 10.0 | vs `Hash32` loop (8.0 / 10.0) | vs hand-written (8.0 / 10.0) |
|---|---|---:|---:|---:|---:|
| `uint` | LoopUInt32 | 1.238 ns | 1.129 ns |  |  |
| `uint` | HandWrittenUInt32 | 0.929 ns | 0.836 ns | 1.33x / 1.35x |  |
| `uint` | BatchUInt32 | 0.276 ns | 0.273 ns | 4.49x / 4.14x | 3.37x / 3.06x |
| `uint` | BatchFallbackUInt32 | 0.899 ns | 0.788 ns | 1.38x / 1.43x | 1.03x / 1.06x |
| `ulong` | LoopUInt64 | 1.585 ns | 1.451 ns |  |  |
| `ulong` | HandWrittenUInt64 | 1.322 ns | 1.273 ns | 1.20x / 1.14x |  |
| `ulong` | BatchUInt64 | 0.434 ns | 0.438 ns | 3.65x / 3.32x | 3.05x / 2.91x |
| `ulong` | BatchFallbackUInt64 | 1.247 ns | 1.140 ns | 1.27x / 1.27x | 1.06x / 1.12x |

The fallback is the hand-written loop with this library's CPU-specific step, hence slightly faster than it.
`BatchUInt64` measured 0.471 / 0.473 ns in the first run and 0.434 / 0.438 ns in this one with the same AVX2 code:
expect run-to-run differences of that size.

Not measured: CPUs with AVX-512 (16 lanes and a native rotate would help), AMD CPUs, and ARM64 (no SIMD kernel yet;
it uses the fallback).
