# Parity Gate CI

Parity gate enforces behavior alignment across .NET and TypeScript in CI.

## What to gate

- Both runtime suites execute shared cases successfully.
- Parity summary reports no mismatches.
- Reports are archived for debugging when failures occur.

## Recommended CI flow

1. Run .NET tests.
2. Run npm tests.
3. Run parity check.
4. Fail build when parity summary indicates mismatch.

## Artifacts

- `tests/parity/parity-report.json`
- `tests/parity/parity-summary.json`

