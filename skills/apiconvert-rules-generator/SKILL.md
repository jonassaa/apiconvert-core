---
name: apiconvert-rules-generator
description: Generate Apiconvert conversion rules from example input/output payloads. Use when a user provides sample input and desired output (JSON/XML/query) and asks for rule sets that conform to the latest Apiconvert schema, including field mappings, array mappings, transforms, and conditions.
---

# Apiconvert Rules Generator

## Overview

Turn example input and output payloads into a valid Apiconvert rules JSON that conforms to the latest schema and runs in both .NET and TypeScript packages.

## Workflow

### 1. Collect inputs and formats

- Ask for example input payload and expected output payload.
- Ask for input/output formats if not explicit (`json`, `xml`, `query`).
- If payloads are large, request a minimal representative sample that includes all required fields and arrays.

### 2. Load the latest schema

- Prefer local schema: `schemas/rules/rules.schema.json` in the repo.
- If not available, use the canonical schema URL from the repo.
- Keep the rules aligned to version `2` and the supported enums.

### 3. Build a mapping plan

- Identify all output fields and decide how they are sourced.
- Classify each output field into one of:
  - Direct path mapping
  - Constant value
  - Transform (`toLowerCase`, `toUpperCase`, `number`, `boolean`, `concat`)
  - Condition (`exists`, `equals`, `notEquals`, `includes`, `gt`, `lt`)
- Detect arrays in the output; map each array via an `arrayMappings` entry.
- Inside `itemMappings`, resolve paths relative to the array item by default. Use `$` or `$.` for root access.

### 4. Translate to rule JSON

- Use `fieldMappings` for scalar fields.
- Use `arrayMappings` for list outputs.
- Always set `version`, `inputFormat`, `outputFormat`.
- Keep `value`, `trueValue`, `falseValue`, and `defaultValue` as strings.
- When using `concat`, put a comma-separated list of tokens in `path` and use `const:` for literals.

### 5. Validate and sanity-check

- Ensure every `outputPath` is present and non-empty.
- Ensure `arrayMappings` input paths resolve to arrays in the sample input.
- Ensure rule JSON matches schema enums and required fields.
- If something can’t be mapped cleanly, call out assumptions and ask for clarification.

## Output Requirements

When replying, always return:

1. The rules JSON block.
2. A brief explanation of how key fields map.
3. Any assumptions or ambiguities.

## Question Checklist

Ask these if not already clear:

- What are the input and output formats (`json`, `xml`, `query`)?
- Are any fields constants or derived values?
- Are there arrays in the output? If yes, which input array do they map from?
- Do any outputs depend on conditions (equals, exists, gt/lt, includes)?
- Do you need any transforms (upper/lower/number/boolean/concat)?

## Example

Input (JSON):

```json
{
  "user": {
    "firstName": "Ada",
    "lastName": "Lovelace",
    "age": 21
  },
  "defaults": { "currency": "USD" },
  "orders": [
    { "orderId": "A1" },
    { "orderId": "A2" }
  ]
}
```

Output (JSON):

```json
{
  "profile": {
    "displayName": "Ada Lovelace",
    "isAdult": true
  },
  "orders": [
    { "id": "A1", "currency": "USD" },
    { "id": "A2", "currency": "USD" }
  ]
}
```

Rules:

```json
{
  "version": 2,
  "inputFormat": "json",
  "outputFormat": "json",
  "fieldMappings": [
    {
      "outputPath": "profile.displayName",
      "source": {
        "type": "transform",
        "transform": "concat",
        "path": "user.firstName, const: , user.lastName"
      }
    },
    {
      "outputPath": "profile.isAdult",
      "source": {
        "type": "condition",
        "condition": { "path": "user.age", "operator": "gt", "value": "17" },
        "trueValue": "true",
        "falseValue": "false"
      }
    }
  ],
  "arrayMappings": [
    {
      "inputPath": "orders",
      "outputPath": "orders",
      "itemMappings": [
        { "outputPath": "id", "source": { "type": "path", "path": "orderId" } },
        { "outputPath": "currency", "source": { "type": "path", "path": "$.defaults.currency" } }
      ]
    }
  ]
}
```

## XML and Query Notes

- XML attributes are exposed as `@_attrName`.
- XML element text is exposed as `#text`.
- Repeated XML elements become arrays.
- Query strings parse into nested objects (e.g., `user.name=Ada` -> `user.name`).

## References

- Read `references/apiconvert-rules.md` for schema summary, path syntax, and templates.
