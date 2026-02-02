# Repository Guidelines

## Project Structure & Module Organization
- `Apiconvert.Core.sln` is the solution entry point.
- Source lives in `src/Apiconvert.Core/` with core modules under:
  - `Converters/` for conversion execution (`ConversionEngine`).
  - `Rules/` for rule models and configuration.
  - `Contracts/` for generation/interop contracts.
- `tests/` exists but is currently empty; add new test projects here (e.g., `tests/Apiconvert.Core.Tests/`).
- `src/Apiconvert.Core/Dockerfile` provides a container build path if needed.

## Build, Test, and Development Commands
- `dotnet build Apiconvert.Core.sln` — compile the solution.
- `dotnet test Apiconvert.Core.sln` — run unit tests.
- `dotnet test Apiconvert.Core.sln --settings coverlet.runsettings --collect:"XPlat Code Coverage"` — run tests with coverage (Cobertura output under `TestResults/`).
- `dotnet pack src/Apiconvert.Core/Apiconvert.Core.csproj -c Release` — produce the NuGet package.
- `dotnet build src/Apiconvert.Core/Apiconvert.Core.csproj` — compile only the core library.

## Coding Style & Naming Conventions
- C# with `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>`.
- Indentation: 4 spaces, no tabs.
- Types, public members: `PascalCase`; locals/parameters: `camelCase`.
- File names match the primary type (e.g., `ConversionEngine.cs`).
- Prefer explicit, side-effect-free methods in core conversion paths.

## Testing Guidelines
- Tests use xUnit.
- Name test projects `*.Tests` and test files `*Tests.cs`.
- Place tests under `tests/` mirroring `src/` namespaces.
- Keep unit tests deterministic; avoid network and filesystem dependencies.

## Commit & Pull Request Guidelines
- Existing history is minimal and does not show a convention. Use short, imperative commit messages (e.g., `Add conversion rule validation`).
- PRs should include: a clear summary, rationale, and any API changes.
- If you add public surface area, update `src/Apiconvert.Core/README.md` with usage examples.

## Configuration & Security Notes
- The package metadata is defined in `src/Apiconvert.Core/Apiconvert.Core.csproj`.
- License metadata is set to `LicenseRef-Proprietary`; confirm before publishing.
