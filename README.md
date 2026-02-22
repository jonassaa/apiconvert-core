# Apiconvert.Core

Apiconvert.Core is the shared conversion engine for Apiconvert, with matching behavior across the .NET package (`Apiconvert.Core`) and TypeScript package (`@apiconvert/core`).

## Architectural Intent

Apiconvert.Core is a rule-driven API transformation engine. It converts payloads between incompatible schemas using declarative rules, not integration-specific mapping code.

Core expectations:
- deterministic conversions
- side-effect-free execution
- no input mutation
- consistent behavior across .NET and TypeScript runtimes

## Scope and Non-Goals

This package is conversion logic only. It does not provide:
- API gateway or middleware behavior
- HTTP proxying or authentication
- persistence or message-bus/workflow orchestration
- UI tooling for rule authoring

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

## Schema Versioning Policy

Schema versioning is lockstep with repository/package SemVer:

- Git tag: `vX.Y.Z`
- NuGet package: `X.Y.Z`
- npm package: `X.Y.Z`
- Rules schema: `X.Y.Z`

For strict pinning, use the versioned schema path:

- `schemas/rules/vX.Y.Z/schema.json` (immutable after release)

Convenience alias:

- `schemas/rules/current/schema.json` (mutable, always latest released version)

Transition alias (deprecated):

- `schemas/rules/rules.schema.json` (legacy path, currently mirrors latest schema)

Schema changes are contract changes. Updates to rule structure, validation behavior, or rule/source types must preserve backward compatibility or be explicitly versioned.

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
npm --prefix tests/npm/apiconvert-core-tests run parity:check
```

Shared conversion behavior should be validated through `tests/cases` in both runtimes.
The parity suite emits a machine-readable report at `tests/parity/parity-report.json`.
The parity gate also emits `tests/parity/parity-summary.json` with pass/fail criteria.
Toolkit docs: [`docs/parity-testing/parity-gate-ci.md`](docs/parity-testing/parity-gate-ci.md).
Consumer recipes: [`docs/recipes`](docs/recipes).

## GitHub Pages Docs

The repository includes a full GitHub Pages docs site powered by MkDocs + Material.

- Site source: [`docs/`](docs/)
- Build config: [`mkdocs.yml`](mkdocs.yml)
- Docs authoring rules: [`docs/contributing/docs-authoring-guide.md`](docs/contributing/docs-authoring-guide.md)

Local docs commands:

```bash
python3 -m venv .venv-docs
source .venv-docs/bin/activate
pip install -r requirements-docs.txt
mkdocs build --strict
mkdocs serve
```

## Release

Publishing is tag-driven for both NuGet and npm.

1. Run `Create Release Tag` in GitHub Actions.
2. Choose `patch`, `minor`, or `major`.
3. The workflow creates `vX.Y.Z`.
4. The publish workflow releases both packages at `X.Y.Z`.
