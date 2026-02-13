# Apiconvert.Core

`Apiconvert.Core` is the .NET conversion engine for JSON, XML, and query payloads.

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

Supported merge modes:
- `concat`
- `firstNonEmpty`
- `array`

Additional rule/source options:
- field: optional `defaultValue`
- array: optional `coerceSingle`
- condition source: optional `trueSource` / `falseSource`, `trueValue` / `falseValue`, `elseIf`, and `conditionOutput` (`branch` or `match`)

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
