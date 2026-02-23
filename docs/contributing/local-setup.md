# Local Setup

## Build and test

```bash
dotnet build Apiconvert.Core.sln
dotnet test Apiconvert.Core.sln
npm --prefix tests/npm/apiconvert-core-tests test
```

## Parity checks

```bash
npm --prefix tests/npm/apiconvert-core-tests run parity:check
```

## Docs development

```bash
npm --prefix docs ci
npm --prefix docs run docs:dev
npm --prefix docs run docs:build
```

