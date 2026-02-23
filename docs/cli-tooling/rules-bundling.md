# Rules Bundling

Bundling resolves include-based modular rules into a single deterministic artifact.

## Why bundle

- Reproducible deploy artifact
- Simpler runtime loading
- Early include failures in CI

## CLI usage

```bash
apiconvert rules bundle --rules entry.rules.json --out bundled.rules.json
```

## Runtime APIs

<div class="runtime-dotnet">

- `ConversionEngine.BundleRules(entryRulesPath)`

</div>

<div class="runtime-typescript">

- `bundleConversionRules(entryRulesPath)`

</div>

