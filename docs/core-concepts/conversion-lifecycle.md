# Conversion Lifecycle

## Lifecycle

1. Parse rules input.
2. Normalize rules (strict/lenient).
3. Parse payload by `inputFormat`.
4. Apply conversion (rules or compiled plan).
5. Inspect errors/warnings/trace.
6. Format payload by `outputFormat`.

## Recommended production flow

- normalize strict at startup/build time
- compile conversion plan for repeated runs
- apply explicit collision policy
- enable trace in debugging paths

