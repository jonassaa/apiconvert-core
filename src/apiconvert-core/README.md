# @apiconvert/core

Shared contracts and rule schemas for Apiconvert.

## Rules Schema

The canonical JSON Schema for conversion rules lives at `schemas/rules/rules.schema.json` in this repository. Both the .NET and npm packages reference that schema as the single source of truth.

GitHub source (for reference):
```
https://github.com/jonassaa/apiconvert-core/blob/main/schemas/rules/rules.schema.json
```

## Exports

This package exports TypeScript types for conversion rules, generation contracts, and shared enums.

## Conversion API

`runConversionCase` is implemented and powers the shared conversion case runner used by the npm tests.

## Formatting Notes

`formatPayload(value, DataFormat.Xml, true)` produces indented XML output. Use `false` for compact XML strings.
