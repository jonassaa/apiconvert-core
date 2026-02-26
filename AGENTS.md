# Repository Guidelines

## Intent
This repo hosts the shared core for Apiconvert, shipping both a .NET library (NuGet) and a TypeScript package (npm) that implement the same conversion contracts and test cases.
- Features should be implemented in both .NET and JS so all shared conversion cases pass across languages.

## Architectural Intent
Apiconvert.Core is a rule-driven API transformation engine.
- Keep conversion behavior declarative via rules, not hardcoded integration logic.
- Keep the conversion engine deterministic, side-effect free, and testable in isolation.
- Keep runtime behavior aligned across .NET and npm implementations.

## Architectural Boundaries
- Conversion logic only: no HTTP, auth, database, UI, or infrastructure concerns.
- Rule-driven over code-driven: prefer extending rule schema/evaluation/primitives over adding custom conditionals.
- No I/O or external service calls in conversion paths.
- Avoid runtime-specific constructs leaking into rule definitions.

## Non-Goals
Apiconvert.Core is not:
- An API gateway
- Middleware server
- HTTP proxy
- Auth provider
- Persistence layer
- UI rule builder
- Orchestration, message bus, or workflow engine

## Schema Contract
- Treat the rules schema as a compatibility contract across runtimes.
- Changes to rule structure, validation behavior, or rule/source types must preserve backward compatibility or be explicitly versioned.
- New schema capabilities should include matching support in both .NET and npm runtimes.

## Implementation Decision Checks
When adding or changing behavior, validate:
1. Does this belong in the conversion engine rather than application/integration layers?
2. Can this be expressed declaratively via rules?
3. Does this preserve determinism and avoid side effects?
4. Does this maintain cross-platform parity and shared case compatibility?

## Project Structure & Module Organization
- `Apiconvert.Core.sln` is the solution entry point.
- `.github/` contains CI workflows and GitHub metadata.
- `schemas/` holds shared JSON schema artifacts.
- `src/Apiconvert.Core/` is the .NET (NuGet) library with core modules under:
  - `Converters/` for conversion execution (`ConversionEngine`).
  - `Rules/` for rule models and configuration.
  - `Contracts/` for generation/interop contracts.
- `src/apiconvert-core/` is the npm package (TypeScript).
- `tests/cases/` contains shared conversion cases used by test runners.
- `tests/nuget/Apiconvert.Core.Tests/` contains the xUnit test project for .NET.
- `tests/npm/apiconvert-core-tests/` contains Node/TypeScript tests for the npm package.
- `src/Apiconvert.Core/Dockerfile` provides a container build path if needed.

## Build, Test, and Development Commands
- `dotnet build Apiconvert.Core.sln` — compile the solution.
- `dotnet test Apiconvert.Core.sln` — run .NET (NuGet) tests.
- `dotnet pack src/Apiconvert.Core/Apiconvert.Core.csproj -c Release` — produce the NuGet package.
- `dotnet build src/Apiconvert.Core/Apiconvert.Core.csproj` — compile only the core library.
- `npm --prefix tests/npm/apiconvert-core-tests test` — run npm package tests (runs build + node test runner).

## Coding Style & Naming Conventions
- C# with `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>`.
- Indentation: 4 spaces, no tabs.
- Types, public members: `PascalCase`; locals/parameters: `camelCase`.
- File names match the primary type (e.g., `ConversionEngine.cs`).
- Prefer explicit, side-effect-free methods in core conversion paths.

## Testing Guidelines
- Tests use xUnit.
- Name test projects `*.Tests` and test files `*Tests.cs`.
- Place .NET tests under `tests/nuget/Apiconvert.Core.Tests/` mirroring `src/` namespaces.
- Place npm/TS tests under `tests/npm/apiconvert-core-tests/`.
- Keep unit tests deterministic; avoid network and filesystem dependencies.
- Prefer shared `tests/cases/` coverage for rule behavior so .NET and npm stay behaviorally aligned.

## Commit & Pull Request Guidelines
- Existing history is minimal and does not show a convention. Use short, imperative commit messages (e.g., `Add conversion rule validation`).
- PRs should include: a clear summary, rationale, and any API changes.
- If you add public surface area, update `src/Apiconvert.Core/README.md` with usage examples.

## Documentation Scope
- Keep `docs/` focused on package-consumer usage only (API usage, configuration, examples, and troubleshooting).
- Do not add internal process artifacts in `docs/` (for example: coverage audit checklists, task tracking notes, or implementation status pages).
- Store implementation plans in `internal-docs/plans/` (not under `docs/`).

## Configuration & Security Notes
- The package metadata is defined in `src/Apiconvert.Core/Apiconvert.Core.csproj`.
- License metadata is set to `LicenseRef-Proprietary`; confirm before publishing.

## Task Tracking (Notion)
- Load Notion task tracker settings from local env vars (do not hardcode private workspace ids in tracked files):
  - `APICONVERT_NOTION_DATABASE_URL` (e.g., `https://www.notion.so/...`)
  - `APICONVERT_NOTION_DATA_SOURCE_ID` (e.g., `collection://...`)
  - `APICONVERT_CODEX_INSTANCE_ID` (stable local identifier for this Codex instance, e.g., `jonas-mbp`)
- Local private config location: `.codex/local.env` (git-ignored).
- When work produces tasks, plans, proposals, or follow-up actions, write them as pages in the data source from `APICONVERT_NOTION_DATA_SOURCE_ID` (not only in chat output).
- If any required env var is missing, ask the user for values before creating/updating Notion pages.
- Use these properties when creating/updating tasks:
  - `Name` (required title)
  - `Status` (`Backlog`, `Ready`, `In Progress`, `Blocked`, `In Review`, `Done`)
  - `Priority` (`P0`, `P1`, `P2`, `P3`)
  - `Owner`, `Due Date`, `Area`, `Tags`, `Estimate (pts)`, `Blocked`, `Target Version`, `Spec/PR Link`, `Codex Instance ID`, `Codex Task ID`
- Tracking rules:
  - Set the actively worked task to `Status = In Progress`.
  - Set `Codex Instance ID` from `APICONVERT_CODEX_INSTANCE_ID`.
  - Set `Codex Task ID` as `<APICONVERT_CODEX_INSTANCE_ID>:<short-task-slug>` and keep it stable across updates.
- Default conventions for generated items unless user specifies otherwise:
  - `Status`: `Backlog` for queued tasks, `In Progress` for the current active task
  - `Priority`: `P2`
  - `Area`: best-fit from the schema (`Rules Engine`, `Schema Contract`, `Dotnet Runtime`, `Npm Runtime`, `Shared Test Cases`, `Docs`, `Release`)
  - `Tags`: include `parity` when work affects both runtimes.

## Skills
A skill is a set of local instructions to follow that is stored in a `SKILL.md` file.

### Available skills
- `apiconvert-core-power-consumer`: Act as a demanding package consumer and produce adoption-focused friction findings, prioritized requests, and implementable mini-RFCs with .NET/JS parity expectations. (file: `skills/apiconvert-core-power-consumer/SKILL.md`)
- `apiconvert-feature-delivery`: Plan, implement, test, and review new Apiconvert features across both runtimes with parity checks and deterministic test coverage. (file: `skills/apiconvert-feature-delivery/SKILL.md`)
- `apiconvert-dotnet-api-review`: Review the .NET package surface/API design and delivery quality from an external consumer lens, with concrete file/symbol findings and migration-safe recommendations. (file: `skills/apiconvert-dotnet-api-review/SKILL.md`)
- `apiconvert-node-api-review`: Review the Node/TypeScript package surface/API design and delivery quality from an external consumer lens, with concrete file/symbol findings and migration-safe recommendations. (file: `skills/apiconvert-node-api-review/SKILL.md`)
- `apiconvert-production-consumer-review`: Review either runtime (`TARGET=.NET` or `TARGET=Node`) as a production adopter focused on observability, resilience, compatibility, and long-term maintainability. (file: `skills/apiconvert-production-consumer-review/SKILL.md`)
