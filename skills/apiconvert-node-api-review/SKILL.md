---
name: apiconvert-node-api-review
description: Review the Node.js/TypeScript apiconvert-core package as a publishable external library with focus on public API ergonomics, type design, correctness, performance, packaging/distribution, maintainability, and tests. Use when asked for a deep TypeScript package review with concrete file/symbol findings and migration-safe recommendations.
---

# Apiconvert Node API Review

Review only the Node.js/TypeScript library in this repository from an external consumer perspective.

## Scope

- Include only Node/TypeScript/JavaScript library code.
- Exclude .NET/C# and any app/frontend concerns.
- Focus on public surface area, DX, correctness, performance, packaging, and maintainability.
- Assume purpose: composable conversion engine driven by rules/configuration.

## Workflow

1. Map the library:
   - Identify primary exports, entry points, key modules, and conversion pipeline flow.
   - Describe the happy path: configure rules -> instantiate engine -> run conversion -> consume output.
2. Review public API and DX:
   - Export hygiene (`index.ts`), naming consistency, module boundaries, sync vs async choices.
   - Type ergonomics: generics/unions, runtime validation alignment, inference quality.
3. Review reliability and correctness:
   - Error handling strategy, validation, null/undefined behavior, determinism.
   - Concurrency assumptions and async race risk.
4. Review performance:
   - Parse/stringify patterns, deep clone costs, regex hotspots, large array behavior.
   - Streaming vs buffering, dependency weight, tree-shaking implications, ESM/CJS compatibility.
5. Review packaging and distribution:
   - `package.json` fields (`exports`, `types`, `main`/`module`, `sideEffects`, `files`, `engines`).
   - Build output (`tsconfig`), declarations, sourcemaps, lint/format workflow.
6. Review tests:
   - Unit vs integration balance, deterministic fixtures, snapshot discipline.
   - Recommend a test matrix reflecting real conversions and edge cases.

## Deliverable format

Produce these sections:

- A) Executive summary (5-10 bullets: strengths + top risks)
- B) Public API review (keep/change + concrete export/restructure proposals)
- C) Types and runtime validation alignment (issues + suggested fixes)
- D) Correctness and edge cases (issues + suggested fixes)
- E) Performance notes (quick wins + benchmark targets)
- F) Packaging and docs (exact `package.json`/`tsconfig`/README changes)
- G) Tests (coverage gaps + proposed matrix)
- H) Prioritized action list (P0/P1/P2 with rationale)

Include at least 3 TypeScript snippets showing ideal consumer experience.

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
## [apiconvert-node-api-review] YYYY-MM-DD HH:mm:ss

{report}
```
