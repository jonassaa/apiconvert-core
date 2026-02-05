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

`runConversionCase` is currently a stub. It will be wired to the JavaScript conversion engine once implemented.
