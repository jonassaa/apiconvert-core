# GitHub Pages Documentation Site Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace `/docs` with a comprehensive MkDocs + Material documentation site for Apiconvert.Core, including a global runtime selector (.NET/TypeScript), strict build validation, and versioned GitHub Pages deployment via `gh-pages`.

**Architecture:** Use a single unified docs source in `/docs` and runtime-specific content blocks controlled by a global selector injected through MkDocs theme overrides. Build locally with strict checks, then publish versioned docs using `mike` in GitHub Actions. Keep the canonical conceptual model shared while hiding runtime-specific snippets based on selector state.

**Tech Stack:** MkDocs, Material for MkDocs, mike (versioned docs), GitHub Actions, JavaScript/CSS theme overrides, markdown content in `/docs`.

---

### Task 1: Bootstrap MkDocs Docs Toolchain

**Files:**
- Create: `/Users/jonas/dev/fun/apiconvert-core/mkdocs.yml`
- Create: `/Users/jonas/dev/fun/apiconvert-core/requirements-docs.txt`
- Modify: `/Users/jonas/dev/fun/apiconvert-core/.gitignore`

**Step 1: Write the failing build check**

Add a temporary CI/local expectation in plan notes: `mkdocs build --strict` should fail before config exists.

**Step 2: Run check to verify it fails**

Run: `cd /Users/jonas/dev/fun/apiconvert-core && mkdocs build --strict`
Expected: FAIL with missing `mkdocs.yml`.

**Step 3: Write minimal implementation**

Create `mkdocs.yml` with:
- site metadata
- Material theme
- plugins (`search`)
- markdown extensions needed for tabs/admonitions/code highlighting
- nav placeholders for full IA

Create `requirements-docs.txt` with pinned docs dependencies.

Update `.gitignore` for local docs build output (`site/`).

**Step 4: Run check to verify it passes**

Run:
- `cd /Users/jonas/dev/fun/apiconvert-core && python3 -m venv .venv-docs`
- `source .venv-docs/bin/activate && pip install -r requirements-docs.txt`
- `mkdocs build --strict`
Expected: PASS or only missing-page errors to be fixed in next task.

**Step 5: Commit**

```bash
git add mkdocs.yml requirements-docs.txt .gitignore
git commit -m "docs: bootstrap mkdocs toolchain"
```

### Task 2: Replace Existing `/docs` Content With New IA Skeleton

**Files:**
- Modify/Create: `/Users/jonas/dev/fun/apiconvert-core/docs/*.md` (replace current structure)
- Create: `/Users/jonas/dev/fun/apiconvert-core/docs/start-here/*.md`
- Create: `/Users/jonas/dev/fun/apiconvert-core/docs/core-concepts/*.md`
- Create: `/Users/jonas/dev/fun/apiconvert-core/docs/runtime-guides/*.md`
- Create: `/Users/jonas/dev/fun/apiconvert-core/docs/rules-reference/*.md`
- Create: `/Users/jonas/dev/fun/apiconvert-core/docs/recipes/*.md`
- Create: `/Users/jonas/dev/fun/apiconvert-core/docs/diagnostics/*.md`
- Create: `/Users/jonas/dev/fun/apiconvert-core/docs/schema-versioning/*.md`
- Create: `/Users/jonas/dev/fun/apiconvert-core/docs/parity-testing/*.md`
- Create: `/Users/jonas/dev/fun/apiconvert-core/docs/cli-tooling/*.md`
- Create: `/Users/jonas/dev/fun/apiconvert-core/docs/contributing/*.md`

**Step 1: Write the failing nav/content check**

Define all target nav pages in `mkdocs.yml` before creating files.

**Step 2: Run check to verify it fails**

Run: `cd /Users/jonas/dev/fun/apiconvert-core && source .venv-docs/bin/activate && mkdocs build --strict`
Expected: FAIL with missing docs files referenced by nav.

**Step 3: Write minimal implementation**

Create all required pages with initial complete section outlines and key content migrated/expanded from:
- legacy `/docs`
- repository `README.md`
- runtime READMEs under `src/Apiconvert.Core/README.md` and `src/apiconvert-core/README.md`

**Step 4: Run check to verify it passes**

Run: `cd /Users/jonas/dev/fun/apiconvert-core && source .venv-docs/bin/activate && mkdocs build --strict`
Expected: PASS.

**Step 5: Commit**

```bash
git add docs mkdocs.yml
git commit -m "docs: replace docs with comprehensive ia structure"
```

### Task 3: Add Runtime Selector (Global, Header-Level, Hidden Non-Selected Content)

**Files:**
- Create: `/Users/jonas/dev/fun/apiconvert-core/docs/overrides/main.html`
- Create: `/Users/jonas/dev/fun/apiconvert-core/docs/assets/javascripts/runtime-selector.js`
- Create: `/Users/jonas/dev/fun/apiconvert-core/docs/assets/stylesheets/runtime-selector.css`
- Modify: `/Users/jonas/dev/fun/apiconvert-core/mkdocs.yml`

**Step 1: Write the failing selector behavior check**

Add a small local assertion script (or manual checklist) expecting runtime selector element and runtime classes in built HTML.

**Step 2: Run check to verify it fails**

Run: `cd /Users/jonas/dev/fun/apiconvert-core && source .venv-docs/bin/activate && mkdocs build --strict && rg -n "runtime-selector|runtime-dotnet|runtime-typescript" site -S`
Expected: FAIL or empty results before implementation.

**Step 3: Write minimal implementation**

Implement selector UI in `overrides/main.html`, JS state persistence in `localStorage`, body class toggling, and CSS that hides non-selected runtime blocks.

Update `mkdocs.yml` with `theme.custom_dir`, extra JS, and extra CSS.

**Step 4: Run check to verify it passes**

Run: `cd /Users/jonas/dev/fun/apiconvert-core && source .venv-docs/bin/activate && mkdocs build --strict && rg -n "runtime-selector|runtime-dotnet|runtime-typescript" site -S`
Expected: PASS with matches present.

**Step 5: Commit**

```bash
git add docs/overrides docs/assets mkdocs.yml
git commit -m "docs: add global runtime selector with hidden non-selected blocks"
```

### Task 4: Apply Runtime-Tagged Content Across Key Pages

**Files:**
- Modify: `/Users/jonas/dev/fun/apiconvert-core/docs/start-here/first-conversion.md`
- Modify: `/Users/jonas/dev/fun/apiconvert-core/docs/runtime-guides/dotnet-api-usage.md`
- Modify: `/Users/jonas/dev/fun/apiconvert-core/docs/runtime-guides/typescript-api-usage.md`
- Modify: `/Users/jonas/dev/fun/apiconvert-core/docs/runtime-guides/streaming.md`
- Modify: `/Users/jonas/dev/fun/apiconvert-core/docs/recipes/*.md`

**Step 1: Write the failing parity-content check**

Define a lint rule/grep check that key pages include both runtime-tagged snippets where applicable.

**Step 2: Run check to verify it fails**

Run: `cd /Users/jonas/dev/fun/apiconvert-core && rg -n "runtime-dotnet|runtime-typescript" docs/start-here docs/runtime-guides docs/recipes -S`
Expected: FAIL for incomplete runtime tags.

**Step 3: Write minimal implementation**

Annotate runtime-specific code and guidance with consistent runtime block classes while keeping shared prose untagged.

**Step 4: Run check to verify it passes**

Run:
- `cd /Users/jonas/dev/fun/apiconvert-core && source .venv-docs/bin/activate && mkdocs build --strict`
- `rg -n "runtime-dotnet|runtime-typescript" docs/start-here docs/runtime-guides docs/recipes -S`
Expected: PASS with adequate tagged coverage.

**Step 5: Commit**

```bash
git add docs/start-here docs/runtime-guides docs/recipes
git commit -m "docs: add runtime-tagged content for dotnet and typescript"
```

### Task 5: Add Authoring Rules and Contributor Guidance

**Files:**
- Create: `/Users/jonas/dev/fun/apiconvert-core/docs/contributing/docs-authoring-guide.md`
- Modify: `/Users/jonas/dev/fun/apiconvert-core/docs/contributing/index.md`
- Modify: `/Users/jonas/dev/fun/apiconvert-core/README.md`

**Step 1: Write the failing docs-contrib discoverability check**

Add expected links to authoring guide in nav and repository README.

**Step 2: Run check to verify it fails**

Run: `cd /Users/jonas/dev/fun/apiconvert-core && rg -n "docs-authoring-guide|GitHub Pages docs" README.md docs -S`
Expected: FAIL before links exist.

**Step 3: Write minimal implementation**

Document:
- runtime tagging rules
- parity update checklist
- style and page ownership expectations
- version update procedure with `mike`

Link this from docs nav and top-level README.

**Step 4: Run check to verify it passes**

Run: `cd /Users/jonas/dev/fun/apiconvert-core && rg -n "docs-authoring-guide|GitHub Pages docs" README.md docs -S`
Expected: PASS.

**Step 5: Commit**

```bash
git add docs/contributing README.md
git commit -m "docs: add docs authoring and maintenance guide"
```

### Task 6: Add GitHub Actions Workflow for Versioned GitHub Pages Deploy

**Files:**
- Create: `/Users/jonas/dev/fun/apiconvert-core/.github/workflows/docs-pages.yml`

**Step 1: Write the failing workflow validation check**

Run action lint or YAML validation expecting missing docs workflow.

**Step 2: Run check to verify it fails**

Run: `cd /Users/jonas/dev/fun/apiconvert-core && rg -n "docs-pages|mike" .github/workflows -S`
Expected: FAIL before workflow exists.

**Step 3: Write minimal implementation**

Create workflow with:
- trigger on docs/mkdocs changes + manual dispatch
- Python setup + dependency install
- `mkdocs build --strict`
- `mike deploy` and `mike set-default` to `gh-pages`
- proper Pages permissions and concurrency

**Step 4: Run check to verify it passes**

Run: `cd /Users/jonas/dev/fun/apiconvert-core && rg -n "mike deploy|gh-pages|mkdocs build --strict" .github/workflows/docs-pages.yml -S`
Expected: PASS.

**Step 5: Commit**

```bash
git add .github/workflows/docs-pages.yml
git commit -m "ci: add versioned github pages docs deploy workflow"
```

### Task 7: Add Local Docs Helper Commands

**Files:**
- Create: `/Users/jonas/dev/fun/apiconvert-core/scripts/docs-serve.sh`
- Create: `/Users/jonas/dev/fun/apiconvert-core/scripts/docs-build.sh`
- Modify: `/Users/jonas/dev/fun/apiconvert-core/README.md`

**Step 1: Write the failing helper-command check**

Attempt to run helper scripts before they exist.

**Step 2: Run check to verify it fails**

Run: `cd /Users/jonas/dev/fun/apiconvert-core && ls scripts/docs-serve.sh scripts/docs-build.sh`
Expected: FAIL (missing files).

**Step 3: Write minimal implementation**

Add executable scripts to:
- bootstrap venv if missing
- install docs deps
- run `mkdocs serve` / `mkdocs build --strict`

Document usage in README.

**Step 4: Run check to verify it passes**

Run:
- `cd /Users/jonas/dev/fun/apiconvert-core && bash scripts/docs-build.sh`
- `cd /Users/jonas/dev/fun/apiconvert-core && timeout 8 bash scripts/docs-serve.sh || true`
Expected: build PASS; serve starts without config/runtime errors.

**Step 5: Commit**

```bash
git add scripts/docs-serve.sh scripts/docs-build.sh README.md
git commit -m "docs: add local docs build and serve helper scripts"
```

### Task 8: Add Runtime Selector Verification Test

**Files:**
- Create: `/Users/jonas/dev/fun/apiconvert-core/tests/docs/runtime-selector.smoke.mjs`
- Modify: `/Users/jonas/dev/fun/apiconvert-core/package.json` (or docs-specific script location)
- Modify: `/Users/jonas/dev/fun/apiconvert-core/.github/workflows/docs-pages.yml`

**Step 1: Write the failing test**

Add a smoke script that opens built docs and checks:
- selector exists
- selecting `.NET` hides TS blocks
- selecting `TypeScript` hides .NET blocks

**Step 2: Run test to verify it fails**

Run: `cd /Users/jonas/dev/fun/apiconvert-core && node tests/docs/runtime-selector.smoke.mjs`
Expected: FAIL before script/fixture setup is complete.

**Step 3: Write minimal implementation**

Implement smoke test with Playwright against local built `site/` served with a simple static server.

**Step 4: Run test to verify it passes**

Run:
- `cd /Users/jonas/dev/fun/apiconvert-core && bash scripts/docs-build.sh`
- `cd /Users/jonas/dev/fun/apiconvert-core && node tests/docs/runtime-selector.smoke.mjs`
Expected: PASS.

**Step 5: Commit**

```bash
git add tests/docs .github/workflows/docs-pages.yml package.json
git commit -m "test: add runtime selector smoke verification for docs"
```

### Task 9: Final Verification and Cleanup

**Files:**
- Modify as needed based on failures

**Step 1: Run full verification suite**

Run:
- `cd /Users/jonas/dev/fun/apiconvert-core && bash scripts/docs-build.sh`
- `cd /Users/jonas/dev/fun/apiconvert-core && mkdocs build --strict`
- `cd /Users/jonas/dev/fun/apiconvert-core && node tests/docs/runtime-selector.smoke.mjs`

Expected: all PASS.

**Step 2: Validate workflow and docs discoverability**

Run:
- `cd /Users/jonas/dev/fun/apiconvert-core && rg -n "docs-pages|GitHub Pages|runtime selector|mike" README.md docs .github/workflows -S`
Expected: PASS with expected references.

**Step 3: Commit final fixes**

```bash
git add -A
git commit -m "docs: finalize comprehensive github pages documentation site"
```

