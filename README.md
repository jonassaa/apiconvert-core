# Apiconvert.Core

Apiconvert.Core is the shared conversion engine for Apiconvert, with matching behavior across the .NET package (`Apiconvert.Core`) and TypeScript package (`@apiconvert/core`).

## Canonical Rules Model

Rules use one ordered `rules` array of recursive nodes:

- `kind: "field"` with `outputPaths` and `source`
- `kind: "array"` with `inputPath`, `outputPaths`, and `itemRules`
- `kind: "branch"` with `expression`, `then`, optional `elseIf`, optional `else`

Supported payload formats:
- `json`
- `xml`
- `query`

Supported value sources (`field.source.type`):
- `path` (`path`)
- `constant` (`value`, parsed as primitive when possible)
- `transform` (`path`, `transform`)
- `merge` (`paths`, optional `mergeMode`, `separator`)
- `condition` (`expression`, branch sources/values, optional `conditionOutput`)

Supported transforms (`transform`):
- `toLowerCase`
- `toUpperCase`
- `number`
- `boolean`
- `concat` (`path` supports comma-separated tokens and `const:` literals)
- `split` (`separator`, `tokenIndex`, `trimAfterSplit`)

Supported merge modes (`mergeMode`):
- `concat`
- `firstNonEmpty`
- `array`

Branch/condition expressions support:
- `path(...)`
- `exists(...)`
- comparisons (`==`, `!=`, `>`, `>=`, `<`, `<=`)
- boolean operators (`&&`, `||`, `!`)

Array rules also support:
- `coerceSingle` to treat a single non-array input value as one array item

The canonical schema is:

- `schemas/rules/rules.schema.json`

## Quickstart (.NET)

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
            Kind = "field",
            OutputPaths = ["customer.name"],
            Source = new ValueSource { Type = "path", Path = "user.fullName" }
        }
    ]
};

var (value, error) = ConversionEngine.ParsePayload("{\"user\": {\"fullName\": \"Ada\"}}", rules.InputFormat);
if (error is not null) throw new Exception(error);

var result = ConversionEngine.ApplyConversion(value, rules);
if (result.Errors.Count > 0) throw new Exception(string.Join("; ", result.Errors));

var outputJson = ConversionEngine.FormatPayload(result.Output, rules.OutputFormat, pretty: true);
```

## Quickstart (TypeScript)

```ts
import {
  applyConversion,
  DataFormat,
  formatPayload,
  normalizeConversionRules,
  parsePayload
} from "@apiconvert/core";

const rules = normalizeConversionRules({
  inputFormat: DataFormat.Json,
  outputFormat: DataFormat.Json,
  rules: [
    {
      kind: "field",
      outputPaths: ["customer.name"],
      source: { type: "path", path: "user.fullName" }
    }
  ]
});

const { value, error } = parsePayload('{"user": {"fullName": "Ada"}}', rules.inputFormat!);
if (error) throw new Error(error);

const result = applyConversion(value, rules);
if (result.errors.length > 0) throw new Error(result.errors.join("; "));

const outputJson = formatPayload(result.output, rules.outputFormat!, true);
```

## Build & Test

```bash
dotnet build Apiconvert.Core.sln
dotnet test Apiconvert.Core.sln
npm --prefix tests/npm/apiconvert-core-tests test
```

## Release

Publishing is tag-driven for both NuGet and npm.

1. Run `Create Release Tag` in GitHub Actions.
2. Choose `patch`, `minor`, or `major`.
3. The workflow creates `vX.Y.Z`.
4. The publish workflow releases both packages at `X.Y.Z`.
