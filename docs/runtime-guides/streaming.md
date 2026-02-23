# Streaming

Streaming mode is for large or continuous inputs where full payload materialization is expensive.

## Input kinds

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

<div class="runtime-dotnet">

<h2 id="streaming-dotnet-example-memory-stream-ndjson">.NET example (MemoryStream + NDJSON)</h2>

```csharp
using System.Text;
using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;

var ndjson = "{\"user\":{\"fullName\":\"Ada\"}}\n{\"user\":{\"fullName\":\"Lin\"}}\n";
using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ndjson));

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

await foreach (var result in ConversionEngine.StreamConversionAsync(
  stream,
  rawRules,
  new StreamConversionOptions
  {
    InputKind = StreamInputKind.Ndjson,
    ErrorMode = StreamErrorMode.ContinueWithReport
  }))
{
  if (result.Errors.Count > 0)
  {
    Console.WriteLine(string.Join("; ", result.Errors));
    continue;
  }

  Console.WriteLine(ConversionEngine.FormatPayload(result.Output, DataFormat.Json, pretty: false));
}
```

</div>

<div class="runtime-typescript">

<h2 id="streaming-typescript-example-async-iterable-ndjson">TypeScript example (async iterable + NDJSON)</h2>

```ts
import {
  DataFormat,
  StreamErrorMode,
  StreamInputKind,
  formatPayload,
  streamConversion
} from "@apiconvert/core";

async function* ndjsonChunks() {
  yield '{"user":{"fullName":"Ada"}}\n';
  yield '{"user":{"fullName":"Lin"}}\n';
}

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

for await (const result of streamConversion(ndjsonChunks(), rawRules, {
  inputKind: StreamInputKind.Ndjson,
  errorMode: StreamErrorMode.ContinueWithReport
})) {
  if (result.errors.length > 0) {
    console.log(result.errors.join("; "));
    continue;
  }

  console.log(formatPayload(result.output, DataFormat.Json, false));
}
```

</div>
