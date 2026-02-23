# Arrays, Branches, Merge, And Split Recipe

This intermediate example combines several common patterns in one rules pack.

## Input

```json
{
  "status": "vip",
  "contacts": [
    { "fullName": "Ada Lovelace", "phones": "+44-1, +44-2" },
    { "fullName": "Grace Hopper", "phones": "+1-1,+1-2" }
  ],
  "fallbackEmail": "support@example.com"
}
```

## Rules

```json
{
  "inputFormat": "json",
  "outputFormat": "json",
  "rules": [
    {
      "kind": "array",
      "inputPath": "contacts",
      "outputPaths": ["customers"],
      "itemRules": [
        {
          "kind": "field",
          "outputPaths": ["name"],
          "source": { "type": "path", "path": "fullName" }
        },
        {
          "kind": "field",
          "outputPaths": ["primaryPhone"],
          "source": {
            "type": "transform",
            "path": "phones",
            "transform": "split",
            "separator": ",",
            "tokenIndex": 0,
            "trimAfterSplit": true
          }
        }
      ]
    },
    {
      "kind": "field",
      "outputPaths": ["support.email"],
      "source": {
        "type": "merge",
        "paths": ["primaryEmail", "fallbackEmail"],
        "mergeMode": "firstNonEmpty"
      }
    },
    {
      "kind": "branch",
      "expression": "path(status) == 'vip'",
      "then": [
        {
          "kind": "field",
          "outputPaths": ["support.tier"],
          "source": { "type": "constant", "value": "priority" }
        }
      ],
      "else": [
        {
          "kind": "field",
          "outputPaths": ["support.tier"],
          "source": { "type": "constant", "value": "standard" }
        }
      ]
    }
  ]
}
```

## Expected output

```json
{
  "customers": [
    { "name": "Ada Lovelace", "primaryPhone": "+44-1" },
    { "name": "Grace Hopper", "primaryPhone": "+1-1" }
  ],
  "support": {
    "email": "support@example.com",
    "tier": "priority"
  }
}
```

## Runtime options worth enabling during authoring

<div class="runtime-dotnet">

`new ConversionOptions { CollisionPolicy = OutputCollisionPolicy.Error, Explain = true }`

</div>

<div class="runtime-typescript">

`{ collisionPolicy: OutputCollisionPolicy.Error, explain: true }`

</div>

Advanced extension example: [Custom transforms guide](../guides/custom-transforms.md).
