# @apiconvert/core

Shared TypeScript conversion runtime for Apiconvert.

## Rules Model

Canonical rules are defined by one ordered `rules` array of recursive nodes:
- `kind: "field"` with `outputPaths` and `source`
- `kind: "array"` with `inputPath`, `outputPaths`, and recursive `itemRules`
- `kind: "branch"` with `expression`, `then`, optional `elseIf`, optional `else`

Supported field source types:
- `path`
- `constant`
- `transform`
- `merge`
- `condition`

Supported transforms:
- `toLowerCase`
- `toUpperCase`
- `number`
- `boolean`
- `concat`
- `split`

Supported merge modes:
- `concat`
- `firstNonEmpty`
- `array`

Additional options:
- field: optional `defaultValue`
- array: optional `coerceSingle`
- condition source: optional `trueSource` / `falseSource`, `trueValue` / `falseValue`, `elseIf`, and `conditionOutput` (`branch` or `match`)

Condition expressions support `path(...)`, `exists(...)`, comparison operators, and boolean operators (`&&`, `||`, `!`).

## Example

```ts
import { applyConversion, normalizeConversionRules } from "@apiconvert/core";

const rules = normalizeConversionRules({
  inputFormat: "json",
  outputFormat: "json",
  rules: [
    {
      kind: "field",
      outputPaths: ["meta.name"],
      source: { type: "path", path: "name" }
    },
    {
      kind: "branch",
      expression: "path(status) == 'active'",
      then: [
        {
          kind: "field",
          outputPaths: ["meta.enabled"],
          source: { type: "constant", value: "true" }
        }
      ],
      else: [
        {
          kind: "field",
          outputPaths: ["meta.enabled"],
          source: { type: "constant", value: "false" }
        }
      ]
    }
  ]
});

const result = applyConversion({ name: "Ada", status: "active" }, rules);
if (result.errors.length > 0) {
  throw new Error(result.errors.join("; "));
}

console.log(result.output);
```

## Parse and Format Helpers

The package also exports:
- `parsePayload(text, format)` for `json`, `xml`, and `query`
- `formatPayload(value, format, pretty)`
