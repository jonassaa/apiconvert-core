# Conversion Error Codes Documentation Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Publish complete, conversion-focused error code documentation that is accurate for both .NET and TypeScript runtimes and guarded by automated docs coverage checks.

**Architecture:** Keep error-code definitions as source of truth in runtime code, and make docs a generated-by-hand reference that is continuously validated by a smoke test. Scope this plan to conversion diagnostics (`ACV-RUN-*`) plus streaming conversion diagnostics (`ACV-STR-*`), and avoid mixing in lint/doctor/compatibility families. Add deterministic test coverage so docs fail CI if codes drift.

**Tech Stack:** Markdown docs (VitePress), Node.js smoke tests, C# and TypeScript runtime sources, npm test runner.

---

### Task 1: Define Scope And Canonical Code Inventory

**Files:**
- Modify: `docs/plans/2026-02-26-conversion-error-codes-documentation.md`
- Inspect: `src/Apiconvert.Core/Converters/MappingExecutor.cs`
- Inspect: `src/Apiconvert.Core/Converters/MappingExecutor.RuleHandlers.cs`
- Inspect: `src/Apiconvert.Core/Converters/MappingExecutor.SourceResolvers.cs`
- Inspect: `src/Apiconvert.Core/Converters/ConversionEngine.cs`
- Inspect: `src/apiconvert-core/src/mapping-engine.ts`
- Inspect: `src/apiconvert-core/src/rule-executor.ts`
- Inspect: `src/apiconvert-core/src/source-resolver.ts`

**Step 1: Capture conversion-only code families and explicit code list**

```text
Target families: ACV-RUN-*, ACV-STR-*
Expected codes today:
ACV-RUN-000, 100, 101, 102, 103, 201, 202, 203, 301, 302, 800, 900, 901
ACV-STR-001
```

**Step 2: Confirm code parity across runtimes**

Run:

```bash
rg -n "ACV-(RUN|STR)-[0-9]{3}" src/Apiconvert.Core src/apiconvert-core -S
```

Expected: Same conversion code set appears across both runtime implementations.

**Step 3: Record severity expectations for each code**

Run:

```bash
rg -n "ACV-(RUN|STR)-[0-9]{3}" src/Apiconvert.Core src/apiconvert-core -S
```

Expected: Each code location can be mapped to `Error` or `Warning` from `AddError`/`AddWarning` (C#) and equivalent TS helpers.

**Step 4: Commit planning metadata**

```bash
git add docs/plans/2026-02-26-conversion-error-codes-documentation.md
git commit -m "docs: plan conversion error code documentation"
```

### Task 2: Replace Generic Error-Codes Page With Conversion Code Reference

**Files:**
- Modify: `docs/troubleshooting/error-codes.md`
- Reference: `docs/reference/validation-and-diagnostics.md`
- Reference: `tests/npm/apiconvert-core-tests/src/conversion-diagnostics.test.ts`
- Reference: `tests/nuget/Apiconvert.Core.Tests/ConversionEngineTests.cs`

**Step 1: Write failing docs-content expectations (table structure and coverage marker)**

Add required anchors/markers to be validated by a new smoke test:

```md
## Conversion Runtime Codes (ACV-RUN)
## Streaming Conversion Codes (ACV-STR)
<!-- ACV-CODES-TABLE-START -->
...
<!-- ACV-CODES-TABLE-END -->
```

**Step 2: Draft conversion code table with one row per code**

Each row should include:

```md
| Code | Severity | Trigger | Typical rulePath | What to do |
```

Include deterministic trigger language, for example:
- `ACV-RUN-101`: array input path missing (`Warning`)
- `ACV-RUN-103`: collision policy `Error` and duplicate output path (`Error`)
- `ACV-STR-001`: stream input parse/read failure (`Error`)

**Step 3: Add triage workflow specific to conversion failures**

Add numbered runbook:
1. Start with first `Error` diagnostic code.
2. Use `rulePath` to isolate failing rule.
3. Re-run with explain mode for trace context.
4. Resolve source/condition expression issues before collision policy tuning.

**Step 4: Link back to validation and diagnostics reference without duplicating it**

Ensure page keeps short “Related” section and routes to reference docs for broader diagnostic families.

**Step 5: Commit**

```bash
git add docs/troubleshooting/error-codes.md
git commit -m "docs: document conversion runtime error codes"
```

### Task 3: Add Docs Coverage Smoke Test For Error Code Drift

**Files:**
- Create: `tests/docs/error-codes-doc-coverage.smoke.mjs`
- Modify: `tests/docs/run-docs-smoke.mjs`

**Step 1: Write failing smoke test that compares runtime code set to docs table**

Test behavior:
- Parse `docs/troubleshooting/error-codes.md`
- Extract codes from markdown table between table markers.
- Extract codes from runtime sources using regex.
- Fail if any runtime code is undocumented or if docs include unknown conversion code.

Starter assertion logic:

```js
assert.deepEqual([...docCodes].sort(), [...runtimeCodes].sort());
```

**Step 2: Wire test into docs smoke runner**

Add to `commands` in `tests/docs/run-docs-smoke.mjs`:

```js
"node tests/docs/error-codes-doc-coverage.smoke.mjs"
```

**Step 3: Run test to verify initial failure (before docs/table is complete)**

Run:

```bash
node tests/docs/error-codes-doc-coverage.smoke.mjs
```

Expected: `FAIL` with missing/extra code details.

**Step 4: Re-run after docs update to verify pass**

Run:

```bash
node tests/docs/run-docs-smoke.mjs
```

Expected: `All docs smoke tests passed.`

**Step 5: Commit**

```bash
git add tests/docs/error-codes-doc-coverage.smoke.mjs tests/docs/run-docs-smoke.mjs
git commit -m "test: enforce conversion error code docs coverage"
```

### Task 4: Fix Consumer Links That Still Point To Old Diagnostics Path

**Files:**
- Modify: `src/Apiconvert.Core/README.md`
- Modify: `src/apiconvert-core/README.md`

**Step 1: Write failing grep check for stale link**

Run:

```bash
rg -n "docs/diagnostics/error-codes.md" src/Apiconvert.Core/README.md src/apiconvert-core/README.md -S
```

Expected: Matches found before fix.

**Step 2: Update links to current docs path**

Target:

```text
../../docs/troubleshooting/error-codes.md
```

**Step 3: Verify stale links removed**

Run:

```bash
rg -n "docs/diagnostics/error-codes.md" src/Apiconvert.Core/README.md src/apiconvert-core/README.md -S
```

Expected: No matches.

**Step 4: Commit**

```bash
git add src/Apiconvert.Core/README.md src/apiconvert-core/README.md
git commit -m "docs: update runtime readme error code links"
```

### Task 5: End-To-End Verification

**Files:**
- Verify: `docs/troubleshooting/error-codes.md`
- Verify: `tests/docs/error-codes-doc-coverage.smoke.mjs`
- Verify: `tests/docs/run-docs-smoke.mjs`

**Step 1: Run docs smoke suite**

```bash
node tests/docs/run-docs-smoke.mjs
```

Expected: All docs smoke checks pass, including error-code coverage.

**Step 2: Run runtime diagnostic tests to confirm referenced examples still valid**

```bash
dotnet test tests/nuget/Apiconvert.Core.Tests/Apiconvert.Core.Tests.csproj --filter "ConversionEngineTests"
npm --prefix tests/npm/apiconvert-core-tests test -- conversion-diagnostics.test.ts
```

Expected: Conversion diagnostics tests pass in both runtimes.

**Step 3: Optional full confidence run**

```bash
dotnet test Apiconvert.Core.sln
npm --prefix tests/npm/apiconvert-core-tests test
```

Expected: No regressions.

**Step 4: Final integration commit**

```bash
git add docs/troubleshooting/error-codes.md tests/docs/error-codes-doc-coverage.smoke.mjs tests/docs/run-docs-smoke.mjs src/Apiconvert.Core/README.md src/apiconvert-core/README.md
git commit -m "docs: add conversion error code reference with coverage checks"
```
