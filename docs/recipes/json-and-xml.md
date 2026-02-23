# JSON and XML Recipes

- map JSON fields into XML elements/attributes
- map XML attributes into JSON fields
- use arrays and branches for mixed nested structures

Use shared cases under `tests/cases` for parity verification.

<div class="runtime-dotnet">

<h2 id="json-xml-dotnet-runner">.NET runner sketch</h2>

```csharp
var rules = ConversionEngine.NormalizeConversionRulesStrict(File.ReadAllText("rules.json"));
var (input, err) = ConversionEngine.ParsePayload(File.ReadAllText("input.json"), rules.InputFormat);
var result = ConversionEngine.ApplyConversion(input!, rules);
var output = ConversionEngine.FormatPayload(result.Output, rules.OutputFormat, pretty: true);
```

</div>

<div class="runtime-typescript">

<h2 id="json-xml-typescript-runner">TypeScript runner sketch</h2>

```ts
const rules = normalizeConversionRulesStrict(readFileSync("rules.json", "utf8"));
const { value } = parsePayload(readFileSync("input.json", "utf8"), rules.inputFormat!);
const result = applyConversion(value, rules);
const output = formatPayload(result.output, rules.outputFormat!, true);
```

</div>
