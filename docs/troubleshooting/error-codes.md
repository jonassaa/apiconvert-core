# Error Codes

This page links diagnostic families to practical next steps.

## Common families

- `ACV-RUN-*`: runtime conversion diagnostics
- `ACV-LINT-*`: rule lint/validation diagnostics
- `ACV-DOCTOR-*`: doctor workflow diagnostics
- `ACV-COMP-*`: compatibility diagnostics

Exact codes can vary by stage and runtime context, but both runtimes emit deterministic machine-readable diagnostics.

## Recommended triage order

1. Fix strict normalization errors first.
2. Resolve lint errors and warnings.
3. Run doctor with sample input.
4. Re-run conversion with explain mode enabled for trace-level details.

## Related

- [Validation and diagnostics](../reference/validation-and-diagnostics.md)
- [Troubleshooting tree](./troubleshooting-tree.md)
