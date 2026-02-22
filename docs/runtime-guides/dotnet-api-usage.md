# .NET API Usage

<div class="runtime-dotnet">

Use `ConversionEngine` for normalize/parse/apply/format workflows.

```csharp
var rules = ConversionEngine.NormalizeConversionRulesStrict(rulesText);
var plan = ConversionEngine.CompileConversionPlan(rules);
var result = plan.Apply(input);
```

Key APIs:

- `NormalizeConversionRulesStrict`
- `ApplyConversion`
- `CompileConversionPlan`
- `RunRuleDoctor`
- `LintRules`

</div>
