# Merge and Collision Policy

Merge policy and collision policy solve different problems.

- merge policy: how one source combines multiple candidate values
- collision policy: what happens when multiple rules write to the same output path

## Merge modes (`source.type = "merge"`)

### `concat`

Concatenates all resolved values as strings, using optional `separator`.

```json
{
  "type": "merge",
  "paths": ["user.firstName", "user.lastName"],
  "mergeMode": "concat",
  "separator": " "
}
```

Input:

```json
{ "user": { "firstName": "Ada", "lastName": "Lovelace" } }
```

Result value: `"Ada Lovelace"`

### `firstNonEmpty`

Returns first value that is not null and not empty string.

```json
{
  "type": "merge",
  "paths": ["user.nickname", "user.fullName", "user.id"],
  "mergeMode": "firstNonEmpty"
}
```

Input:

```json
{ "user": { "nickname": "", "fullName": "Ada Lovelace", "id": "42" } }
```

Result value: `"Ada Lovelace"`

### `array`

Returns all candidate values as an array in source-path order.

```json
{
  "type": "merge",
  "paths": ["a", "b", "c"],
  "mergeMode": "array"
}
```

Input:

```json
{ "a": 1, "b": null, "c": 3 }
```

Result value: `[1, null, 3]`

## Collision policies (output writes)

Collision policies apply when two or more rules target the same `outputPath`.

### `LastWriteWins` / `lastWriteWins` (default)

Later rule overwrites earlier value.

### `FirstWriteWins` / `firstWriteWins`

First written value is kept; later writes are ignored.

### `Error` / `error`

First value is kept and collision is reported as error.

## Collision example

Rules:

```json
{
  "rules": [
    {
      "kind": "field",
      "outputPaths": ["customer.name"],
      "source": { "type": "path", "path": "user.fullName" }
    },
    {
      "kind": "field",
      "outputPaths": ["customer.name"],
      "source": { "type": "path", "path": "user.displayName" }
    }
  ]
}
```

Input:

```json
{ "user": { "fullName": "Ada Lovelace", "displayName": "Ada" } }
```

Behavior:

<div class="runtime-dotnet">

- `LastWriteWins` => `customer.name = "Ada"`
- `FirstWriteWins` => `customer.name = "Ada Lovelace"`
- `Error` => `customer.name = "Ada Lovelace"` + conversion error diagnostic

</div>

<div class="runtime-typescript">

- `lastWriteWins` => `customer.name = "Ada"`
- `firstWriteWins` => `customer.name = "Ada Lovelace"`
- `error` => `customer.name = "Ada Lovelace"` + conversion error diagnostic

</div>
