# Sources and Transforms

`field.source` defines how values are resolved before writing output.

## Source types

- `path`: resolve from payload path (`path`)
- `constant`: literal value (`value`)
- `transform`: resolve `path` then apply transform (`transform` or `customTransform`)
- `merge`: combine `paths` with `mergeMode`
- `condition`: evaluate `expression` and return branch/match output

## `path` source

```json
{ "type": "path", "path": "user.fullName" }
```

## `constant` source

```json
{ "type": "constant", "value": "VIP" }
```

## `transform` source

```json
{ "type": "transform", "path": "user.email", "transform": "toLowerCase" }
```

Built-in transform values:

- `toLowerCase`
- `toUpperCase`
- `number`
- `boolean`
- `concat`
- `split`

### `concat` behavior

`concat` reads tokens from `path` split by commas. Each token is either:

- a path token (resolved from input)
- a literal token prefixed with `const:`

Example:

```json
{ "type": "transform", "transform": "concat", "path": "user.firstName,const: ,user.lastName" }
```

### `split` behavior

- `separator` (default: single space)
- `tokenIndex` (supports negative index from end)
- `trimAfterSplit` (default: `true`)

```json
{
  "type": "transform",
  "path": "user.fullName",
  "transform": "split",
  "separator": " ",
  "tokenIndex": -1,
  "trimAfterSplit": true
}
```

### Custom transforms

Use `customTransform` with runtime registration:

- .NET: `ConversionOptions.TransformRegistry`
- TypeScript: `applyConversion(..., { transforms })`

## `merge` source

```json
{
  "type": "merge",
  "paths": ["user.firstName", "user.lastName"],
  "mergeMode": "concat",
  "separator": " "
}
```

`mergeMode` values:

- `concat`
- `firstNonEmpty`
- `array`

## `condition` source

`condition` evaluates `expression` and returns branch value or boolean match.

```json
{
  "type": "condition",
  "expression": "path(status) eq 'active'",
  "trueValue": "enabled",
  "falseValue": "disabled",
  "conditionOutput": "branch"
}
```

Advanced form with `elseIf` and nested sources:

```json
{
  "type": "condition",
  "expression": "path(status) eq 'active'",
  "trueSource": { "type": "path", "path": "user.tier" },
  "elseIf": [
    {
      "expression": "path(status) eq 'trial'",
      "value": "trial"
    }
  ],
  "falseValue": "inactive",
  "conditionOutput": "branch"
}
```

Set `conditionOutput` to `match` to return only boolean result.

## Authoring shorthand recap

These field aliases are runtime-normalizer shorthands:

- `from` -> path source
- `const` -> constant source
- `as` + `from` -> transform source

Example:

```json
{ "kind": "field", "to": "customer.age", "from": "user.age", "as": "number" }
```

