A) Executive summary
- The package has a strong, centralized façade (`ConversionEngine`) with normalization, conversion, linting, compatibility, bundling, profiling, and streaming APIs, which is excellent for discoverability by external consumers.【F:src/Apiconvert.Core/Converters/ConversionEngine.cs†L13-L100】【F:src/Apiconvert.Core/Converters/ConversionEngine.cs†L325-L340】
- NuGet packaging hygiene is good: symbols, XML docs, analyzers, readme, repository metadata, and license file are all configured, which is above average for early libraries.【F:src/Apiconvert.Core/Apiconvert.Core.csproj†L7-L29】
- Main API risk: the engine is static-only, which limits DI-based integration, mocking seams, and per-tenant configuration patterns often expected by enterprise consumers.【F:src/Apiconvert.Core/Converters/ConversionEngine.cs†L13-L13】
- Rules model is still stringly in key places (`RuleNode.Kind`, `ValueSource.Type`, constant/default values as strings), which weakens compile-time safety and leads to authoring/runtime drift risk.【F:src/Apiconvert.Core/Rules/Models.cs†L136-L143】【F:src/Apiconvert.Core/Rules/Models.cs†L262-L275】【F:src/Apiconvert.Core/Rules/Models.cs†L320-L325】
- The XML streaming path currently buffers full input (`ReadToEndAsync`) before parsing/selecting elements, so it is not truly incremental for large streams.【F:src/Apiconvert.Core/Converters/ConversionEngine.cs†L805-L837】
- Normalization behavior can silently coerce missing field defaults to `string.Empty`, which may produce surprising output semantics versus intent (“missing default” vs “empty-string default”).【F:src/Apiconvert.Core/Converters/RulesNormalizer.cs†L131-L137】【F:src/Apiconvert.Core/Rules/Models.cs†L320-L325】
- Tests cover many functional paths (conversion, cache, stream, compatibility, doctor), but there is no public API approval baseline to protect NuGet consumers from accidental surface-breaking changes.【F:tests/nuget/Apiconvert.Core.Tests/ConversionEngineTests.cs†L1-L20】【F:tests/nuget/Apiconvert.Core.Tests/StreamingConversionTests.cs†L1-L20】【F:tests/nuget/Apiconvert.Core.Tests/ConversionPlanCacheTests.cs†L1-L33】

B) Public API review (keep/change)
Keep:
- Keep the happy-path ergonomics (`Normalize` -> `CompilePlan` -> `Apply`), which is clear and performant for repeated conversions.【F:src/Apiconvert.Core/Converters/ConversionEngine.cs†L65-L80】【F:src/Apiconvert.Core/Converters/ConversionPlan.cs†L26-L35】
- Keep deterministic diagnostics model in results (`Errors`, `Warnings`, `Diagnostics`) because it is automation-friendly in production handlers.【F:src/Apiconvert.Core/Rules/Models.cs†L414-L440】

Change proposals:
1) Add interface-based runtime entrypoint without removing static façade (non-breaking)
- Introduce `IConversionEngine` + default implementation wrapping current logic.
- Keep `ConversionEngine` static methods as convenience wrappers for migration safety.

2) Strengthen typed authoring surface
- Keep JSON compatibility, but add typed authoring builders/wrappers (e.g., `FieldRule`, `ArrayRule`, `BranchRule` helper APIs) or discriminator enums for `Kind` and `Type`.
- Migration-safe approach: additive APIs; do not remove current string fields yet.

3) Introduce explicit strict/report mode options for top-level APIs
- Current mix of throw/non-throw methods is useful but inconsistent across operations and harder to standardize in consuming apps.

Ideal consumer snippet #1 (today + recommended shape):
```csharp
var plan = ConversionEngine.CompileConversionPlanStrict(rulesJson);
var result = plan.Apply(input, new ConversionOptions { Explain = true });
if (result.Errors.Count > 0)
    throw new InvalidOperationException(string.Join("; ", result.Errors));
```

C) Correctness and edge cases
1) Default-value normalization can blur intent
- `RuleNode.DefaultValue` is a non-nullable `string` with default empty string, and normalizer enforces empty string when absent; that makes it hard to distinguish omitted default from explicit empty-string default in runtime behavior and tooling flows.【F:src/Apiconvert.Core/Rules/Models.cs†L320-L325】【F:src/Apiconvert.Core/Converters/RulesNormalizer.cs†L131-L137】
- Suggested fix: add nullable/default-state semantics in normalized model (additive: new property, preserve legacy behavior).

2) Type fidelity for constants and condition values
- Condition and source literal values are string-typed (`Value`, `TrueValue`, `FalseValue`, else-if `Value`), limiting strongly-typed authoring for numeric/bool/object constants despite conversion outputs being object-typed.【F:src/Apiconvert.Core/Rules/Models.cs†L129-L130】【F:src/Apiconvert.Core/Rules/Models.cs†L159-L160】【F:src/Apiconvert.Core/Rules/Models.cs†L183-L190】
- Suggested fix: move to JSON-value-capable type (e.g., `JsonElement?` or dedicated `JsonValue` abstraction) while keeping legacy string fields for compatibility window.

3) Stream behavior differs by mode
- NDJSON/query are line-incremental, but XML reads entire stream before yielding, which can surprise consumers expecting bounded-memory semantics across all stream kinds.【F:src/Apiconvert.Core/Converters/ConversionEngine.cs†L706-L785】【F:src/Apiconvert.Core/Converters/ConversionEngine.cs†L805-L837】
- Suggested fix: document this clearly now, then incrementally move XML to forward-only parse strategy.

Ideal consumer snippet #2 (defensive runtime pattern):
```csharp
await foreach (var item in ConversionEngine.StreamConversionAsync(
    stream,
    rules,
    new StreamConversionOptions { InputKind = StreamInputKind.Ndjson, ErrorMode = StreamErrorMode.ContinueWithReport },
    cancellationToken: ct))
{
    if (item.Errors.Count > 0) { /* route to dead-letter/report */ }
}
```

D) Performance notes
Important:
- XML element stream mode is full-buffer parse (`ReadToEndAsync` + parse + select) and should be treated as high-risk on large payloads/multi-tenant environments.【F:src/Apiconvert.Core/Converters/ConversionEngine.cs†L805-L837】

Quick wins:
- Expose/encourage plan reuse in docs even more aggressively; `ConversionPlan` already computes and exposes cache key, ideal for app-level plan caches.【F:src/Apiconvert.Core/Converters/ConversionPlan.cs†L13-L25】
- Add benchmark coverage for XML elements, deep nesting, and transform-heavy flows to prevent regressions.

Ideal consumer snippet #3 (plan cache pattern):
```csharp
var cache = new ConcurrentDictionary<string, ConversionPlan>();
var key = ConversionEngine.ComputeRulesCacheKey(rulesText);
var plan = cache.GetOrAdd(key, _ => ConversionEngine.CompileConversionPlan(rulesText));
var result = plan.Apply(input);
```

E) Packaging and docs
What’s good:
- Packaging metadata is already strong (`GenerateDocumentationFile`, symbols/snupkg, analyzers, readme/license inclusion).【F:src/Apiconvert.Core/Apiconvert.Core.csproj†L14-L24】【F:src/Apiconvert.Core/Apiconvert.Core.csproj†L26-L29】

Recommended changes:
1) Re-evaluate TFM strategy for consumer reach
- Current TFMs are `net8.0;net10.0`; that is modern but may unnecessarily exclude teams pinned to LTS + older ecosystems (if intentional, document support policy explicitly).【F:src/Apiconvert.Core/Apiconvert.Core.csproj†L4-L4】
2) Add API compatibility guardrails in CI
- Add public API baseline checks so NuGet surface changes are deliberate.
3) Expand README “Production patterns”
- Include cache lifecycle, transform registry safety guidance, and stream-mode memory expectations (especially XML mode).【F:src/Apiconvert.Core/README.md†L127-L134】

F) Tests (coverage gaps + proposed matrix)
Current strengths:
- Good runtime breadth: core conversion behavior, streaming, plan cache, compatibility, custom transforms, and doctor/lint suites are present.【F:tests/nuget/Apiconvert.Core.Tests/ConversionEngineTests.cs†L1-L20】【F:tests/nuget/Apiconvert.Core.Tests/StreamingConversionTests.cs†L1-L20】【F:tests/nuget/Apiconvert.Core.Tests/CompatibilityCheckerTests.cs†L1-L20】

Coverage gaps to add:
1) Public API approval tests
- Lock public API signatures/namespaces to protect consumers from accidental breaks.
2) Concurrency/thread-safety stress tests
- Validate plan reuse under parallel apply with transform registry variations.
3) Large-input streaming tests
- Explicit memory/latency regression tests for XML element mode and long NDJSON lines.
4) Null/default semantics tests
- Assert behavior differences between omitted defaults and explicit empty defaults (once model is improved).

G) Prioritized action list
P0 (highest adoption risk)
1) Add XML forward-only incremental streaming strategy (or clearly document temporary full-buffer limitation and target milestone).
2) Improve rule value typing surface (JSON-compatible constants/defaults) with migration-safe additive model.
3) Introduce API compatibility baseline checks in CI for NuGet public surface.

P1 (DX + maintainability)
4) Add optional interface-based engine entrypoint for DI-friendly integrations.
5) Clarify strict/report error-mode contract across top-level methods.
6) Expand README with production caching + streaming guidance.

P2 (polish)
7) Evaluate and document TFM support policy rationale.
8) Add extra stress benchmarks around transform-heavy + deeply nested rules.
