# Troubleshooting Tree

Use this sequence to isolate failures quickly.

## Decision flow

1. **Parse fails**: confirm input text matches selected format (`json`, `xml`, `query`).
2. **Strict normalize fails**: fix schema/rule shape issues first.
3. **Runtime conversion errors**: inspect `rulePath`, source type, and path resolution.
4. **Unexpected output**: enable trace mode and compare branch decisions.
5. **Parity mismatch**: run shared case in both runtimes and inspect parity report.

## Quick commands

```bash
dotnet test Apiconvert.Core.sln
npm --prefix tests/npm/apiconvert-core-tests test
npm --prefix tests/npm/apiconvert-core-tests run parity:check
```

