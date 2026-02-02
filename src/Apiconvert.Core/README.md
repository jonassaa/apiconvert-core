# Apiconvert.Core

Core conversion engine, rule models, and contracts for Apiconvert.

## Highlights
- Normalize and apply conversion rules across JSON, XML, and query-string payloads.
- Typed rule models for mapping definitions.
- Contracts for rule generation providers.

## Quick start
```csharp
using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;

var rules = new ConversionRules
{
    InputFormat = DataFormat.Json,
    OutputFormat = DataFormat.Json
};

var (value, error) = ConversionEngine.ParsePayload("{\"name\":\"Ada\"}", rules.InputFormat);
if (error is null)
{
    var result = ConversionEngine.ApplyConversion(value, rules);
    var output = ConversionEngine.FormatPayload(result.Output, rules.OutputFormat, pretty: true);
}
```

## License
License: LicenseRef-Proprietary (update to match your repository license before publishing).
