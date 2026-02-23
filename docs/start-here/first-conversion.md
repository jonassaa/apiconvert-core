# First Conversion

This walkthrough maps `user.fullName` to `customer.name` and demonstrates normalize, parse, apply, and format.

## Rules

```json
{
  "inputFormat": "json",
  "outputFormat": "json",
  "rules": [
    {
      "kind": "field",
      "outputPaths": ["customer.name"],
      "source": { "type": "path", "path": "user.fullName" }
    }
  ]
}
```

## Input

```json
{
  "user": {
    "fullName": "Ada Lovelace"
  }
}
```

## Run conversion

<div class="runtime-dotnet">

<h3 id="first-conversion-dotnet">.NET</h3>

```csharp
using Apiconvert.Core.Converters;

var rulesJson = File.ReadAllText("rules.json");
var rules = ConversionEngine.NormalizeConversionRulesStrict(rulesJson);

var inputText = File.ReadAllText("input.json");
var (value, parseError) = ConversionEngine.ParsePayload(inputText, rules.InputFormat);
if (parseError is not null) throw new FormatException(parseError);

var result = ConversionEngine.ApplyConversion(value, rules);
if (result.Errors.Count > 0) throw new InvalidOperationException(string.Join("; ", result.Errors));

var output = ConversionEngine.FormatPayload(result.Output, rules.OutputFormat, pretty: true);
Console.WriteLine(output);
```

</div>

<div class="runtime-typescript">

<h3 id="first-conversion-typescript">TypeScript</h3>

```ts
import { readFile } from "node:fs/promises";
import {
  applyConversion,
  formatPayload,
  normalizeConversionRulesStrict,
  parsePayload
} from "@apiconvert/core";

const rulesJson = await readFile("rules.json", "utf8");
const rules = normalizeConversionRulesStrict(rulesJson);

const inputText = await readFile("input.json", "utf8");
const parsed = parsePayload(inputText, rules.inputFormat!);
if (parsed.error) throw new Error(parsed.error);

const result = applyConversion(parsed.value, rules);
if (result.errors.length > 0) throw new Error(result.errors.join("; "));

const output = formatPayload(result.output, rules.outputFormat!, true);
console.log(output);
```

</div>

## Expected output

```json
{
  "customer": {
    "name": "Ada Lovelace"
  }
}
```

## What to do next

1. Add branching and defaults: [/rules-reference/condition-expressions](/rules-reference/condition-expressions)
2. Add array mapping: [/rules-reference/node-types](/rules-reference/node-types)
3. Add quality gates: [/reference/validation-and-diagnostics](/reference/validation-and-diagnostics)
