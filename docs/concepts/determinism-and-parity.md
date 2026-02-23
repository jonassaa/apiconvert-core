# Determinism And Parity

This package is designed so the same rules and input produce equivalent behavior in .NET and TypeScript.

## Determinism guarantees

- conversion execution is side-effect free
- rule evaluation order is stable
- diagnostics and trace ordering are stable
- formatting and cache-key utilities are deterministic

## Parity strategy

Behavioral parity is maintained through shared conversion cases in `tests/cases/` and runtime-specific test runners.

- .NET tests: `tests/nuget/Apiconvert.Core.Tests/`
- npm tests: `tests/npm/apiconvert-core-tests/`

## Practical guidance

- Keep custom transforms equivalent across runtimes.
- Prefer canonical rule structure from [Rules schema reference](../reference/rules-schema.md).
- Use compatibility checks when schema/runtime versions change.

## Related pages

- [Rules schema reference](../reference/rules-schema.md)
