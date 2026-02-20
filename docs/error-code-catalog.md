# Error Code Catalog

This catalog maps common conversion diagnostics to deterministic error codes and fix guidance.
The diagnostics are parity-aligned between `.NET` (`Apiconvert.Core`) and npm (`@apiconvert/core`).

## Conversion Diagnostics

| Code | Severity | Message Pattern | Typical Cause | Recommended Fix |
| --- | --- | --- | --- | --- |
| `ACV-CONV-001` | Error | `...: outputPaths is required.` | `field` or `array` rule is missing `outputPaths`. | Add at least one output path on the rule node. |
| `ACV-CONV-002` | Error | `...: input path did not resolve to an array (...)` | `array.inputPath` resolved to a scalar/object. | Point `inputPath` to an array or set `coerceSingle: true` when supported. |
| `ACV-CONV-003` | Warning | `Array mapping skipped: inputPath "..." not found (...)` | Missing optional array source with `coerceSingle` disabled. | Provide input data at the path or treat warning as expected optional behavior. |
| `ACV-CONV-004` | Error | `...: output collision at "..." (already written by ...)` | Multiple rules target the same output path with `collisionPolicy=error`. | Use distinct output paths, or pick `firstWriteWins`/`lastWriteWins` intentionally. |
| `ACV-CONV-005` | Error | `...: unsupported kind '...'` | Unknown `rule.kind` reached execution. | Fix the rule kind to `field`, `array`, or `branch`. |
| `ACV-CONV-006` | Error | `...: rule recursion limit exceeded.` | Rule graph nesting exceeded depth safety limit. | Simplify nested rules or split conversion into multiple passes. |
| `ACV-CONV-007` | Error | `...: condition/source recursion limit exceeded.` | Recursive condition source tree exceeded depth safety limit. | Flatten condition sources or remove recursive source references. |
| `ACV-CONV-008` | Error | `...: unsupported source type '...'` | Unknown `source.type` was used. | Use supported source types: `path`, `constant`, `transform`, `merge`, `condition`. |
| `ACV-CONV-009` | Error | `...: <label> is required.` | Required expression/source metadata missing. | Populate the missing rule/source field in schema-compliant form. |
| `ACV-CONV-010` | Error | `...: invalid <label> "..."` | Condition expression parse/eval failed. | Fix expression syntax; validate with `normalizeConversionRulesStrict` or `validateConversionRules`. |

## Rules Validation Diagnostics

| Code | Severity | Message Pattern | Typical Cause | Recommended Fix |
| --- | --- | --- | --- | --- |
| `ACV-RULE-001` | Error | `root: invalid JSON in rules payload.` | Non-JSON rules payload was provided. | Fix JSON syntax before normalization. |
| `ACV-RULE-002` | Error | `rules: must be an array.` | Root `rules` property is missing/not an array. | Provide a top-level `rules` array. |
| `ACV-RULE-003` | Error | `...: kind is required.` | Rule node missing `kind`. | Set `kind` to `field`, `array`, or `branch`. |
| `ACV-RULE-004` | Error | `...: unsupported kind '...'` | Unknown rule kind in raw rules. | Replace with supported rule kinds. |
| `ACV-RULE-005` | Error | `...: unsupported source type '...'` | Unsupported source type in rules. | Replace with supported source types. |
| `ACV-RULE-006` | Error | `...: expression is required.` | Branch/condition expression missing. | Add expression string for `branch`/`condition` usage. |
| `ACV-RULE-007` | Error | `...: unsupported transform '...'` | Transform name not recognized. | Use one of `toLowerCase`, `toUpperCase`, `number`, `boolean`, `concat`, `split`. |
| `ACV-RULE-008` | Error | `...: unsupported merge mode '...'` | Unsupported merge mode. | Use `concat`, `firstNonEmpty`, or `array`. |
| `ACV-RULE-009` | Error | `...: outputPaths is required.` | Field/array rule missing outputs at normalization time. | Add one or more output paths. |
| `ACV-RULE-010` | Error | `...: inputPath is required.` | Array rule missing `inputPath`. | Provide `inputPath` for array rules. |

## Strict vs Lenient Handling

- Lenient mode keeps conversion/rules errors in `ConversionResult.Errors` or `validationErrors`.
- Strict mode throws early:
  - .NET: `NormalizeConversionRulesStrict(...)`
  - npm: `normalizeConversionRulesStrict(...)`

## Troubleshooting Workflow

1. Normalize rules in strict mode first and resolve all `ACV-RULE-*` errors.
2. Run conversion with deterministic `collisionPolicy` for your use case.
3. Enable trace mode for deep debugging:
   - .NET: `new ConversionOptions { Explain = true }`
   - npm: `applyConversion(input, rules, { explain: true })`
4. Review warnings separately from errors; warnings indicate non-fatal but important behavior.
