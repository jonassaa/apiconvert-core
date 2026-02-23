# Sources And Transforms

This page explains `field.source` options.

## `path`

```json
{ "type": "path", "path": "user.email" }
```

## `constant`

```json
{ "type": "constant", "value": "active" }
```

## `transform`

Built-ins:

- `toLowerCase`
- `toUpperCase`
- `number`
- `boolean`
- `concat`
- `split`

Example:

```json
{
  "type": "transform",
  "path": "name",
  "transform": "split",
  "separator": ",",
  "tokenIndex": 0,
  "trimAfterSplit": true
}
```

## `merge`

Merge values from multiple paths.

```json
{
  "type": "merge",
  "paths": ["user.firstName", "user.lastName"],
  "mergeMode": "concat",
  "separator": " "
}
```

`mergeMode` values: `concat`, `firstNonEmpty`, `array`.

## `condition`

Condition source chooses values or nested sources based on expression.

```json
{
  "type": "condition",
  "expression": "path(status) == 'active'",
  "trueValue": "enabled",
  "falseValue": "disabled",
  "conditionOutput": "branch"
}
```

## Custom transforms

Use `customTransform` with runtime registration:

- .NET: `ConversionOptions.TransformRegistry`
- TypeScript: `applyConversion(..., { transforms })`

See [Custom transforms guide](../guides/custom-transforms.md).
