# Runtime APIs

This page documents each public runtime API with a short explanation of what it does and when to use it.

## Core conversion

<div class="runtime-dotnet">

```csharp
ConversionEngine.NormalizeConversionRules(raw)
```

Normalizes raw rule input into canonical `ConversionRules` and collects validation errors instead of throwing.

```csharp
ConversionEngine.NormalizeConversionRulesStrict(raw)
```

Same normalization flow, but throws when rules are invalid. Use this in CI or production startup.

```csharp
ConversionEngine.ApplyConversion(input, rawRules, options?)
```

One-step convert path that normalizes rules and applies them to an already parsed input object.

```csharp
ConversionEngine.ApplyConversion(input, plan, options?)
```

Applies a precompiled `ConversionPlan` to input. Use for repeated conversions with the same rules.

```csharp
ConversionEngine.ParsePayload(text, format)
```

Parses text payload (`json`, `xml`, `query`) into runtime objects and returns `(Value, Error)`.

```csharp
ConversionEngine.FormatPayload(value, format, pretty)
```

Formats converted output back to text (`json`, `xml`, `query`).

</div>

<div class="runtime-typescript">

```ts
normalizeConversionRules(raw)
```

Normalizes raw rule input and keeps validation errors in `rules.validationErrors`.

```ts
normalizeConversionRulesStrict(raw)
```

Normalizes and throws on validation errors.

```ts
applyConversion(input, rawRules, options?)
```

One-step convert path that normalizes rules and applies them to an already parsed input value.

```ts
runConversionCase({ rulesText, inputText, inputExtension, outputExtension })
```

Convenience helper for case-runner style workflows using text inputs and file extensions.

```ts
parsePayload(text, format)
```

Parses text payload (`json`, `xml`, `query`) into runtime values and returns `{ value, error? }`.

```ts
formatPayload(value, format, pretty)
```

Formats output runtime value back to text (`json`, `xml`, `query`).

</div>

## Rules tooling

<div class="runtime-dotnet">

```csharp
ConversionEngine.FormatConversionRules(rawRules, pretty?)
```

Converts rules into deterministic canonical JSON for stable diffs.

```csharp
ConversionEngine.LintRules(rawRules)
```

Runs lint diagnostics (code, severity, rule path, message, suggestion) without executing conversion.

```csharp
ConversionEngine.RunRuleDoctor(rawRules, sampleInputText?, inputFormat?, applySafeFixes?)
```

Combines validation, lint, and optional runtime checks into one report.

```csharp
ConversionEngine.CheckCompatibility(rawRules, targetVersion)
```

Verifies rules compatibility against a target schema/runtime version.

```csharp
ConversionEngine.BundleRules(entryRulesPath, options?)
```

Resolves `include` chains into one bundled rules object.

</div>

<div class="runtime-typescript">

```ts
formatConversionRules(rawRules, options?)
```

Produces canonical deterministic JSON for rules.

```ts
lintConversionRules(rawRules)
```

Returns lint diagnostics without running conversion.

```ts
runRuleDoctor(rawRules, options?)
```

Runs combined validation/lint/runtime diagnostics.

```ts
checkRulesCompatibility(rawRules, { targetVersion })
```

Checks if the rules are compatible with a target version.

```ts
bundleConversionRules(entryRulesPath, options?)
```

Resolves `include` references and returns a bundled rules object.

</div>

## Plans and performance

<div class="runtime-dotnet">

```csharp
ConversionEngine.CompileConversionPlan(rawRules)
```

Normalizes once and returns reusable `ConversionPlan` with stable `CacheKey`.

```csharp
ConversionEngine.CompileConversionPlanStrict(rawRules)
```

Strict version of plan compilation; throws if rules are invalid.

```csharp
ConversionEngine.ComputeRulesCacheKey(rawRules)
```

Computes a stable key for caching compiled plans.

```csharp
ConversionEngine.ProfileConversionPlan(rawRulesOrPlan, inputs, options?)
```

Benchmarks compile/apply latency and returns percentile metrics.

```csharp
ConversionPlan.Apply(input, options?)
```

Applies already compiled rules to one input payload.

</div>

<div class="runtime-typescript">

```ts
compileConversionPlan(rawRules)
```

Creates a reusable plan with `cacheKey` and `apply`.

```ts
computeRulesCacheKey(rawRules)
```

Computes deterministic cache key for plan reuse.

```ts
profileConversionPlan(rawRulesOrPlan, inputs, options?)
```

Profiles conversion latency across iterations.

```ts
CompiledConversionPlan.apply(input, options?)
```

Applies the compiled plan to one input value.

</div>

## Streaming

<div class="runtime-dotnet">

```csharp
ConversionEngine.StreamJsonArrayConversionAsync(stream, rawRules, options?, ct?)
```

Converts each item from a top-level JSON array stream.

```csharp
ConversionEngine.StreamConversionAsync(stream, rawRules, streamOptions?, options?, ct?)
```

General streaming converter for `JsonArray`, `Ndjson`, `QueryLines`, and `XmlElements` modes.

</div>

<div class="runtime-typescript">

```ts
streamJsonArrayConversion(items, rawRules, options?)
```

Converts each item from iterable/async-iterable JSON array items.

```ts
streamConversion(input, rawRules, streamOptions?, options?)
```

General streaming converter for `jsonArray`, `ndjson`, `queryLines`, and `xmlElements`.

</div>

See [Streaming guide](./streaming.md).

## Contracts for rules generation integration

These are contracts only; the package does not ship an external AI/model provider implementation.

<div class="runtime-dotnet">

```csharp
ConversionRulesGenerationRequest
```

Request model for generating rules from example input/output payloads.

```csharp
IConversionRulesGenerator
```

Interface for pluggable rule generators.

</div>

<div class="runtime-typescript">

```ts
ConversionRulesGenerationRequest
```

Request shape for generation workflows.

```ts
ConversionRulesGenerator
```

Interface for custom async rule generator implementations.

</div>

## Related references

- [Rules schema reference](../reference/rules-schema.md)
- [Validation and diagnostics](../reference/validation-and-diagnostics.md)
- [CLI reference](../reference/cli.md)
