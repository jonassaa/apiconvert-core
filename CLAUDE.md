# CLAUDE.md — AI Assistant Guide for apiconvert-core

This file orients AI assistants (Claude, Copilot, etc.) to the repository structure, workflows, and conventions. Read this before making any changes.

---

## What This Repository Is

`apiconvert-core` is a **rule-driven API transformation engine** shipping two production packages from a single codebase:

| Package | Runtime | Registry |
|---------|---------|----------|
| `Apiconvert.Core` | .NET (net8.0 / net10.0) | NuGet |
| `@apiconvert/core` | Node.js ≥18 / TypeScript | npm |

Both packages implement **identical behavior** from a single set of shared test cases. Cross-runtime parity is enforced by CI and must never regress.

---

## Repository Layout

```
apiconvert-core/
├── src/
│   ├── Apiconvert.Core/          # .NET library
│   │   ├── Converters/           # ConversionEngine + MappingExecutor
│   │   ├── Rules/                # Rule models
│   │   └── Contracts/            # Generation/interop contracts
│   └── apiconvert-core/          # TypeScript/npm package
│       ├── src/                  # ~22 TypeScript modules
│       ├── bin/apiconvert.js     # CLI entry point
│       └── schemas/              # Versioned JSON schemas
├── tests/
│   ├── cases/                    # Shared test cases (40+ scenarios)
│   ├── nuget/Apiconvert.Core.Tests/   # xUnit tests (.NET)
│   ├── npm/apiconvert-core-tests/     # Node test runner (TypeScript)
│   └── parity/                   # Cross-runtime parity gate
├── docs/                         # VitePress documentation site
├── scripts/                      # Build/test/docs automation
├── skills/                       # Codex agent skill definitions
├── .github/workflows/            # CI/CD workflows
├── AGENTS.md                     # Repository guidelines (read this too)
└── Apiconvert.Core.sln           # .NET solution file
```

---

## Core Architecture

### Design Principles

1. **Deterministic** — same rules + same input → same output, always.
2. **Parity** — .NET and TypeScript implementations must behave identically.
3. **Declarative** — behavior is expressed via rules, not hardcoded logic.
4. **Side-effect-free** — conversion paths have no I/O, network, or mutation.
5. **Immutable** — input payloads are never modified.

### Rule Model

Conversions are driven by a single ordered `rules` array with three node kinds:

```json
{ "kind": "field",  "target": "...", "source": { ... } }
{ "kind": "array",  "target": "...", "source": { ... }, "rules": [...] }
{ "kind": "branch", "condition": "...", "rules": [...] }
```

Source types: `path`, `constant`, `transform`, `merge`, `condition`.

Supported payload formats: `json`, `xml`, `query` (URL-encoded).

### TypeScript Public API (`src/apiconvert-core/src/index.ts`)

Key exports: `applyConversion`, `compileConversionPlan`, `parsePayload`, `formatPayload`, `normalizeConversionRules`, `validateConversionRules`, `lintConversionRules`, `bundleConversionRules`, `formatConversionRules`, `runRuleDoctor`, `checkRulesCompatibility`, `streamConversion`, `streamJsonArrayConversion`, `profileConversionPlan`, `computeRulesCacheKey`.

### .NET Public API (`src/Apiconvert.Core/Converters/ConversionEngine.cs`)

Mirrors the TypeScript surface with equivalent static methods.

---

## Build Commands

### .NET

```bash
dotnet build Apiconvert.Core.sln                                      # compile
dotnet test Apiconvert.Core.sln                                       # run tests
dotnet pack src/Apiconvert.Core/Apiconvert.Core.csproj -c Release     # produce NuGet
```

### TypeScript / npm

```bash
# from src/apiconvert-core/
npm run sync-schemas && tsc -p tsconfig.json   # build

# tests (builds automatically)
npm --prefix tests/npm/apiconvert-core-tests test

# coverage
npm --prefix tests/npm/apiconvert-core-tests run coverage
```

### Docs

```bash
bash scripts/docs-build.sh    # build VitePress site
bash scripts/docs-serve.sh    # serve locally
```

### All Coverage

```bash
bash scripts/test-coverage-all.sh
```

---

## Running Tests

### .NET (xUnit)

```bash
dotnet test Apiconvert.Core.sln
```

Test files live in `tests/nuget/Apiconvert.Core.Tests/`. Key files:
- `ConversionCasesTests.cs` — runs every shared case in `tests/cases/`
- `ConversionEngineTests.cs` — unit tests for the engine
- `StreamingConversionTests.cs` — streaming API
- `RulesLinterTests.cs` — linting diagnostics

### TypeScript (Node built-in test runner)

```bash
npm --prefix tests/npm/apiconvert-core-tests test
```

Test files (`*.test.ts`) are in `tests/npm/apiconvert-core-tests/`. Key files:
- `conversion-*.test.ts` — conversion scenarios
- `compile-plan.test.ts` — plan compilation/caching
- `parity*.test.ts` — cross-runtime parity
- `cli.test.ts` — CLI smoke tests

### Parity Gate

```bash
node ./tests/parity/parity-gate.mjs \
  --report tests/parity/parity-report.json \
  --summary tests/parity/parity-summary.json \
  --max-mismatches 0
```

Zero mismatches are required. If parity breaks, fix both runtimes.

### CLI Smoke Tests

```bash
node ./src/apiconvert-core/bin/apiconvert.js rules validate ./tests/cases/basic-json/rules.json
node ./src/apiconvert-core/bin/apiconvert.js rules lint ./tests/cases/basic-json/rules.json
node ./src/apiconvert-core/bin/apiconvert.js convert \
  --rules ./tests/cases/basic-json/rules.json \
  --input ./tests/cases/basic-json/input.json
```

---

## Shared Test Cases

`tests/cases/` is the single source of truth for behavioral expectations. Each subdirectory is one scenario:

```
tests/cases/<scenario-name>/
├── rules.json      # conversion rules
├── input.json      # (or .xml / .txt)
└── output.json     # expected result
```

**When adding a feature:** add a shared case here first so both runtimes pick it up automatically.

---

## Adding Features — Checklist

Every feature that affects conversion behavior must:

- [ ] Add or update a shared case in `tests/cases/`
- [ ] Implement in **both** `.NET` (`src/Apiconvert.Core/`) and **TypeScript** (`src/apiconvert-core/src/`)
- [ ] Pass `dotnet test Apiconvert.Core.sln`
- [ ] Pass `npm --prefix tests/npm/apiconvert-core-tests test`
- [ ] Pass the parity gate (zero mismatches)
- [ ] If adding public API: update `src/Apiconvert.Core/README.md` with usage examples
- [ ] If adding a rule kind, source type, or transform: update `schemas/` and bump appropriately

---

## Schema Versioning

Schemas live at `src/apiconvert-core/schemas/rules/`.

- **Versioned** (immutable): `vX.Y.Z/schema.json` — never modify after release
- **Current alias** (mutable): `current/schema.json` — points to latest
- Schema versions are lockstep with package SemVer (git tag `vX.Y.Z`)

Rules:
- Backward-incompatible changes require a new major version
- New optional fields are minor bumps
- Never edit a versioned schema that has been published

---

## Code Style

### C# (`.NET`)

- Nullable: `<Nullable>enable</Nullable>` — use `?` and null-check where needed
- Implicit usings enabled
- **4 spaces**, no tabs
- `PascalCase` for types and public members; `camelCase` for locals and parameters
- File names match their primary type: `ConversionEngine.cs` → `ConversionEngine`
- Partial classes for large types: `MappingExecutor.cs`, `MappingExecutor.RuleHandlers.cs`, etc.

### TypeScript

- `strict: true` in tsconfig
- **kebab-case** file names: `condition-expression-evaluator.ts`
- `PascalCase` types/interfaces; `camelCase` functions/variables
- All exported types go in `types.ts`; enums for constants
- Target: ES2020, CommonJS modules

### Both Runtimes

- No side effects in conversion paths
- No `throw` in hot paths — prefer returning error/diagnostic objects
- Prefer explicit over implicit — name things clearly

---

## CI/CD Overview

Workflows in `.github/workflows/`:

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `build-and-test.yml` | push to main / PRs | Build, test, parity gate, CLI smoke tests |
| `publish-packages.yml` | git tag `vX.Y.Z` | Publish to NuGet + npm |
| `create-release-tag.yml` | manual dispatch | Create version tag |
| `docs-pages.yml` | push to main | Deploy VitePress docs to GitHub Pages |

The parity gate runs in CI with `--max-mismatches 0`. It must pass for every merge.

---

## Commit and PR Conventions

- **Commit messages**: short imperative phrases, e.g. `Add array flattening rule`, `Fix condition evaluation for null paths`
- **PRs should include**: summary, rationale, and note of any API surface changes
- Run both test suites locally before pushing

---

## Scope Boundaries — What This Repo Is NOT

Do not add:
- HTTP handling, auth, middleware, or proxy logic
- Database or persistence concerns
- UI components or rule builders
- Orchestration, workflow, or message bus logic
- Infrastructure or deployment configuration (beyond the existing Dockerfile)

If a feature can't be expressed as a conversion rule or engine primitive, it likely belongs outside this repo.

---

## Documentation

`docs/` is a **VitePress** site for **package consumers only**.

- `docs/concepts/` — core concepts (rules model, lifecycle, determinism)
- `docs/reference/` — rule nodes, sources, transforms, conditions, CLI
- `docs/recipes/` — common patterns (arrays, branches, JSON↔XML, merge/split)
- `docs/runtime-guides/` — streaming, caching, performance, custom transforms
- `docs/getting-started/` — quick start

Do **not** add internal process notes, coverage audits, or implementation status to `docs/`. Use `internal-docs/plans/` for that.

---

## Available Skills (Codex)

Skill definitions live in `skills/`. Invoke via `@<skill-name>` in Codex:

| Skill | Purpose |
|-------|---------|
| `apiconvert-core-power-consumer` | Consumer friction findings and mini-RFCs |
| `apiconvert-feature-delivery` | Plan + implement features across both runtimes |
| `apiconvert-dotnet-api-review` | .NET API design review |
| `apiconvert-node-api-review` | TypeScript/Node API design review |
| `apiconvert-production-consumer-review` | Production-readiness review |

---

## Task Tracking (Notion — optional)

If Notion integration is configured, load settings from `.codex/local.env` (git-ignored):

```bash
APICONVERT_NOTION_DATABASE_URL=https://www.notion.so/...
APICONVERT_NOTION_DATA_SOURCE_ID=collection://...
APICONVERT_CODEX_INSTANCE_ID=<your-identifier>
```

See `AGENTS.md` for full Notion task property conventions.

---

## Quick Reference

```bash
# Build everything
dotnet build Apiconvert.Core.sln
npm --prefix tests/npm/apiconvert-core-tests test   # also builds TS

# Run all tests
dotnet test Apiconvert.Core.sln
npm --prefix tests/npm/apiconvert-core-tests test

# Verify parity
node ./tests/parity/parity-gate.mjs \
  --report tests/parity/parity-report.json \
  --summary tests/parity/parity-summary.json \
  --max-mismatches 0

# CLI sanity check
node ./src/apiconvert-core/bin/apiconvert.js rules validate ./tests/cases/basic-json/rules.json
```
