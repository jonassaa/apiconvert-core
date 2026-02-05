# Apiconvert.Core

Apiconvert.Core is the .NET package that powers conversion between JSON, XML, and query-string payloads using declarative rules.

## Install

```bash
dotnet add package Apiconvert.Core
```

## Rules Schema

The canonical JSON Schema for conversion rules lives at `schemas/rules/rules.schema.json` in this repository. Treat it as the single source of truth for rule shape changes.

GitHub source (for reference):
```
https://github.com/jonassaa/apiconvert-core/blob/main/schemas/rules/rules.schema.json
```

## Usage (Sketch)

```csharp
using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;

var rules = new ConversionRules
{
    InputFormat = DataFormat.Json,
    OutputFormat = DataFormat.Xml,
    FieldMappings = new List<FieldRule>
    {
        new()
        {
            OutputPath = "profile.name",
            Source = new ValueSource { Type = "path", Path = "user.name" }
        }
    }
};

var (value, error) = ConversionEngine.ParsePayload("{\"user\":{\"name\":\"Ada\"}}", DataFormat.Json);
if (error is null)
{
    var result = ConversionEngine.ApplyConversion(value, rules);
    var output = ConversionEngine.FormatPayload(result.Output, DataFormat.Xml, pretty: false);
}
```
