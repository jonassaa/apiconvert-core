# Apiconvert.Core

Apiconvert.Core is the shared conversion engine for Apiconvert. It applies a rule model to structured payloads and produces deterministic output models that are consistent across the .NET library (NuGet) and the TypeScript package (npm).

This README is a comprehensive guide to the conversion engine, rule capabilities, and path syntax (including XML attributes and element text access).

## Capabilities At A Glance

- Parse and format `json`, `xml`, and `query` payloads.
- Map scalar fields and arrays using a declarative rule model.
- Built-in transforms: lowercase, uppercase, number, boolean, concat.
- Conditional value selection with comparison operators.
- Consistent rule schema shared across .NET and TypeScript.

## Quickstart

### .NET

```csharp
using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;

var rules = new ConversionRules
{
    InputFormat = DataFormat.Json,
    OutputFormat = DataFormat.Json,
    FieldMappings = new()
    {
        new FieldRule
        {
            OutputPath = "customer.name",
            Source = new ValueSource { Type = "path", Path = "user.fullName" }
        }
    }
};

var (value, error) = ConversionEngine.ParsePayload("{\"user\": {\"fullName\": \"Ada\"}}", rules.InputFormat);
if (error != null) throw new Exception(error);

var result = ConversionEngine.ApplyConversion(value, rules);
if (result.Errors.Count > 0) throw new Exception(string.Join("; ", result.Errors));

var outputJson = ConversionEngine.FormatPayload(result.Output, rules.OutputFormat, pretty: true);
```

### TypeScript

```ts
import {
  applyConversion,
  DataFormat,
  formatPayload,
  normalizeConversionRules,
  parsePayload
} from "apiconvert-core";

const rules = normalizeConversionRules({
  version: 2,
  inputFormat: DataFormat.Json,
  outputFormat: DataFormat.Json,
  fieldMappings: [
    {
      outputPath: "customer.name",
      source: { type: "path", path: "user.fullName" }
    }
  ]
});

const { value, error } = parsePayload("{\"user\": {\"fullName\": \"Ada\"}}", rules.inputFormat!);
if (error) throw new Error(error);

const result = applyConversion(value, rules);
if (result.errors.length) throw new Error(result.errors.join("; "));

const outputJson = formatPayload(result.output, rules.outputFormat!, true);
```

## Core Concepts

- `ConversionEngine` (C#) / `applyConversion` (TS) applies rules to an already-parsed payload.
- `ParsePayload` / `parsePayload` turns raw JSON/XML/query text into a structured object model.
- `FormatPayload` / `formatPayload` renders a structured object model to JSON/XML/query.
- `ConversionResult` always includes the output plus a list of conversion errors.

## Supported Formats

Supported `DataFormat` values:

- `json`
- `xml`
- `query`

All formats are parsed into a plain object model used by the rule engine. Output is always generated from that same model.

## Path Syntax & Resolution

Paths are dot-delimited and apply to the current object being read or written.

- Object property: `user.name`
- Array index: `items[0].id` or `items.0.id`
- Root shortcut: `$` (entire root object)
- Force root access: `$.user.name`

Inside array mappings, paths are resolved against the array item first. To always resolve from the root, prefix with `$` or `$.`.

## XML Access Patterns

XML is parsed into the same object model with two special conventions:

- Attributes become properties prefixed with `@_`.
- Element text becomes a `#text` property.
- Repeated child elements become arrays.

Example XML:

```xml
<order id="A1">
  <status>new</status>
  <line sku="X1">2</line>
  <line sku="X2">5</line>
</order>
```

Parsed path examples:

- `order.@_id` -> `A1`
- `order.status.#text` -> `new`
- `order.line[0].@_sku` -> `X1`
- `order.line[0].#text` -> `2`

When building XML output, those same keys are used to produce attributes and element text.

## Rules Model Overview

The canonical rule schema lives at `schemas/rules/rules.schema.json`.

Top-level `ConversionRules` fields:

- `version` (currently `2`)
- `inputFormat` / `outputFormat`
- `fieldMappings` (scalar field mappings)
- `arrayMappings` (array mappings)

## Field Rules

A field rule writes a single output value to an `outputPath`.

```json
{
  "outputPath": "profile.displayName",
  "source": { "type": "path", "path": "user.fullName" },
  "defaultValue": "Anonymous"
}
```

If the resolved source is `null` or an empty string, `defaultValue` is used.

## ValueSource Types

A `ValueSource` describes where a field value comes from.

### `path`

```json
{ "type": "path", "path": "user.email" }
```

### `constant`

```json
{ "type": "constant", "value": "internal" }
```

### `transform`

```json
{ "type": "transform", "transform": "toUpperCase", "path": "user.country" }
```

### `condition`

```json
{
  "type": "condition",
  "condition": { "path": "user.vip", "operator": "equals", "value": "true" },
  "trueValue": "gold",
  "falseValue": "standard"
}
```

## Transforms

Supported `transform` values:

- `toLowerCase`
- `toUpperCase`
- `number`
- `boolean`
- `concat`

`concat` uses a comma-separated list of tokens in `path`. Tokens are either input paths or string literals prefixed with `const:`.

```json
{
  "type": "transform",
  "transform": "concat",
  "path": "user.firstName, const: , user.lastName"
}
```

## Conditions

Supported operators:

- `exists`
- `equals`
- `notEquals`
- `includes`
- `gt`
- `lt`

Conditions resolve the input path, compare with `value`, then choose `trueValue` or `falseValue`.

## Array Mappings

Array mappings transform each item from an input array and write the mapped array to an output path.

```json
{
  "inputPath": "orders",
  "outputPath": "orderSummaries",
  "coerceSingle": true,
  "itemMappings": [
    { "outputPath": "id", "source": { "type": "path", "path": "orderId" } },
    { "outputPath": "total", "source": { "type": "path", "path": "amount" } }
  ]
}
```

Notes:

- `itemMappings` are resolved relative to the current array item by default.
- `coerceSingle` treats a non-array value as a single-item array.

## Error Handling & Validation

The conversion engine returns errors such as:

- Missing required output paths.
- Array rules whose input path did not resolve to an array.

Use the schema at `schemas/rules/rules.schema.json` to validate rules externally.

## Examples

**Basics**

### JSON -> JSON (simple field mapping)

Input payload:

```json
{
  "user": {
    "fullName": "Ada Lovelace"
  }
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
      "outputPath": "profile.name",
      "source": { "type": "path", "path": "user.fullName" }
    }
  ]
}
```

Output payload:

```json
{
  "profile": {
    "name": "Ada Lovelace"
  }
}
```

### Query -> JSON

Input query: `?user.name=Ada&user.active=true`

Rules:

```json
{
  "version": 2,
  "inputFormat": "query",
  "outputFormat": "json",
  "fieldMappings": [
    {
      "outputPath": "profile.name",
      "source": { "type": "path", "path": "user.name" }
    },
    {
      "outputPath": "profile.active",
      "source": { "type": "path", "path": "user.active" }
    }
  ]
}
```

Output payload:

```json
{
  "profile": {
    "name": "Ada",
    "active": true
  }
}
```

### XML -> JSON (attributes + text)

Input payload:

```xml
<order id="A1">
  <status>new</status>
</order>
```

Rules:

```json
{
  "version": 2,
  "inputFormat": "xml",
  "outputFormat": "json",
  "fieldMappings": [
    {
      "outputPath": "orderId",
      "source": { "type": "path", "path": "order.@_id" }
    },
    {
      "outputPath": "status",
      "source": { "type": "path", "path": "order.status.#text" }
    }
  ]
}
```

Output payload:

```json
{
  "orderId": "A1",
  "status": "new"
}
```

**Value Sources**

### Constant Value Source

Input payload:

```json
{
  "user": {
    "name": "Ada"
  }
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
      "outputPath": "profile.name",
      "source": { "type": "path", "path": "user.name" }
    },
    {
      "outputPath": "profile.role",
      "source": { "type": "constant", "value": "admin" }
    }
  ]
}
```

Output payload:

```json
{
  "profile": {
    "name": "Ada",
    "role": "admin"
  }
}
```

### Transform Sources (toUpperCase, number, boolean)

Input payload:

```json
{
  "user": {
    "country": "se",
    "age": "42",
    "emailVerified": "true"
  }
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
      "outputPath": "profile.country",
      "source": { "type": "transform", "transform": "toUpperCase", "path": "user.country" }
    },
    {
      "outputPath": "profile.age",
      "source": { "type": "transform", "transform": "number", "path": "user.age" }
    },
    {
      "outputPath": "profile.verified",
      "source": { "type": "transform", "transform": "boolean", "path": "user.emailVerified" }
    }
  ]
}
```

Output payload:

```json
{
  "profile": {
    "country": "SE",
    "age": 42,
    "verified": true
  }
}
```

### Transform Source (concat)

Input payload:

```json
{
  "user": {
    "firstName": "Ada",
    "lastName": "Lovelace"
  }
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
    }
  ]
}
```

Output payload:

```json
{
  "profile": {
    "displayName": "Ada Lovelace"
  }
}
```

**Conditions**

### Condition Source (equals)

Input payload:

```json
{
  "user": {
    "tier": "pro"
  }
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
      "outputPath": "profile.plan",
      "source": {
        "type": "condition",
        "condition": {
          "path": "user.tier",
          "operator": "equals",
          "value": "pro"
        },
        "trueValue": "paid",
        "falseValue": "free"
      }
    }
  ]
}
```

Output payload:

```json
{
  "profile": {
    "plan": "paid"
  }
}
```

### Condition Source (notEquals)

Input payload:

```json
{
  "user": {
    "status": "trial"
  }
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
      "outputPath": "profile.isActive",
      "source": {
        "type": "condition",
        "condition": {
          "path": "user.status",
          "operator": "notEquals",
          "value": "disabled"
        },
        "trueValue": "true",
        "falseValue": "false"
      }
    }
  ]
}
```

Output payload:

```json
{
  "profile": {
    "isActive": true
  }
}
```

### Condition Source (exists)

Input payload:

```json
{
  "user": {
    "email": "ada@example.com"
  }
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
      "outputPath": "profile.hasEmail",
      "source": {
        "type": "condition",
        "condition": {
          "path": "user.email",
          "operator": "exists"
        },
        "trueValue": "true",
        "falseValue": "false"
      }
    }
  ]
}
```

Output payload:

```json
{
  "profile": {
    "hasEmail": true
  }
}
```

### Condition Source (includes)

Input payload:

```json
{
  "user": {
    "tags": ["beta", "vip"]
  }
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
      "outputPath": "profile.isVip",
      "source": {
        "type": "condition",
        "condition": {
          "path": "user.tags",
          "operator": "includes",
          "value": "vip"
        },
        "trueValue": "true",
        "falseValue": "false"
      }
    }
  ]
}
```

Output payload:

```json
{
  "profile": {
    "isVip": true
  }
}
```

### Condition Source (gt / lt)

Input payload:

```json
{
  "user": {
    "age": 21
  }
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
      "outputPath": "profile.isAdult",
      "source": {
        "type": "condition",
        "condition": {
          "path": "user.age",
          "operator": "gt",
          "value": "17"
        },
        "trueValue": "true",
        "falseValue": "false"
      }
    },
    {
      "outputPath": "profile.isTeen",
      "source": {
        "type": "condition",
        "condition": {
          "path": "user.age",
          "operator": "lt",
          "value": "20"
        },
        "trueValue": "true",
        "falseValue": "false"
      }
    }
  ]
}
```

Output payload:

```json
{
  "profile": {
    "isAdult": true,
    "isTeen": false
  }
}
```

### XML Condition Source (exists)

Input payload:

```xml
<account id="A1">
  <email>ada@example.com</email>
</account>
```

Rules:

```json
{
  "version": 2,
  "inputFormat": "xml",
  "outputFormat": "json",
  "fieldMappings": [
    {
      "outputPath": "hasEmail",
      "source": {
        "type": "condition",
        "condition": {
          "path": "account.email.#text",
          "operator": "exists"
        },
        "trueValue": "true",
        "falseValue": "false"
      }
    }
  ]
}
```

Output payload:

```json
{
  "hasEmail": true
}
```

**Arrays**

### Array Mapping With Root Access

Input payload:

```json
{
  "defaults": {
    "currency": "USD"
  },
  "orders": [
    { "orderId": "A1" },
    { "orderId": "A2" }
  ]
}
```

Rules:

```json
{
  "version": 2,
  "inputFormat": "json",
  "outputFormat": "json",
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

Output payload:

```json
{
  "orders": [
    { "id": "A1", "currency": "USD" },
    { "id": "A2", "currency": "USD" }
  ]
}
```

**Defaults**

### Default Value When Source Is Missing

Input payload:

```json
{
  "user": {
    "name": "Ada"
  }
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
      "outputPath": "profile.name",
      "source": { "type": "path", "path": "user.name" }
    },
    {
      "outputPath": "profile.timezone",
      "source": { "type": "path", "path": "user.timezone" },
      "defaultValue": "UTC"
    }
  ]
}
```

Output payload:

```json
{
  "profile": {
    "name": "Ada",
    "timezone": "UTC"
  }
}
```
