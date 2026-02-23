# Determinism

Deterministic behavior means same input + same normalized rules => same output and diagnostics.

## In practice

- Rules are normalized before use.
- Conversion does not mutate input payloads.
- Collision handling is explicit and policy-based.
- Trace output is ordered by execution path.

## Determinism risks to avoid

- Random/time-dependent transforms
- Network/filesystem reads in transform functions
- Mutable shared state across conversions

