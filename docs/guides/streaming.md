# Streaming Guide

Use streaming when inputs are large or continuous and full materialization is expensive.

## Supported input kinds

<div class="runtime-dotnet">

- `JsonArray`
- `Ndjson`
- `QueryLines`
- `XmlElements` (requires `XmlItemPath`)

</div>

<div class="runtime-typescript">

- `jsonArray`
- `ndjson`
- `queryLines`
- `xmlElements` (requires `xmlItemPath`)

</div>

## Error modes

<div class="runtime-dotnet">

- `FailFast`
- `ContinueWithReport`

</div>

<div class="runtime-typescript">

- `failFast`
- `continueWithReport`

</div>

## NDJSON example (both runtimes)

<div class="runtime-dotnet">

```csharp
using System.Text;
using Apiconvert.Core.Converters;

var rawRules = """
{
  "inputFormat": "json",
  "outputFormat": "json",
  "rules": [
    {
      "kind": "field",
      "outputPaths": ["customer.name"],
      "source": { "type": "path", "path": "user.fullName" }
    }
  ]
}
""";

var ndjson = "{\"user\":{\"fullName\":\"Ada\"}}\n{\"user\":{\"fullName\":\"Lin\"}}\n";
using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ndjson));

await foreach (var result in ConversionEngine.StreamConversionAsync(
    stream,
    rawRules,
    new StreamConversionOptions
    {
        InputKind = StreamInputKind.Ndjson,
        ErrorMode = StreamErrorMode.ContinueWithReport
    }))
{
    Console.WriteLine(ConversionEngine.FormatPayload(result.Output, Apiconvert.Core.Rules.DataFormat.Json, pretty: false));
}
```

</div>

<div class="runtime-typescript">

```ts
import {
  DataFormat,
  StreamErrorMode,
  StreamInputKind,
  formatPayload,
  streamConversion
} from "@apiconvert/core";

const rawRules = {
  inputFormat: "json",
  outputFormat: "json",
  rules: [
    {
      kind: "field",
      outputPaths: ["customer.name"],
      source: { type: "path", path: "user.fullName" }
    }
  ]
};

const ndjson = '{"user":{"fullName":"Ada"}}\n{"user":{"fullName":"Lin"}}\n';

for await (const result of streamConversion(ndjson, rawRules, {
  inputKind: StreamInputKind.Ndjson,
  errorMode: StreamErrorMode.ContinueWithReport
})) {
  console.log(formatPayload(result.output, DataFormat.Json, false));
}
```

</div>

## Related pages

- [Runtime APIs](./runtime-api.md)
- [Rules schema reference](../reference/rules-schema.md)
