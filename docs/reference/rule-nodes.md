# Rule Nodes

This page documents each canonical rule node with minimal examples.

## `field`

```json
{
  "kind": "field",
  "outputPaths": ["customer.name"],
  "source": { "type": "path", "path": "user.fullName" },
  "defaultValue": "Unknown"
}
```

## `array`

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

```json
{
  "kind": "branch",
  "expression": "path(status) == 'active'",
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

## `use` (fragment reference)

```json
{
  "fragments": {
    "customerName": {
      "kind": "field",
      "outputPaths": ["customer.name"],
      "source": { "type": "path", "path": "user.fullName" }
    }
  },
  "rules": [{ "use": "customerName" }]
}
```

## Runtime shorthand accepted by normalizers

Both runtimes currently accept shorthand aliases:

- `kind: "map"` with `entries` (expanded into field rules)
- `to` / `outputPath` aliases for `outputPaths`
- `from` alias for path source
- `const` alias for constant source
- `as` alias for built-in transform key with `from`

Canonical output should still be treated as the durable format.
