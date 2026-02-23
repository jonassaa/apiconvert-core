# Condition Expressions

Conditions are used by `branch` rules and `source.type = "condition"`.

## Built-in functions

- `path(<inputPath>)`
- `exists(<value>)`

## Operators

- Equality and comparison: `==`, `!=`, `<`, `<=`, `>`, `>=`
- Boolean: `&&`, `||`, `!`
- Parentheses for grouping: `(` `)`

## Examples

```text
path(score) >= 70
path($.meta.source) == 'api' && exists(path(value))
!exists(path(user.deletedAt))
```

## Where used

- Branch rules: `rules[i].expression`
- Condition sources: `rules[i].source.expression`
- Else-if in branch and condition source nodes

See also [Rule nodes](./rule-nodes.md) and [Rules model](../concepts/rules-model.md).
