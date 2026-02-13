---
name: apiconvert-feature-delivery
description: Plan, implement, test, and review new Apiconvert features across both the .NET library and TypeScript package. Use when a request adds or changes conversion behavior, rule/contract models, or execution semantics and must remain aligned across `src/Apiconvert.Core`, `src/apiconvert-core`, and shared test cases under `tests/cases`.
---

# Apiconvert Feature Delivery

Implement features with parity across runtimes, deterministic tests, and explicit review checkpoints.

## Workflow

### 1. Frame the feature and constraints

- Restate expected behavior as input/output examples.
- Confirm whether the change affects converters, rules, contracts, or multiple layers.
- Identify compatibility constraints:
  - Existing test cases that must keep passing.
  - Public API surface changes that require README updates.

### 2. Create a short implementation plan

- Define scope in 3-6 concrete tasks:
  - Shared behavior/spec change.
  - .NET changes.
  - TypeScript changes.
  - Shared and runtime-specific tests.
  - Verification and review.
- Explicitly call out risky assumptions before coding.

### 3. Inspect current patterns before editing

- Read nearby implementations first; follow existing naming and file layout.
- Prefer extending current modules over introducing new abstractions.
- Keep behavior side-effect free in conversion paths.
- Use `references/feature-workflow.md` for path and command quick-reference.

### 4. Implement with cross-runtime parity

- Apply the behavior in `.NET` under `src/Apiconvert.Core/`.
- Apply the equivalent behavior in TypeScript under `src/apiconvert-core/`.
- Keep contracts and defaults aligned across both implementations.
- Preserve backward compatibility unless the task explicitly requires breaking changes.

### 5. Add or update tests

- Add/update shared cases under `tests/cases/` when behavior is language-agnostic.
- Add/update .NET tests under `tests/nuget/Apiconvert.Core.Tests/`.
- Add/update npm tests under `tests/npm/apiconvert-core-tests/`.
- Ensure tests are deterministic; avoid network and filesystem dependencies.
- Cover:
  - Happy path behavior.
  - Edge cases and invalid input handling.
  - Regression scenario for prior behavior that must remain stable.

### 6. Run verification commands

- Run:
  - `dotnet build Apiconvert.Core.sln`
  - `dotnet test Apiconvert.Core.sln`
  - `npm --prefix tests/npm/apiconvert-core-tests test`
- If a command cannot run, report exactly what was skipped and why.

### 7. Review before handoff

- Check parity: do .NET and TypeScript produce equivalent results for the new behavior?
- Check test impact: does each functional change have test coverage in at least one relevant layer?
- Check API/docs impact: if public surface changed, update `src/Apiconvert.Core/README.md`.
- Summarize changes by behavior, then by files touched.

### 8. Evaluate version bump and release tagging

- Always assess SemVer impact of the change:
  - `patch`: fixes and non-breaking internal changes.
  - `minor`: backward-compatible feature additions.
  - `major`: breaking behavior or contract changes.
- For this repository, release versioning is tag-driven (`vX.Y.Z`) and package/schema versions are lockstep with the tag.
- When preparing commits intended for auto-tagging, use commit subject prefixes:
  - `[major]` for major bump intent.
  - `[minor]` for minor bump intent.
  - No prefix defaults to patch bump.
- If schema behavior changes, ensure versioned schema artifacts and docs align with the intended release version.

## Completion criteria

Treat work as complete only when all are true:

- Feature behavior is implemented in both runtimes.
- Relevant shared/runtime tests are added or updated.
- Local verification commands pass, or skips are explicitly documented.
- Any public API change is reflected in docs.
- Version bump intent is explicitly stated (patch/minor/major) and release-tag readiness is confirmed.
- Residual risks and follow-ups are stated explicitly.
