# Performance And Caching

For repeated conversions with the same rules, compile and reuse a plan.

## Compile once, apply many

<div class="runtime-dotnet">

```csharp
using Apiconvert.Core.Converters;

var plan = ConversionEngine.CompileConversionPlan(rawRules);
var result1 = plan.Apply(input1);
var result2 = plan.Apply(input2);
Console.WriteLine(plan.CacheKey);
```

</div>

<div class="runtime-typescript">

```ts
import { compileConversionPlan } from "@apiconvert/core";

const plan = compileConversionPlan(rawRules);
const result1 = plan.apply(input1);
const result2 = plan.apply(input2);
console.log(plan.cacheKey);
```

</div>

## Stable cache keys

Use cache keys to reuse compiled plans safely:

<div class="runtime-dotnet">

- `ConversionEngine.ComputeRulesCacheKey(rawRules)`

</div>

<div class="runtime-typescript">

- `computeRulesCacheKey(rawRules)`

</div>

## Profile plan performance

Both runtimes include plan profiling utilities.

- .NET: `ConversionEngine.ProfileConversionPlan(...)`
- TypeScript: `profileConversionPlan(...)`

### .NET profile options and report

- `ConversionProfileOptions.Iterations`: measured iteration count (default `100`)
- `ConversionProfileOptions.WarmupIterations`: pre-measure warmup runs (default `10`)
- `ConversionProfileReport`: `PlanCacheKey`, `CompileMs`, `WarmupIterations`, `Iterations`, `TotalRuns`
- `ConversionProfileReport.LatencyMs`: percentile summary (`Min`, `P50`, `P95`, `P99`, `Max`, `Mean`)

See [CLI](../reference/cli.md) for benchmark command usage.
