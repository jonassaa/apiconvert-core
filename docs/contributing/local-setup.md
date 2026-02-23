# Local Setup

```bash
dotnet build Apiconvert.Core.sln
dotnet test Apiconvert.Core.sln
npm --prefix tests/npm/apiconvert-core-tests test
```

For docs:

```bash
npm --prefix docs ci
npm --prefix docs run docs:dev
```
