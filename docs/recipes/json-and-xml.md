# JSON And XML Recipe

This intermediate example maps JSON order data into XML-like output structure.

## Input (JSON)

```json
{
  "order": {
    "id": "PO-1001",
    "customer": "Ada",
    "priority": "high"
  }
}
```

## Rules

```json
{
  "inputFormat": "json",
  "outputFormat": "xml",
  "rules": [
    {
      "kind": "field",
      "outputPaths": ["PurchaseOrder.@id"],
      "source": { "type": "path", "path": "order.id" }
    },
    {
      "kind": "field",
      "outputPaths": ["PurchaseOrder.Customer"],
      "source": { "type": "path", "path": "order.customer" }
    },
    {
      "kind": "branch",
      "expression": "path(order.priority) == 'high'",
      "then": [
        {
          "kind": "field",
          "outputPaths": ["PurchaseOrder.Flags.Expedite"],
          "source": { "type": "constant", "value": "true" }
        }
      ]
    }
  ]
}
```

## Runtime runner (same flow)

- .NET: `NormalizeConversionRulesStrict` -> `ParsePayload` -> `ApplyConversion` -> `FormatPayload`
- TypeScript: `normalizeConversionRulesStrict` -> `parsePayload` -> `applyConversion` -> `formatPayload`

See complete runnable snippets in:

- [Getting started](../getting-started/index.md)

## Expected output (XML)

```xml
<PurchaseOrder id="PO-1001">
  <Customer>Ada</Customer>
  <Flags>
    <Expedite>true</Expedite>
  </Flags>
</PurchaseOrder>
```

Related: [Rules schema reference](../reference/rules-schema.md), [Condition expressions](../reference/conditions.md).
