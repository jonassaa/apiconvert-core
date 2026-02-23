# API Usage

This page is the unified runtime API reference. Use the runtime selector in the top navigation to view the .NET or TypeScript snippets.

## Core workflow

<div class="runtime-dotnet">

- `ConversionEngine.NormalizeConversionRulesStrict(raw)`
- `ConversionEngine.ParsePayload(text, format)`
- `ConversionEngine.ApplyConversion(input, rules, options?)`
- `ConversionEngine.FormatPayload(output, format, pretty)`

```csharp
using Apiconvert.Core.Converters;

var rules = ConversionEngine.NormalizeConversionRulesStrict(rulesJson);
var parsed = ConversionEngine.ParsePayload(inputText, rules.InputFormat);
if (parsed.Error is not null) throw new Exception(parsed.Error);

var result = ConversionEngine.ApplyConversion(parsed.Value, rules);
var output = ConversionEngine.FormatPayload(result.Output, rules.OutputFormat, pretty: true);
```

</div>

<div class="runtime-typescript">

- `normalizeConversionRulesStrict(raw)`
- `parsePayload(text, format)`
- `applyConversion(input, rules, options?)`
- `formatPayload(output, format, pretty)`

```ts
import {
  applyConversion,
  formatPayload,
  normalizeConversionRulesStrict,
  parsePayload
} from "@apiconvert/core";

const rules = normalizeConversionRulesStrict(rulesJson);
const parsed = parsePayload(inputText, rules.inputFormat!);
if (parsed.error) throw new Error(parsed.error);

const result = applyConversion(parsed.value, rules);
const output = formatPayload(result.output, rules.outputFormat!, true);
```

</div>

## Plan compilation and cache keys

<div class="runtime-dotnet">

- `ConversionEngine.CompileConversionPlan(rawRules)`
- `ConversionEngine.CompileConversionPlanStrict(rawRules)`
- `ConversionEngine.ComputeRulesCacheKey(rawRules)`

```csharp
var plan = ConversionEngine.CompileConversionPlan(rulesJson);
var result = plan.Apply(parsed.Value);
Console.WriteLine(plan.CacheKey);
```

</div>

<div class="runtime-typescript">

- `compileConversionPlan(rawRules)`
- `computeRulesCacheKey(rawRules)`

```ts
import { compileConversionPlan, computeRulesCacheKey } from "@apiconvert/core";

const plan = compileConversionPlan(rulesJson);
const result = plan.apply(parsed.value);
const cacheKey = computeRulesCacheKey(rulesJson);
```

</div>

## Diagnostics and quality APIs

<div class="runtime-dotnet">

- `ConversionEngine.LintRules(rawRules)`
- `ConversionEngine.RunRuleDoctor(rawRules, options?)`
- `ConversionEngine.CheckCompatibility(rawRules, targetVersion)`

</div>

<div class="runtime-typescript">

- `lintConversionRules(rawRules)`
- `runRuleDoctor(rawRules, options?)`
- `checkRulesCompatibility(rawRules, { targetVersion })`

</div>

