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

## Rule doctor

- .NET: `ConversionEngine.RunRuleDoctor(...)`
- TypeScript: `runRuleDoctor(...)`

Rule doctor combines validation, lint, and optional runtime sample checks.

## Compatibility checks

- .NET: `ConversionEngine.CheckCompatibility(rawRules, targetVersion)`
- TypeScript: `checkRulesCompatibility(rawRules, { targetVersion })`

Use this when pinning to a target rules schema/runtime version.

## Runtime diagnostics in conversion result

Both runtimes return conversion diagnostics in results:

- errors and warnings lists
- structured diagnostics entries
- optional trace when explain mode is enabled

## Related

- [Error codes](../troubleshooting/error-codes.md)
- [Troubleshooting tree](../troubleshooting/troubleshooting-tree.md)
- [Runtime APIs](../guides/runtime-api.md)
- [CLI](./cli.md)
