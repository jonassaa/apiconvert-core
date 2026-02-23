# JSON And Query Recipe

This intermediate example converts JSON profile data into a query payload.

## Input (JSON)

```json
{
  "profile": {
    "name": "Ada Lovelace",
    "email": "ada@example.com",
    "tags": ["math", "history"]
  }
}
```

## Rules

```json
{
  "inputFormat": "json",
  "outputFormat": "query",
  "rules": [
    {
      "kind": "field",
      "outputPaths": ["name"],
      "source": { "type": "path", "path": "profile.name" }
    },
    {
      "kind": "field",
      "outputPaths": ["email"],
      "source": { "type": "path", "path": "profile.email" }
    },
    {
      "kind": "field",
      "outputPaths": ["tags"],
      "source": {
        "type": "merge",
        "paths": ["profile.tags.0", "profile.tags.1"],
        "mergeMode": "concat",
        "separator": ","
      }
    }
  ]
}
```

## Expected output (query)

```text
name=Ada%20Lovelace&email=ada%40example.com&tags=math%2Chistory
```

## Runtime notes

- Use `DataFormat.Query` when parsing/formatting directly.
- Query values are strings; convert to typed values with transforms when needed.

Related: [Sources and transforms](../reference/sources-and-transforms.md).
