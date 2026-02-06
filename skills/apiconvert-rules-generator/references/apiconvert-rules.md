# Apiconvert Rules Reference

## Schema Location

- Canonical (raw): `https://raw.githubusercontent.com/jonassaa/apiconvert-core/main/schemas/rules/rules.schema.json`
- Version: `2`

## Supported Formats

- `json`
- `xml`
- `query`

## Paths

- Dot paths: `user.name`
- Array index: `items[0].id` or `items.0.id`
- Root: `$` for full root
- Force root: `$.defaults.currency`

## XML Access

- Attributes: `@_attrName`
- Text: `#text`
- Repeated elements become arrays

## Rule Shapes

### ConversionRules

```json
{
  "version": 2,
  "inputFormat": "json",
  "outputFormat": "json",
  "fieldMappings": [],
  "arrayMappings": []
}
```

### FieldRule

```json
{
  "outputPath": "profile.name",
  "source": { "type": "path", "path": "user.fullName" },
  "defaultValue": "Anonymous"
}
```

### ValueSource Types

- `path`
- `constant`
- `transform`
- `condition`

### Transform Types

- `toLowerCase`
- `toUpperCase`
- `number`
- `boolean`
- `concat`

`concat` uses a comma-separated list of tokens in `path`. Use `const:` for literals.

Example:

```json
{
  "type": "transform",
  "transform": "concat",
  "path": "user.firstName, const: , user.lastName"
}
```

### Condition Operators

- `exists`
- `equals`
- `notEquals`
- `includes`
- `gt`
- `lt`

Example:

```json
{
  "type": "condition",
  "condition": { "path": "user.age", "operator": "gt", "value": "17" },
  "trueValue": "true",
  "falseValue": "false"
}
```

### ArrayRule

```json
{
  "inputPath": "orders",
  "outputPath": "orders",
  "itemMappings": [
    { "outputPath": "id", "source": { "type": "path", "path": "orderId" } }
  ],
  "coerceSingle": false
}
```
