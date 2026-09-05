# FixedPointNano delivery state

Last updated: 2026-09-05

## Completed Copilot PR review

- PR 225 merged into `develop` as `2687d7a6911f012d78f8d33cfe3fb06be32b756d`.
  It adds opt-in JSON conversion with invariant parsing, precise JSON errors, and
  compatibility, rounding, range, nullable, collection, and path coverage.
- PR 226 merged into `develop` as `3a5da0a8e3fc28ec85c27a3114832c1634af36f6`.
  It makes parsing overflow and null handling consistent across overloads and
  adds 23 boundary cases; 11 failed before the fix.
- PR 227 merged into `develop` as `f5827326ab39fefcba12b5de967fccf13dae2740`.
  It consolidates corrected benchmark coverage without adopting unmeasured
  arithmetic rewrites or inlining hints.
- Exact-head Copilot follow-up reviews found no remaining actionable issues.
  Individual CI and post-merge `develop` CI run `33962663152` passed.
- Combined local validation passed 1,560 tests and a zero-warning Release build.
  BenchmarkDotNet Dry smoke executed all 33 selected benchmark cases.
- Replacement source branches, their local worktrees, and the seven superseded
  Repo Assist source branches were deleted after merge/closure verification.

## Completed multiplication inlining experiment

- Baseline `c4263974dbab1c3692ee9f01b46fc5176e16b197` and one-line candidate
  `48ff2f09673c2c190099d192c7f289060080e62c` passed 1,560 tests and produced
  byte-identical deterministic corpus checksums.
- Five ABBA blocks completed all 20 process invocations. Every workload's paired
  95% confidence interval crossed parity; none reached the required 8/10 direction
  count. The aggregate candidate/baseline ratio was 1.0224 with a 95% interval of
  0.9803-1.0640 and the candidate faster in 4/10 pairs.
- The attribute was rejected because it demonstrated no repeatable consumer
  benefit. It is absent from the delivery branch. The fixture and concise evidence
  are retained under `docs/performance/results/2026-09-05-multiplication-inlining`;
  raw artifacts remain in `C:\dev\FixedPointNano-inlining-evidence\2026-09-05`.
- PR 223 was closed intentionally with the measured rationale. Its rejected remote
  source branch was deleted after verifying that its only useful change was the
  tested inlining attribute.
