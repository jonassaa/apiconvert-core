# Validation Modes

Use strict validation for production/runtime entry points and lenient validation in authoring workflows.

## Strict normalization

Throws when invalid rules are detected.

<div class="runtime-dotnet">

- `ConversionEngine.NormalizeConversionRulesStrict(...)`

</div>

<div class="runtime-typescript">

- `normalizeConversionRulesStrict(...)`

</div>

## Lenient normalization

Returns normalized rules plus `validationErrors`.

<div class="runtime-dotnet">

- `ConversionEngine.NormalizeConversionRules(...)`

</div>

<div class="runtime-typescript">

- `normalizeConversionRules(...)`

</div>

## What is validated

- required node fields (for example `outputPaths`, `expression`, `itemRules`)
- supported `kind`, `source.type`, `transform`, `mergeMode`, `conditionOutput`
- fragment resolution issues (`use` unknown/cycle)
- shorthand constraints (for example `as` requires `from`)

## Schema vs runtime shorthand note

Versioned JSON schema documents canonical nodes (`field`, `array`, `branch`, `use`).

Runtime normalizers also accept authoring shorthands such as:

- `kind: "map"` + `entries`
- `to`, `outputPath`, `from`, `const`, `as`

For strict schema pinning workflows, prefer canonical node representation in persisted rules artifacts.

