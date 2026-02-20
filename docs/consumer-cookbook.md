# Consumer Cookbook

Copy-paste oriented rule recipes for first-time adopters.
Every recipe maps to an existing shared case under `tests/cases` so behavior stays parity-verified across `.NET` and `npm`.

## How to use this cookbook

1. Copy the rule snippet.
2. Start from the referenced shared case input/output.
3. Run both test suites or parity gate after edits.

## Recipe Index

| Recipe | Shared Case | Primary Primitive |
| --- | --- | --- |
| Field map with default value | `tests/cases/json-defaults-constants` | `field` + `defaultValue` |
| Conditional branch with elseIf | `tests/cases/json-branch-elseif-else` | `branch` |
| Array mapping with coerceSingle | `tests/cases/json-array-coerce` | `array` |
| Merge with firstNonEmpty | `tests/cases/json-field-merge` | `source.type=merge` |
| Split + token extraction | `tests/cases/json-field-split` | `transform=split` |
| Concat with constants | `tests/cases/json-transform-concat` | `transform=concat` |
| Query list mapping | `tests/cases/json-to-query-lists` | `query` output |
| XML attribute mapping | `tests/cases/xml-attributes-to-json` | XML pathing |
| Collision policy pattern | `tests/cases/json-field-outputpaths-only` | collision options |
| Trace-mode debugging | `tests/cases/json-purchase-order-detailed` | `explain/trace` |

## 1) Field map with default value

Scenario: source field may be missing but output requires a fallback.

```json
{
  "kind": "field",
  "outputPaths": ["customer.email"],
  "source": { "type": "path", "path": "user.email" },
  "defaultValue": "unknown@example.com"
}
```

Pitfall: omitting `defaultValue` can produce null/empty writes.
Parity note: same result expected in `.NET` and `npm`.

## 2) Conditional branch with elseIf

Scenario: map status by tiered condition.

```json
{
  "kind": "branch",
  "expression": "path(score) >= 90",
  "then": [{ "kind": "field", "outputPaths": ["grade"], "source": { "type": "constant", "value": "A" } }],
  "elseIf": [
    { "expression": "path(score) >= 75", "then": [{ "kind": "field", "outputPaths": ["grade"], "source": { "type": "constant", "value": "B" } }] }
  ],
  "else": [{ "kind": "field", "outputPaths": ["grade"], "source": { "type": "constant", "value": "C" } }]
}
```

Pitfall: non-deterministic expressions or typos in `path(...)`.
Parity note: branch ordering is deterministic by rule index.

## 3) Array mapping with coerceSingle

Scenario: payload alternates between single object and array.

```json
{
  "kind": "array",
  "inputPath": "items",
  "outputPaths": ["order.lines"],
  "coerceSingle": true,
  "itemRules": [
    {
      "kind": "field",
      "outputPaths": ["sku"],
      "source": { "type": "path", "path": "id" }
    }
  ]
}
```

Pitfall: forgetting `coerceSingle` when upstream sends a single object.
Parity note: coercion behavior is shared in both runtimes.

## 4) Merge with firstNonEmpty

Scenario: pick the first available identity field.

```json
{
  "kind": "field",
  "outputPaths": ["customer.id"],
  "source": {
    "type": "merge",
    "paths": ["legacyId", "crmId", "externalId"],
    "mergeMode": "firstNonEmpty"
  }
}
```

Pitfall: ordering of `paths` changes business outcome.
Parity note: path precedence is identical across runtimes.

## 5) Split + token extraction

Scenario: parse tokenized values from a source string.

```json
{
  "kind": "field",
  "outputPaths": ["customer.firstName"],
  "source": {
    "type": "transform",
    "path": "fullName",
    "transform": "split",
    "separator": " ",
    "tokenIndex": 0,
    "trimAfterSplit": true
  }
}
```

Pitfall: wrong `tokenIndex` silently returns empty when index is out of range.
Parity note: split normalization and trimming are parity-tested.

## 6) Concat with constants

Scenario: compose values and literals into one output field.

```json
{
  "kind": "field",
  "outputPaths": ["displayName"],
  "source": {
    "type": "transform",
    "path": "firstName,lastName,const:(VIP)",
    "transform": "concat",
    "separator": " "
  }
}
```

Pitfall: forgetting `const:` prefix for literals.
Parity note: concat token parsing is shared by both runtimes.

## 7) Query list mapping

Scenario: map repeated values into query output.

Use: `tests/cases/json-to-query-lists`.
Pitfall: list ordering changes generated query output text.
Parity note: query formatting order is normalized in parity checks.

## 8) XML attribute mapping

Scenario: read attributes and move into JSON fields.

Use: `tests/cases/xml-attributes-to-json`.
Pitfall: confusion between element text and attribute paths.
Parity note: attribute extraction is covered in shared XML cases.

## 9) Collision policy pattern

Scenario: two rules intentionally target same output path.

`.NET`:

```csharp
var result = ConversionEngine.ApplyConversion(input, rules,
    new ConversionOptions { CollisionPolicy = OutputCollisionPolicy.Error });
```

`npm`:

```ts
const result = applyConversion(input, rules, { collisionPolicy: OutputCollisionPolicy.Error });
```

Pitfall: relying on implicit overwrite semantics without explicit policy.
Parity note: collision policy has dedicated parity tests.

## 10) Trace-mode debugging pattern

Scenario: explain why a field is missing or overwritten.

`.NET`:

```csharp
var result = ConversionEngine.ApplyConversion(input, rules, new ConversionOptions { Explain = true });
```

`npm`:

```ts
const result = applyConversion(input, rules, { explain: true });
```

Pitfall: inspecting only final output hides branch decisions.
Parity note: trace ordering is deterministic and parity-tested.

## Invocation snippets

`.NET`:

```csharp
var rules = ConversionEngine.NormalizeConversionRulesStrict(File.ReadAllText("rules.json"));
var (input, parseError) = ConversionEngine.ParsePayload(File.ReadAllText("input.json"), rules.InputFormat);
var result = ConversionEngine.ApplyConversion(input, rules);
```

`npm`:

```ts
const rules = normalizeConversionRulesStrict(readFileSync("rules.json", "utf8"));
const parsed = parsePayload(readFileSync("input.json", "utf8"), rules.inputFormat!);
const result = applyConversion(parsed.value, rules);
```
