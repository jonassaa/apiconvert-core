# Performance and Caching

Performance usually depends on two phases: rule normalization/plan compilation, then apply-time execution.

## Recommended strategy

1. Normalize and compile once per ruleset.
2. Cache by stable rules cache key.
3. Reuse the compiled plan for many inputs.
4. Profile with representative input sizes and shapes.

## APIs

<div class="runtime-dotnet">

- `ConversionEngine.CompileConversionPlan(rawRules)`
- `ConversionEngine.ComputeRulesCacheKey(rawRules)`
- `ConversionEngine.ProfileConversionPlan(...)`

</div>

<div class="runtime-typescript">

- `compileConversionPlan(rawRules)`
- `computeRulesCacheKey(rawRules)`
- `profileConversionPlan(...)`

</div>

## Benchmarking guidance

- Separate compile latency from apply latency.
- Include warmup iterations before measuring.
- Track p50/p95 latency and memory usage for large payloads.
- Run the same input set in both runtimes when parity is critical.

## Cache key usage pattern

```text
cacheKey = computeRulesCacheKey(rules)
if (planCache has cacheKey) use cached plan
else compile plan and store
```

