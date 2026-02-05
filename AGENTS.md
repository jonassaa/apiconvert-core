# Repository Guidelines

## Intent
This repo hosts the shared core for Apiconvert, shipping both a .NET library (NuGet) and a TypeScript package (npm) that implement the same conversion contracts and test cases.
- Features should be implemented in both .NET and JS so all shared conversion cases pass across languages.

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
- `dotnet test Apiconvert.Core.sln --settings coverlet.runsettings --collect:"XPlat Code Coverage"` — run tests with coverage (Cobertura output under `TestResults/`).
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

## Commit & Pull Request Guidelines
- Existing history is minimal and does not show a convention. Use short, imperative commit messages (e.g., `Add conversion rule validation`).
- PRs should include: a clear summary, rationale, and any API changes.
- If you add public surface area, update `src/Apiconvert.Core/README.md` with usage examples.

## Configuration & Security Notes
- The package metadata is defined in `src/Apiconvert.Core/Apiconvert.Core.csproj`.
- License metadata is set to `LicenseRef-Proprietary`; confirm before publishing.
