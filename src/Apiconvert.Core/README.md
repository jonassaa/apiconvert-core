# Apiconvert.Core

`Apiconvert.Core` is the .NET conversion engine for JSON, XML, and query payloads.

## Getting Started

```csharp
using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;

var rules = ConversionEngine.NormalizeConversionRulesStrict(File.ReadAllText("rules.json"));
var plan = ConversionEngine.CompileConversionPlan(rules);

var (input, parseError) = ConversionEngine.ParsePayload("""{"name":"Ada"}""", rules.InputFormat);
if (parseError is not null) throw new FormatException(parseError);

var result = plan.Apply(input);
if (result.Errors.Count > 0) throw new InvalidOperationException(string.Join("; ", result.Errors));

var output = ConversionEngine.FormatPayload(result.Output, rules.OutputFormat, pretty: true);
Console.WriteLine(output);
```

## Design Constraints

The engine is intentionally rule-driven and deterministic:
- prefer declarative rule behavior over hardcoded integration logic
- keep conversion paths side-effect free and isolated from infrastructure concerns
- avoid transport/auth/database/UI behavior in this package
- preserve behavioral parity with `@apiconvert/core`

## Rules Model

Rules use a single ordered `rules` array with recursive nodes:
- `kind: "field"` maps one source into one or more `outputPaths`
- `kind: "array"` maps array items using recursive `itemRules`
- `kind: "branch"` evaluates an expression and runs `then` / `elseIf` / `else`

Supported field source types:
- `path`
- `constant`
- `transform`
- `merge`
- `condition`

Supported transforms:
- `toLowerCase`
- `toUpperCase`
- `number`
- `boolean`
- `concat`
- `split`
- custom transforms via `source.customTransform` + `ConversionOptions.TransformRegistry`

Supported merge modes:
- `concat`
- `firstNonEmpty`
- `array`

Additional rule/source options:
- field: optional `defaultValue`
- array: optional `coerceSingle`
- condition source: optional `trueSource` / `falseSource`, `trueValue` / `falseValue`, `elseIf`, and `conditionOutput` (`branch` or `match`)
- output root path `$` is not supported for `outputPaths`

## Example

```csharp
using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;

var rules = new ConversionRules
{
    InputFormat = DataFormat.Json,
    OutputFormat = DataFormat.Json,
    Rules =
    [
        new RuleNode
        {
            Kind = "branch",
            Expression = "path(status) == 'active'",
            Then =
            [
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["meta.enabled"],
                    Source = new ValueSource { Type = "constant", Value = "true" }
                }
            ],
            Else =
            [
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["meta.enabled"],
                    Source = new ValueSource { Type = "constant", Value = "false" }
                }
            ]
        }
    ]
};

var inputJson = """{ "status": "active" }""";
var (input, parseError) = ConversionEngine.ParsePayload(inputJson, rules.InputFormat);
if (parseError is not null) throw new Exception(parseError);

var result = ConversionEngine.ApplyConversion(input, rules);
if (result.Errors.Count > 0) throw new Exception(string.Join("; ", result.Errors));

var outputJson = ConversionEngine.FormatPayload(result.Output, rules.OutputFormat, pretty: true);
Console.WriteLine(outputJson);
```

## Condition Expressions

Condition sources and branch rules use expression syntax with `path(...)` and `exists(...)`.

Examples:
- `path(score) >= 70`
- `path($.meta.source) == 'api' && exists(path(value))`

## Parse and Format Helpers

`ConversionEngine` also exposes payload helpers:
- `ParsePayload(string, DataFormat)` and stream/`JsonNode` overloads
- `FormatPayload(object?, DataFormat, bool)` and stream overload
- `StreamJsonArrayConversionAsync(Stream, rawRules, ...)` for top-level JSON array item streaming

## Output Collision Policy

When multiple rules write to the same output path, configure deterministic behavior with `ConversionOptions.CollisionPolicy`:
- `LastWriteWins` (default)
- `FirstWriteWins`
- `Error` (keeps first value and records collision errors)

```csharp
var result = ConversionEngine.ApplyConversion(
    input,
    rules,
    new ConversionOptions { CollisionPolicy = OutputCollisionPolicy.Error });
```

## Explain / Trace Mode

Enable per-rule execution tracing with `ConversionOptions.Explain`.
`ConversionResult.Trace` is ordered deterministically by execution order and includes rule path, kind, decision, source value, and output paths.

```csharp
var result = ConversionEngine.ApplyConversion(
    input,
    rules,
    new ConversionOptions { Explain = true });

foreach (var entry in result.Trace)
{
    Console.WriteLine($"{entry.RulePath} [{entry.RuleKind}] => {entry.Decision}");
}
```

## Custom Transform Plugins

Register deterministic custom transform functions in `ConversionOptions.TransformRegistry` and reference them from rules using `source.customTransform`.

```csharp
var rules = ConversionEngine.NormalizeConversionRulesStrict("""
{
  "rules": [
    {
      "kind": "field",
      "outputPaths": ["user.code"],
      "source": { "type": "transform", "path": "name", "customTransform": "reverse" }
    }
  ]
}
""");

var result = ConversionEngine.ApplyConversion(
    new Dictionary<string, object?> { ["name"] = "Ada" },
    rules,
    new ConversionOptions
    {
        TransformRegistry = new Dictionary<string, Func<object?, object?>>
        {
            ["reverse"] = value => new string((value?.ToString() ?? string.Empty).Reverse().ToArray())
        }
    });
```

## Strict vs Lenient Rules Handling

- `NormalizeConversionRules(...)` is lenient and returns validation errors in `ConversionRules.ValidationErrors`.
- `NormalizeConversionRulesStrict(...)` throws when rules input is invalid.
- `LintRules(...)` returns non-mutating diagnostics with severity, code, rule path, and suggested fixes.
- `RunRuleDoctor(...)` combines validation, lint, and optional runtime diagnostics into one deterministic report.
- `CheckCompatibility(...)` reports schema/runtime compatibility findings for a target version.
- `BundleRules(...)` resolves modular `include` files into one deterministic rules object.
- `CompileConversionPlan(...)` reuses normalized rules for repeated conversions.
- `CompileConversionPlanStrict(...)` combines strict validation with plan compilation.
- `ComputeRulesCacheKey(...)` returns a stable cache key for normalized rules.

```csharp
var diagnostics = ConversionEngine.LintRules(File.ReadAllText("rules.json"));
foreach (var diagnostic in diagnostics)
{
    Console.WriteLine($"{diagnostic.Severity} {diagnostic.Code} {diagnostic.RulePath}: {diagnostic.Message}");
}

var plan = ConversionEngine.CompileConversionPlan(rulesJson);
Console.WriteLine(plan.CacheKey);

var doctor = ConversionEngine.RunRuleDoctor(
    rulesJson,
    sampleInputText: File.ReadAllText("sample.json"),
    inputFormat: DataFormat.Json);
Console.WriteLine($"Doctor findings: {doctor.Findings.Count}");

var compatibility = ConversionEngine.CheckCompatibility(rulesJson, targetVersion: "1.0.0");
Console.WriteLine($"Compatible: {compatibility.IsCompatible}");

var bundled = ConversionEngine.BundleRules("entry.rules.json");
Console.WriteLine($"Bundled rules: {bundled.Rules.Count}");
```

## Error Codes and Troubleshooting

Use the shared catalog for deterministic diagnostic mapping and remediation guidance:
- [`docs/error-code-catalog.md`](../../docs/error-code-catalog.md)

## Thread Safety

- Conversion execution is deterministic and side-effect free.
- Treat rule objects as immutable after configuration. Public rule lists are mutable; avoid mutating shared rules while converting on multiple threads.

## Streaming Notes

- Streaming API currently supports top-level JSON arrays only.
- Unsupported/non-JSON stream scenarios should continue to use `ParsePayload(...)` + `ApplyConversion(...)`.
