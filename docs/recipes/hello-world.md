# Hello World Recipe

This is the smallest useful conversion example with both runtime snippets in one page.

## Input

```json
{ "user": { "fullName": "Ada Lovelace" } }
```

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

## Runner

<div class="runtime-dotnet">

```csharp
using Apiconvert.Core.Converters;

var rules = ConversionEngine.NormalizeConversionRulesStrict(File.ReadAllText("rules.json"));
var (value, error) = ConversionEngine.ParsePayload(File.ReadAllText("input.json"), rules.InputFormat);
if (error is not null) throw new Exception(error);

var result = ConversionEngine.ApplyConversion(value, rules);
if (result.Errors.Count > 0) throw new Exception(string.Join("; ", result.Errors));

Console.WriteLine(ConversionEngine.FormatPayload(result.Output, rules.OutputFormat, pretty: true));
```

</div>

<div class="runtime-typescript">

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

## Output

```json
{
  "customer": {
    "name": "Ada Lovelace"
  }
}
```
