# Hash128 benchmarks (2026-11)

`Reference` is a direct port of SMHasher's `MurmurHash3_x64_128` (indexed loop, fall-through switch for the tail).
`Hash128` is the public API, which uses the fused-LEA code path on this CPU (Raptor Lake); `Hash128_GeneralPath` is
the code path other CPUs use. `Hash128MixedLengthBenchmarks` hashes 1024 inputs of random lengths (0 to `MaxLength`)
per invocation and reports the time per input, so the tail's branches cannot be predicted.

## Implementation variants

Measured before choosing the implementation (BenchmarkDotNet short run: 4 warmup, 12 iterations), mean time relative
to the multiply step with the reference switch tail. "Overlap" is the tail that shipped.

| Input | Runtime | Multiply + Switch | Multiply + Overlap | Fused LEA + Switch | Fused LEA + Overlap |
|---|---|---:|---:|---:|---:|
| 3 B | .NET 8.0 | 3.37 ns | 1.11 | 1.00 | 1.26 |
| 3 B | .NET 10.0 | 2.62 ns | 1.04 | 1.00 | 1.04 |
| 7 B | .NET 8.0 | 4.78 ns | 0.74 | 1.01 | 0.74 |
| 7 B | .NET 10.0 | 3.15 ns | 0.86 | 1.00 | 0.86 |
| 12 B | .NET 8.0 | 5.83 ns | 0.74 | 1.00 | 0.71 |
| 12 B | .NET 10.0 | 3.81 ns | 0.76 | 1.00 | 0.70 |
| 15 B | .NET 8.0 | 6.45 ns | 0.67 | 1.00 | 0.64 |
| 15 B | .NET 10.0 | 4.10 ns | 0.65 | 1.00 | 0.65 |
| 16 B | .NET 8.0 | 3.81 ns | 1.24 | 0.97 | 0.99 |
| 16 B | .NET 10.0 | 3.47 ns | 1.01 | 0.96 | 0.97 |
| 31 B | .NET 8.0 | 7.73 ns | 0.62 | 0.99 | 0.63 |
| 31 B | .NET 10.0 | 5.04 ns | 0.74 | 0.98 | 0.71 |
| 40 B | .NET 8.0 | 7.84 ns | 0.67 | 0.98 | 0.66 |
| 40 B | .NET 10.0 | 5.34 ns | 0.91 | 0.95 | 0.85 |
| 128 B | .NET 8.0 | 13.73 ns | 1.05 | 0.86 | 0.91 |
| 128 B | .NET 10.0 | 13.68 ns | 1.00 | 0.85 | 0.86 |
| 1024 B | .NET 8.0 | 103.06 ns | 1.00 | 0.83 | 0.85 |
| 1024 B | .NET 10.0 | 103.24 ns | 0.99 | 0.82 | 0.82 |
| 16384 B | .NET 8.0 | 1646.99 ns | 1.00 | 0.82 | 0.82 |
| 16384 B | .NET 10.0 | 1645.42 ns | 1.00 | 0.81 | 0.82 |
| mixed 0-32 B | .NET 8.0 | 6.36 ns | 0.57 | 1.00 | 0.56 |
| mixed 0-32 B | .NET 10.0 | 7.54 ns | 0.48 | 0.94 | 0.47 |
| mixed 0-256 B | .NET 8.0 | 16.45 ns | 0.84 | 0.92 | 0.76 |
| mixed 0-256 B | .NET 10.0 | 16.65 ns | 0.83 | 0.93 | 0.75 |

A third tail ("BackRead", one 8-byte load reaching back into the last block) measured the same as Overlap on .NET 10
and on mixed lengths, and was dropped as the more complex of the two.

## Tail mixing

The first version mixed both tail words unconditionally, relying on `MixK1(0) == MixK2(0) == 0`. Mixing each word
only inside the branch that reads it adds no branches and removes two to four multiplies from every input whose
length is not 9-15 bytes past a multiple of 16. Full runs, public `Hash128`, new time relative to the old:

| Input | .NET 8.0 | .NET 10.0 | .NET 11.0 |
|---|---:|---:|---:|
| 3 B | 0.92 | 0.91 | 0.91 |
| 7 B | 0.94 | 0.94 | 0.94 |
| 15 B | 1.00 | 1.01 | 1.02 |
| 16 B | 0.82 | 0.82 | 0.83 |
| 40 B | 0.95 | 0.95 | 0.95 |
| 128 B | 0.90 | 0.90 | 0.90 |
| 1024 B | 0.99 | 1.03 | 0.99 |
| 16384 B | 1.00 | 1.00 | 1.00 |
| mixed 0-32 B | 0.97 | 0.97 | 0.96 |
| mixed 0-256 B | 1.00 | 0.99 | 1.02 |

15 B (both words present) does the same work as before. The .NET 10 1024 B result has a higher error (0.35 ns) than
its neighbours, and the same code is 1% faster on .NET 8 and .NET 11; the reference port was unchanged between the runs.

## Final

```

BenchmarkDotNet v0.16.0-preview.2, Linux Fedora Linux 45.20261104.0 (Kinoite)
13th Gen Intel Core i9-13900K 2.99GHz, 1 CPU, 26 logical and 26 physical cores
Memory: 82.4 GB Total, 5.27 GB Available
.NET SDK 11.0.100
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.34 (8.0.34, 8.0.3426.52217), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  .NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v3

Arguments=Default  

```
| Type                         | Method              | Job       | Runtime   | Length | MaxLength | Mean         | Error     | StdDev    | Median       | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |-------------------- |---------- |---------- |------- |---------- |-------------:|----------:|----------:|-------------:|------:|--------:|-------:|----------:|------------:|
| **Hash128Benchmarks**            | **Reference**           | **.NET 8.0**  | **.NET 8.0**  | **3**      | **?**         |     **3.508 ns** | **0.0013 ns** | **0.0011 ns** |     **3.507 ns** |  **1.00** |    **0.00** |      **-** |         **-** |          **NA** |
| Hash128Benchmarks            | Hash128             | .NET 8.0  | .NET 8.0  | 3      | ?         |     2.433 ns | 0.0004 ns | 0.0003 ns |     2.433 ns |  0.69 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 8.0  | .NET 8.0  | 3      | ?         |     2.431 ns | 0.0004 ns | 0.0003 ns |     2.431 ns |  0.69 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 8.0  | .NET 8.0  | 3      | ?         |     2.995 ns | 0.0010 ns | 0.0009 ns |     2.995 ns |  0.85 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 8.0  | .NET 8.0  | 3      | ?         |     2.378 ns | 0.0082 ns | 0.0076 ns |     2.377 ns |  0.68 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 8.0  | .NET 8.0  | 3      | ?         |    20.246 ns | 0.0310 ns | 0.0258 ns |    20.251 ns |  5.77 |    0.01 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 10.0 | .NET 10.0 | 3      | ?         |     3.229 ns | 0.0011 ns | 0.0009 ns |     3.229 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 10.0 | .NET 10.0 | 3      | ?         |     2.410 ns | 0.0006 ns | 0.0005 ns |     2.410 ns |  0.75 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 10.0 | .NET 10.0 | 3      | ?         |     2.406 ns | 0.0011 ns | 0.0009 ns |     2.406 ns |  0.75 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 10.0 | .NET 10.0 | 3      | ?         |     2.580 ns | 0.0107 ns | 0.0100 ns |     2.580 ns |  0.80 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 10.0 | .NET 10.0 | 3      | ?         |     2.339 ns | 0.0090 ns | 0.0085 ns |     2.336 ns |  0.72 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 10.0 | .NET 10.0 | 3      | ?         |    19.042 ns | 0.0809 ns | 0.0756 ns |    19.050 ns |  5.90 |    0.02 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 11.0 | .NET 11.0 | 3      | ?         |     3.249 ns | 0.0054 ns | 0.0045 ns |     3.249 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 11.0 | .NET 11.0 | 3      | ?         |     2.425 ns | 0.0055 ns | 0.0051 ns |     2.427 ns |  0.75 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 11.0 | .NET 11.0 | 3      | ?         |     2.426 ns | 0.0094 ns | 0.0088 ns |     2.427 ns |  0.75 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 11.0 | .NET 11.0 | 3      | ?         |     2.569 ns | 0.0054 ns | 0.0051 ns |     2.571 ns |  0.79 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 11.0 | .NET 11.0 | 3      | ?         |     2.330 ns | 0.0057 ns | 0.0050 ns |     2.331 ns |  0.72 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 11.0 | .NET 11.0 | 3      | ?         |    12.602 ns | 0.1042 ns | 0.0975 ns |    12.578 ns |  3.88 |    0.03 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| **Hash128Benchmarks**            | **Reference**           | **.NET 8.0**  | **.NET 8.0**  | **7**      | **?**         |     **4.234 ns** | **0.0013 ns** | **0.0012 ns** |     **4.233 ns** |  **1.00** |    **0.00** |      **-** |         **-** |          **NA** |
| Hash128Benchmarks            | Hash128             | .NET 8.0  | .NET 8.0  | 7      | ?         |     2.533 ns | 0.0112 ns | 0.0093 ns |     2.531 ns |  0.60 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 8.0  | .NET 8.0  | 7      | ?         |     2.519 ns | 0.0111 ns | 0.0099 ns |     2.519 ns |  0.59 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 8.0  | .NET 8.0  | 7      | ?         |     3.214 ns | 0.0095 ns | 0.0085 ns |     3.215 ns |  0.76 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 8.0  | .NET 8.0  | 7      | ?         |     2.411 ns | 0.0005 ns | 0.0005 ns |     2.411 ns |  0.57 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 8.0  | .NET 8.0  | 7      | ?         |    20.017 ns | 0.0915 ns | 0.0856 ns |    20.013 ns |  4.73 |    0.02 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 10.0 | .NET 10.0 | 7      | ?         |     4.009 ns | 0.0102 ns | 0.0096 ns |     4.009 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 10.0 | .NET 10.0 | 7      | ?         |     2.494 ns | 0.0006 ns | 0.0005 ns |     2.494 ns |  0.62 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 10.0 | .NET 10.0 | 7      | ?         |     2.504 ns | 0.0070 ns | 0.0066 ns |     2.502 ns |  0.62 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 10.0 | .NET 10.0 | 7      | ?         |     2.637 ns | 0.0006 ns | 0.0005 ns |     2.637 ns |  0.66 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 10.0 | .NET 10.0 | 7      | ?         |     2.380 ns | 0.0059 ns | 0.0055 ns |     2.381 ns |  0.59 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 10.0 | .NET 10.0 | 7      | ?         |    18.555 ns | 0.0467 ns | 0.0365 ns |    18.560 ns |  4.63 |    0.01 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 11.0 | .NET 11.0 | 7      | ?         |     3.999 ns | 0.0011 ns | 0.0010 ns |     3.999 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 11.0 | .NET 11.0 | 7      | ?         |     2.427 ns | 0.0007 ns | 0.0005 ns |     2.427 ns |  0.61 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 11.0 | .NET 11.0 | 7      | ?         |     2.427 ns | 0.0004 ns | 0.0003 ns |     2.427 ns |  0.61 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 11.0 | .NET 11.0 | 7      | ?         |     2.567 ns | 0.0082 ns | 0.0073 ns |     2.568 ns |  0.64 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 11.0 | .NET 11.0 | 7      | ?         |     2.309 ns | 0.0006 ns | 0.0005 ns |     2.309 ns |  0.58 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 11.0 | .NET 11.0 | 7      | ?         |    12.303 ns | 0.0167 ns | 0.0148 ns |    12.305 ns |  3.08 |    0.00 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| **Hash128Benchmarks**            | **Reference**           | **.NET 8.0**  | **.NET 8.0**  | **15**     | **?**         |     **5.355 ns** | **0.0017 ns** | **0.0014 ns** |     **5.355 ns** |  **1.00** |    **0.00** |      **-** |         **-** |          **NA** |
| Hash128Benchmarks            | Hash128             | .NET 8.0  | .NET 8.0  | 15     | ?         |     2.639 ns | 0.0006 ns | 0.0005 ns |     2.639 ns |  0.49 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 8.0  | .NET 8.0  | 15     | ?         |     2.639 ns | 0.0007 ns | 0.0005 ns |     2.638 ns |  0.49 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 8.0  | .NET 8.0  | 15     | ?         |     3.118 ns | 0.0030 ns | 0.0028 ns |     3.117 ns |  0.58 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 8.0  | .NET 8.0  | 15     | ?         |     2.554 ns | 0.0040 ns | 0.0035 ns |     2.553 ns |  0.48 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 8.0  | .NET 8.0  | 15     | ?         |    19.179 ns | 0.0304 ns | 0.0269 ns |    19.192 ns |  3.58 |    0.00 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 10.0 | .NET 10.0 | 15     | ?         |     5.260 ns | 0.0010 ns | 0.0009 ns |     5.260 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 10.0 | .NET 10.0 | 15     | ?         |     2.819 ns | 0.0557 ns | 0.0663 ns |     2.840 ns |  0.54 |    0.01 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 10.0 | .NET 10.0 | 15     | ?         |     2.620 ns | 0.0011 ns | 0.0010 ns |     2.620 ns |  0.50 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 10.0 | .NET 10.0 | 15     | ?         |     2.792 ns | 0.0013 ns | 0.0012 ns |     2.792 ns |  0.53 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 10.0 | .NET 10.0 | 15     | ?         |     2.538 ns | 0.0006 ns | 0.0006 ns |     2.538 ns |  0.48 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 10.0 | .NET 10.0 | 15     | ?         |    19.257 ns | 0.0349 ns | 0.0309 ns |    19.257 ns |  3.66 |    0.01 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 11.0 | .NET 11.0 | 15     | ?         |     5.247 ns | 0.0078 ns | 0.0065 ns |     5.248 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 11.0 | .NET 11.0 | 15     | ?         |     2.756 ns | 0.0005 ns | 0.0005 ns |     2.756 ns |  0.53 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 11.0 | .NET 11.0 | 15     | ?         |     2.558 ns | 0.0051 ns | 0.0045 ns |     2.558 ns |  0.49 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 11.0 | .NET 11.0 | 15     | ?         |     2.702 ns | 0.0072 ns | 0.0064 ns |     2.699 ns |  0.51 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 11.0 | .NET 11.0 | 15     | ?         |     2.451 ns | 0.0005 ns | 0.0004 ns |     2.451 ns |  0.47 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 11.0 | .NET 11.0 | 15     | ?         |    12.306 ns | 0.0240 ns | 0.0187 ns |    12.307 ns |  2.35 |    0.00 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| **Hash128Benchmarks**            | **Reference**           | **.NET 8.0**  | **.NET 8.0**  | **16**     | **?**         |     **4.095 ns** | **0.0011 ns** | **0.0010 ns** |     **4.095 ns** |  **1.00** |    **0.00** |      **-** |         **-** |          **NA** |
| Hash128Benchmarks            | Hash128             | .NET 8.0  | .NET 8.0  | 16     | ?         |     2.739 ns | 0.0003 ns | 0.0003 ns |     2.740 ns |  0.67 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 8.0  | .NET 8.0  | 16     | ?         |     2.920 ns | 0.0007 ns | 0.0006 ns |     2.920 ns |  0.71 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 8.0  | .NET 8.0  | 16     | ?         |     3.156 ns | 0.0064 ns | 0.0057 ns |     3.154 ns |  0.77 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 8.0  | .NET 8.0  | 16     | ?         |     2.662 ns | 0.0010 ns | 0.0008 ns |     2.662 ns |  0.65 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 8.0  | .NET 8.0  | 16     | ?         |    16.775 ns | 0.0203 ns | 0.0158 ns |    16.774 ns |  4.10 |    0.00 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 10.0 | .NET 10.0 | 16     | ?         |     3.716 ns | 0.0047 ns | 0.0042 ns |     3.714 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 10.0 | .NET 10.0 | 16     | ?         |     2.735 ns | 0.0104 ns | 0.0097 ns |     2.741 ns |  0.74 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 10.0 | .NET 10.0 | 16     | ?         |     2.891 ns | 0.0004 ns | 0.0004 ns |     2.891 ns |  0.78 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 10.0 | .NET 10.0 | 16     | ?         |     2.846 ns | 0.0006 ns | 0.0005 ns |     2.846 ns |  0.77 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 10.0 | .NET 10.0 | 16     | ?         |     2.636 ns | 0.0016 ns | 0.0014 ns |     2.636 ns |  0.71 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 10.0 | .NET 10.0 | 16     | ?         |     7.256 ns | 0.0216 ns | 0.0202 ns |     7.254 ns |  1.95 |    0.01 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 11.0 | .NET 11.0 | 16     | ?         |     3.719 ns | 0.0012 ns | 0.0011 ns |     3.719 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 11.0 | .NET 11.0 | 16     | ?         |     2.692 ns | 0.0033 ns | 0.0029 ns |     2.691 ns |  0.72 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 11.0 | .NET 11.0 | 16     | ?         |     2.842 ns | 0.0019 ns | 0.0017 ns |     2.841 ns |  0.76 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 11.0 | .NET 11.0 | 16     | ?         |     2.808 ns | 0.0010 ns | 0.0009 ns |     2.809 ns |  0.76 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 11.0 | .NET 11.0 | 16     | ?         |     2.591 ns | 0.0006 ns | 0.0005 ns |     2.591 ns |  0.70 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 11.0 | .NET 11.0 | 16     | ?         |     6.506 ns | 0.0179 ns | 0.0168 ns |     6.510 ns |  1.75 |    0.00 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| **Hash128MixedLengthBenchmarks** | **Reference**           | **.NET 8.0**  | **.NET 8.0**  | **?**      | **32**        |     **8.770 ns** | **0.0229 ns** | **0.0203 ns** |     **8.768 ns** |  **1.00** |    **0.00** |     **NA** |        **NA** |          **NA** |
| Hash128MixedLengthBenchmarks | Hash128             | .NET 8.0  | .NET 8.0  | ?      | 32        |     3.777 ns | 0.0034 ns | 0.0028 ns |     3.775 ns |  0.43 |    0.00 |     NA |        NA |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| Hash128MixedLengthBenchmarks | Reference           | .NET 10.0 | .NET 10.0 | ?      | 32        |     7.522 ns | 0.0481 ns | 0.0402 ns |     7.513 ns |  1.00 |    0.00 |     NA |        NA |          NA |
| Hash128MixedLengthBenchmarks | Hash128             | .NET 10.0 | .NET 10.0 | ?      | 32        |     3.802 ns | 0.0102 ns | 0.0090 ns |     3.801 ns |  0.51 |    0.00 |     NA |        NA |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| Hash128MixedLengthBenchmarks | Reference           | .NET 11.0 | .NET 11.0 | ?      | 32        |     7.286 ns | 0.0264 ns | 0.0247 ns |     7.285 ns |  1.00 |    0.00 |     NA |        NA |          NA |
| Hash128MixedLengthBenchmarks | Hash128             | .NET 11.0 | .NET 11.0 | ?      | 32        |     3.647 ns | 0.0023 ns | 0.0021 ns |     3.647 ns |  0.50 |    0.00 |     NA |        NA |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| **Hash128Benchmarks**            | **Reference**           | **.NET 8.0**  | **.NET 8.0**  | **40**     | **?**         |     **6.849 ns** | **0.0014 ns** | **0.0012 ns** |     **6.849 ns** |  **1.00** |    **0.00** |      **-** |         **-** |          **NA** |
| Hash128Benchmarks            | Hash128             | .NET 8.0  | .NET 8.0  | 40     | ?         |     4.272 ns | 0.0035 ns | 0.0033 ns |     4.271 ns |  0.62 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 8.0  | .NET 8.0  | 40     | ?         |     4.512 ns | 0.0018 ns | 0.0016 ns |     4.513 ns |  0.66 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 8.0  | .NET 8.0  | 40     | ?         |     4.567 ns | 0.0023 ns | 0.0022 ns |     4.568 ns |  0.67 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 8.0  | .NET 8.0  | 40     | ?         |     4.227 ns | 0.0008 ns | 0.0007 ns |     4.227 ns |  0.62 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 8.0  | .NET 8.0  | 40     | ?         |    18.488 ns | 0.0311 ns | 0.0260 ns |    18.497 ns |  2.70 |    0.00 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 10.0 | .NET 10.0 | 40     | ?         |     6.740 ns | 0.0020 ns | 0.0017 ns |     6.740 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 10.0 | .NET 10.0 | 40     | ?         |     4.257 ns | 0.0103 ns | 0.0097 ns |     4.260 ns |  0.63 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 10.0 | .NET 10.0 | 40     | ?         |     4.525 ns | 0.0007 ns | 0.0006 ns |     4.525 ns |  0.67 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 10.0 | .NET 10.0 | 40     | ?         |     4.413 ns | 0.0064 ns | 0.0054 ns |     4.410 ns |  0.65 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 10.0 | .NET 10.0 | 40     | ?         |     4.137 ns | 0.0004 ns | 0.0003 ns |     4.137 ns |  0.61 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 10.0 | .NET 10.0 | 40     | ?         |    13.853 ns | 0.0131 ns | 0.0109 ns |    13.851 ns |  2.06 |    0.00 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 11.0 | .NET 11.0 | 40     | ?         |     6.732 ns | 0.0014 ns | 0.0012 ns |     6.732 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 11.0 | .NET 11.0 | 40     | ?         |     4.169 ns | 0.0039 ns | 0.0030 ns |     4.169 ns |  0.62 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 11.0 | .NET 11.0 | 40     | ?         |     4.412 ns | 0.0053 ns | 0.0050 ns |     4.413 ns |  0.66 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 11.0 | .NET 11.0 | 40     | ?         |     4.287 ns | 0.0149 ns | 0.0132 ns |     4.285 ns |  0.64 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 11.0 | .NET 11.0 | 40     | ?         |     4.063 ns | 0.0085 ns | 0.0080 ns |     4.065 ns |  0.60 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 11.0 | .NET 11.0 | 40     | ?         |     9.290 ns | 0.0294 ns | 0.0260 ns |     9.285 ns |  1.38 |    0.00 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| **Hash128Benchmarks**            | **Reference**           | **.NET 8.0**  | **.NET 8.0**  | **128**    | **?**         |    **15.130 ns** | **0.0322 ns** | **0.0286 ns** |    **15.138 ns** |  **1.00** |    **0.00** |      **-** |         **-** |          **NA** |
| Hash128Benchmarks            | Hash128             | .NET 8.0  | .NET 8.0  | 128    | ?         |    10.470 ns | 0.0013 ns | 0.0012 ns |    10.470 ns |  0.69 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 8.0  | .NET 8.0  | 128    | ?         |    12.584 ns | 0.0292 ns | 0.0258 ns |    12.575 ns |  0.83 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 8.0  | .NET 8.0  | 128    | ?         |    11.016 ns | 0.0182 ns | 0.0152 ns |    11.010 ns |  0.73 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 8.0  | .NET 8.0  | 128    | ?         |    10.321 ns | 0.0167 ns | 0.0148 ns |    10.314 ns |  0.68 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 8.0  | .NET 8.0  | 128    | ?         |    29.374 ns | 0.0652 ns | 0.0578 ns |    29.351 ns |  1.94 |    0.01 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 10.0 | .NET 10.0 | 128    | ?         |    14.932 ns | 0.0084 ns | 0.0075 ns |    14.933 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 10.0 | .NET 10.0 | 128    | ?         |    10.444 ns | 0.0304 ns | 0.0270 ns |    10.441 ns |  0.70 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 10.0 | .NET 10.0 | 128    | ?         |    12.528 ns | 0.0301 ns | 0.0266 ns |    12.520 ns |  0.84 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 10.0 | .NET 10.0 | 128    | ?         |    10.824 ns | 0.0281 ns | 0.0249 ns |    10.807 ns |  0.72 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 10.0 | .NET 10.0 | 128    | ?         |    10.274 ns | 0.0204 ns | 0.0191 ns |    10.273 ns |  0.69 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 10.0 | .NET 10.0 | 128    | ?         |    17.634 ns | 0.0394 ns | 0.0308 ns |    17.638 ns |  1.18 |    0.00 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 11.0 | .NET 11.0 | 128    | ?         |    14.935 ns | 0.0027 ns | 0.0024 ns |    14.935 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 11.0 | .NET 11.0 | 128    | ?         |    10.329 ns | 0.0089 ns | 0.0074 ns |    10.326 ns |  0.69 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 11.0 | .NET 11.0 | 128    | ?         |    12.428 ns | 0.0038 ns | 0.0032 ns |    12.428 ns |  0.83 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 11.0 | .NET 11.0 | 128    | ?         |    10.719 ns | 0.0168 ns | 0.0157 ns |    10.725 ns |  0.72 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 11.0 | .NET 11.0 | 128    | ?         |    10.195 ns | 0.0090 ns | 0.0075 ns |    10.193 ns |  0.68 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 11.0 | .NET 11.0 | 128    | ?         |    16.903 ns | 0.0283 ns | 0.0265 ns |    16.899 ns |  1.13 |    0.00 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| **Hash128MixedLengthBenchmarks** | **Reference**           | **.NET 8.0**  | **.NET 8.0**  | **?**      | **256**       |    **18.908 ns** | **0.0431 ns** | **0.0336 ns** |    **18.896 ns** |  **1.00** |    **0.00** |     **NA** |        **NA** |          **NA** |
| Hash128MixedLengthBenchmarks | Hash128             | .NET 8.0  | .NET 8.0  | ?      | 256       |    12.909 ns | 0.1694 ns | 0.1415 ns |    12.853 ns |  0.68 |    0.01 |     NA |        NA |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| Hash128MixedLengthBenchmarks | Reference           | .NET 10.0 | .NET 10.0 | ?      | 256       |    18.606 ns | 0.0851 ns | 0.0710 ns |    18.581 ns |  1.00 |    0.00 |     NA |        NA |          NA |
| Hash128MixedLengthBenchmarks | Hash128             | .NET 10.0 | .NET 10.0 | ?      | 256       |    12.857 ns | 0.0650 ns | 0.0608 ns |    12.852 ns |  0.69 |    0.00 |     NA |        NA |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| Hash128MixedLengthBenchmarks | Reference           | .NET 11.0 | .NET 11.0 | ?      | 256       |    18.877 ns | 0.0151 ns | 0.0134 ns |    18.876 ns |  1.00 |    0.00 |     NA |        NA |          NA |
| Hash128MixedLengthBenchmarks | Hash128             | .NET 11.0 | .NET 11.0 | ?      | 256       |    13.049 ns | 0.2595 ns | 0.4476 ns |    12.796 ns |  0.69 |    0.02 |     NA |        NA |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| **Hash128Benchmarks**            | **Reference**           | **.NET 8.0**  | **.NET 8.0**  | **1024**   | **?**         |   **108.662 ns** | **0.0955 ns** | **0.0798 ns** |   **108.650 ns** |  **1.00** |    **0.00** |      **-** |         **-** |          **NA** |
| Hash128Benchmarks            | Hash128             | .NET 8.0  | .NET 8.0  | 1024   | ?         |    84.265 ns | 0.0407 ns | 0.0361 ns |    84.272 ns |  0.78 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 8.0  | .NET 8.0  | 1024   | ?         |   102.163 ns | 0.0393 ns | 0.0367 ns |   102.176 ns |  0.94 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 8.0  | .NET 8.0  | 1024   | ?         |    84.121 ns | 0.0460 ns | 0.0430 ns |    84.106 ns |  0.77 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 8.0  | .NET 8.0  | 1024   | ?         |    83.964 ns | 0.0612 ns | 0.0511 ns |    83.961 ns |  0.77 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 8.0  | .NET 8.0  | 1024   | ?         |   119.857 ns | 0.2106 ns | 0.1970 ns |   119.899 ns |  1.10 |    0.00 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 10.0 | .NET 10.0 | 1024   | ?         |   105.983 ns | 0.1415 ns | 0.1254 ns |   105.912 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 10.0 | .NET 10.0 | 1024   | ?         |    86.325 ns | 0.3463 ns | 0.3070 ns |    86.212 ns |  0.81 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 10.0 | .NET 10.0 | 1024   | ?         |   102.917 ns | 0.0494 ns | 0.0462 ns |   102.924 ns |  0.97 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 10.0 | .NET 10.0 | 1024   | ?         |    84.444 ns | 0.1648 ns | 0.1461 ns |    84.379 ns |  0.80 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 10.0 | .NET 10.0 | 1024   | ?         |    85.222 ns | 0.1503 ns | 0.1406 ns |    85.271 ns |  0.80 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 10.0 | .NET 10.0 | 1024   | ?         |   110.342 ns | 0.0727 ns | 0.0644 ns |   110.338 ns |  1.04 |    0.00 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 11.0 | .NET 11.0 | 1024   | ?         |   108.394 ns | 0.0970 ns | 0.0907 ns |   108.386 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 11.0 | .NET 11.0 | 1024   | ?         |    83.376 ns | 0.0214 ns | 0.0200 ns |    83.377 ns |  0.77 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 11.0 | .NET 11.0 | 1024   | ?         |   102.622 ns | 0.0940 ns | 0.0785 ns |   102.646 ns |  0.95 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 11.0 | .NET 11.0 | 1024   | ?         |    84.268 ns | 0.0969 ns | 0.0906 ns |    84.257 ns |  0.78 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 11.0 | .NET 11.0 | 1024   | ?         |    84.891 ns | 0.0584 ns | 0.0547 ns |    84.911 ns |  0.78 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 11.0 | .NET 11.0 | 1024   | ?         |   109.745 ns | 0.0845 ns | 0.0791 ns |   109.727 ns |  1.01 |    0.00 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| **Hash128Benchmarks**            | **Reference**           | **.NET 8.0**  | **.NET 8.0**  | **16384**  | **?**         | **1,648.797 ns** | **0.2380 ns** | **0.2226 ns** | **1,648.792 ns** |  **1.00** |    **0.00** |      **-** |         **-** |          **NA** |
| Hash128Benchmarks            | Hash128             | .NET 8.0  | .NET 8.0  | 16384  | ?         | 1,341.119 ns | 0.5697 ns | 0.5050 ns | 1,341.278 ns |  0.81 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 8.0  | .NET 8.0  | 16384  | ?         | 1,645.079 ns | 0.3042 ns | 0.2697 ns | 1,645.057 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 8.0  | .NET 8.0  | 16384  | ?         | 1,341.063 ns | 0.7685 ns | 0.7189 ns | 1,340.735 ns |  0.81 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 8.0  | .NET 8.0  | 16384  | ?         | 1,344.217 ns | 0.2720 ns | 0.2412 ns | 1,344.162 ns |  0.82 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 8.0  | .NET 8.0  | 16384  | ?         | 1,663.219 ns | 0.6902 ns | 0.6119 ns | 1,663.239 ns |  1.01 |    0.00 | 0.0038 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 10.0 | .NET 10.0 | 16384  | ?         | 1,649.568 ns | 1.0634 ns | 0.9947 ns | 1,649.022 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 10.0 | .NET 10.0 | 16384  | ?         | 1,339.828 ns | 0.4011 ns | 0.3132 ns | 1,339.828 ns |  0.81 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 10.0 | .NET 10.0 | 16384  | ?         | 1,645.051 ns | 0.2663 ns | 0.2361 ns | 1,645.019 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 10.0 | .NET 10.0 | 16384  | ?         | 1,343.185 ns | 0.3639 ns | 0.3404 ns | 1,343.092 ns |  0.81 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 10.0 | .NET 10.0 | 16384  | ?         | 1,345.551 ns | 1.1969 ns | 1.0611 ns | 1,345.415 ns |  0.82 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 10.0 | .NET 10.0 | 16384  | ?         | 1,657.342 ns | 0.6965 ns | 0.6515 ns | 1,657.127 ns |  1.00 |    0.00 | 0.0038 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |              |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 11.0 | .NET 11.0 | 16384  | ?         | 1,648.434 ns | 0.6400 ns | 0.5987 ns | 1,648.481 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 11.0 | .NET 11.0 | 16384  | ?         | 1,342.419 ns | 0.2408 ns | 0.2135 ns | 1,342.386 ns |  0.81 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 11.0 | .NET 11.0 | 16384  | ?         | 1,642.237 ns | 0.4307 ns | 0.3363 ns | 1,642.306 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 11.0 | .NET 11.0 | 16384  | ?         | 1,344.535 ns | 0.4096 ns | 0.3198 ns | 1,344.491 ns |  0.82 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 11.0 | .NET 11.0 | 16384  | ?         | 1,342.059 ns | 0.1692 ns | 0.1500 ns | 1,342.012 ns |  0.81 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 11.0 | .NET 11.0 | 16384  | ?         | 1,663.389 ns | 0.3652 ns | 0.2851 ns | 1,663.450 ns |  1.01 |    0.00 | 0.0038 |      40 B |          NA |
