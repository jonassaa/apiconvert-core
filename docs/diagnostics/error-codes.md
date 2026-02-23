# Error Codes

Diagnostics include stable codes so CI and tooling can automate remediation.

## Categories

- Rules validation errors: schema/shape/semantic issues
- Lint diagnostics: maintainability and rule quality findings
- Runtime conversion errors: parse/source/write-path issues

## Runtime collection points

<div class="runtime-dotnet">

- `ConversionEngine.NormalizeConversionRulesStrict`
- `ConversionEngine.LintRules`
- `ConversionEngine.RunRuleDoctor`

</div>

<div class="runtime-typescript">

- `normalizeConversionRulesStrict`
- `lintConversionRules`
- `runRuleDoctor`

</div>

