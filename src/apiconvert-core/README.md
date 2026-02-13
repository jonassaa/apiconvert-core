# @apiconvert/core

Shared TypeScript conversion runtime for Apiconvert.

## Rules Model

Canonical rules are defined by one ordered `rules` array of recursive nodes:
- `kind: "field"` with `outputPaths` and `source`
- `kind: "array"` with `inputPath`, `outputPaths`, and recursive `itemRules`
- `kind: "branch"` with `expression`, `then`, optional `elseIf`, optional `else`

Legacy keys are rejected:
- `fieldMappings`
- `arrayMappings`
- `itemMappings`
- `outputPath`

## Example

```ts
import { applyConversion, normalizeConversionRules } from "@apiconvert/core";

const rules = normalizeConversionRules({
  version: 2,
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
