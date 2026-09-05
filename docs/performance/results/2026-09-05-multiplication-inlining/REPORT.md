# Multiplication inlining experiment

- Baseline: `c4263974dbab1c3692ee9f01b46fc5176e16b197`
- Candidate: `48ff2f09673c2c190099d192c7f289060080e62c`
- Runtime: .NET 10.0.9; SDK 10.0.301; BenchmarkDotNet 0.14.0
- Host: Windows 11 Pro build 26200; Intel Core i7-1265U, 10 cores / 12 logical processors
- Protocol: five ABBA blocks, 20 separate processes, 10 warmups, 15 measurements, 250 ms target iteration

## Recommendation

**Reject the candidate and do not merge the inlining attribute.** The mandatory adoption gates do not pass. The formal timing classification is inconclusive because every 95% paired confidence interval crosses 1.00, while the aggregate point estimate is 2.24% slower and only 4/10 aggregate pairs favor the candidate. A repeat on dedicated lab hardware is the valid next action only if further evidence is desired.

## Results

| Workload | Baseline geometric mean | Candidate geometric mean | Candidate / baseline | Paired bootstrap 95% CI | Candidate faster | Gate |
|---|---:|---:|---:|---:|---:|---|
| IndependentMultiplyBatch | 9.209 ns | 9.779 ns | 1.0619 | 0.9672-1.1750 | 4/10 | Fail |
| DependentMultiplyChain | 5.814 ns | 5.726 ns | 0.9848 | 0.9123-1.0552 | 6/10 | Fail |
| NotionalBatch | 7.955 ns | 8.004 ns | 1.0061 | 0.9153-1.1003 | 5/10 | Fail |
| LerpBatch | 11.025 ns | 11.734 ns | 1.0644 | 0.8919-1.2633 | 5/10 | Fail |
| SquareBatch | 7.376 ns | 7.020 ns | 0.9518 | 0.8217-1.1031 | 6/10 | Fail |
| PowBatch | 42.365 ns | 45.384 ns | 1.0713 | 0.9319-1.2395 | 6/10 | Fail |
| **Aggregate** | - | - | **1.0224** | **0.9803-1.0640** | **4/10** | **Fail** |

All benchmarked workloads allocated 0 B in both variants. Both variants passed all 1,560 tests, the focused 1,356-case math comparison suite, and byte-identical corpus checksum verification.

## Generated code

Under runtime defaults, IndependentMultiplyBatch, NotionalBatch, and PowBatch were instruction-equivalent after address normalization. DependentMultiplyChain, LerpBatch, and SquareBatch differed, but all six hot callers had exactly identical native code sizes between variants (23,287 B aggregate, 0% growth). With tiered compilation disabled, all six hot callers were instruction-equivalent and size-equivalent. The diagnostic SingleMultiply changed and is excluded from the adoption gate.

## Gate decision

- Relevant generated-code difference: pass under defaults for three consumers.
- IndependentMultiplyBatch improves at least 2% with CI below 1.00: fail; point estimate is 6.19% slower.
- DependentMultiplyChain improves at least 2% with CI below 1.00: fail; point estimate improves 1.52%, CI crosses 1.00.
- Representative workload improves at least 1% with CI below 1.00: fail; every CI crosses 1.00.
- At least 8/10 pairs favor the candidate: fail for every workload.
- No representative regression over 1%: fail by point estimate for IndependentMultiplyBatch, LerpBatch, and PowBatch.
- Allocation and code-size budgets: pass.

## Quality and evidence

Fifteen of 20 default-runtime invocations emitted a multimodal-distribution warning, confirming meaningful host noise. No command failed and all raw reports were retained; the wide confidence intervals prevent claiming a speedup.

The exact tested fixture validated its checksums during `GlobalSetup`, invoking
each workload once before BenchmarkDotNet's ten warmup iterations. The retained
fixture performs validation only through `--verify-inlining-corpus` so future
measurements begin with corpus initialization alone.

- Machine/runtime: `dotnet-info.txt`, `os.txt`, `cpu.txt`, `power-plan.txt`
- Correctness: `baseline-tests.txt`, `candidate-tests.txt`, `baseline-checksums.txt`, `candidate-checksums.txt`, `checksum-comparison.txt`
- Assembly: `assembly-comparison.txt`, `disasm-default-*`, `disasm-tieredoff-*`
- Timing: `timing-default/`, `paired-ratios.csv`, `paired-analysis.txt`
- Secondary sensitivity: `tieredoff-sensitivity.txt`
- Machine-readable result: `summary.json`
