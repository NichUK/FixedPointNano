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

## JSON serialization

`FixedPointNanoJsonConverter` is opt-in. Register it for an options instance:

```csharp
using System.Text.Json;
using Seerstone;

var options = new JsonSerializerOptions();
options.Converters.Add(new FixedPointNanoJsonConverter());
var json = JsonSerializer.Serialize(FixedPointNano.FromDecimal(123.456789123m), options);
var value = JsonSerializer.Deserialize<FixedPointNano>(json, options);
```

Alternatively, apply `[JsonConverter(typeof(FixedPointNanoJsonConverter))]` to a
property (with `using System.Text.Json.Serialization`). Without registration,
the existing default object representation is unchanged.

The converter writes decimal JSON numbers and reads JSON numbers or invariant
numeric strings. Strings follow `FixedPointNano.TryParse` rules, including
invariant group separators; JSON numbers also accept exponent notation. Values
are parsed as .NET decimals and rounded to nine places using banker's rounding
(`MidpointRounding.ToEven`), just like `FromDecimal`. Invalid inputs and values
whose rounded result is outside the representable range throw `JsonException`.
Nullable values retain the serializer's normal `null` behavior.

The numeric output preserves every stored digit for decimal-aware consumers.
JavaScript and other binary floating-point consumers can lose precision. If a
consumer requires quoted numbers, choose a string representation in your DTO or
provide a custom converter; this converter always writes JSON numbers.

## Benchmarks

BenchmarkDotNet microbenchmarks live under `benchmarks/FixedPointNano.Benchmarks`.
They compare `FixedPointNano` raw math against decimal-reference and double-reference paths.
Run a short local pass with:

```powershell
dotnet run --project benchmarks/FixedPointNano.Benchmarks/FixedPointNano.Benchmarks.csproj -c Release -- --filter "*" --warmupCount 1 --iterationCount 1
```
