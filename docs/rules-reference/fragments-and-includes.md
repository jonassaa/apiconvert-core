# Fragments and Includes

## Fragments with `use`

Declare reusable fragments under top-level `fragments`, then reference with `{ "use": "name" }`.

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

## `use` overrides (simplification)

`use` nodes can override fragment fields.

```json
{
  "fragments": {
    "copyValue": {
      "kind": "field",
      "outputPaths": ["a"],
      "source": { "type": "path", "path": "input.value" }
    }
  },
  "rules": [
    {
      "use": "copyValue",
      "outputPaths": ["b"],
      "source": { "type": "path", "path": "input.altValue" }
    }
  ]
}
```

## Fragment safety

Runtime normalizers detect:

- unknown fragment names
- fragment cycles (`A -> B -> A`)

## Includes and bundling

Use rules bundling to resolve include-based modular rules into a deterministic deploy artifact.

## Runtime APIs

<div class="runtime-dotnet">

- `ConversionEngine.BundleRules(entryRulesPath)`

</div>

<div class="runtime-typescript">

- `bundleConversionRules(entryRulesPath)`

</div>

