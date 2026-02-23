# Rules Schema Reference

This is the primary source of truth for rules authoring.

- Canonical schema file: `src/apiconvert-core/schemas/rules/rules.schema.json`
- Versioned schema file: `src/apiconvert-core/schemas/rules/v1.0.0/schema.json`

## Top-level object

```json
{
  "schemaVersion": "1.0.0",
  "inputFormat": "json",
  "outputFormat": "json",
  "fragments": {},
  "rules": []
}
```

## Formats

- `inputFormat`: `json` | `xml` | `query`
- `outputFormat`: `json` | `xml` | `query`

## Rule node kinds

- `field`
- `array`
- `branch`
- `use` fragment reference node

See [Rule nodes](./rule-nodes.md).

## Field source types

- `path`
- `constant`
- `transform`
- `merge`
- `condition`

See [Sources and transforms](./sources-and-transforms.md).

## Expressions

Condition and branch expressions support `path(...)`, `exists(...)`, comparisons, and boolean operators.

See [Condition expressions](./conditions.md).

## Merge and collision policies

- Merge source modes: `concat`, `firstNonEmpty`, `array`
- Output collision policies:
  - .NET: `LastWriteWins`, `FirstWriteWins`, `Error`
  - TypeScript: `lastWriteWins`, `firstWriteWins`, `error`

See [Merge and collision](./merge-and-collision.md).

## Fragments and includes

- `fragments` + `use` for in-document reuse
- `include` for multi-file rule packs with bundling

See [Fragments and includes](./fragments-and-includes.md).

## Validation and compatibility

Use these tools before production rollouts:

- normalize/strict normalize
- lint
- rule doctor
- compatibility checks

See [Validation and diagnostics](./validation-and-diagnostics.md).
