# Rules Model

This page is the shared conceptual model for both runtimes. For exact field-level schema details, use [Rules schema reference](../reference/rules-schema.md).

## Top-level structure

A rules document is a JSON object with:

- `inputFormat`: `json` | `xml` | `query`
- `outputFormat`: `json` | `xml` | `query`
- `rules`: ordered list of rule nodes
- `fragments` (optional): reusable rule snippets

Execution follows the order of `rules`.

## Rule node kinds

- `field`: read one source and write to one or more `outputPaths`
- `array`: read a collection at `inputPath`, apply `itemRules`, write to `outputPaths`
- `branch`: evaluate expression, run `then`/`elseIf`/`else`
- `use`: include a named fragment from `fragments`

See [Rule nodes reference](../reference/rule-nodes.md).

## Source types

For `field` rules, `source.type` can be:

- `path`
- `constant`
- `transform`
- `merge`
- `condition`

See [Sources and transforms](../reference/sources-and-transforms.md).

## Deterministic behavior

- No network calls, filesystem writes, or hidden side effects in conversion paths.
- Rule execution and diagnostics are deterministic for the same input/rules/options.
- Collision handling is explicit (`lastWriteWins`, `firstWriteWins`, or `error`).

See [Determinism and parity](./determinism-and-parity.md).
