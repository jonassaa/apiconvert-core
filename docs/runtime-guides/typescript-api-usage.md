# TypeScript API Usage

<div class="runtime-typescript">

Use package exports for normalize/parse/apply/format workflows.

```ts
import { normalizeConversionRulesStrict, compileConversionPlan } from "@apiconvert/core";

const rules = normalizeConversionRulesStrict(rulesText);
const plan = compileConversionPlan(rules);
const result = plan.apply(input);
```

Key APIs:

- `normalizeConversionRulesStrict`
- `applyConversion`
- `compileConversionPlan`
- `lintConversionRules`
- `checkRulesCompatibility`

</div>
