# First Conversion

This walkthrough maps `user.fullName` to `customer.name` using one `field` rule.

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

<div class="runtime-dotnet">

## .NET

```csharp
using Apiconvert.Core.Converters;

var rules = ConversionEngine.NormalizeConversionRulesStrict(rulesJson);
var (value, error) = ConversionEngine.ParsePayload("{\"user\":{\"fullName\":\"Ada\"}}", rules.InputFormat);
if (error is not null) throw new Exception(error);

var result = ConversionEngine.ApplyConversion(value, rules);
if (result.Errors.Count > 0) throw new Exception(string.Join("; ", result.Errors));

var output = ConversionEngine.FormatPayload(result.Output, rules.OutputFormat, pretty: true);
```

</div>

<div class="runtime-typescript">

## TypeScript

```ts
import { normalizeConversionRulesStrict, parsePayload, applyConversion, formatPayload } from "@apiconvert/core";

const rules = normalizeConversionRulesStrict(rulesJson);
const { value, error } = parsePayload('{"user":{"fullName":"Ada"}}', rules.inputFormat!);
if (error) throw new Error(error);

const result = applyConversion(value, rules);
if (result.errors.length > 0) throw new Error(result.errors.join("; "));

const output = formatPayload(result.output, rules.outputFormat!, true);
```

</div>
