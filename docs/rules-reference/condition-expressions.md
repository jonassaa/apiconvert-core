# Condition Expressions

Expressions support:

- `path(...)`
- `exists(...)`
- comparisons: `==`, `!=`, `>`, `>=`, `<`, `<=`
- boolean operators: `&&`, `||`, `!`

Examples:

- `path(score) >= 70`
- `path($.meta.source) == 'api' && exists(path(value))`
