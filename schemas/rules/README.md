# Apiconvert Rules Schema

`rules.schema.json` is the canonical JSON Schema for Apiconvert conversion rules. It is the single source of truth for the rule shape used by both the .NET and npm packages.

## Canonical Shape

Rules now use a single ordered `rules` array with recursive nodes:
- `kind: "field"` for scalar mappings (`outputPaths`, `source`, optional `defaultValue`)
- `kind: "array"` for array mappings (`inputPath`, `outputPaths`, `itemRules`, optional `coerceSingle`)
- `kind: "branch"` for conditional blocks (`expression`, `then`, optional `elseIf`, optional `else`)

Legacy keys are intentionally unsupported and should be rejected:
- `fieldMappings`
- `arrayMappings`
- `itemMappings`
- `outputPath`

## Consistency Across C# and TypeScript

- The C# rules models live in `src/Apiconvert.Core/Rules/Models.cs`.
- The TypeScript contracts live in `src/apiconvert-core/src/index.ts`.
- Any changes to rule models must update this schema first, then update both implementations to match.

## Condition Expressions

`source.type = "condition"` uses a single `expression` string instead of a typed `condition` object.
Legacy `condition` objects are intentionally unsupported.
