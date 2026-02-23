# Canonical Rules Model

Rules are an ordered recursive structure under a top-level `rules` array.

## Top-level shape

```json
{
  "inputFormat": "json",
  "outputFormat": "json",
  "rules": []
}
```

## Rule node kinds

- `field`: map one resolved source value to one or more output paths
- `array`: iterate an input array path and apply recursive `itemRules`
- `branch`: evaluate `expression`, then execute `then` / `elseIf` / `else`
- `use`: expand a named fragment (from `fragments` map)

## Source types (`field.source.type`)

- `path`
- `constant`
- `transform`
- `merge`
- `condition`

## Supported formats

- `json`
- `xml`
- `query`

## Related references

- Node details: [/rules-reference/node-types](/rules-reference/node-types)
- Source and transforms: [/rules-reference/sources-and-transforms](/rules-reference/sources-and-transforms)
- Branch expression grammar: [/rules-reference/condition-expressions](/rules-reference/condition-expressions)
