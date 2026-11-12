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

## Final

```

BenchmarkDotNet v0.16.0-preview.2, Linux Fedora Linux 45.20261104.0 (Kinoite)
13th Gen Intel Core i9-13900K 2.99GHz, 1 CPU, 26 logical and 26 physical cores
Memory: 82.4 GB Total, 10.1 GB Available
.NET SDK 11.0.100
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.34 (8.0.34, 8.0.3426.52217), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  .NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v3

Arguments=Default  

```
| Type                         | Method              | Job       | Runtime   | Length | MaxLength | Mean         | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |-------------------- |---------- |---------- |------- |---------- |-------------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| **Hash128Benchmarks**            | **Reference**           | **.NET 8.0**  | **.NET 8.0**  | **3**      | **?**         |     **3.508 ns** | **0.0010 ns** | **0.0010 ns** |  **1.00** |    **0.00** |      **-** |         **-** |          **NA** |
| Hash128Benchmarks            | Hash128             | .NET 8.0  | .NET 8.0  | 3      | ?         |     2.651 ns | 0.0062 ns | 0.0052 ns |  0.76 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 8.0  | .NET 8.0  | 3      | ?         |     2.649 ns | 0.0005 ns | 0.0004 ns |  0.76 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 8.0  | .NET 8.0  | 3      | ?         |     3.336 ns | 0.0086 ns | 0.0076 ns |  0.95 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 8.0  | .NET 8.0  | 3      | ?         |     2.576 ns | 0.0070 ns | 0.0062 ns |  0.73 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 8.0  | .NET 8.0  | 3      | ?         |    20.152 ns | 0.1704 ns | 0.1331 ns |  5.74 |    0.04 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 10.0 | .NET 10.0 | 3      | ?         |     3.229 ns | 0.0022 ns | 0.0018 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 10.0 | .NET 10.0 | 3      | ?         |     2.648 ns | 0.0009 ns | 0.0008 ns |  0.82 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 10.0 | .NET 10.0 | 3      | ?         |     2.648 ns | 0.0006 ns | 0.0005 ns |  0.82 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 10.0 | .NET 10.0 | 3      | ?         |     2.805 ns | 0.0064 ns | 0.0054 ns |  0.87 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 10.0 | .NET 10.0 | 3      | ?         |     2.555 ns | 0.0007 ns | 0.0005 ns |  0.79 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 10.0 | .NET 10.0 | 3      | ?         |    18.777 ns | 0.0434 ns | 0.0362 ns |  5.81 |    0.01 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 11.0 | .NET 11.0 | 3      | ?         |     3.230 ns | 0.0006 ns | 0.0005 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 11.0 | .NET 11.0 | 3      | ?         |     2.662 ns | 0.0034 ns | 0.0027 ns |  0.82 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 11.0 | .NET 11.0 | 3      | ?         |     2.665 ns | 0.0058 ns | 0.0051 ns |  0.83 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 11.0 | .NET 11.0 | 3      | ?         |     2.800 ns | 0.0033 ns | 0.0026 ns |  0.87 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 11.0 | .NET 11.0 | 3      | ?         |     2.557 ns | 0.0009 ns | 0.0008 ns |  0.79 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 11.0 | .NET 11.0 | 3      | ?         |    12.473 ns | 0.0431 ns | 0.0382 ns |  3.86 |    0.01 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| **Hash128Benchmarks**            | **Reference**           | **.NET 8.0**  | **.NET 8.0**  | **7**      | **?**         |     **4.185 ns** | **0.0050 ns** | **0.0047 ns** |  **1.00** |    **0.00** |      **-** |         **-** |          **NA** |
| Hash128Benchmarks            | Hash128             | .NET 8.0  | .NET 8.0  | 7      | ?         |     2.693 ns | 0.0103 ns | 0.0091 ns |  0.64 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 8.0  | .NET 8.0  | 7      | ?         |     2.691 ns | 0.0108 ns | 0.0101 ns |  0.64 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 8.0  | .NET 8.0  | 7      | ?         |     3.261 ns | 0.0018 ns | 0.0015 ns |  0.78 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 8.0  | .NET 8.0  | 7      | ?         |     2.620 ns | 0.0018 ns | 0.0017 ns |  0.63 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 8.0  | .NET 8.0  | 7      | ?         |    20.086 ns | 0.1257 ns | 0.1114 ns |  4.80 |    0.03 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 10.0 | .NET 10.0 | 7      | ?         |     4.015 ns | 0.0162 ns | 0.0151 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 10.0 | .NET 10.0 | 7      | ?         |     2.664 ns | 0.0024 ns | 0.0021 ns |  0.66 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 10.0 | .NET 10.0 | 7      | ?         |     2.662 ns | 0.0008 ns | 0.0008 ns |  0.66 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 10.0 | .NET 10.0 | 7      | ?         |     2.811 ns | 0.0010 ns | 0.0009 ns |  0.70 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 10.0 | .NET 10.0 | 7      | ?         |     2.551 ns | 0.0008 ns | 0.0007 ns |  0.64 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 10.0 | .NET 10.0 | 7      | ?         |    18.962 ns | 0.1008 ns | 0.0943 ns |  4.72 |    0.03 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 11.0 | .NET 11.0 | 7      | ?         |     4.002 ns | 0.0098 ns | 0.0077 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 11.0 | .NET 11.0 | 7      | ?         |     2.592 ns | 0.0074 ns | 0.0069 ns |  0.65 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 11.0 | .NET 11.0 | 7      | ?         |     2.587 ns | 0.0016 ns | 0.0013 ns |  0.65 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 11.0 | .NET 11.0 | 7      | ?         |     2.738 ns | 0.0016 ns | 0.0013 ns |  0.68 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 11.0 | .NET 11.0 | 7      | ?         |     2.510 ns | 0.0069 ns | 0.0065 ns |  0.63 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 11.0 | .NET 11.0 | 7      | ?         |    12.361 ns | 0.0666 ns | 0.0590 ns |  3.09 |    0.02 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| **Hash128Benchmarks**            | **Reference**           | **.NET 8.0**  | **.NET 8.0**  | **15**     | **?**         |     **5.357 ns** | **0.0037 ns** | **0.0029 ns** |  **1.00** |    **0.00** |      **-** |         **-** |          **NA** |
| Hash128Benchmarks            | Hash128             | .NET 8.0  | .NET 8.0  | 15     | ?         |     2.639 ns | 0.0181 ns | 0.0161 ns |  0.49 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 8.0  | .NET 8.0  | 15     | ?         |     2.628 ns | 0.0076 ns | 0.0071 ns |  0.49 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 8.0  | .NET 8.0  | 15     | ?         |     3.255 ns | 0.0045 ns | 0.0037 ns |  0.61 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 8.0  | .NET 8.0  | 15     | ?         |     2.863 ns | 0.0024 ns | 0.0022 ns |  0.53 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 8.0  | .NET 8.0  | 15     | ?         |    19.773 ns | 0.3647 ns | 0.3411 ns |  3.69 |    0.06 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 10.0 | .NET 10.0 | 15     | ?         |     5.266 ns | 0.0144 ns | 0.0134 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 10.0 | .NET 10.0 | 15     | ?         |     2.786 ns | 0.0537 ns | 0.0716 ns |  0.53 |    0.01 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 10.0 | .NET 10.0 | 15     | ?         |     2.599 ns | 0.0009 ns | 0.0008 ns |  0.49 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 10.0 | .NET 10.0 | 15     | ?         |     2.734 ns | 0.0053 ns | 0.0049 ns |  0.52 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 10.0 | .NET 10.0 | 15     | ?         |     2.499 ns | 0.0062 ns | 0.0058 ns |  0.47 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 10.0 | .NET 10.0 | 15     | ?         |    18.699 ns | 0.2355 ns | 0.2087 ns |  3.55 |    0.04 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 11.0 | .NET 11.0 | 15     | ?         |     5.280 ns | 0.0140 ns | 0.0131 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 11.0 | .NET 11.0 | 15     | ?         |     2.714 ns | 0.0074 ns | 0.0070 ns |  0.51 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 11.0 | .NET 11.0 | 15     | ?         |     2.731 ns | 0.0068 ns | 0.0053 ns |  0.52 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 11.0 | .NET 11.0 | 15     | ?         |     2.650 ns | 0.0073 ns | 0.0061 ns |  0.50 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 11.0 | .NET 11.0 | 15     | ?         |     2.425 ns | 0.0008 ns | 0.0007 ns |  0.46 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 11.0 | .NET 11.0 | 15     | ?         |    12.386 ns | 0.0643 ns | 0.0602 ns |  2.35 |    0.01 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| **Hash128Benchmarks**            | **Reference**           | **.NET 8.0**  | **.NET 8.0**  | **16**     | **?**         |     **4.102 ns** | **0.0051 ns** | **0.0048 ns** |  **1.00** |    **0.00** |      **-** |         **-** |          **NA** |
| Hash128Benchmarks            | Hash128             | .NET 8.0  | .NET 8.0  | 16     | ?         |     3.322 ns | 0.0106 ns | 0.0099 ns |  0.81 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 8.0  | .NET 8.0  | 16     | ?         |     3.473 ns | 0.0110 ns | 0.0098 ns |  0.85 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 8.0  | .NET 8.0  | 16     | ?         |     3.814 ns | 0.0111 ns | 0.0098 ns |  0.93 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 8.0  | .NET 8.0  | 16     | ?         |     3.226 ns | 0.0089 ns | 0.0083 ns |  0.79 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 8.0  | .NET 8.0  | 16     | ?         |    16.727 ns | 0.0696 ns | 0.0617 ns |  4.08 |    0.02 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 10.0 | .NET 10.0 | 16     | ?         |     3.732 ns | 0.0190 ns | 0.0168 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 10.0 | .NET 10.0 | 16     | ?         |     3.323 ns | 0.0081 ns | 0.0076 ns |  0.89 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 10.0 | .NET 10.0 | 16     | ?         |     3.452 ns | 0.0090 ns | 0.0084 ns |  0.92 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 10.0 | .NET 10.0 | 16     | ?         |     3.436 ns | 0.0135 ns | 0.0126 ns |  0.92 |    0.01 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 10.0 | .NET 10.0 | 16     | ?         |     3.212 ns | 0.0021 ns | 0.0020 ns |  0.86 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 10.0 | .NET 10.0 | 16     | ?         |     7.203 ns | 0.0392 ns | 0.0367 ns |  1.93 |    0.01 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 11.0 | .NET 11.0 | 16     | ?         |     3.721 ns | 0.0044 ns | 0.0039 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 11.0 | .NET 11.0 | 16     | ?         |     3.253 ns | 0.0037 ns | 0.0032 ns |  0.87 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 11.0 | .NET 11.0 | 16     | ?         |     3.409 ns | 0.0080 ns | 0.0071 ns |  0.92 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 11.0 | .NET 11.0 | 16     | ?         |     3.389 ns | 0.0098 ns | 0.0087 ns |  0.91 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 11.0 | .NET 11.0 | 16     | ?         |     3.188 ns | 0.0046 ns | 0.0043 ns |  0.86 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 11.0 | .NET 11.0 | 16     | ?         |     6.635 ns | 0.0583 ns | 0.0545 ns |  1.78 |    0.01 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| **Hash128MixedLengthBenchmarks** | **Reference**           | **.NET 8.0**  | **.NET 8.0**  | **?**      | **32**        |     **8.740 ns** | **0.0542 ns** | **0.0507 ns** |  **1.00** |    **0.00** |     **NA** |        **NA** |          **NA** |
| Hash128MixedLengthBenchmarks | Hash128             | .NET 8.0  | .NET 8.0  | ?      | 32        |     3.891 ns | 0.0016 ns | 0.0015 ns |  0.45 |    0.00 |     NA |        NA |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| Hash128MixedLengthBenchmarks | Reference           | .NET 10.0 | .NET 10.0 | ?      | 32        |     7.336 ns | 0.0393 ns | 0.0386 ns |  1.00 |    0.00 |     NA |        NA |          NA |
| Hash128MixedLengthBenchmarks | Hash128             | .NET 10.0 | .NET 10.0 | ?      | 32        |     3.938 ns | 0.0080 ns | 0.0071 ns |  0.54 |    0.00 |     NA |        NA |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| Hash128MixedLengthBenchmarks | Reference           | .NET 11.0 | .NET 11.0 | ?      | 32        |     7.631 ns | 0.0732 ns | 0.0685 ns |  1.00 |    0.00 |     NA |        NA |          NA |
| Hash128MixedLengthBenchmarks | Hash128             | .NET 11.0 | .NET 11.0 | ?      | 32        |     3.793 ns | 0.0123 ns | 0.0103 ns |  0.50 |    0.00 |     NA |        NA |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| **Hash128Benchmarks**            | **Reference**           | **.NET 8.0**  | **.NET 8.0**  | **40**     | **?**         |     **6.835 ns** | **0.0035 ns** | **0.0027 ns** |  **1.00** |    **0.00** |      **-** |         **-** |          **NA** |
| Hash128Benchmarks            | Hash128             | .NET 8.0  | .NET 8.0  | 40     | ?         |     4.498 ns | 0.0039 ns | 0.0036 ns |  0.66 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 8.0  | .NET 8.0  | 40     | ?         |     4.803 ns | 0.0110 ns | 0.0097 ns |  0.70 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 8.0  | .NET 8.0  | 40     | ?         |     4.858 ns | 0.0012 ns | 0.0009 ns |  0.71 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 8.0  | .NET 8.0  | 40     | ?         |     4.454 ns | 0.0075 ns | 0.0059 ns |  0.65 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 8.0  | .NET 8.0  | 40     | ?         |    18.365 ns | 0.0819 ns | 0.0684 ns |  2.69 |    0.01 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 10.0 | .NET 10.0 | 40     | ?         |     6.763 ns | 0.0187 ns | 0.0175 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 10.0 | .NET 10.0 | 40     | ?         |     4.495 ns | 0.0116 ns | 0.0109 ns |  0.66 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 10.0 | .NET 10.0 | 40     | ?         |     4.772 ns | 0.0024 ns | 0.0019 ns |  0.71 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 10.0 | .NET 10.0 | 40     | ?         |     4.627 ns | 0.0006 ns | 0.0005 ns |  0.68 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 10.0 | .NET 10.0 | 40     | ?         |     4.397 ns | 0.0014 ns | 0.0012 ns |  0.65 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 10.0 | .NET 10.0 | 40     | ?         |    14.466 ns | 0.0315 ns | 0.0295 ns |  2.14 |    0.01 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 11.0 | .NET 11.0 | 40     | ?         |     6.739 ns | 0.0015 ns | 0.0012 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 11.0 | .NET 11.0 | 40     | ?         |     4.399 ns | 0.0081 ns | 0.0067 ns |  0.65 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 11.0 | .NET 11.0 | 40     | ?         |     4.690 ns | 0.0013 ns | 0.0010 ns |  0.70 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 11.0 | .NET 11.0 | 40     | ?         |     4.531 ns | 0.0009 ns | 0.0007 ns |  0.67 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 11.0 | .NET 11.0 | 40     | ?         |     4.301 ns | 0.0005 ns | 0.0004 ns |  0.64 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 11.0 | .NET 11.0 | 40     | ?         |     9.313 ns | 0.0337 ns | 0.0315 ns |  1.38 |    0.00 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| **Hash128Benchmarks**            | **Reference**           | **.NET 8.0**  | **.NET 8.0**  | **128**    | **?**         |    **15.117 ns** | **0.0618 ns** | **0.0578 ns** |  **1.00** |    **0.00** |      **-** |         **-** |          **NA** |
| Hash128Benchmarks            | Hash128             | .NET 8.0  | .NET 8.0  | 128    | ?         |    11.684 ns | 0.0199 ns | 0.0176 ns |  0.77 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 8.0  | .NET 8.0  | 128    | ?         |    13.622 ns | 0.0080 ns | 0.0063 ns |  0.90 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 8.0  | .NET 8.0  | 128    | ?         |    12.090 ns | 0.0024 ns | 0.0021 ns |  0.80 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 8.0  | .NET 8.0  | 128    | ?         |    11.467 ns | 0.0034 ns | 0.0030 ns |  0.76 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 8.0  | .NET 8.0  | 128    | ?         |    29.242 ns | 0.0328 ns | 0.0274 ns |  1.93 |    0.01 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 10.0 | .NET 10.0 | 128    | ?         |    14.938 ns | 0.0038 ns | 0.0032 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 10.0 | .NET 10.0 | 128    | ?         |    11.556 ns | 0.0150 ns | 0.0133 ns |  0.77 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 10.0 | .NET 10.0 | 128    | ?         |    13.625 ns | 0.0187 ns | 0.0156 ns |  0.91 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 10.0 | .NET 10.0 | 128    | ?         |    11.955 ns | 0.0062 ns | 0.0055 ns |  0.80 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 10.0 | .NET 10.0 | 128    | ?         |    11.395 ns | 0.0030 ns | 0.0024 ns |  0.76 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 10.0 | .NET 10.0 | 128    | ?         |    17.652 ns | 0.0367 ns | 0.0287 ns |  1.18 |    0.00 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 11.0 | .NET 11.0 | 128    | ?         |    14.948 ns | 0.0085 ns | 0.0080 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 11.0 | .NET 11.0 | 128    | ?         |    11.437 ns | 0.0015 ns | 0.0014 ns |  0.77 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 11.0 | .NET 11.0 | 128    | ?         |    13.530 ns | 0.0033 ns | 0.0026 ns |  0.91 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 11.0 | .NET 11.0 | 128    | ?         |    11.862 ns | 0.0017 ns | 0.0014 ns |  0.79 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 11.0 | .NET 11.0 | 128    | ?         |    11.321 ns | 0.0027 ns | 0.0021 ns |  0.76 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 11.0 | .NET 11.0 | 128    | ?         |    16.822 ns | 0.0241 ns | 0.0201 ns |  1.13 |    0.00 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| **Hash128MixedLengthBenchmarks** | **Reference**           | **.NET 8.0**  | **.NET 8.0**  | **?**      | **256**       |    **19.027 ns** | **0.0224 ns** | **0.0187 ns** |  **1.00** |    **0.00** |     **NA** |        **NA** |          **NA** |
| Hash128MixedLengthBenchmarks | Hash128             | .NET 8.0  | .NET 8.0  | ?      | 256       |    12.895 ns | 0.0033 ns | 0.0031 ns |  0.68 |    0.00 |     NA |        NA |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| Hash128MixedLengthBenchmarks | Reference           | .NET 10.0 | .NET 10.0 | ?      | 256       |    18.974 ns | 0.1835 ns | 0.1627 ns |  1.00 |    0.00 |     NA |        NA |          NA |
| Hash128MixedLengthBenchmarks | Hash128             | .NET 10.0 | .NET 10.0 | ?      | 256       |    12.937 ns | 0.0047 ns | 0.0044 ns |  0.68 |    0.01 |     NA |        NA |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| Hash128MixedLengthBenchmarks | Reference           | .NET 11.0 | .NET 11.0 | ?      | 256       |    18.913 ns | 0.0716 ns | 0.0670 ns |  1.00 |    0.00 |     NA |        NA |          NA |
| Hash128MixedLengthBenchmarks | Hash128             | .NET 11.0 | .NET 11.0 | ?      | 256       |    12.856 ns | 0.0571 ns | 0.0477 ns |  0.68 |    0.00 |     NA |        NA |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| **Hash128Benchmarks**            | **Reference**           | **.NET 8.0**  | **.NET 8.0**  | **1024**   | **?**         |   **107.772 ns** | **0.0961 ns** | **0.0899 ns** |  **1.00** |    **0.00** |      **-** |         **-** |          **NA** |
| Hash128Benchmarks            | Hash128             | .NET 8.0  | .NET 8.0  | 1024   | ?         |    85.303 ns | 0.1938 ns | 0.1718 ns |  0.79 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 8.0  | .NET 8.0  | 1024   | ?         |   102.444 ns | 0.0698 ns | 0.0653 ns |  0.95 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 8.0  | .NET 8.0  | 1024   | ?         |    85.402 ns | 0.0677 ns | 0.0600 ns |  0.79 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 8.0  | .NET 8.0  | 1024   | ?         |    84.282 ns | 0.0326 ns | 0.0305 ns |  0.78 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 8.0  | .NET 8.0  | 1024   | ?         |   119.543 ns | 0.2488 ns | 0.2206 ns |  1.11 |    0.00 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 10.0 | .NET 10.0 | 1024   | ?         |   108.043 ns | 0.1889 ns | 0.1577 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 10.0 | .NET 10.0 | 1024   | ?         |    84.213 ns | 0.0260 ns | 0.0230 ns |  0.78 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 10.0 | .NET 10.0 | 1024   | ?         |   102.631 ns | 0.0395 ns | 0.0369 ns |  0.95 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 10.0 | .NET 10.0 | 1024   | ?         |    84.303 ns | 0.0142 ns | 0.0133 ns |  0.78 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 10.0 | .NET 10.0 | 1024   | ?         |    84.094 ns | 0.0190 ns | 0.0168 ns |  0.78 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 10.0 | .NET 10.0 | 1024   | ?         |   110.463 ns | 0.0537 ns | 0.0476 ns |  1.02 |    0.00 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 11.0 | .NET 11.0 | 1024   | ?         |   108.063 ns | 0.1824 ns | 0.1707 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 11.0 | .NET 11.0 | 1024   | ?         |    84.219 ns | 0.0211 ns | 0.0176 ns |  0.78 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 11.0 | .NET 11.0 | 1024   | ?         |   102.336 ns | 0.0642 ns | 0.0569 ns |  0.95 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 11.0 | .NET 11.0 | 1024   | ?         |    85.301 ns | 0.1733 ns | 0.1447 ns |  0.79 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 11.0 | .NET 11.0 | 1024   | ?         |    85.832 ns | 0.2015 ns | 0.1885 ns |  0.79 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 11.0 | .NET 11.0 | 1024   | ?         |   108.976 ns | 0.0750 ns | 0.0702 ns |  1.01 |    0.00 | 0.0048 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| **Hash128Benchmarks**            | **Reference**           | **.NET 8.0**  | **.NET 8.0**  | **16384**  | **?**         | **1,649.128 ns** | **0.2939 ns** | **0.2605 ns** |  **1.00** |    **0.00** |      **-** |         **-** |          **NA** |
| Hash128Benchmarks            | Hash128             | .NET 8.0  | .NET 8.0  | 16384  | ?         | 1,343.949 ns | 0.5843 ns | 0.5179 ns |  0.81 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 8.0  | .NET 8.0  | 16384  | ?         | 1,644.354 ns | 0.3079 ns | 0.2880 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 8.0  | .NET 8.0  | 16384  | ?         | 1,337.882 ns | 1.8916 ns | 1.5795 ns |  0.81 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 8.0  | .NET 8.0  | 16384  | ?         | 1,345.444 ns | 1.7084 ns | 1.5980 ns |  0.82 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 8.0  | .NET 8.0  | 16384  | ?         | 1,662.449 ns | 1.0352 ns | 0.8644 ns |  1.01 |    0.00 | 0.0038 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 10.0 | .NET 10.0 | 16384  | ?         | 1,648.647 ns | 0.1420 ns | 0.1186 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 10.0 | .NET 10.0 | 16384  | ?         | 1,341.129 ns | 0.8397 ns | 0.7855 ns |  0.81 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 10.0 | .NET 10.0 | 16384  | ?         | 1,648.027 ns | 0.9727 ns | 0.9098 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 10.0 | .NET 10.0 | 16384  | ?         | 1,345.532 ns | 1.1544 ns | 1.0233 ns |  0.82 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 10.0 | .NET 10.0 | 16384  | ?         | 1,344.673 ns | 0.3500 ns | 0.2923 ns |  0.82 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 10.0 | .NET 10.0 | 16384  | ?         | 1,658.529 ns | 1.3178 ns | 1.1682 ns |  1.01 |    0.00 | 0.0038 |      40 B |          NA |
|                              |                     |           |           |        |           |              |           |           |       |         |        |           |             |
| Hash128Benchmarks            | Reference           | .NET 11.0 | .NET 11.0 | 16384  | ?         | 1,648.572 ns | 0.3120 ns | 0.2918 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128             | .NET 11.0 | .NET 11.0 | 16384  | ?         | 1,342.275 ns | 1.3440 ns | 1.1914 ns |  0.81 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_GeneralPath | .NET 11.0 | .NET 11.0 | 16384  | ?         | 1,645.141 ns | 0.9978 ns | 0.9334 ns |  1.00 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash128_Destination | .NET 11.0 | .NET 11.0 | 16384  | ?         | 1,345.395 ns | 0.2398 ns | 0.2003 ns |  0.82 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | Hash64              | .NET 11.0 | .NET 11.0 | 16384  | ?         | 1,340.094 ns | 0.4566 ns | 0.4271 ns |  0.81 |    0.00 |      - |         - |          NA |
| Hash128Benchmarks            | HashDepot_Hash128   | .NET 11.0 | .NET 11.0 | 16384  | ?         | 1,666.116 ns | 1.1767 ns | 1.0431 ns |  1.01 |    0.00 | 0.0038 |      40 B |          NA |
