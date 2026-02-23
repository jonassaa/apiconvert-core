# Rule Node Types

This page covers canonical schema node types and runtime-accepted authoring shorthands.

## Canonical schema node kinds

The versioned rules schema (`schemas/rules/vX.Y.Z/schema.json`) allows these node forms:

- `field`
- `array`
- `branch`
- `use` (fragment reference node)

## `field`

Maps one source value to one or more output paths.

```json
{
  "kind": "field",
  "outputPaths": ["customer.name"],
  "source": { "type": "path", "path": "user.fullName" },
  "defaultValue": "Unknown"
}
```

## `array`

Reads items from `inputPath`, applies `itemRules` per item, writes to `outputPaths`.

```json
{
  "kind": "array",
  "inputPath": "orders",
  "outputPaths": ["customer.orders"],
  "coerceSingle": true,
  "itemRules": [
    {
      "kind": "field",
      "outputPaths": ["id"],
      "source": { "type": "path", "path": "orderId" }
    }
  ]
}
```

## `branch`

Evaluates an expression and executes `then`, optional `elseIf`, optional `else`.

```json
{
  "kind": "branch",
  "expression": "path(status) eq 'active'",
  "then": [
    {
      "kind": "field",
      "outputPaths": ["meta.enabled"],
      "source": { "type": "constant", "value": "true" }
    }
  ],
  "else": [
    {
      "kind": "field",
      "outputPaths": ["meta.enabled"],
      "source": { "type": "constant", "value": "false" }
    }
  ]
}
```

## `use`

Expands a named fragment from top-level `fragments`.

```json
{
  "fragments": {
    "customerName": {
      "kind": "field",
      "outputPaths": ["customer.name"],
      "source": { "type": "path", "path": "user.fullName" }
    }
  },
  "rules": [
    { "use": "customerName" }
  ]
}
```

`use` also supports overrides for node fields, for example overriding `outputPaths` or `source`.

## Runtime shorthand nodes and aliases

Both runtime normalizers accept additional authoring simplifications:

- `kind: "map"` with `entries[]`: expands to multiple `field` nodes
- `to` and `outputPath`: aliases for `outputPaths`
- `from`: alias for `source.type = "path"` + `source.path`
- `const`: alias for `source.type = "constant"` + `source.value`
- `as`: alias for `source.type = "transform"` + `source.transform` (requires `from`)

### `map` shorthand example

```json
{
  "kind": "map",
  "entries": [
    { "to": "customer.name", "from": "user.fullName" },
    { "to": "customer.id", "from": "user.id" }
  ]
}
```

Equivalent canonical form is two `field` rules.

## Coverage checklist

- canonical schema node kinds: `field`, `array`, `branch`, `use`
- fragment expansion via `use`
- runtime shorthand support via `map` and field aliases

