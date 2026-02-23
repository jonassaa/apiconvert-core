# Rule Doctor Workflow

Rule doctor combines validation, linting, and optional runtime execution checks into one deterministic report.

## Recommended workflow

1. Author or modify rules.
2. Run doctor with representative input.
3. Review findings by severity.
4. Add/update shared cases.
5. Promote when findings are resolved or accepted.

## Runtime APIs

<div class="runtime-dotnet">

```csharp
var report = ConversionEngine.RunRuleDoctor(
    rawRules,
    sampleInputText: sampleInput,
    inputFormat: DataFormat.Json);
```

</div>

<div class="runtime-typescript">

```ts
const report = runRuleDoctor(rawRules, {
  inputText: sampleInput,
  format: DataFormat.Json
});
```

</div>

