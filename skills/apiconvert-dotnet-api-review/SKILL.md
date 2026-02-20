---
name: apiconvert-dotnet-api-review
description: Review the .NET/C# Apiconvert.Core library as a publishable external package with focus on public API design, developer experience, correctness, performance, packaging, maintainability, and tests. Use when asked for a deep .NET library review with concrete file/symbol findings and migration-safe recommendations.
---

# Apiconvert .NET API Review

Review only the .NET/C# library in this repository from an external consumer perspective.

## Scope

- Include only .NET/C# library code.
- Exclude Node/TypeScript/JavaScript and any app/frontend concerns.
- Focus on public surface area, DX, correctness, performance, packaging, and maintainability.
- Assume purpose: composable conversion engine driven by rules/configuration.

## Workflow

1. Map the library:
   - Identify entry points, namespaces, abstractions, and conversion pipeline flow.
   - Describe the happy path: configure rules -> instantiate engine -> run conversion -> consume output.
2. Review public API:
   - Breaking-change risk, naming consistency, cohesion, layering, leaky abstractions.
   - Excessive generic complexity, unclear responsibilities, exposed internals.
3. Review reliability and correctness:
   - Nullability, error handling, exception strategy, validation boundaries, invariants.
   - Determinism and thread-safety assumptions.
4. Review performance:
   - Hotspots: allocations, regex, serialization, LINQ in hot paths, large object graphs.
   - Streaming vs buffering, caching opportunities, avoidable IO.
5. Review packaging:
   - NuGet metadata, TFMs, versioning strategy, strong naming (if relevant), XML docs, analyzers.
   - README quality: getting started, examples, configuration reference.
6. Review tests:
   - Unit/integration quality, deterministic fixtures, realism of conversion scenarios.
   - Recommend additional tests for edge cases.

## Deliverable format

Produce these sections:

- A) Executive summary (5-10 bullets: strengths + top risks)
- B) Public API review (keep/change + concrete rename/restructure proposals)
- C) Correctness and edge cases (issues + suggested fixes)
- D) Performance notes (important vs non-critical + quick wins)
- E) Packaging and docs (exact file changes)
- F) Tests (coverage gaps + proposed test matrix)
- G) Prioritized action list (P0/P1/P2 with rationale)

Include at least 3 C# snippets showing ideal consumer experience.

## Rules

- Every problem claim must cite exact file path and symbol(s).
- Prefer minimal breaking change; if breaking change is required, provide a migration path.

## Findings persistence

Always append the full report to `.codex/review-findings.md` (Git-ignored):

1. Create the file if missing.
2. Append, do not overwrite.
3. Add a header with timestamp and scope.

Use this template:

```markdown
## [apiconvert-dotnet-api-review] YYYY-MM-DD HH:mm:ss

{report}
```
