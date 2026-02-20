# Parity Gate Toolkit

The parity gate validates cross-runtime behavior between `.NET` and `npm` using shared `tests/cases`.

## Local one-command run

Run from repo root:

```bash
npm --prefix tests/npm/apiconvert-core-tests run parity:check
```

This command:
- executes the parity conformance runner
- writes `tests/parity/parity-report.json`
- validates report schema stability
- writes `tests/parity/parity-summary.json`
- exits non-zero when mismatches exceed `--max-mismatches` (default `0`)

## Direct gate command

```bash
node tests/parity/parity-gate.mjs \
  --report tests/parity/parity-report.json \
  --summary tests/parity/parity-summary.json \
  --max-mismatches 0
```

## GitHub Actions snippet

```yaml
- name: Run Apiconvert parity gate
  run: node tests/parity/parity-gate.mjs --report tests/parity/parity-report.json --summary tests/parity/parity-summary.json --max-mismatches 0

- name: Upload parity artifacts
  uses: actions/upload-artifact@v4
  with:
    name: parity-artifacts
    path: |
      tests/parity/parity-report.json
      tests/parity/parity-summary.json
```

## Report schema (stable fields)

`tests/parity/parity-report.json`:
- `generatedAtUtc`
- `totalCases`
- `matchingCases`
- `mismatches[]`
- `mismatches[].caseName`
- `mismatches[].diffFields`

`tests/parity/parity-summary.json`:
- `generatedAtUtc`
- `reportPath`
- `totalCases`
- `matchingCases`
- `mismatchCount`
- `maxMismatches`
- `passed`
