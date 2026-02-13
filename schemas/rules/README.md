# Apiconvert Rules Schema

Apiconvert schema versioning is lockstep with repository/package SemVer.

For release `vX.Y.Z`:
- Immutable canonical schema: `vX.Y.Z/schema.json`
- Mutable latest alias: `current/schema.json`
- Deprecated legacy alias: `rules.schema.json`

Use versioned paths (`vX.Y.Z`) for strict pinning.

## Canonical Shape

Rules now use a single ordered `rules` array with recursive nodes:
- `kind: "field"` for scalar mappings (`outputPaths`, `source`, optional `defaultValue`)
- `kind: "array"` for array mappings (`inputPath`, `outputPaths`, `itemRules`, optional `coerceSingle`)
- `kind: "branch"` for conditional blocks (`expression`, `then`, optional `elseIf`, optional `else`)

## Consistency Across C# and TypeScript

- The C# rules models live in `src/Apiconvert.Core/Rules/Models.cs`.
- The TypeScript contracts live in `src/apiconvert-core/src/index.ts`.
- Any changes to rule models must update the next versioned schema, then update both implementations to match.
- Released `vX.Y.Z/schema.json` files are immutable.

## Condition Expressions

`source.type = "condition"` uses a single `expression` string instead of a typed `condition` object.
