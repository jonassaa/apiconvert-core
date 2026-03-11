A) Executive summary
- Strong breadth: core conversion, linting, compatibility checks, bundling, profiling, trace mode, and CLI utilities are all exposed from a single entrypoint, which is excellent for discoverability.【F:src/apiconvert-core/src/index.ts†L1-L53】
- Runtime diagnostics are consistently structured (`code`, `rulePath`, `severity`), which is good for production observability and automated triage workflows.【F:src/apiconvert-core/src/types.ts†L180-L190】
- Biggest consumer risk: the package is CJS-only today (`type: commonjs`, `module: CommonJS`, `exports` only `require/default`), which is behind modern Node+TS library expectations and weakens ESM-first adoption in current toolchains.【F:src/apiconvert-core/package.json†L9-L22】【F:src/apiconvert-core/tsconfig.json†L3-L6】
- Type model is too string-biased for value-bearing fields (`defaultValue`, `source.value`, `ConditionElseIfBranch.value`), causing avoidable casting and loss of type fidelity for numeric/boolean/object constants that the runtime otherwise handles as unknown values.【F:src/apiconvert-core/src/types.ts†L81-L85】【F:src/apiconvert-core/src/types.ts†L90-L103】【F:src/apiconvert-core/src/types.ts†L134-L139】
- Stream APIs currently buffer full text for non-array modes (NDJSON/query/XML), which limits true streaming behavior on large inputs and can create memory pressure for production ingestion paths.【F:src/apiconvert-core/src/mapping-engine.ts†L186-L210】【F:src/apiconvert-core/src/mapping-engine.ts†L234-L255】
- CLI ergonomics are serviceable but still “internal-tool grade”: minimal flag parser, no structured/helpful argument UX, and no standardized output format switches for automation pipelines beyond JSON dumps.【F:src/apiconvert-core/bin/apiconvert.js†L208-L249】
- Docs are solid on features, but some links reference paths outside the npm package boundary (`../../docs/...`), which can degrade npm-consumer experience depending on renderer/context.【F:src/apiconvert-core/README.md†L187-L191】【F:src/apiconvert-core/package.json†L28-L33】
- Test suite breadth is good for behavior, but there is no dedicated consumer-contract layer for API/type-level semver safety (e.g., declaration/API snapshot tests).【F:tests/npm/apiconvert-core-tests/src/conversion-cases.test.ts†L1-L27】【F:tests/npm/apiconvert-core-tests/src/validation-behavior.test.ts†L1-L28】

B) Public API review (keep/change)
Keep:
- Single entrypoint export map and clear top-level function set (`applyConversion`, `compileConversionPlan`, lint/doctor/profile/compatibility) is the right shape for consumers.【F:src/apiconvert-core/src/index.ts†L1-L53】
- Separation between one-shot conversion (`applyConversion`) and reusable plans (`compileConversionPlan`) is a strong performance-oriented design for integrators.【F:src/apiconvert-core/src/conversion-plan.ts†L10-L21】

Change proposals:
1) Publish dual runtime outputs (ESM + CJS)
- Add conditional exports with `import` and `require`, and emit ESM build alongside CJS.
- Migration-safe path: keep current CJS paths working; add ESM without breaking existing `require` consumers.

2) Introduce higher-level parse/convert wrapper with typed result envelope
- `runConversionCase` exists, but it is narrow and extension-driven; consider a first-class `convertText` API with explicit `inputFormat/outputFormat` and optional `strict` mode for ergonomics and safer error handling.

3) API surface partitioning
- Add subpath exports such as `@apiconvert/core/streaming`, `@apiconvert/core/diagnostics`, `@apiconvert/core/cli-types` to improve tree-shaking clarity and communicate module intent.

Ideal consumer snippet #1 (ESM-ready):
```ts
import { compileConversionPlan } from "@apiconvert/core";

const plan = compileConversionPlan(rulesJson);
const result = plan.apply(input, { explain: true });
if (result.errors.length) throw new Error(result.errors.join("; "));
```

C) Types and runtime-validation alignment
Issues:
1) Value types are over-restricted to string in several rule fields
- `ValueSourceBase.value`, `trueValue`, `falseValue`, `ConditionElseIfBranch.value`, and `FieldRule.defaultValue` are typed as `string | null`, which forces non-string constants into string workflows in TS even though runtime paths treat values generically in conversion output.【F:src/apiconvert-core/src/types.ts†L81-L85】【F:src/apiconvert-core/src/types.ts†L90-L103】【F:src/apiconvert-core/src/types.ts†L134-L139】

2) Lint diagnostic `suggestion` is required
- `RuleLintDiagnostic.suggestion` is non-optional, which can lead to low-value placeholder suggestions and discourages nuanced diagnostics where no safe suggestion exists.【F:src/apiconvert-core/src/types.ts†L62-L68】

Suggested fixes:
- Widen value-bearing fields to `unknown` or a JSONValue union; preserve backward compatibility because this is a widening change.
- Make `suggestion?: string` optional and standardize helper text only when actionable.
- Add type-level tests (dts/assertions) to lock API contracts.

Ideal consumer snippet #2 (typed constants):
```ts
const rules = {
  inputFormat: "json",
  outputFormat: "json",
  rules: [
    { kind: "field", outputPaths: ["flags.active"], source: { type: "constant", value: true } },
    { kind: "field", outputPaths: ["limits.max"], source: { type: "constant", value: 250 } }
  ]
};
```

D) Correctness and edge cases
1) Stream conversion modes are not truly incremental for NDJSON/query/XML
- `readAllText` concatenates the entire stream before parsing lines/elements, so “stream” semantics are item-emission only after full buffering for these modes.【F:src/apiconvert-core/src/mapping-engine.ts†L186-L210】【F:src/apiconvert-core/src/mapping-engine.ts†L234-L255】
- Improvement: line-buffer incremental parser for NDJSON/query; SAX-style or chunked parser strategy for XML element extraction.

2) Error envelope consistency across APIs
- Some APIs throw (`normalizeConversionRulesStrict`, `streamConversion` in fail-fast) while others return error arrays. This is manageable but inconsistent from a consumer ergonomics perspective.【F:src/apiconvert-core/src/rules-normalizer.ts†L40-L46】【F:src/apiconvert-core/src/mapping-engine.ts†L111-L124】
- Improvement: document a strict non-throw path and a strict throw path clearly, or offer an explicit `mode: "throw" | "report"` across top-level operations.

E) Performance notes
Quick wins:
- Avoid re-normalizing for cache-key calls where caller already normalized; `computeRulesCacheKey` normalizes again by default.【F:src/apiconvert-core/src/conversion-plan.ts†L23-L27】
- Add benchmarks for large NDJSON (100k lines), deeply nested arrays, and collision-heavy rules to characterize p95 apply latency and memory peaks.

Targets:
- NDJSON streaming should process in bounded memory regardless of input size.
- Plan compile path should remain sub-linear with fragment reuse where possible.

F) Packaging and docs
Packaging improvements:
- Add dual build artifacts and conditional exports (`import`/`require`) in `package.json`.
- Consider `typesVersions` or explicit export conditions if subpaths are introduced.
- Keep `sideEffects: false` (good), but verify CLI + bin remains unaffected in dual-package output.

Docs improvements:
- Keep README feature richness; add a "Production Patterns" section (plan caching, transform registry lifecycle, error code routing).
- Replace or mirror external-relative docs links so npm readme remains fully navigable from package context.【F:src/apiconvert-core/README.md†L187-L191】

Ideal consumer snippet #3 (production pattern):
```ts
import { compileConversionPlan } from "@apiconvert/core";

const planCache = new Map<string, ReturnType<typeof compileConversionPlan>>();

function getPlan(rulesText: string) {
  const key = rulesText; // replace with a stable hash strategy
  if (!planCache.has(key)) planCache.set(key, compileConversionPlan(rulesText));
  return planCache.get(key)!;
}
```

G) Tests (coverage gaps + proposed matrix)
Current strengths:
- Good breadth in conversion, diagnostics, streaming, compatibility, linting, and CLI behavior tests.【F:tests/npm/apiconvert-core-tests/src/stream-conversion.test.ts†L1-L48】【F:tests/npm/apiconvert-core-tests/src/compatibility.test.ts†L1-L39】【F:tests/npm/apiconvert-core-tests/src/cli.test.ts†L1-L52】

Gaps to add:
1) API contract tests:
- Declaration/API snapshots to detect accidental breaking changes in exported types/functions.
2) Module-system tests:
- Validate both `require` and `import` consumer projects in CI once dual output ships.
3) Stress/perf regression tests:
- Large NDJSON incremental processing and memory-bound assertions.
4) Transform safety tests:
- Verify deterministic behavior when custom transforms throw and when transform names are missing.

H) Prioritized action list
P0 (consumer adoption blockers)
1) Ship dual ESM+CJS package support with conditional exports.
2) Fix value-type ergonomics (widen string-only constants/defaults to JSON-compatible values).
3) Implement truly incremental NDJSON/query streaming (bounded memory).

P1 (DX and reliability)
4) Harmonize error-handling modes across top-level APIs.
5) Improve CLI argument parsing/help and add machine-friendly output switches.
6) Add API contract/type snapshot tests.

P2 (polish)
7) Improve npm README link robustness and production guidance section.
8) Add subpath exports to clarify module boundaries for advanced users.
