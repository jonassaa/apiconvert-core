# Apiconvert.Core

Apiconvert.Core is the .NET package for applying conversion rules to JSON, XML, and query payloads.

## Install

```bash
dotnet add package Apiconvert.Core
```

## Basic JSON -> JSON

```csharp
using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;

var rules = new ConversionRules
{
    InputFormat = DataFormat.Json,
    OutputFormat = DataFormat.Json,
    FieldMappings = new()
    {
        new FieldRule
        {
            OutputPath = "profile.name",
            Source = new ValueSource { Type = "path", Path = "user.fullName" }
        }
    }
};

var input = "{\"user\": {\"fullName\": \"Ada Lovelace\"}}";
var (value, error) = ConversionEngine.ParsePayload(input, rules.InputFormat);
if (error != null) throw new Exception(error);

var result = ConversionEngine.ApplyConversion(value, rules);
if (result.Errors.Count > 0) throw new Exception(string.Join("; ", result.Errors));

var outputJson = ConversionEngine.FormatPayload(result.Output, rules.OutputFormat, pretty: true);
```

Input:

```json
{ "user": { "fullName": "Ada Lovelace" } }
```

Output:

```json
{ "profile": { "name": "Ada Lovelace" } }
```

## Load Rules From rules.json

```csharp
using System.IO;
using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;

var rulesJson = File.ReadAllText("rules.json");
var rules = ConversionEngine.NormalizeConversionRules(rulesJson);

var input = File.ReadAllText("input.json");
var (value, error) = ConversionEngine.ParsePayload(input, rules.InputFormat);
if (error != null) throw new Exception(error);

var result = ConversionEngine.ApplyConversion(value, rules);
if (result.Errors.Count > 0) throw new Exception(string.Join("; ", result.Errors));

var outputJson = ConversionEngine.FormatPayload(result.Output, rules.OutputFormat, pretty: true);
```

## XML Attributes And Text

XML attributes are addressed with `@_` and element text with `#text`.

```csharp
var rules = new ConversionRules
{
    InputFormat = DataFormat.Xml,
    OutputFormat = DataFormat.Json,
    FieldMappings = new()
    {
        new FieldRule
        {
            OutputPath = "orderId",
            Source = new ValueSource { Type = "path", Path = "order.@_id" }
        },
        new FieldRule
        {
            OutputPath = "status",
            Source = new ValueSource { Type = "path", Path = "order.status.#text" }
        }
    }
};

var input = "<order id=\"A1\"><status>new</status></order>";
```

Input:

```xml
<order id="A1">
  <status>new</status>
</order>
```

Output:

```json
{ "orderId": "A1", "status": "new" }
```

## Array Mapping

```csharp
var rules = new ConversionRules
{
    InputFormat = DataFormat.Json,
    OutputFormat = DataFormat.Json,
    ArrayMappings = new()
    {
        new ArrayRule
        {
            InputPath = "orders",
            OutputPath = "orders",
            ItemMappings = new()
            {
                new FieldRule
                {
                    OutputPath = "id",
                    Source = new ValueSource { Type = "path", Path = "orderId" }
                },
                new FieldRule
                {
                    OutputPath = "currency",
                    Source = new ValueSource { Type = "path", Path = "$.defaults.currency" }
                }
            }
        }
    }
};
```

Input:

```json
{
  "defaults": { "currency": "USD" },
  "orders": [{ "orderId": "A1" }, { "orderId": "A2" }]
}
```

Output:

```json
{
  "orders": [
    { "id": "A1", "currency": "USD" },
    { "id": "A2", "currency": "USD" }
  ]
}
```

## Transforms And Conditions

```csharp
var rules = new ConversionRules
{
    InputFormat = DataFormat.Json,
    OutputFormat = DataFormat.Json,
    FieldMappings = new()
    {
        new FieldRule
        {
            OutputPath = "profile.country",
            Source = new ValueSource { Type = "transform", Transform = TransformType.ToUpperCase, Path = "user.country" }
        },
        new FieldRule
        {
            OutputPath = "profile.isAdult",
            Source = new ValueSource
            {
                Type = "condition",
                Condition = new ConditionRule
                {
                    Path = "user.age",
                    Operator = ConditionOperator.Gt,
                    Value = "17"
                },
                TrueValue = "true",
                FalseValue = "false"
            }
        }
    }
};
```

Input:

```json
{ "user": { "country": "se", "age": 21 } }
```

Output:

```json
{ "profile": { "country": "SE", "isAdult": true } }
```

## Formatting

Use `pretty: true` for indented JSON/XML output and `pretty: false` for compact output.
