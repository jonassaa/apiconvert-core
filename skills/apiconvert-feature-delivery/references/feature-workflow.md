# Apiconvert Feature Workflow Reference

## Repository Map

- Solution entry: `Apiconvert.Core.sln`
- .NET library: `src/Apiconvert.Core/`
- TypeScript package: `src/apiconvert-core/`
- Shared test cases: `tests/cases/`
- .NET tests: `tests/nuget/Apiconvert.Core.Tests/`
- npm tests: `tests/npm/apiconvert-core-tests/`

## Core Command Set

- Build all: `dotnet build Apiconvert.Core.sln`
- Test all .NET: `dotnet test Apiconvert.Core.sln`
- Test npm package: `npm --prefix tests/npm/apiconvert-core-tests test`
- Pack NuGet (when needed):
  - `dotnet pack src/Apiconvert.Core/Apiconvert.Core.csproj -c Release`

## Feature Parity Checklist

- Define expected behavior from example input/output.
- Implement behavior in both runtimes.
- Keep rule/contract names and defaults equivalent.
- Add or update shared case(s) when behavior is runtime-independent.
- Add targeted .NET and npm tests for runtime-specific edges.
- Run both test suites before handoff.

## Review Checklist

- Is there any runtime mismatch in behavior, naming, or defaults?
- Are regressions covered by tests?
- Are error messages/validation semantics consistent enough across runtimes?
- Did public API changes require docs updates?
