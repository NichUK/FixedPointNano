# Multiplication inlining test plan

## Decision

This experiment decides whether to add
`MethodImplOptions.AggressiveInlining` to `FixedPointNano.operator *`.
The attribute is accepted only when generated code proves that it changes relevant
call sites and controlled measurements show a repeatable consumer benefit without
a material regression or excessive native code growth.

PR 223 is not a suitable candidate branch. It is based on `main` and predates the
current benchmark work on `develop`. Build both variants from the same exact
`origin/develop` commit after the benchmark fixture described here has landed.
The candidate must differ from the baseline by the single inlining attribute.

## Hypotheses

- **H0:** the default .NET 10 JIT already inlines the multiplication wrapper where
  profitable, or the attribute does not improve representative workloads enough
  to distinguish it from measurement noise.
- **H1:** the attribute removes a remaining wrapper call at useful call sites and
  improves steady-state multiplication workloads without unacceptable code growth
  or regressions.

The multiplication operator is a small wrapper around `Int128` multiplication,
rounding, and range checking. Inlining the wrapper may still leave calls to the
larger rounding helper. A faster single call cannot establish a consumer benefit.

## Benchmark fixture

Add a dedicated `FixedPointNanoMultiplyInliningBenchmarks` fixture. Keep the
fixture identical in baseline and candidate builds and execute it from the
separate benchmark assembly so that it represents an external library consumer.

| Case | Shape | Purpose | Decision role |
| --- | --- | --- | --- |
| `SingleMultiply` | One multiplication | Make the smallest call site easy to inspect | Disassembly only |
| `IndependentMultiplyBatch` | Multiply 1,024 independent operand pairs and checksum the raw results | Measure repeated calls without a dependency chain | Primary |
| `DependentMultiplyChain` | Multiply a seed by 1,024 near-one, mixed-direction factors | Expose call cost on a serial dependency path | Primary |
| `NotionalBatch` | Accumulate 1,024 price-times-quantity results | Represent a common financial consumer loop | Primary |
| `LerpBatch` | Interpolate 1,024 start/end/amount tuples | Exercise multiplication through `Lerp` | Representative |
| `SquareBatch` | Square 1,024 inputs | Exercise multiplication through `Square` | Representative |
| `PowBatch` | Raise prepared inputs to exponent 5 | Exercise repeated multiplication through `Pow` | Representative |

Populate mutable arrays in `GlobalSetup` from a deterministic corpus. Include
positive and negative operands, values below and above one, midpoint-adjacent raw
values, and safe large and small values. Choose magnitudes that cannot overflow
the expected operation chain. Do not embed the corpus as constant expressions in
benchmark methods.

Every batch returns a `long` checksum derived from each result's `RawValue`.
Calculate and record the expected checksum for every case before timing. This
prevents dead-code elimination and detects accidental input or numerical changes.
Set `OperationsPerInvoke = 1024` on each batch benchmark so reported values remain
per operation. The current `MultiplyRaw` benchmark remains useful as a general API
comparison, but it is not part of this decision gate.

## Generated-code checks

Run `DisassemblyDiagnoser` with source and combined reports enabled and a depth
that includes `operator *`, `Lerp`, `Square`, `Pow`, and the rounding helper.
For every case, record:

- whether the caller contains a call to `operator *`;
- whether the rounding helper remains a call;
- the caller's native code size;
- the instruction sequence around the multiply, divide, rounding, and overflow
  checks.

Compare normalized disassembly rather than addresses. If baseline and candidate
generate the same relevant instructions, reject the attribute as redundant. A
single-call disassembly difference permits timing analysis but does not satisfy
the performance gate by itself.

## Runtime jobs

Use the repository-pinned .NET 10 SDK, `Release`, x64, and BenchmarkDotNet's normal
out-of-process toolchain. Record the resolved SDK and runtime versions rather than
assuming the pin selected a particular installed patch.

The primary job uses the runtime defaults, including tiered compilation and
dynamic PGO. Give it enough warmup to reach optimized Tier 1 code, then use at
least 15 measurement iterations of at least 250 ms. Set BenchmarkDotNet's
`LaunchCount=1`: each A or B entry in the ABBA protocol below is a separate
process invocation and one paired observation; iterations within that invocation
are not additional observations. Do not set `DOTNET_TieredPGO`,
`DOTNET_ReadyToRun`, or JIT diagnostic variables in this job.

Run a secondary diagnostic job with `DOTNET_TieredCompilation=0`. It shows the
fully optimized non-tiered heuristic and helps explain a result, but an improvement
that appears only in this job is insufficient for adoption. Cold-start and Tier 0
inspection may identify a startup or code-size regression; process startup timing
is reported separately and is not combined with steady-state results.

## Paired execution protocol

1. Freeze the benchmark fixture on current `develop` and record its commit as
   `BASE_SHA`.
2. Create `CANDIDATE_SHA` from `BASE_SHA` with only the inlining attribute added.
   Verify `git diff BASE_SHA..CANDIDATE_SHA` before measuring.
3. Create clean baseline and candidate worktrees. Restore once, then build both in
   `Release` before starting measurements. Do not build, restore, edit, or run
   unrelated tests during a measurement block.
4. Run on AC power with a fixed power plan and the same CPU affinity. Stop avoidable
   background work and record CPU model, logical-core count, RAM, OS build, power
   plan, SDK, runtime, BenchmarkDotNet version, both SHAs, and relevant environment
   variables.
5. Perform five `ABBA` blocks, where `A` is baseline and `B` is candidate. This
   produces ten observations of each variant while balancing warm-machine and
   time-order effects. Reboot or restart the measurement session if thermal or
   background-load changes invalidate a block; discard and explain the whole
   affected block, not an individual unfavorable result.
6. Preserve the BenchmarkDotNet Markdown, CSV, JSON, logs, and disassembly artifacts
   from every invocation under a timestamped evidence directory. Do not commit raw
   benchmark artifacts to the repository.

For each matched A/B observation, calculate the candidate-to-baseline ratio. Use
the geometric mean of the paired ratios and a paired 95% bootstrap confidence
interval. Retain BenchmarkDotNet's distribution and outlier diagnostics in the
evidence; do not select only the fastest launch or iteration.

## Correctness gate

Before performance analysis:

- run the full Release test suite for both exact commits;
- require identical test counts and all tests to pass;
- require every benchmark corpus checksum to match between variants;
- run explicit multiplication boundary and exception cases and require identical
  values and exception types;
- require zero allocations for both variants in all multiplication batch cases.

Any mismatch rejects the candidate regardless of timing.

## Adoption rule

Adopt the attribute only when all of these conditions hold in the primary job:

- generated code changes at a relevant external-consumer call site;
- both `IndependentMultiplyBatch` and `DependentMultiplyChain` improve by at least
  2%, with their paired 95% confidence intervals wholly below 1.00;
- at least one of `NotionalBatch`, `LerpBatch`, `SquareBatch`, or `PowBatch`
  improves by at least 1%, with its paired 95% confidence interval wholly below
  1.00;
- at least eight of the ten paired observations show the same direction for each
  qualifying workload;
- no primary or representative case regresses by more than 1%;
- allocations remain unchanged;
- aggregate native code size across the hot callers grows by no more than 5%, and
  no individual caller grows by more than 10% without a separately justified
  consumer benefit.

Reject the attribute when baseline already produces equivalent generated code,
the measured difference is noise, benefit exists only with tiering disabled, a
stable regression exceeds the limit, or code growth has no representative payoff.
Classify a result as inconclusive when confidence intervals cross 1.00 or execution
quality fails the protocol; repeat the full paired experiment rather than relaxing
the thresholds.

The resulting PR must include the exact SHAs, environment record, checksum record,
summary table of paired ratios and confidence intervals, and links to the retained
raw artifacts. Rebuild any accepted change on current `develop`; do not merge the
stale PR 223 branch.

## References

- Microsoft advises measuring `AggressiveInlining` because unnecessary use can
  reduce performance: [MethodImplOptions documentation](https://learn.microsoft.com/dotnet/api/system.runtime.compilerservices.methodimploptions?view=net-10.0).
- Runtime controls and environment variables are defined in [.NET runtime
  compilation settings](https://learn.microsoft.com/dotnet/core/runtime-config/compilation).
- Tier transitions and optimized compilation are described in the [.NET tiered
  compilation design](https://github.com/dotnet/runtime/blob/main/docs/design/features/tiered-compilation.md)
  and [dynamic PGO design](https://github.com/dotnet/runtime/blob/main/docs/design/features/DynamicPgo.md).
- Benchmark isolation, jobs, and disassembly behavior are documented by
  BenchmarkDotNet in [how it works](https://benchmarkdotnet.org/articles/guides/how-it-works.html),
  [jobs](https://benchmarkdotnet.org/articles/configs/jobs.html), and the
  [disassembly diagnoser](https://benchmarkdotnet.org/articles/features/disassembler.html).
