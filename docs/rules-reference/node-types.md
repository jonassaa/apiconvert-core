# Rule Node Types

## `field`
Maps a source value to one or more output paths.

## `array`
Iterates array input path and executes recursive `itemRules`.

## `branch`
Evaluates an expression and executes `then`, optional `elseIf`, optional `else`.

## `use`
Expands a named fragment, optionally with overrides.
