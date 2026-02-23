# Merge And Collision

This page covers how multiple values and multiple writes are handled.

## Merge source behavior

When `source.type = "merge"`, combine values with `mergeMode`:

- `concat`: join as string (optional `separator`)
- `firstNonEmpty`: first non-empty value wins
- `array`: keep all values as array

## Output collision policy

When two rules target the same output path, choose one policy:

<div class="runtime-dotnet">

- `OutputCollisionPolicy.LastWriteWins` (default)
- `OutputCollisionPolicy.FirstWriteWins`
- `OutputCollisionPolicy.Error`

</div>

<div class="runtime-typescript">

- `OutputCollisionPolicy.LastWriteWins` (default enum value `lastWriteWins`)
- `OutputCollisionPolicy.FirstWriteWins` (enum value `firstWriteWins`)
- `OutputCollisionPolicy.Error` (enum value `error`)

</div>

## Example

```json
{
  "rules": [
    {
      "kind": "field",
      "outputPaths": ["customer.id"],
      "source": { "type": "path", "path": "legacy.id" }
    },
    {
      "kind": "field",
      "outputPaths": ["customer.id"],
      "source": { "type": "path", "path": "modern.id" }
    }
  ]
}
```

With `Error` policy, the first value is kept and collision diagnostics are emitted.
