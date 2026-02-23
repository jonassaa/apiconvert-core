# Condition Expressions

Condition expressions are used by:

- `branch` rule `expression`
- `source.type = "condition"` `expression`
- `condition` source `elseIf[].expression`

## Expression building blocks

- value lookup: `path(...)`
- existence check: `exists(...)`
- literals: strings (`'text'`), numbers (`42`, `3.14`), `true`, `false`, `null`
- array literals (for `in`): `['a', 'b', 'c']`

`path(...)` accepts either identifier-like paths or quoted paths:

- `path(status)`
- `path($.meta.source)`
- `path('$.meta.source')`

## Operators (symbolic and semantic)

### Comparison operators

- equals: `==` or `eq`
- not equals: `!=` or `not eq`
- greater than: `>` or `gt`
- greater than/equal: `>=` or `gte`
- less than: `<` or `lt`
- less than/equal: `<=` or `lte`
- membership: `in`

### Boolean operators

- and: `&&` or `and`
- or: `||` or `or`
- not: `!` or `not`

## Equivalent examples

These pairs are equivalent:

- `path(score) >= 70`
- `path(score) gte 70`

- `path(status) == 'active'`
- `path(status) eq 'active'`

- `path(role) != 'guest'`
- `path(role) not eq 'guest'`

- `exists(path(customer.id)) && path(source) eq 'api'`
- `exists(path(customer.id)) and path(source) eq 'api'`

- `!(path(state) eq 'archived')`
- `not (path(state) eq 'archived')`

## `in` examples

```text
path(country) in ['SE', 'NO', 'DK']
path(score) in [10, 20, 30]
```

Right-hand side of `in` must be an array literal.

## Precedence and grouping

Evaluation order:

1. unary `!` / `not`
2. comparisons (`==`, `eq`, `gt`, `in`, ...)
3. `&&` / `and`
4. `||` / `or`

Use parentheses for clarity and deterministic intent:

```text
(path(status) eq 'active' or path(status) eq 'trial') and path(score) gte 70
```

## Branch rule example

```json
{
  "kind": "branch",
  "expression": "path(status) eq 'active' and path(score) gte 70",
  "then": [
    {
      "kind": "field",
      "outputPaths": ["meta.segment"],
      "source": { "type": "constant", "value": "qualified" }
    }
  ],
  "else": [
    {
      "kind": "field",
      "outputPaths": ["meta.segment"],
      "source": { "type": "constant", "value": "standard" }
    }
  ]
}
```

## Condition source example (`branch` output)

```json
{
  "kind": "field",
  "outputPaths": ["meta.state"],
  "source": {
    "type": "condition",
    "expression": "path(status) eq 'active'",
    "trueValue": "enabled",
    "falseValue": "disabled",
    "conditionOutput": "branch"
  }
}
```

## Condition source example (`match` output)

```json
{
  "kind": "field",
  "outputPaths": ["meta.isQualified"],
  "source": {
    "type": "condition",
    "expression": "path(score) gte 70 and exists(path(customer.id))",
    "conditionOutput": "match"
  }
}
```

## Common mistakes

- Using `neq` (unsupported) instead of `not eq` or `!=`
- Using `equals` or `is` (unsupported) instead of `eq` or `==`
- Omitting array literal on right side of `in`
- Writing bare identifiers (`status eq 'active'`) instead of `path(status)`

