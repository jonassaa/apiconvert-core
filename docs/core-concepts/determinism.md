# Determinism

Deterministic behavior means the same input payload + same normalized rules must produce the same output and diagnostics.

## In practice

- rules are normalized before use
- conversion does not mutate input payloads
- collision handling is policy-based and explicit
- tracing is ordered by rule execution path
