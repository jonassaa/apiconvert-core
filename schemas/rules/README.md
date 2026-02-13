# Apiconvert Rules Schema

`rules.schema.json` is the canonical JSON Schema for Apiconvert conversion rules. It is the single source of truth for the rule shape used by both the .NET and npm packages.

## Versioning

The schema describes the current rules format (`version: 2`). When the rules shape changes, publish a new schema file and update references to point at the new version.

## Consistency Across C# and TypeScript

- The C# rules models live in `src/Apiconvert.Core/Rules/Models.cs`.
- The TypeScript contracts live in `src/apiconvert-core/src/index.ts`.
- Any changes to rule models must update this schema first, then update both implementations to match.

## Condition Expressions

`source.type = "condition"` uses a single `expression` string instead of a typed `condition` object.
Legacy `condition` objects are intentionally unsupported.
