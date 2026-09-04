# FixedPointNano

`FixedPointNano` is a small C# library for representing fixed-point numeric values using an `Int64` scaled to 9 decimal places.

## Design

- Storage type: `long`
- Scale: `1_000_000_000`
- Precision: 9 decimal places
- Target runtime: .NET 10

The library is intended for domains where deterministic 9-decimal fixed-point values are preferred over binary floating-point storage.

## Features

- Deterministic `long`-backed storage
- Conversion operators for .NET numeric types
- `IConvertible` support
- Standard numeric formatting via `ToString(...)` and `TryFormat(...)`
- Raw scaled arithmetic and comparison operators
- Fast helper methods for `Square`, `Sqrt`, population variance/standard deviation, integer division, and ratio multiplication
- Explicit finite-only `double` conversion with nano-scale rounding

## Example

```csharp
using Seerstone;

var price = (FixedPointNano)123.456789123m;
var quantity = (FixedPointNano)2;
var total = price * quantity;
var average = FixedPointNano.Divide(total, 2);
var volatility = FixedPointNano.Sqrt(FixedPointNano.Square(price - average));

Console.WriteLine(total.ToString("F9"));
```

## Benchmarks

BenchmarkDotNet microbenchmarks live under `benchmarks/FixedPointNano.Benchmarks`.
They compare `FixedPointNano` raw math against decimal-reference and double-reference paths.
Run a short local pass with:

```powershell
dotnet run --project benchmarks/FixedPointNano.Benchmarks/FixedPointNano.Benchmarks.csproj -c Release -- --filter "*" --warmupCount 1 --iterationCount 1
```

The suite includes addition, subtraction, decimal conversion, integer powers,
interpolation, variance, formatting, parsing, and Clamp inputs below, inside, and
above its bounds. `FromDecimalRaw` uses a prepared decimal input so it measures
only conversion into fixed point. New decimal arithmetic references use prepared
decimal operands and include conversion of their result to `FixedPointNano`;
they are not isolated decimal-operation timings. Existing older reference cases
may also include conversion of their operands.

`PowDecimalReference` uses decimal exponentiation by squaring, while the double
reference uses `Math.Pow`. The selected base (1.5) and exponent (5) are exact in
all three representations; this is a performance fixture, not a general claim
that decimal and fixed-point intermediate rounding are identical. Variance
inputs are aggregated during setup, so those cases measure finalisation only.

Text benchmarks explicitly use invariant culture and the same valid input.
Formatting cases stay in the existing `FixedPointNanoFormatBenchmarks` fixture.
`ParseDecimalReference` includes conversion into fixed point; the two `TryParse`
benchmarks measure each API's success path, including fixed-point conversion
only in the fixed-point API. Formatting references use prepared decimal values.

Use `--job Dry` for an execution smoke check. Dry or single-iteration results
are not evidence of a speedup. Rounding rewrites and additional inlining hints
remain deferred until controlled before/after benchmarks demonstrate a benefit.
