# Fragments And Includes

Use these features to keep large rule sets maintainable.

## Fragments (`fragments` + `use`)

Use `fragments` for reusable rule nodes in a single rules file.

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

Fragment nodes can be overridden at use site (for example, to override `outputPaths` or `source`).

## Includes (`include` + bundling)

Use `include` to compose multiple files:

```json
{
  "include": ["./base.rules.json", "./customer.rules.json"],
  "rules": [
    {
      "kind": "field",
      "outputPaths": ["meta.version"],
      "source": { "type": "constant", "value": "v1" }
    }
  ]
}
```

Then bundle before shipping:

- .NET: `ConversionEngine.BundleRules(entryPath, options?)`
- TypeScript: `bundleConversionRules(entryPath, options?)`

## Current behavior

- Circular include chains are rejected.
- Missing include files fail bundling.
- Included and local rules are combined deterministically.
