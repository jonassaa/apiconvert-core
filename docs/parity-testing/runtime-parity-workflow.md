# Runtime Parity Workflow

Run this workflow for any change that can affect conversion behavior.

## Commands

```bash
dotnet test Apiconvert.Core.sln
npm --prefix tests/npm/apiconvert-core-tests test
npm --prefix tests/npm/apiconvert-core-tests run parity:check
```

## Triage order when parity fails

1. Confirm failing case input/rules fixture.
2. Compare runtime diagnostics and output diffs.
3. Reproduce with focused tests per runtime.
4. Add/adjust shared case to lock intended behavior.

