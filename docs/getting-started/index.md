# Getting Started

This page gets you from install to a working first conversion. Runtime-specific snippets are shown with runtime visibility classes.

## 1. Install

<div class="runtime-dotnet">

```bash
dotnet add package Apiconvert.Core
```

</div>

<div class="runtime-typescript">

```bash
npm install @apiconvert/core
```

`pnpm add @apiconvert/core` and `yarn add @apiconvert/core` also work.

</div>

## 2. Add imports

<div class="runtime-dotnet">

```csharp
using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;
```

</div>

<div class="runtime-typescript">

```ts
import {
  applyConversion,
  formatPayload,
  normalizeConversionRulesStrict,
  parsePayload
} from "@apiconvert/core";
```

</div>

## 3. Run a hello-world conversion

<div class="runtime-dotnet">

```csharp
using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;

var rulesJson = """
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
""";

var rules = ConversionEngine.NormalizeConversionRulesStrict(rulesJson);
var (input, parseError) = ConversionEngine.ParsePayload(
    """{ "user": { "fullName": "Ada Lovelace" } }""",
    rules.InputFormat);

if (parseError is not null) throw new FormatException(parseError);

var result = ConversionEngine.ApplyConversion(input, rules);
if (result.Errors.Count > 0) throw new InvalidOperationException(string.Join("; ", result.Errors));

var output = ConversionEngine.FormatPayload(result.Output, rules.OutputFormat, pretty: true);
Console.WriteLine(output);
```

</div>

<div class="runtime-typescript">

```ts
import {
  applyConversion,
  formatPayload,
  normalizeConversionRulesStrict,
  parsePayload
} from "@apiconvert/core";

const rulesJson = `
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
}`;

const rules = normalizeConversionRulesStrict(rulesJson);
const parsed = parsePayload('{ "user": { "fullName": "Ada Lovelace" } }', rules.inputFormat!);
if (parsed.error) throw new Error(parsed.error);

const result = applyConversion(parsed.value, rules);
if (result.errors.length > 0) throw new Error(result.errors.join("; "));

const output = formatPayload(result.output, rules.outputFormat!, true);
console.log(output);
```

</div>

## Next steps

- Shared concepts: [Rules model](../concepts/rules-model.md)
- First full walkthrough: [First conversion](./first-conversion.md)
- More examples: [Hello world recipe](../recipes/hello-world.md)
- API surface: [Runtime APIs](../guides/runtime-api.md)
- Rules authoring details: [Rules schema reference](../reference/rules-schema.md)
