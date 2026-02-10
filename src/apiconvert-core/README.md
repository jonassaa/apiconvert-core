# @apiconvert/core

TypeScript package for applying Apiconvert conversion rules to JSON, XML, and query payloads.

## Install

```bash
npm install @apiconvert/core
```

## Basic JSON -> JSON

```ts
import {
  applyConversion,
  DataFormat,
  formatPayload,
  normalizeConversionRules,
  parsePayload
} from "@apiconvert/core";

const rules = normalizeConversionRules({
  version: 2,
  inputFormat: DataFormat.Json,
  outputFormat: DataFormat.Json,
  fieldMappings: [
    {
      outputPath: "profile.name",
      source: { type: "path", path: "user.fullName" }
    }
  ]
});

const input = '{"user": {"fullName": "Ada Lovelace"}}';
const { value, error } = parsePayload(input, rules.inputFormat!);
if (error) throw new Error(error);

const result = applyConversion(value, rules);
if (result.errors.length) throw new Error(result.errors.join("; "));

const outputJson = formatPayload(result.output, rules.outputFormat!, true);
```

Input:

```json
{ "user": { "fullName": "Ada Lovelace" } }
```

Output:

```json
{ "profile": { "name": "Ada Lovelace" } }
```

## Load Rules From rules.json

```ts
import fs from "node:fs";
import {
  applyConversion,
  formatPayload,
  normalizeConversionRules,
  parsePayload
} from "@apiconvert/core";

const rulesText = fs.readFileSync("rules.json", "utf-8");
const rules = normalizeConversionRules(rulesText);

const input = fs.readFileSync("input.json", "utf-8");
const { value, error } = parsePayload(input, rules.inputFormat!);
if (error) throw new Error(error);

const result = applyConversion(value, rules);
if (result.errors.length) throw new Error(result.errors.join("; "));

const outputJson = formatPayload(result.output, rules.outputFormat!, true);
```

## XML Attributes And Text

XML attributes are addressed with `@_` and element text with `#text`.

```ts
const rules = normalizeConversionRules({
  version: 2,
  inputFormat: DataFormat.Xml,
  outputFormat: DataFormat.Json,
  fieldMappings: [
    { outputPath: "orderId", source: { type: "path", path: "order.@_id" } },
    { outputPath: "status", source: { type: "path", path: "order.status.#text" } }
  ]
});

const input = '<order id="A1"><status>new</status></order>';
```

Input:

```xml
<order id="A1">
  <status>new</status>
</order>
```

Output:

```json
{ "orderId": "A1", "status": "new" }
```

## Array Mapping

Array mapping paths support root-prefixed JSONPath-style syntax:
- `inputPath`: `orders` and `$.orders` are equivalent.
- `outputPath`: `orders` and `$.orders` are equivalent.

`$` resolves from the root input for reads, and `$.<path>` writes at the root output path.

```ts
const rules = normalizeConversionRules({
  version: 2,
  inputFormat: DataFormat.Json,
  outputFormat: DataFormat.Json,
  arrayMappings: [
    {
      inputPath: "$.orders",
      outputPath: "$.ordersNormalized",
      itemMappings: [
        { outputPath: "id", source: { type: "path", path: "orderId" } },
        { outputPath: "currency", source: { type: "path", path: "$.defaults.currency" } }
      ]
    }
  ]
});
```

Input:

```json
{
  "defaults": { "currency": "USD" },
  "orders": [
    { "orderId": "A1" }, 
    { "orderId": "A2" }
  ]
}
```

Output:

```json
{
  "ordersNormalized": [
    { "id": "A1", "currency": "USD" },
    { "id": "A2", "currency": "USD" }
  ]
}
```

## Transforms And Conditions

```ts
const rules = normalizeConversionRules({
  version: 2,
  inputFormat: DataFormat.Json,
  outputFormat: DataFormat.Json,
  fieldMappings: [
    {
      outputPath: "profile.country",
      source: { type: "transform", transform: "toUpperCase", path: "user.country" }
    },
    {
      outputPath: "profile.isAdult",
      source: {
        type: "condition",
        condition: { path: "user.age", operator: "gt", value: "18" },
        trueValue: "true",
        falseValue: "false"
      }
    }
  ]
});
```

Input:

```json
{ "user": { "country": "no", "age": 21 } }
```

Output:

```json
{ "profile": { "country": "NO", "isAdult": true } }
```

## Formatting

Use `formatPayload(value, DataFormat.Xml, true)` for indented XML and `false` for compact output. The same `pretty` flag controls JSON formatting.
