# Validation And Diagnostics

Use this page as the diagnostics hub.

## Validation and normalization

- Lenient normalize:
  - .NET: `NormalizeConversionRules`
  - TypeScript: `normalizeConversionRules`
- Strict normalize:
  - .NET: `NormalizeConversionRulesStrict`
  - TypeScript: `normalizeConversionRulesStrict`
- Explicit validation result:
  - TypeScript: `validateConversionRules`

## Lint

- .NET: `ConversionEngine.LintRules(rawRules)`
- TypeScript: `lintConversionRules(rawRules)`

Lint diagnostics include code, severity, rule path, message, and suggestion.

In .NET this contract is `RuleLintDiagnostic`:
- `Code`
- `Severity`
- `RulePath`
- `Message`
- `Suggestion`

In TypeScript:
- `lintConversionRules(rawRules)` returns `ConversionRulesLintResult` with `diagnostics` and `hasErrors`.

## Rule doctor

- .NET: `ConversionEngine.RunRuleDoctor(...)`
- TypeScript: `runRuleDoctor(...)`

Rule doctor combines validation, lint, and optional runtime sample checks.

In .NET this returns `RuleDoctorReport`:
- `Findings` (`RuleDoctorFinding` entries)
- `HasErrors`
- `CanApplySafeFixes`
- `SafeFixPreview`

In TypeScript options/contract:
- `RuleDoctorOptions`: `sampleInputText`, `inputFormat`, `applySafeFixes`
- `RuleDoctorReport`: `findings`, `hasErrors`, `canApplySafeFixes`, `safeFixPreview`

## Compatibility checks

- .NET: `ConversionEngine.CheckCompatibility(rawRules, targetVersion)`
- TypeScript: `checkRulesCompatibility(rawRules, { targetVersion })`

Use this when pinning to a target rules schema/runtime version.

In .NET this returns `RulesCompatibilityReport` with:
- `TargetVersion`, `SchemaVersion`
- `SupportedRangeMin`, `SupportedRangeMax`
- `IsCompatible`
- `Diagnostics` (`RulesCompatibilityDiagnostic` entries)

In TypeScript:
- `checkRulesCompatibility(rawRules, options)` accepts `RulesCompatibilityOptions` (`targetVersion`)
- Returns `RulesCompatibilityReport` with `targetVersion`, `schemaVersion`, `supportedRangeMin`, `supportedRangeMax`, `isCompatible`, and `diagnostics`

## Runtime diagnostics in conversion result

Both runtimes return conversion diagnostics in results:

- errors and warnings lists
- structured diagnostics entries
- optional trace when explain mode is enabled

In .NET result contracts:
- `ConversionResult.Diagnostics`: `ConversionDiagnostic` entries (`Code`, `RulePath`, `Severity`, `Message`)
- `ConversionResult.Trace`: `ConversionTraceEntry` entries (`RulePath`, `RuleKind`, `Decision`, `SourceValue`, `OutputPaths`, `Expression`, `Warning`, `Error`)

## Related

- [Error codes](../troubleshooting/error-codes.md)
- [Troubleshooting tree](../troubleshooting/troubleshooting-tree.md)
- [Runtime APIs](../guides/runtime-api.md)
- [CLI](./cli.md)
