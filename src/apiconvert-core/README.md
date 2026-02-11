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

## Split And Merge Field Rules

Use `outputPaths` to split one source value into multiple output fields.  
Use `source.type = "merge"` with `paths` to combine multiple inputs into one output.

```ts
const rules = normalizeConversionRules({
  version: 2,
  inputFormat: DataFormat.Json,
  outputFormat: DataFormat.Json,
  fieldMappings: [
    {
      outputPaths: ["profile.name", "profile.displayName"],
      source: { type: "path", path: "user.name" }
    },
    {
      outputPath: "profile.fullName",
      source: {
        type: "merge",
        paths: ["user.firstName", "user.lastName"],
        mergeMode: "concat",
        separator: " "
      }
    }
  ]
});
```

Input:

```json
{
  "user": {
    "name": "Ada Lovelace",
    "firstName": "Ada",
    "lastName": "Lovelace"
  }
}
```

Output:

```json
{
  "profile": {
    "name": "Ada Lovelace",
    "displayName": "Ada Lovelace",
    "fullName": "Ada Lovelace"
  }
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

For splitting a full name into parts, use `transform: "split"`, `separator`, and `tokenIndex`.
`tokenIndex` supports negative values (`-1` = last token):
`trimAfterSplit` defaults to `true` and can be set to `false` to preserve token whitespace.
`separator: ""` is invalid for split transforms.

```ts
const rules = normalizeConversionRules({
  version: 2,
  inputFormat: DataFormat.Json,
  outputFormat: DataFormat.Json,
  fieldMappings: [
    {
      outputPath: "firstName",
      source: {
        type: "transform",
        transform: "split",
        path: "name",
        separator: " ",
        tokenIndex: 0
      }
    },
    {
      outputPath: "lastName",
      source: {
        type: "transform",
        transform: "split",
        path: "name",
        separator: " ",
        tokenIndex: -1
      }
    }
  ]
});
```

Input:

```json
{ "name": "Jonas Strand Aasberg" }
```

Output:

```json
{ "firstName": "Jonas", "lastName": "Aasberg" }
```

Comma-separated names are supported as well. With default trim (`trimAfterSplit` omitted or `true`):

```json
{ "name": "Jonas, Aasberg" }
```

maps to:

```json
{ "firstName": "Jonas", "lastName": "Aasberg" }
```

With `trimAfterSplit: false`, whitespace is preserved:

```json
{ "firstName": "Jonas", "lastName": " Aasberg" }
```

## Formatting

Use `formatPayload(value, DataFormat.Xml, true)` for indented XML and `false` for compact output. The same `pretty` flag controls JSON formatting.
