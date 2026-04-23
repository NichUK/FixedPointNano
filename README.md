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
