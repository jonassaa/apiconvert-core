# First Conversion (Both Runtimes)

This walkthrough uses the same scenario in .NET and TypeScript so you can compare APIs directly.

## Scenario

Input contains a user payload. We produce a customer payload with:

- direct field mapping
- a default fallback
- a branch based on status

## Rules

```json
{
  "inputFormat": "json",
  "outputFormat": "json",
  "rules": [
    {
      "kind": "field",
      "outputPaths": ["customer.name"],
      "source": { "type": "path", "path": "user.fullName" },
      "defaultValue": "Unknown"
    },
    {
      "kind": "branch",
      "expression": "path(status) == 'active'",
      "then": [
        {
          "kind": "field",
          "outputPaths": ["customer.enabled"],
          "source": { "type": "constant", "value": "true" }
        }
      ],
      "else": [
        {
          "kind": "field",
          "outputPaths": ["customer.enabled"],
          "source": { "type": "constant", "value": "false" }
        }
      ]
    }
  ]
}
```

## Input

```json
{
  "user": { "fullName": "Ada Lovelace" },
  "status": "active"
}
```

<div class="runtime-dotnet">

## .NET

```csharp
using Apiconvert.Core.Converters;

var rules = ConversionEngine.NormalizeConversionRulesStrict(File.ReadAllText("rules.json"));
var (input, parseError) = ConversionEngine.ParsePayload(File.ReadAllText("input.json"), rules.InputFormat);
if (parseError is not null) throw new FormatException(parseError);

var result = ConversionEngine.ApplyConversion(input, rules);
if (result.Errors.Count > 0) throw new InvalidOperationException(string.Join("; ", result.Errors));

Console.WriteLine(ConversionEngine.FormatPayload(result.Output, rules.OutputFormat, pretty: true));
```

</div>

<div class="runtime-typescript">

## TypeScript

```ts
import { readFile } from "node:fs/promises";
import {
  applyConversion,
  formatPayload,
  normalizeConversionRulesStrict,
  parsePayload
} from "@apiconvert/core";

const rules = normalizeConversionRulesStrict(await readFile("rules.json", "utf8"));
const parsed = parsePayload(await readFile("input.json", "utf8"), rules.inputFormat!);
if (parsed.error) throw new Error(parsed.error);

const result = applyConversion(parsed.value, rules);
if (result.errors.length > 0) throw new Error(result.errors.join("; "));

console.log(formatPayload(result.output, rules.outputFormat!, true));
```

</div>

## Expected output

```json
{
  "customer": {
    "name": "Ada Lovelace",
    "enabled": "true"
  }
}
```

## Learn more

- How rules are structured: [Rules model](../concepts/rules-model.md)
- All schema options: [Rules schema reference](../reference/rules-schema.md)
- More scenarios: [Recipes](../recipes/json-and-xml.md)
