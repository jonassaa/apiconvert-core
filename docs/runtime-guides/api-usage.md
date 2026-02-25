# API Usage

This page is the unified runtime API reference. Use the runtime selector in the top navigation to view the .NET or TypeScript snippets.

## Core workflow

<div class="runtime-dotnet">

- `ConversionEngine.NormalizeConversionRulesStrict(raw)`
- `ConversionEngine.ParsePayload(text, format)`
- `ConversionEngine.ParsePayload(stream, format, encoding?, leaveOpen?)`
- `ConversionEngine.ParsePayload(jsonNode, DataFormat.Json)`
- `ConversionEngine.ApplyConversion(input, rules, options?)`
- `ConversionEngine.FormatPayload(output, format, pretty)`
- `ConversionEngine.FormatPayload(output, format, stream, pretty, encoding?, leaveOpen?)`

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
- `ConversionEngine.BundleRules(entryRulesPath, options?)` (`RuleBundleOptions.BaseDirectory` supported)

</div>

<div class="runtime-typescript">

- `lintConversionRules(rawRules)`
- `runRuleDoctor(rawRules, options?)`
- `checkRulesCompatibility(rawRules, { targetVersion })`
- `validateConversionRules(rawRules)`

</div>

## Options and model contracts

<div class="runtime-dotnet">

- `ConversionOptions`: `CollisionPolicy`, `Explain`, `TransformRegistry`
- `StreamConversionOptions`: `InputKind`, `ErrorMode`, optional `Encoding`, required `XmlItemPath` for `XmlElements`
- `RuleLintDiagnostic`, `RuleDoctorReport`, `RulesCompatibilityReport`, `ConversionProfileReport`

</div>

<div class="runtime-typescript">

- `ApplyConversionOptions`: `collisionPolicy`, `explain`, `transforms`
- `StreamConversionOptions`: `inputKind`, `errorMode`, `xmlItemPath`
- `ConversionRulesLintResult`: `{ diagnostics, hasErrors }`
- `RuleDoctorOptions`: `{ sampleInputText, inputFormat, applySafeFixes }`
- `RulesCompatibilityOptions`: `{ targetVersion }`
- `RuleDoctorReport`, `RulesCompatibilityReport`, `ConversionProfileReport`

</div>

## Generation contracts

<div class="runtime-dotnet">

- `ConversionRulesGenerationRequest`: `InputFormat`, `OutputFormat`, `InputSample`, `OutputSample`, optional `Model`
- `IConversionRulesGenerator.GenerateAsync(request, cancellationToken)`

</div>

<div class="runtime-typescript">

- `ConversionRulesGenerationRequest`: `inputFormat`, `outputFormat`, `inputSample`, `outputSample`, optional `model`
- `ConversionRulesGenerator.generate(request, { signal? })`

</div>

## Schema constants (TypeScript)

- `rulesSchemaPath`
- `rulesSchemaVersion`
- `rulesSchemaVersionedPath`
