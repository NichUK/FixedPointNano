# AGENTS.md

You are Codex, based on GPT-5. You are running as a coding agent in the Codex CLI on a user's computer.

## Repo Purpose

`FixedPointNano` is a standalone C# library in `C:\Dev\FixedPointNano`.

It provides a `long`-backed fixed-point numeric primitive with:

- scale `1_000_000_000`
- precision of 9 decimal places
- conversions to and from .NET numeric types
- formatting support through `ToString(...)` and `TryFormat(...)`
- deterministic arithmetic/comparison behavior

The GitHub repository is:

- `https://github.com/NichUK/FixedPointNano`

Default branch currently in use:

- `main`

For normal work, do not continue editing directly on `main`.
Create a scoped branch first.

## Current Project Layout

- `FixedPointNano.slnx`
- `Directory.Build.props`
- `global.json`
- `.editorconfig`
- `README.md`
- `src/FixedPointNano/FixedPointNano.csproj`
- `src/FixedPointNano/FixedPointNano.cs`
- `tests/FixedPointNano.Tests/FixedPointNano.Tests.csproj`
- `tests/FixedPointNano.Tests/FixedPointNanoTests.cs`
- `benchmarks/FixedPointNano.Benchmarks/FixedPointNano.Benchmarks.csproj`
- `benchmarks/FixedPointNano.Benchmarks/FixedPointNanoMathBenchmarks.cs`
  - compares raw `FixedPointNano` math against decimal-reference and double-reference paths

## Current Runtime And Tooling

- Target framework: `.NET 10` (`net10.0`)
- SDK pin: `10.0.201` via `global.json`
- Nullable reference types: enabled
- Implicit usings: enabled
- Warnings as errors: enabled through `Directory.Build.props`

## Current Validation Baseline

The initial implementation has already been validated with:

- `dotnet build C:\Dev\FixedPointNano\FixedPointNano.slnx -c Release`
- `dotnet test C:\Dev\FixedPointNano\tests\FixedPointNano.Tests\FixedPointNano.Tests.csproj -c Release`
- `dotnet test C:\Dev\FixedPointNano\tests\FixedPointNano.Tests\FixedPointNano.Tests.csproj -c Release /p:CollectCoverage=true /p:CoverletOutput=C:\Dev\FixedPointNano\TestResults\coverage.json /p:CoverletOutputFormat=json`

Coverage baseline at that point was:

- line: `100%`
- branch: `100%`
- method: `100%`

Do not regress this without a deliberate reason.

## Existing Implementation Notes

The current `FixedPointNano` type:

- stores raw scaled values in `long`
- exposes `RawValue`
- uses banker’s rounding (`MidpointRounding.ToEven`) when constructing from floating/decimal inputs
- supports arithmetic operators `+`, `-`, unary `-`, `*`, `/`, `%`
- supports comparisons and helper methods like `Abs`, `Min`, `Max`, `Round`, `Floor`, `Ceiling`, `Truncate`
- supports fast fixed-point helper methods:
  - `Divide(FixedPointNano, int)`
  - `Divide(FixedPointNano, long)`
  - `MultiplyRatio(FixedPointNano, long, long)`
  - `Square(FixedPointNano)`
  - `PopulationVariance(FixedPointNano, Int128, int)`
  - `PopulationStandardDeviation(FixedPointNano, Int128, int)`
  - `Sqrt(FixedPointNano)`
- core arithmetic operators and math helpers use raw scaled integer arithmetic rather than decimal round-trips
- `FromDouble` uses finite-only double scaling and midpoint-to-even raw rounding without decimal conversion
- supports conversions for:
  - `byte`, `sbyte`
  - `short`, `ushort`
  - `int`, `uint`
  - `long`, `ulong`
  - `nint`, `nuint`
  - `Half`, `float`, `double`, `decimal`
  - `Int128`, `UInt128`
  - `BigInteger`
- implements:
  - `IComparable`
  - `IComparable<FixedPointNano>`
  - `IEquatable<FixedPointNano>`
  - `IFormattable`
  - `ISpanFormattable`
  - `IConvertible`

The current implementation intentionally favors correctness and deterministic behavior over an oversized generic-math surface.

## Known Follow-On Areas

The component is in a good initial state, but likely next areas of work are:

- package metadata and NuGet publishing automation
- XML documentation comments for public API surface
- additional parsing APIs such as `Parse` / `TryParse`
- performance tuning for multiply/divide if decimal-based implementation becomes a bottleneck
- explicit serialization helpers if consumers need them
- generic math interfaces if they become worth the complexity

Any expansion should stay small and reviewable.

## Mandatory C# Style Enforcement

All C# code in this repo must follow the root ruleset file:

- `C:\Dev\FixedPointNano\.editorconfig`

The repo already contains the Quantauma standard C# style ruleset.
Do not add C# code that violates it.

## Standard Agent Rules

### General

- Prefer `rg` / `rg --files` for searches when available.
- Default expectation: deliver working code, not just a plan.
- Read enough context first, then make coherent edits rather than repeated micro-patches.
- Preserve the existing design unless a change is clearly required.

### Autonomy And Persistence

- Work end-to-end when feasible: inspect, implement, validate, and explain.
- Bias toward action with reasonable assumptions.
- If a detail is missing but the safe path is obvious, proceed.
- If you discover unexpected conflicting user changes, stop and ask before proceeding.

### Editing Constraints

- Default to ASCII.
- Keep comments brief and only where they add real value.
- Use `apply_patch` for manual edits when practical.
- Never revert unrelated user changes.
- Never use destructive git commands such as `reset --hard` unless explicitly requested.

### Testing And Quality

- Keep the repo buildable and testable.
- Run relevant validation before finishing changes.
- Maintain or improve coverage where practical.
- Treat warnings as errors as part of normal development.

### Git And Branching

- Do not do normal feature or chore work directly on `main`.
- Create a focused branch before editing.
- Keep branch scope narrow.
- Make small, descriptive commits.
- Do not amend unless explicitly asked.

### Repo-Specific Branch Guidance

Suggested naming:

- `feature/<short-scope>`
- `bugfix/<short-scope>`
- `chore/<short-scope>`

## Important Paths

- library code: `C:\Dev\FixedPointNano\src\FixedPointNano\FixedPointNano.cs`
- library project: `C:\Dev\FixedPointNano\src\FixedPointNano\FixedPointNano.csproj`
- tests: `C:\Dev\FixedPointNano\tests\FixedPointNano.Tests\FixedPointNanoTests.cs`
- comparison tests: `C:\Dev\FixedPointNano\tests\FixedPointNano.Tests\FixedPointNanoMathComparisonTests.cs`
- test project: `C:\Dev\FixedPointNano\tests\FixedPointNano.Tests\FixedPointNano.Tests.csproj`
- benchmarks: `C:\Dev\FixedPointNano\benchmarks\FixedPointNano.Benchmarks`
- solution: `C:\Dev\FixedPointNano\FixedPointNano.slnx`
- style rules: `C:\Dev\FixedPointNano\.editorconfig`

## Handoff Expectation

Before finishing a future session:

- state the current branch
- state what was changed
- state what was validated
- mention any remaining gaps or next logical steps

Keep this file current as the repo evolves.
