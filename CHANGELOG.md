# Changelog

All notable changes to this project are documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.2.0] - 2026-11-13

### Added
- MurmurHash3 x64 128-bit, with output identical to the reference implementation (SMHasher `MurmurHash3_x64_128`) on every platform and endianness:
  - `Hash128(ReadOnlySpan<byte> bytes, Span<byte> destination, uint seed = 0)` writes the 16-byte hash in the reference byte order (all target frameworks).
  - `Hash128(ReadOnlySpan<byte> bytes, uint seed = 0)` returns a `UInt128` (.NET 8 and later).
  - `Hash64(ReadOnlySpan<byte> bytes, uint seed = 0)` returns the first 64 bits of the 128-bit hash.
  - `Hash128SizeInBytes` (16).
- The `MurmurHash.DisableCpuSpecificOptimizations` switch also applies to the 128-bit hash.

## [0.1.0] - 2026-11-12

### Fixed
- `Hash32` read the final 1–3 bytes of inputs whose length is not a multiple of 4 incorrectly, reading up to 11 bytes past the end of the span. This produced incorrect hashes when the input was a slice of a larger buffer, and could crash the process with an `AccessViolationException` when the input ended at the end of readable memory (for example native, memory-mapped or pooled buffers). The tail is now read byte by byte and never accesses memory outside the input.

### Changed
- **Hash values change for inputs affected by the bug above**: inputs whose length is not a multiple of 4 and that were not followed by zero bytes in memory now return the correct MurmurHash3 value. Inputs from exact-size arrays (for example `Encoding.UTF8.GetBytes(...)`) already returned correct values and are unaffected. Output now matches the reference implementation (SMHasher `MurmurHash3_x86_32`) for all inputs, on every platform and endianness.
- Target frameworks: `net7.0` was replaced by `netstandard2.0`, `netstandard2.1` and `net8.0`. Applications on .NET 8 and later use the `net8.0` build, which has no dependencies.

### Added
- Support for .NET Framework 4.6.2+, .NET Core 3.x, Mono, Xamarin and Unity via the `netstandard2.0` and `netstandard2.1` builds.
- `Hash32(ReadOnlySpan<byte> bytes, uint seed = 0)`. The existing `Hash32(ref ReadOnlySpan<byte>, uint)` overload remains available for compatibility.
- Trimming and NativeAOT compatibility (`IsAotCompatible`), verified in CI with a NativeAOT application on .NET 8, 10 and 11.
- Symbol package (`.snupkg`) and SourceLink.
- The `MurmurHash.DisableCpuSpecificOptimizations` AppContext switch, which turns off CPU-specific code paths.

### Performance
- `Hash32` is 10–19% faster for inputs of 16 bytes and larger on Intel CPUs from Ice Lake onward, using a CPU-specific code path that is selected once at startup. Other CPUs use the general code path, which performs the same as 0.0.1 for 16 bytes and larger and 12–16% faster for very short inputs.

## [0.0.1] - 2023-01-03

### Added
- Initial release: zero-allocation `MurmurHash3.Hash32` (x86, 32-bit) for `net7.0`.

[Unreleased]: https://github.com/JeremyEspresso/MurmurHash/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/JeremyEspresso/MurmurHash/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/JeremyEspresso/MurmurHash/compare/v0.0.1...v0.1.0
[0.0.1]: https://github.com/JeremyEspresso/MurmurHash/releases/tag/v0.0.1
