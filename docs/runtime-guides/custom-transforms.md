# Custom Transforms

Custom transforms let you register deterministic runtime functions and reference them from rules via `source.customTransform`.

## Rule snippet

```json
{
  "kind": "field",
  "outputPaths": ["user.code"],
  "source": {
    "type": "transform",
    "path": "name",
    "customTransform": "reverse"
  }
}
```

## Runtime registration

<div class="runtime-dotnet">

```csharp
var result = ConversionEngine.ApplyConversion(
    input,
    rules,
    new ConversionOptions
    {
        TransformRegistry = new Dictionary<string, Func<object?, object?>>
        {
            ["reverse"] = value => new string((value?.ToString() ?? string.Empty).Reverse().ToArray())
        }
    });
```

</div>

<div class="runtime-typescript">

```ts
const result = applyConversion(input, rules, {
  transforms: {
    reverse: (value) => String(value ?? "").split("").reverse().join("")
  }
});
```

</div>

## Rules for safe transform design

- Keep functions pure and deterministic.
- Do not perform I/O, random generation, current-time reads, or network access.
- Return explicit fallbacks for null/undefined values.
- Keep runtime parity by implementing equivalent behavior in both runtimes.
