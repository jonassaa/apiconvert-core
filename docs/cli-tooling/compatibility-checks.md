# Compatibility Checks

Compatibility checks validate whether rules are safe for a target schema/runtime version.

## Runtime APIs

<div class="runtime-dotnet">

- `ConversionEngine.CheckCompatibility(rawRules, targetVersion)`

</div>

<div class="runtime-typescript">

- `checkRulesCompatibility(rawRules, { targetVersion })`

</div>

## CLI usage

```bash
apiconvert rules compatibility --rules rules.json --target 1.0.0
```

