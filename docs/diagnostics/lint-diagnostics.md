# Lint Diagnostics

Lint diagnostics provide non-mutating feedback for authoring quality.

## Diagnostic fields

- `severity`
- `code`
- `rulePath`
- `message`
- Optional fix hints

## Runtime APIs

<div class="runtime-dotnet">

```csharp
var diagnostics = ConversionEngine.LintRules(rawRules);
```

</div>

<div class="runtime-typescript">

```ts
const lint = lintConversionRules(rawRules);
```

</div>

