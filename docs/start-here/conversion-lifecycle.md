# Conversion Lifecycle

This is the practical conversion lifecycle to run Apiconvert.Core safely in production.

## Lifecycle phases

1. rules intake
2. rule validation + normalization
3. plan compilation and caching
4. input parsing
5. conversion execution
6. output formatting
7. diagnostics and troubleshooting

## 1) Rules intake

Rules can be authored as canonical schema nodes and runtime-supported shorthands.

- Canonical nodes: `field`, `array`, `branch`, `use`
- Runtime shorthands: `map`, `from`, `to`, `outputPath`, `const`, `as`

Use canonical form for long-term pinned artifacts; use shorthands for authoring convenience.

## 2) Validation and normalization

Use strict normalization before deploying rules.

<div class="runtime-dotnet">

```csharp
var rules = ConversionEngine.NormalizeConversionRulesStrict(rulesJson);
```

</div>

<div class="runtime-typescript">

```ts
const rules = normalizeConversionRulesStrict(rulesJson);
```

</div>

Strict mode blocks invalid rules early and keeps runtime execution predictable.

## 3) Plan compilation and caching

Compile once for repeated use, cache by rules cache key.

<div class="runtime-dotnet">

```csharp
var plan = ConversionEngine.CompileConversionPlan(rulesJson);
var cacheKey = plan.CacheKey;
```

</div>

<div class="runtime-typescript">

```ts
const plan = compileConversionPlan(rulesJson);
const cacheKey = computeRulesCacheKey(rulesJson);
```

</div>

## 4) Input parsing

Always parse using declared `inputFormat` (`json`, `xml`, `query`).

<div class="runtime-dotnet">

```csharp
var (input, parseError) = ConversionEngine.ParsePayload(inputText, rules.InputFormat);
if (parseError is not null) throw new Exception(parseError);
```

</div>

<div class="runtime-typescript">

```ts
const parsed = parsePayload(inputText, rules.inputFormat!);
if (parsed.error) throw new Error(parsed.error);
```

</div>

## 5) Conversion execution

Use explicit collision policy when needed.

<div class="runtime-dotnet">

```csharp
var result = ConversionEngine.ApplyConversion(input, rules, new ConversionOptions
{
  CollisionPolicy = OutputCollisionPolicy.Error
});
```

</div>

<div class="runtime-typescript">

```ts
const result = applyConversion(parsed.value, rules, {
  collisionPolicy: OutputCollisionPolicy.Error
});
```

</div>

## 6) Output formatting

Format output by `outputFormat`.

<div class="runtime-dotnet">

```csharp
var output = ConversionEngine.FormatPayload(result.Output, rules.OutputFormat, pretty: true);
```

</div>

<div class="runtime-typescript">

```ts
const output = formatPayload(result.output, rules.outputFormat!, true);
```

</div>

## 7) Diagnostics flow

Recommended checks for every rule change:

1. strict normalize
2. lint
3. rule doctor
4. sample conversion run

For runtime failures, start from error code + rule path and inspect expression/source resolution.
