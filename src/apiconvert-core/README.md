# @apiconvert/core

Shared TypeScript conversion runtime for Apiconvert.

## Design Constraints

This runtime is intentionally rule-driven and deterministic:
- prefer declarative rule behavior over hardcoded integration logic
- keep conversion paths side-effect free and isolated from infrastructure concerns
- avoid transport/auth/database/UI behavior in this package
- preserve behavioral parity with `Apiconvert.Core` (.NET)

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
- custom transforms via `source.customTransform` + `applyConversion(..., { transforms })`

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

## Validation Modes

- `normalizeConversionRules(raw)` normalizes and accumulates `validationErrors` without throwing.
- `normalizeConversionRulesStrict(raw)` throws when validation errors exist.
- `validateConversionRules(raw)` returns `{ isValid, errors, rules }` for explicit validation workflows.
- `lintConversionRules(raw)` returns deterministic diagnostics with severity, code, path, and fix hints.
- `compileConversionPlan(raw)` returns a reusable plan with `cacheKey` and `apply(...)`.
- `computeRulesCacheKey(raw)` returns a stable cache key for normalized rules.

```ts
import { normalizeConversionRulesStrict } from "@apiconvert/core";

const rules = normalizeConversionRulesStrict(rulesText);
```

```ts
import { lintConversionRules } from "@apiconvert/core";

const lint = lintConversionRules(rulesText);
for (const diagnostic of lint.diagnostics) {
  console.log(`${diagnostic.severity} ${diagnostic.code} ${diagnostic.rulePath}: ${diagnostic.message}`);
}

const plan = compileConversionPlan(rulesText);
console.log(plan.cacheKey);
```

## Parse and Format Helpers

The package also exports:
- `parsePayload(text, format)` for `json`, `xml`, and `query`
- `formatPayload(value, format, pretty)`

## Output Collision Policy

When multiple rules write to the same output path, pass `collisionPolicy` to `applyConversion`:
- `lastWriteWins` (default)
- `firstWriteWins`
- `error` (keeps first value and records collision errors)

```ts
import { applyConversion, OutputCollisionPolicy } from "@apiconvert/core";

const result = applyConversion(input, rules, {
  collisionPolicy: OutputCollisionPolicy.Error
});
```

## Explain / Trace Mode

Enable deterministic per-rule tracing by passing `explain: true` to `applyConversion`.
`result.trace` contains ordered events with rule path, kind, decision, source value, and output paths.

```ts
const result = applyConversion(input, rules, { explain: true });

for (const entry of result.trace) {
  console.log(`${entry.rulePath} [${entry.ruleKind}] => ${entry.decision}`);
}
```

## Custom Transform Plugins

Register deterministic custom transform functions at runtime and reference them from rules using `source.customTransform`.

```ts
const rules = normalizeConversionRules({
  inputFormat: "json",
  outputFormat: "json",
  rules: [
    {
      kind: "field",
      outputPaths: ["user.code"],
      source: { type: "transform", path: "name", customTransform: "reverse" }
    }
  ]
});

const result = applyConversion({ name: "Ada" }, rules, {
  transforms: {
    reverse: (value) => String(value ?? "").split("").reverse().join("")
  }
});
```

## Schema Paths

Schema constants point to package-local files:
- `rulesSchemaPath` => `/schemas/rules/rules.schema.json`
- `rulesSchemaVersionedPath` => `/schemas/rules/v{version}/schema.json`

## Error Codes and Troubleshooting

Use the shared catalog for deterministic diagnostic mapping and remediation guidance:
- [`docs/error-code-catalog.md`](../../docs/error-code-catalog.md)
