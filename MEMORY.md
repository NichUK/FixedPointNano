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

## Retained work

- PR 223 (https://github.com/NichUK/FixedPointNano/pull/223) remains
  intentionally open as a draft from
  `repo-assist/perf-multiply-inline-2026-07-12-d0c46fd9cc0e034f` into `main`.
- Its exact head is `83b673f452ed33e4d0d4653fc6aebb8becf5bc7b`. GitHub reports
  it mergeable but unstable: the bot auto-approval workflow fails because the
  GitHub Actions author cannot approve its own PR; this is not a code/test failure.
- It has no Copilot review or activity. Persistent monitoring is unavailable
  outside an active task; resume from this record before taking action.
- Do not adopt its multiplication inlining hint until a current `develop`
  candidate has controlled baseline-versus-candidate BenchmarkDotNet evidence
  for multiplication and representative loops, with numerical equivalence and
  generated-code/code-size inspection. Next action is to gather that evidence or
  close the draft deliberately; do not merge its stale `main`-based branch.
