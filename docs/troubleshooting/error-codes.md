# Error Codes

This page documents conversion and streaming diagnostics emitted during conversion execution.

## Conversion Runtime Codes (ACV-RUN)

<!-- ACV-CODES-TABLE-START -->

| Code | Severity | Trigger | Typical rulePath | What to do |
| --- | --- | --- | --- | --- |
| `ACV-RUN-000` | Error | Normalized rules include validation errors before execution starts. | `rules` | Fix schema/normalization issues first, then re-run conversion. |
| `ACV-RUN-100` | Error | A `field` or `array` rule has no usable `outputPaths`. | `rules[i]` | Add at least one non-empty output path for the failing rule. |
| `ACV-RUN-101` | Warning | An `array` rule input path is missing (`null` source value). | `rules[i]` | Verify `inputPath` and source payload shape, or keep as intentional optional mapping. |
| `ACV-RUN-102` | Error | An `array` rule input path resolves to a non-array while `coerceSingle` is not producing a list. | `rules[i]` | Align rule/input shape; use `coerceSingle` only when a single-item coercion is intended. |
| `ACV-RUN-103` | Error | Two rules write to the same output path while collision policy is `Error`. | `rules[i]` | Split output paths, adjust write order policy, or switch collision policy intentionally. |
| `ACV-RUN-201` | Error | `customTransform` is referenced but not registered in conversion options. | `rules[i].source` | Register the transform name in runtime options or remove the custom transform reference. |
| `ACV-RUN-202` | Error | Registered `customTransform` throws at runtime. | `rules[i].source` | Fix transform implementation and re-run with the same input to verify deterministic output. |
| `ACV-RUN-203` | Error | A source type is unsupported by the runtime. | `rules[i].source` | Use supported source types only and confirm schema/runtime version compatibility. |
| `ACV-RUN-301` | Error | A required condition expression is missing/blank. | `rules[i]` or `rules[i].source` | Provide a non-empty expression for branch/condition sources. |
| `ACV-RUN-302` | Error | Condition expression parsing/evaluation fails. | `rules[i]` or `rules[i].source` | Correct expression syntax and referenced paths; re-run with explain mode for context. |
| `ACV-RUN-800` | Error | Condition/source recursion depth limit is exceeded. | `rules[i].source...` | Simplify nested condition/source graphs to avoid recursive dependency loops. |
| `ACV-RUN-900` | Error | Rule recursion depth limit is exceeded while traversing nested rules. | `rules[...]` | Reduce nested rule depth and flatten deeply recursive rule structures. |
| `ACV-RUN-901` | Error | Rule kind is unsupported at execution time. | `rules[i]` | Use supported rule kinds (`field`, `array`, `branch`) and validate rule generation. |

## Streaming Conversion Codes (ACV-STR)

| Code | Severity | Trigger | Typical rulePath | What to do |
| --- | --- | --- | --- | --- |
| `ACV-STR-001` | Error | Streaming input record cannot be parsed/read for the configured stream mode. | `stream[i]` | Inspect raw stream record format, fix parser/input mismatch, then replay the failed record. |

<!-- ACV-CODES-TABLE-END -->

## Conversion Triage Workflow

1. Start with the first `Error` diagnostic code in the result.
2. Use `rulePath` to locate the exact failing rule or stream record.
3. Re-run conversion with explain mode enabled to inspect rule-level decisions.
4. Resolve source/condition issues before tuning collision behavior.

## Related

- [Validation and diagnostics](../reference/validation-and-diagnostics.md)
- [Troubleshooting tree](./troubleshooting-tree.md)
