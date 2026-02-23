# Custom Transforms Guide

Custom transforms let you register deterministic runtime functions and call them from rules using `source.customTransform`.

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

## .NET

```csharp
using Apiconvert.Core.Converters;

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

## TypeScript

```ts
import { applyConversion } from "@apiconvert/core";

const result = applyConversion(input, rules, {
  transforms: {
    reverse: (value) => String(value ?? "").split("").reverse().join("")
  }
});
```

## Current behavior

If `customTransform` is specified but missing from the runtime registry, conversion reports an error diagnostic.

## Guidance

- Keep transforms pure and side-effect free.
- Keep equivalent behavior in both runtimes.
- Avoid locale/time/random/network dependencies.

Related: [Rules schema reference](../reference/rules-schema.md), [Determinism and parity](../concepts/determinism-and-parity.md).
