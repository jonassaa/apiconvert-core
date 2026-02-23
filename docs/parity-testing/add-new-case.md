# Add a New Shared Case

## Step-by-step

1. Create `tests/cases/<case-name>/`.
2. Add `rules.json`.
3. Add input fixture(s).
4. Add expected output fixture(s).
5. Run both runtime test suites.
6. Run parity checks.

## Validation commands

```bash
dotnet test Apiconvert.Core.sln
npm --prefix tests/npm/apiconvert-core-tests test
npm --prefix tests/npm/apiconvert-core-tests run parity:check
```

## Naming guidance

- Use behavior-focused case names.
- Keep one primary concern per case.
- Use suffixes for format-specific variants when needed.

