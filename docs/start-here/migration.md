# Migration from Hand-Written Mappers

## Recommended migration steps

1. Start with one high-value conversion and model it as rules.
2. Validate with strict normalization before runtime use.
3. Add shared `tests/cases` coverage for the conversion.
4. Compile plans if you run the same rules repeatedly.
5. Adopt lint + doctor checks in CI before cutting over.

## Why migrate

- deterministic behavior with explicit schema contract
- shared behavior across .NET and TypeScript
- diagnostics with stable codes
- easier parity testing with shared case files
