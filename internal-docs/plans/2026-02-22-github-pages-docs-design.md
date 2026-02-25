# GitHub Pages Documentation Site Design

**Date:** 2026-02-22
**Project:** Apiconvert.Core
**Status:** Approved

## Goal
Build a comprehensive GitHub Pages documentation site for Apiconvert.Core using MkDocs + Material, with a top-level runtime selector that switches between .NET and TypeScript content while keeping one unified documentation flow.

## Audience
- New adopters evaluating Apiconvert.Core
- Existing users needing deep reference documentation

## Chosen Approach
Approach 1: Single docs source + runtime toggle + versioned deploy (`mike`).

### Why this approach
- Maintains one canonical conceptual narrative across runtimes
- Preserves cross-runtime parity intent
- Minimizes duplication and long-term drift
- Supports a strong onboarding experience and fast reference lookup

## Information Architecture
Top-level navigation:
- Start Here
- Core Concepts
- Runtime Guides
- Rules Reference
- Recipes
- Diagnostics & Troubleshooting
- Schema & Versioning
- Parity & Testing
- CLI & Tooling
- Contributing

## Detailed Page Map
### Start Here
- Overview
- Install (.NET / npm)
- First Conversion (runtime-switched walkthrough)
- Migration from hand-written mappers

### Core Concepts
- Architectural Intent & Boundaries
- Determinism and Side Effects
- Canonical Rules Model
- Conversion Lifecycle (parse -> normalize -> apply -> format)

### Runtime Guides
- Runtime Selector UX
- .NET API Usage Guide
- TypeScript API Usage Guide
- Streaming in .NET and Node
- Custom Transform Plugins
- Performance and plan caching

### Rules Reference
- Rule node types (field/array/branch/use)
- Sources and transforms
- Condition expression grammar
- Merge modes and collision policy
- Fragments and includes
- Schema validation behavior (strict/lenient)

### Recipes
- JSON <> XML patterns
- JSON <> query patterns
- Array/branch/merge/split cookbook
- Diagnostics-first authoring flow

### Diagnostics & Troubleshooting
- Error code catalog
- Lint diagnostics reference
- Rule doctor workflow
- Troubleshooting decision tree

### Schema & Versioning
- Schema compatibility contract
- SemVer lockstep policy
- Version pinning (`current` vs `vX.Y.Z`)
- Breaking change policy

### Parity & Testing
- Shared test cases model
- .NET + npm parity workflow
- Parity gate CI integration
- How to add a new shared case

### CLI & Tooling
- CLI command reference
- Rules bundling
- Plan profiling
- Compatibility checks

### Contributing
- Local dev setup
- Docs contribution guide
- Release flow (tag-driven)
- Governance for rule/schema changes

## Technical Design
- Tooling: MkDocs + Material
- Deployment target: GitHub Pages via `gh-pages` branch, deployed by GitHub Actions
- Versioning: enabled from day one via `mike`
- Content source: replace existing `/docs` with the new site content and structure
- Runtime switching: top app-level selector in header (`.NET`, `TypeScript`)
- Runtime visibility: non-selected runtime content is fully hidden

### Runtime selector behavior
- Selector state persisted in `localStorage`
- Selector applied globally across pages
- Runtime-specific content blocks tagged by runtime class
- Untagged content remains always visible

## Validation Strategy
- `mkdocs build --strict` required in CI
- Link and nav integrity validated on docs pull requests
- Runtime selector behavior validated with Playwright checks:
  - default runtime rendering
  - selector toggle persistence
  - non-selected runtime content hidden

## Maintainability Strategy
- Unified pages with runtime-tagged snippets only where needed
- Docs authoring guide defines tagging, parity expectations, and version update workflow
- Canonical reference pages for rules/schema/diagnostics to prevent drift

## Migration Policy
- Replace existing `/docs` content with the new docs site structure
- Preserve key legacy knowledge by integrating into the new IA rather than keeping disconnected legacy pages

## Non-Goals
- Building UI rule editors
- Adding runtime behavior not present in package APIs
- Introducing non-deterministic or integration-layer guidance into core docs

## Open Follow-ups
- Confirm first published docs version label strategy (`latest` + semantic versions)
- Decide how to map release tags to version publish automation cadence
