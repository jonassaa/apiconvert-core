# Runtime Parity Workflow

Run both suites when changing conversion behavior:

```bash
dotnet test Apiconvert.Core.sln
npm --prefix tests/npm/apiconvert-core-tests test
```

Then run parity gate tooling.
