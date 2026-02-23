# Troubleshooting Tree

Use this quick flow when conversion output is incorrect or conversion fails.

## 1. Does strict normalization fail?

- Yes: fix rule shape first.
- No: continue.

## 2. Does lint report errors?

- Yes: fix lint findings.
- No: continue.

## 3. Does conversion return runtime errors?

- Yes: inspect `diagnostics` and enable explain mode (`Explain` / `explain`).
- No: continue.

## 4. Is output wrong but no errors?

- Verify `outputPaths` and rule order.
- Check collision policy (`LastWriteWins` can mask earlier writes).
- Validate branch expressions and `path(...)` references.

## 5. Is behavior different between runtimes?

- Re-run the same shared case under both test suites.
- Compare custom transform implementations for parity.
- Confirm same schema/rules version and compatibility target.

## Related

- [Determinism and parity](../concepts/determinism-and-parity.md)
- [Validation and diagnostics](../reference/validation-and-diagnostics.md)
