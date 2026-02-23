# Conversion Lifecycle

This page explains the end-to-end flow shared by .NET and TypeScript.

## 1. Normalize rules

Use strict mode for CI and release workflows:

- .NET: `ConversionEngine.NormalizeConversionRulesStrict(raw)`
- TypeScript: `normalizeConversionRulesStrict(raw)`

Lenient mode keeps validation errors inside the rules object:

- .NET: `ConversionEngine.NormalizeConversionRules(raw)`
- TypeScript: `normalizeConversionRules(raw)`

## 2. Parse input payload

- .NET: `ConversionEngine.ParsePayload(text, format)`
- TypeScript: `parsePayload(text, format)`

Supported formats: `json`, `xml`, `query`.

## 3. Apply conversion

- .NET: `ConversionEngine.ApplyConversion(input, rules, options?)`
- TypeScript: `applyConversion(input, rules, options?)`

Returned result includes `output`, `errors`, `warnings`, and `diagnostics`.

## 4. Format output payload

- .NET: `ConversionEngine.FormatPayload(value, format, pretty)`
- TypeScript: `formatPayload(value, format, pretty)`

## 5. Optional quality gates

Before or after conversion:

- lint rules
- run rule doctor
- check compatibility with target schema/runtime version
- compile reusable plans and profile performance

See [Runtime APIs](../guides/runtime-api.md) and [Validation and diagnostics](../reference/validation-and-diagnostics.md).
