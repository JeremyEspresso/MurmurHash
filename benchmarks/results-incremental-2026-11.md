# Incremental hashing benchmarks (2026-11)

`MurmurHash32` / `MurmurHash128` (`Incremental32` / `Incremental128`) against the one-shot `MurmurHash3.Hash32` /
`Hash128` and System.IO.Hashing 8.0.0's `XxHash32` / `XxHash128`, each instance reset and reused per operation.
`IncrementalBenchmarks` appends the whole input at once; `ChunkedAppendBenchmarks` appends 16 KB in chunks of 7, 64
and 1500 bytes. The machine was idle during the run (1-minute load average median 1.06, maximum 1.42).

## Partial-block handling

Partial blocks are kept in registers: each piece is read with overlapping loads, as in the one-shot tail, and shifted
into the pending value. Two earlier versions were measured on the way (16 KB appended in 7-byte pieces, .NET 10):

| Version | MurmurHash32 | MurmurHash128 |
|---|---:|---:|
| One byte at a time | 7.6 us | 16.0 us |
| Copy into an in-object buffer, read back as a word | 14.9 us | 9.8 us |
| In registers (shipped) | 6.8 us | 5.7 us |

The byte-at-a-time numbers come from a short run on a loaded machine and are only indicative. The buffer version stalls
on store forwarding: the CPU cannot forward several narrow stores into one wider load.

## Results

```

BenchmarkDotNet v0.16.0-preview.2, Linux Fedora Linux 45.20261104.0 (Kinoite)
13th Gen Intel Core i9-13900K 2.99GHz, 1 CPU, 26 logical and 26 physical cores
Memory: 82.4 GB Total, 8.78 GB Available
.NET SDK 11.0.100
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.34 (8.0.34, 8.0.3426.52217), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Arguments=Default  

```
| Type                    | Method         | Job       | Runtime   | Chunk | Length | Mean          | Error       | StdDev      | Allocated |
|------------------------ |--------------- |---------- |---------- |------ |------- |--------------:|------------:|------------:|----------:|
| **ChunkedAppendBenchmarks** | **Incremental32**  | **.NET 8.0**  | **.NET 8.0**  | **7**     | **?**      |  **6,802.887 ns** |  **53.5410 ns** |  **50.0822 ns** |        **NA** |
| ChunkedAppendBenchmarks | XxHash32       | .NET 8.0  | .NET 8.0  | 7     | ?      | 11,804.925 ns | 162.5185 ns | 152.0199 ns |        NA |
| ChunkedAppendBenchmarks | Incremental128 | .NET 8.0  | .NET 8.0  | 7     | ?      |  5,682.166 ns |  58.4482 ns |  54.6725 ns |        NA |
| ChunkedAppendBenchmarks | XxHash128      | .NET 8.0  | .NET 8.0  | 7     | ?      |  8,164.148 ns |  98.8576 ns |  92.4715 ns |        NA |
| ChunkedAppendBenchmarks | Incremental32  | .NET 10.0 | .NET 10.0 | 7     | ?      |  6,805.795 ns |  87.5468 ns |  81.8913 ns |        NA |
| ChunkedAppendBenchmarks | XxHash32       | .NET 10.0 | .NET 10.0 | 7     | ?      |  9,360.667 ns |  85.3708 ns |  79.8559 ns |        NA |
| ChunkedAppendBenchmarks | Incremental128 | .NET 10.0 | .NET 10.0 | 7     | ?      |  5,707.299 ns |  30.8125 ns |  28.8220 ns |        NA |
| ChunkedAppendBenchmarks | XxHash128      | .NET 10.0 | .NET 10.0 | 7     | ?      |  7,545.698 ns |  67.9184 ns |  63.5309 ns |        NA |
| **IncrementalBenchmarks**   | **OneShot32**      | **.NET 8.0**  | **.NET 8.0**  | **?**     | **16**     |      **2.369 ns** |   **0.0354 ns** |   **0.0331 ns** |         **-** |
| IncrementalBenchmarks   | Incremental32  | .NET 8.0  | .NET 8.0  | ?     | 16     |      4.381 ns |   0.0565 ns |   0.0528 ns |         - |
| IncrementalBenchmarks   | XxHash32       | .NET 8.0  | .NET 8.0  | ?     | 16     |      5.042 ns |   0.0384 ns |   0.0359 ns |         - |
| IncrementalBenchmarks   | OneShot128     | .NET 8.0  | .NET 8.0  | ?     | 16     |      2.734 ns |   0.0088 ns |   0.0078 ns |         - |
| IncrementalBenchmarks   | Incremental128 | .NET 8.0  | .NET 8.0  | ?     | 16     |      5.900 ns |   0.0540 ns |   0.0505 ns |         - |
| IncrementalBenchmarks   | XxHash128      | .NET 8.0  | .NET 8.0  | ?     | 16     |      7.784 ns |   0.0961 ns |   0.0899 ns |         - |
| IncrementalBenchmarks   | OneShot32      | .NET 10.0 | .NET 10.0 | ?     | 16     |      2.371 ns |   0.0281 ns |   0.0263 ns |         - |
| IncrementalBenchmarks   | Incremental32  | .NET 10.0 | .NET 10.0 | ?     | 16     |      4.099 ns |   0.0436 ns |   0.0408 ns |         - |
| IncrementalBenchmarks   | XxHash32       | .NET 10.0 | .NET 10.0 | ?     | 16     |      4.504 ns |   0.0577 ns |   0.0540 ns |         - |
| IncrementalBenchmarks   | OneShot128     | .NET 10.0 | .NET 10.0 | ?     | 16     |      2.690 ns |   0.0338 ns |   0.0316 ns |         - |
| IncrementalBenchmarks   | Incremental128 | .NET 10.0 | .NET 10.0 | ?     | 16     |      5.441 ns |   0.0468 ns |   0.0438 ns |         - |
| IncrementalBenchmarks   | XxHash128      | .NET 10.0 | .NET 10.0 | ?     | 16     |      7.008 ns |   0.0581 ns |   0.0544 ns |         - |
| **ChunkedAppendBenchmarks** | **Incremental32**  | **.NET 8.0**  | **.NET 8.0**  | **64**    | **?**      |  **3,171.568 ns** |  **24.6849 ns** |  **23.0903 ns** |        **NA** |
| ChunkedAppendBenchmarks | XxHash32       | .NET 8.0  | .NET 8.0  | 64    | ?      |  7,824.703 ns |  67.3351 ns |  62.9853 ns |        NA |
| ChunkedAppendBenchmarks | Incremental128 | .NET 8.0  | .NET 8.0  | 64    | ?      |  1,404.859 ns |  13.0633 ns |  12.2194 ns |        NA |
| ChunkedAppendBenchmarks | XxHash128      | .NET 8.0  | .NET 8.0  | 64    | ?      |  1,623.626 ns |  17.6958 ns |  16.5526 ns |        NA |
| ChunkedAppendBenchmarks | Incremental32  | .NET 10.0 | .NET 10.0 | 64    | ?      |  3,172.909 ns |  46.2949 ns |  43.3043 ns |        NA |
| ChunkedAppendBenchmarks | XxHash32       | .NET 10.0 | .NET 10.0 | 64    | ?      |  1,494.832 ns |  14.3768 ns |  13.4480 ns |        NA |
| ChunkedAppendBenchmarks | Incremental128 | .NET 10.0 | .NET 10.0 | 64    | ?      |  1,366.074 ns |  13.9609 ns |  13.0591 ns |        NA |
| ChunkedAppendBenchmarks | XxHash128      | .NET 10.0 | .NET 10.0 | 64    | ?      |  1,162.760 ns |  13.4422 ns |  12.5739 ns |        NA |
| **IncrementalBenchmarks**   | **OneShot32**      | **.NET 8.0**  | **.NET 8.0**  | **?**     | **128**    |     **20.397 ns** |   **0.1147 ns** |   **0.1073 ns** |         **-** |
| IncrementalBenchmarks   | Incremental32  | .NET 8.0  | .NET 8.0  | ?     | 128    |     22.613 ns |   0.1712 ns |   0.1602 ns |         - |
| IncrementalBenchmarks   | XxHash32       | .NET 8.0  | .NET 8.0  | ?     | 128    |     14.342 ns |   0.1659 ns |   0.1552 ns |         - |
| IncrementalBenchmarks   | OneShot128     | .NET 8.0  | .NET 8.0  | ?     | 128    |     10.394 ns |   0.1142 ns |   0.1068 ns |         - |
| IncrementalBenchmarks   | Incremental128 | .NET 8.0  | .NET 8.0  | ?     | 128    |     13.020 ns |   0.1274 ns |   0.1192 ns |         - |
| IncrementalBenchmarks   | XxHash128      | .NET 8.0  | .NET 8.0  | ?     | 128    |     14.181 ns |   0.1553 ns |   0.1453 ns |         - |
| IncrementalBenchmarks   | OneShot32      | .NET 10.0 | .NET 10.0 | ?     | 128    |     20.418 ns |   0.0945 ns |   0.0884 ns |         - |
| IncrementalBenchmarks   | Incremental32  | .NET 10.0 | .NET 10.0 | ?     | 128    |     22.396 ns |   0.2599 ns |   0.2431 ns |         - |
| IncrementalBenchmarks   | XxHash32       | .NET 10.0 | .NET 10.0 | ?     | 128    |     13.671 ns |   0.0781 ns |   0.0731 ns |         - |
| IncrementalBenchmarks   | OneShot128     | .NET 10.0 | .NET 10.0 | ?     | 128    |     10.345 ns |   0.0868 ns |   0.0812 ns |         - |
| IncrementalBenchmarks   | Incremental128 | .NET 10.0 | .NET 10.0 | ?     | 128    |     12.968 ns |   0.0247 ns |   0.0219 ns |         - |
| IncrementalBenchmarks   | XxHash128      | .NET 10.0 | .NET 10.0 | ?     | 128    |     13.835 ns |   0.1297 ns |   0.1213 ns |         - |
| **IncrementalBenchmarks**   | **OneShot32**      | **.NET 8.0**  | **.NET 8.0**  | **?**     | **1024**   |    **193.532 ns** |   **0.5749 ns** |   **0.5377 ns** |         **-** |
| IncrementalBenchmarks   | Incremental32  | .NET 8.0  | .NET 8.0  | ?     | 1024   |    195.779 ns |   0.6658 ns |   0.6228 ns |         - |
| IncrementalBenchmarks   | XxHash32       | .NET 8.0  | .NET 8.0  | ?     | 1024   |    101.718 ns |   0.4092 ns |   0.3828 ns |         - |
| IncrementalBenchmarks   | OneShot128     | .NET 8.0  | .NET 8.0  | ?     | 1024   |     84.018 ns |   0.6663 ns |   0.6233 ns |         - |
| IncrementalBenchmarks   | Incremental128 | .NET 8.0  | .NET 8.0  | ?     | 1024   |     87.007 ns |   0.5976 ns |   0.5590 ns |         - |
| IncrementalBenchmarks   | XxHash128      | .NET 8.0  | .NET 8.0  | ?     | 1024   |     34.676 ns |   0.0089 ns |   0.0079 ns |         - |
| IncrementalBenchmarks   | OneShot32      | .NET 10.0 | .NET 10.0 | ?     | 1024   |    193.747 ns |   0.4698 ns |   0.4395 ns |         - |
| IncrementalBenchmarks   | Incremental32  | .NET 10.0 | .NET 10.0 | ?     | 1024   |    192.908 ns |   2.2112 ns |   2.0683 ns |         - |
| IncrementalBenchmarks   | XxHash32       | .NET 10.0 | .NET 10.0 | ?     | 1024   |     98.883 ns |   1.5609 ns |   1.4600 ns |         - |
| IncrementalBenchmarks   | OneShot128     | .NET 10.0 | .NET 10.0 | ?     | 1024   |     83.021 ns |   1.0981 ns |   1.0272 ns |         - |
| IncrementalBenchmarks   | Incremental128 | .NET 10.0 | .NET 10.0 | ?     | 1024   |     85.886 ns |   1.1118 ns |   1.0399 ns |         - |
| IncrementalBenchmarks   | XxHash128      | .NET 10.0 | .NET 10.0 | ?     | 1024   |     32.174 ns |   0.0078 ns |   0.0069 ns |         - |
| **ChunkedAppendBenchmarks** | **Incremental32**  | **.NET 8.0**  | **.NET 8.0**  | **1500**  | **?**      |  **3,183.751 ns** |  **21.3915 ns** |  **17.8629 ns** |        **NA** |
| ChunkedAppendBenchmarks | XxHash32       | .NET 8.0  | .NET 8.0  | 1500  | ?      |  8,527.280 ns |  61.4678 ns |  57.4970 ns |        NA |
| ChunkedAppendBenchmarks | Incremental128 | .NET 8.0  | .NET 8.0  | 1500  | ?      |  1,369.529 ns |   4.3995 ns |   4.1153 ns |        NA |
| ChunkedAppendBenchmarks | XxHash128      | .NET 8.0  | .NET 8.0  | 1500  | ?      |    624.715 ns |   0.1677 ns |   0.1569 ns |        NA |
| ChunkedAppendBenchmarks | Incremental32  | .NET 10.0 | .NET 10.0 | 1500  | ?      |  3,203.439 ns |  29.6574 ns |  27.7416 ns |        NA |
| ChunkedAppendBenchmarks | XxHash32       | .NET 10.0 | .NET 10.0 | 1500  | ?      |  1,517.115 ns |  19.2790 ns |  18.0336 ns |        NA |
| ChunkedAppendBenchmarks | Incremental128 | .NET 10.0 | .NET 10.0 | 1500  | ?      |  1,341.488 ns |  17.9875 ns |  16.8256 ns |        NA |
| ChunkedAppendBenchmarks | XxHash128      | .NET 10.0 | .NET 10.0 | 1500  | ?      |    398.433 ns |   0.1964 ns |   0.1640 ns |        NA |
| **IncrementalBenchmarks**   | **OneShot32**      | **.NET 8.0**  | **.NET 8.0**  | **?**     | **16384**  |  **3,193.944 ns** |  **22.7415 ns** |  **21.2724 ns** |         **-** |
| IncrementalBenchmarks   | Incremental32  | .NET 8.0  | .NET 8.0  | ?     | 16384  |  3,209.929 ns |  31.4392 ns |  29.4082 ns |         - |
| IncrementalBenchmarks   | XxHash32       | .NET 8.0  | .NET 8.0  | ?     | 16384  |  1,548.545 ns |  12.1409 ns |  10.7626 ns |         - |
| IncrementalBenchmarks   | OneShot128     | .NET 8.0  | .NET 8.0  | ?     | 16384  |  1,328.671 ns |  10.7062 ns |  10.0146 ns |         - |
| IncrementalBenchmarks   | Incremental128 | .NET 8.0  | .NET 8.0  | ?     | 16384  |  1,337.861 ns |  12.8562 ns |  12.0257 ns |         - |
| IncrementalBenchmarks   | XxHash128      | .NET 8.0  | .NET 8.0  | ?     | 16384  |    499.395 ns |   0.2088 ns |   0.1744 ns |         - |
| IncrementalBenchmarks   | OneShot32      | .NET 10.0 | .NET 10.0 | ?     | 16384  |  3,185.838 ns |  33.8571 ns |  31.6700 ns |         - |
| IncrementalBenchmarks   | Incremental32  | .NET 10.0 | .NET 10.0 | ?     | 16384  |  3,266.257 ns |  20.7051 ns |  19.3675 ns |         - |
| IncrementalBenchmarks   | XxHash32       | .NET 10.0 | .NET 10.0 | ?     | 16384  |  1,511.562 ns |  12.1911 ns |  11.4036 ns |         - |
| IncrementalBenchmarks   | OneShot128     | .NET 10.0 | .NET 10.0 | ?     | 16384  |  1,318.046 ns |  15.9462 ns |  14.9160 ns |         - |
| IncrementalBenchmarks   | Incremental128 | .NET 10.0 | .NET 10.0 | ?     | 16384  |  1,343.720 ns |   9.8219 ns |   9.1874 ns |         - |
| IncrementalBenchmarks   | XxHash128      | .NET 10.0 | .NET 10.0 | ?     | 16384  |    283.170 ns |   0.0716 ns |   0.0635 ns |         - |
