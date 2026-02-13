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

## Stream Input And Output

```csharp
using System.IO;
using System.Text;
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
            Source = new ValueSource { Type = "path", Path = "user.name" }
        }
    }
};

using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes("""{"user":{"name":"Ada"}}"""));
var (value, error) = ConversionEngine.ParsePayload(inputStream, rules.InputFormat);
if (error != null) throw new Exception(error);

var result = ConversionEngine.ApplyConversion(value, rules);
if (result.Errors.Count > 0) throw new Exception(string.Join("; ", result.Errors));

using var outputStream = new MemoryStream();
ConversionEngine.FormatPayload(result.Output, rules.OutputFormat, outputStream, pretty: true);
outputStream.Position = 0;
using var reader = new StreamReader(outputStream);
var outputJson = reader.ReadToEnd();
```

Input:

```json
{ "user": { "name": "Ada" } }
```

Output:

```json
{ "profile": { "name": "Ada" } }
```

`ParsePayload(Stream, ...)` and `FormatPayload(..., Stream, ...)` default to `leaveOpen: true`.

## Parse JSON From JsonNode

```csharp
using System.Text.Json.Nodes;
using Apiconvert.Core.Converters;

var jsonNode = JsonNode.Parse("""{"user":{"name":"Ada"}}""");
var (value, error) = ConversionEngine.ParsePayload(jsonNode);
if (error != null) throw new Exception(error);
```

`JsonNode` input is supported only for `DataFormat.Json`.

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

Array mapping paths support root-prefixed JSONPath-style syntax:
- `inputPath`: `orders` and `$.orders` are equivalent.
- `outputPath`: `orders` and `$.orders` are equivalent.

`$` resolves from the root input for reads, and `$.<path>` writes at the root output path.

```csharp
var rules = new ConversionRules
{
    InputFormat = DataFormat.Json,
    OutputFormat = DataFormat.Json,
    ArrayMappings = new()
    {
        new ArrayRule
        {
            InputPath = "$.orders",
            OutputPath = "$.ordersNormalized",
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

## Split And Merge Field Rules

Use `outputPaths` to split one source value into multiple output fields.  
Use `source.type = "merge"` with `paths` to combine multiple inputs into one output.

```csharp
var rules = new ConversionRules
{
    InputFormat = DataFormat.Json,
    OutputFormat = DataFormat.Json,
    FieldMappings = new()
    {
        new FieldRule
        {
            OutputPaths = new() { "profile.name", "profile.displayName" },
            Source = new ValueSource { Type = "path", Path = "user.name" }
        },
        new FieldRule
        {
            OutputPath = "profile.fullName",
            Source = new ValueSource
            {
                Type = "merge",
                Paths = new() { "user.firstName", "user.lastName" },
                MergeMode = MergeMode.Concat,
                Separator = " "
            }
        }
    }
};
```

Input:

```json
{
  "user": {
    "name": "Ada Lovelace",
    "firstName": "Ada",
    "lastName": "Lovelace"
  }
}
```

Output:

```json
{
  "profile": {
    "name": "Ada Lovelace",
    "displayName": "Ada Lovelace",
    "fullName": "Ada Lovelace"
  }
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
                Expression = "path(user.age) > 17",
                TrueValue = "true",
                FalseValue = "false"
            }
        }
    }
};
```

Condition expressions support:
- Value lookups with `path(...)` (including root lookups like `path($.meta.source)`).
- `exists(...)`.
- Logical operators: `&&`, `||`, `!`, `and`, `or`, `not`.
- Comparison operators: `==`, `!=`, `>`, `>=`, `<`, `<=`, and aliases `eq`, `not eq`, `gt`, `gte`, `lt`, `lte`.
- Membership with `in` against array literals, for example `path(user.name) in ['ada', 'john']`.

For splitting a full name into parts, use `transform = Split`, `separator`, and `tokenIndex`.
`tokenIndex` supports negative values (`-1` = last token):
`trimAfterSplit` defaults to `true` and can be set to `false` to preserve token whitespace.
`separator = ""` is invalid for split transforms.

```csharp
var rules = new ConversionRules
{
    InputFormat = DataFormat.Json,
    OutputFormat = DataFormat.Json,
    FieldMappings = new()
    {
        new FieldRule
        {
            OutputPath = "firstName",
            Source = new ValueSource
            {
                Type = "transform",
                Transform = TransformType.Split,
                Path = "name",
                Separator = " ",
                TokenIndex = 0
            }
        },
        new FieldRule
        {
            OutputPath = "lastName",
            Source = new ValueSource
            {
                Type = "transform",
                Transform = TransformType.Split,
                Path = "name",
                Separator = " ",
                TokenIndex = -1
            }
        }
    }
};
```

Input:

```json
{ "name": "Jonas Strand Aasberg" }
```

Output:

```json
{ "firstName": "Jonas", "lastName": "Aasberg" }
```

Comma-separated names are supported as well. With default trim (`trimAfterSplit` omitted or `true`):

```json
{ "name": "Jonas, Aasberg" }
```

maps to:

```json
{ "firstName": "Jonas", "lastName": "Aasberg" }
```

With `trimAfterSplit = false`, whitespace is preserved:

```json
{ "firstName": "Jonas", "lastName": " Aasberg" }
```

## Formatting

Use `pretty: true` for indented JSON/XML output and `pretty: false` for compact output.
