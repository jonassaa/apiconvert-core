# Conversion Lifecycle

## Lifecycle

1. Parse payload by `inputFormat`.
2. Normalize rules (strict or lenient).
3. Apply conversion.
4. Inspect errors/warnings/trace.
5. Format payload by `outputFormat`.

## Recommended production flow

- normalize strict at startup or build time
- compile conversion plan for repeated runs
- apply with explicit collision policy
- include trace mode when debugging
