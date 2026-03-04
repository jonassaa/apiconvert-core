# Repository Adoption Tasks

This backlog focuses on reducing adoption friction for new package consumers who need deterministic conversion in both .NET and Node/TypeScript. The tasks prioritize fast time-to-first-success, trust signals (tests, releases, metadata), and cross-runtime parity clarity. Every item is scoped to concrete repository files so contributors can execute without guesswork. The outcome should be a repo where users can install, run, validate, and ship confidently from day one.

## README & Positioning

### [A01] Add a “Start in 5 Minutes” section to root README
**Why:** The root README is feature-rich but does not provide a single copy-paste minimal path optimized for first success. New users need one obvious starting lane.
**Scope:**
- `README.md`
- `tests/cases/basic-json/*`
**Steps:**
1. Add a `## Start in 5 Minutes` section near the top of `README.md`.
2. Include one .NET command block (`dotnet add package`, sample program entrypoint).
3. Include one Node command block (`npm install @apiconvert/core`, sample script entrypoint).
4. Reuse `tests/cases/basic-json/rules.json` and input/output files as canonical minimal sample.
5. Add expected output inline so users can confirm correctness immediately.
6. Link directly to `docs/getting-started/index.md` for full walkthrough.
**Acceptance Criteria:**
- Root README includes a clearly labeled `Start in 5 Minutes` section.
- Section contains runnable snippets for both runtimes.
- Instructions reference existing files in `tests/cases/basic-json`.
- A new user can complete first conversion without reading other docs.
**Notes:** Keep snippets intentionally short and parity-aligned.

### [A02] Add explicit install/version matrix for both packages
**Why:** Adoption depends on quick compatibility checks (runtime versions, package names, schema version pinning).
**Scope:**
- `README.md`
- `src/Apiconvert.Core/README.md`
- `src/apiconvert-core/README.md`
**Steps:**
1. Add an `Install & Compatibility` table to root README.
2. Include .NET target frameworks (`net8.0`, `net10.0`) and Node engine (`>=18`).
3. Document package coordinates (`Apiconvert.Core`, `@apiconvert/core`).
4. Add schema pinning guidance using `schemas/rules/vX.Y.Z/schema.json`.
5. Mirror a short compatibility note in both package READMEs.
6. Add links to rules schema docs and release policy.
**Acceptance Criteria:**
- Compatibility details are visible in first-screen README content.
- Both package READMEs include install + engine/framework constraints.
- Schema pinning path is documented consistently.
**Notes:** Keep terminology identical across README files.

### [A03] Add a “Choose Your Runtime” quick decision block
**Why:** Users arriving from either C# or Node need a direct route without scanning long mixed content.
**Scope:**
- `README.md`
- `docs/getting-started/index.md`
**Steps:**
1. Add a compact decision block in root README with two paths: `.NET` and `Node/TypeScript`.
2. For each path, link to one install command and one runnable quickstart snippet.
3. Add “when to use which runtime” one-liners.
4. Ensure links target stable docs URLs.
5. Add anchor links from docs getting-started back to the README section.
**Acceptance Criteria:**
- Runtime choice is visible above deep reference sections.
- Both runtime paths have equal prominence and link depth.
- All links resolve in local docs build.

### [A04] Add “What this package is not” callout in package READMEs
**Why:** Preventing scope confusion improves adoption quality and reduces misuse tickets.
**Scope:**
- `src/Apiconvert.Core/README.md`
- `src/apiconvert-core/README.md`
**Steps:**
1. Add a short `Not an API gateway/middleware` callout near top of each package README.
2. Copy language from architectural boundaries in `AGENTS.md` and root README.
3. Keep wording runtime-neutral and deterministic-focused.
4. Link to docs page that explains boundaries.
5. Validate both READMEs communicate the same constraints.
**Acceptance Criteria:**
- Both package READMEs include explicit non-goal callout.
- Messaging is consistent and does not introduce runtime-specific behavior claims.

### [A05] Add badges for package, docs, CI, and parity gate
**Why:** Trust signals improve conversion from repository visitors to package adopters.
**Scope:**
- `README.md`
- `.github/workflows/build-and-test.yml`
- `.github/workflows/docs-pages.yml`
- `.github/workflows/publish-packages.yml`
**Steps:**
1. Add badge row in README for CI, docs deployment, NuGet package, npm package.
2. Add parity status badge tied to main branch workflow result.
3. Use stable badge URLs and links to package pages.
4. Ensure badge alt text is descriptive for accessibility.
5. Verify badges render on GitHub markdown preview.
**Acceptance Criteria:**
- README displays working badges with correct destinations.
- Badge set includes both runtime packages and CI/docs status.
- No broken image or link checks in CI.

## Docs Information Architecture

### [A06] Remove duplicated concept trees and consolidate canonical docs paths
**Why:** `docs/concepts/*` and `docs/core-concepts/*` overlap, creating confusion and stale-content risk.
**Scope:**
- `docs/concepts/*`
- `docs/core-concepts/*`
- `docs/.vitepress/config.js`
- `docs/index.md`
**Steps:**
1. Choose one canonical concepts directory (`docs/concepts/` or `docs/core-concepts/`).
2. Migrate content and preserve frontmatter/anchors.
3. Add redirect stubs or link aliases for moved pages.
4. Update sidebar and nav links to canonical files.
5. Run docs build and smoke tests.
6. Remove orphaned duplicate files.
**Acceptance Criteria:**
- Only one concepts tree remains as canonical source.
- Sidebar and internal links reference canonical paths only.
- Docs smoke tests pass without broken links.
**Notes:** Prefer minimal URL churn; add redirects where possible.

### [A07] Add docs IA map page with explicit navigation paths
**Why:** Adoption docs should show where to start, where to go next, and where advanced APIs live.
**Scope:**
- `docs/overview/docs-map.md` (new)
- `docs/index.md`
- `docs/.vitepress/config.js`
**Steps:**
1. Create `docs/overview/docs-map.md` with sections: Start, Learn, Build, Troubleshoot.
2. Link to getting started, recipes, runtime API, schema reference, troubleshooting.
3. Add docs map entry to sidebar near top.
4. Link docs map from homepage quick path.
5. Add “You are here” breadcrumbs or short path hints in page intro.
**Acceptance Criteria:**
- Docs map page exists and is linked from home and sidebar.
- New users can identify a path for first conversion in under 3 clicks.
- All docs map links resolve in local build.

### [A08] Harden VitePress sidebar ordering for onboarding progression
**Why:** Good IA should guide users from first run to advanced diagnostics in a deterministic sequence.
**Scope:**
- `docs/.vitepress/config.js`
**Steps:**
1. Reorder sidebar sections to: Start, Core Concepts, Quick Recipes, Runtime APIs, Rules Reference, Troubleshooting.
2. Move `Recipes` above full reference to favor practical adoption.
3. Ensure `getting-started/index` and `getting-started/first-conversion` stay first in Start section.
4. Add a dedicated “Parity & Testing” item under Guides or Concepts.
5. Verify all sidebar links point to existing files.
**Acceptance Criteria:**
- Sidebar reflects onboarding-first sequence.
- First three sidebar clicks get users to runnable examples.
- No dead links in sidebar configuration.

### [A09] Add explicit internal cross-links at end of every docs page
**Why:** Orphan pages hurt discoverability and increase bounce after first read.
**Scope:**
- `docs/getting-started/*.md`
- `docs/guides/*.md`
- `docs/reference/*.md`
- `docs/recipes/*.md`
- `docs/troubleshooting/*.md`
**Steps:**
1. Add a consistent footer section (`Next`, `Related`, `Troubleshooting`) to each docs page.
2. Ensure each page links to at least one upstream and one downstream page.
3. Include runtime API and schema links where relevant.
4. Add script check to enforce at least two internal links per docs page.
5. Run docs smoke tests and link checker.
**Acceptance Criteria:**
- Every docs page ends with internal navigation links.
- No page in docs tree is isolated from nav graph.
- CI fails if new docs pages omit required link footer.

### [A10] Publish API reference generation strategy and ownership
**Why:** Runtime API docs drift is a major trust and adoption risk.
**Scope:**
- `docs/reference/api-reference-strategy.md` (new)
- `docs/guides/runtime-api.md`
- `tests/docs/api-doc-coverage.smoke.mjs`
**Steps:**
1. Create a strategy page explaining inventory markers and update flow.
2. Document ownership for .NET and TypeScript API sections.
3. Add “How to update when API changes” checklist.
4. Link strategy from `runtime-api.md` and contributing docs.
5. Add CI guidance for resolving missing/stale symbol failures.
**Acceptance Criteria:**
- Strategy doc exists and is linked from runtime API docs.
- Contributors can follow a deterministic update checklist.
- CI API coverage test remains green with updated guidance.

### [A11] Add a dedicated parity-testing docs page for consumers
**Why:** Consumers need confidence that runtime outputs match for shared cases before adoption.
**Scope:**
- `docs/guides/parity-testing.md` (new)
- `docs/.vitepress/config.js`
- `README.md`
**Steps:**
1. Create parity guide explaining `tests/cases`, `tests/parity/parity-gate.mjs`, and report artifacts.
2. Show commands for local parity check.
3. Document interpretation of `parity-summary.json` fields.
4. Link guide from README and sidebar.
5. Add remediation steps when mismatches are found.
**Acceptance Criteria:**
- Parity guide is discoverable from README and docs nav.
- Guide includes runnable commands and expected outputs.
- Users can self-serve parity validation without reading test code.

### [A12] Add docs page for schema upgrade and compatibility workflow
**Why:** Schema changes are contract changes and must be easy for adopters to reason about.
**Scope:**
- `docs/reference/schema-versioning-workflow.md` (new)
- `schemas/rules/README.md`
- `src/apiconvert-core/schemas/rules/README.md`
**Steps:**
1. Document release-tag-to-schema mapping and immutable versioned directories.
2. Explain when to use `current` vs `vX.Y.Z` schema paths.
3. Add upgrade checklist for consumers moving between versions.
4. Link to compatibility APIs in both runtimes.
5. Cross-link from schema README files and runtime API docs.
**Acceptance Criteria:**
- Schema versioning workflow is documented in consumer docs.
- Both schema README files link to the same canonical workflow page.
- Compatibility check APIs are explicitly referenced for .NET and Node.

## Examples & Quickstarts

### [A13] Create a “killer use case” example workspace (purchase order normalization)
**Why:** A meaningful end-to-end example drives adoption better than abstract snippets.
**Scope:**
- `examples/purchase-order-normalization/README.md` (new)
- `examples/purchase-order-normalization/rules.json` (new)
- `examples/purchase-order-normalization/input.json` (new)
- `examples/purchase-order-normalization/output.json` (new)
- `tests/cases/json-purchase-order-detailed/*`
**Steps:**
1. Create `examples/purchase-order-normalization` directory.
2. Seed files from `tests/cases/json-purchase-order-detailed` and simplify for teaching.
3. Add runtime-specific run instructions for .NET and Node.
4. Include one “why this matters” business context paragraph.
5. Add expected output diff/check command.
6. Link example from root README and docs recipes.
**Acceptance Criteria:**
- Example folder exists with `input`, `rules`, `output`, and README.
- Both runtime instructions produce matching expected output.
- Example is linked from README and docs home/recipes.

### [A14] Add runnable .NET quickstart project under examples
**Why:** Copying code blocks into blank projects slows onboarding and increases setup errors.
**Scope:**
- `examples/dotnet-quickstart/Apiconvert.Quickstart.csproj` (new)
- `examples/dotnet-quickstart/Program.cs` (new)
- `examples/dotnet-quickstart/rules.json` (new)
- `examples/dotnet-quickstart/input.json` (new)
**Steps:**
1. Scaffold a minimal console app targeting supported framework.
2. Add package reference to `Apiconvert.Core`.
3. Read `rules.json` and `input.json`, run conversion, print output.
4. Add validation/error handling and exit codes.
5. Document run command in local README.
6. Add script/CI smoke check for project run.
**Acceptance Criteria:**
- `dotnet run` in quickstart folder works on clean checkout.
- Output matches expected JSON in repo.
- Example is linked from docs getting-started page.

### [A15] Add runnable Node/TypeScript quickstart project under examples
**Why:** Node adopters need a full project skeleton with tsconfig and scripts, not only inline snippets.
**Scope:**
- `examples/node-quickstart/package.json` (new)
- `examples/node-quickstart/tsconfig.json` (new)
- `examples/node-quickstart/src/index.ts` (new)
- `examples/node-quickstart/rules.json` (new)
- `examples/node-quickstart/input.json` (new)
**Steps:**
1. Scaffold minimal TS project with start/build scripts.
2. Add dependency on `@apiconvert/core`.
3. Implement conversion script reading local files.
4. Include strict error handling and deterministic pretty output.
5. Add README with install/run commands.
6. Add CI smoke check that executes the example.
**Acceptance Criteria:**
- `npm install && npm run start` works in example folder.
- Output matches expected JSON.
- Example is linked from getting-started and root README.

### [A16] Add CLI-first conversion recipe page using existing package bin
**Why:** Many users evaluate packages through CLI before embedding runtime APIs.
**Scope:**
- `docs/recipes/cli-first-conversion.md` (new)
- `docs/reference/cli.md`
- `src/apiconvert-core/bin/apiconvert.js`
**Steps:**
1. Create recipe that validates, lints, and converts one sample file.
2. Use existing CLI commands from package README.
3. Add examples for JSON input and output verification.
4. Link to troubleshooting for common parse/schema errors.
5. Add cross-links to runtime API equivalents.
**Acceptance Criteria:**
- Recipe page is accessible from sidebar.
- Commands run against repo samples without modification.
- Users can complete first conversion without writing application code.

### [A17] Add examples index page with runtime selector
**Why:** Multiple examples need a single discovery hub for adoption.
**Scope:**
- `docs/recipes/index.md` (new)
- `docs/.vitepress/config.js`
- `examples/`
**Steps:**
1. Create recipes index listing all examples by scenario and complexity.
2. Tag each example with `.NET`, `Node`, or `Both`.
3. Add links to example folders and docs recipes.
4. Surface runtime-selector classes for dual snippets.
5. Add sidebar link to recipes index.
**Acceptance Criteria:**
- Recipes index exists and is in sidebar.
- Every example folder has a corresponding entry.
- Runtime tags make path selection obvious.

## Tests & Shared Cases

### [A18] Add `tests/cases/README.md` explaining case contract
**Why:** Shared cases are central to parity but lack a documented contributor contract.
**Scope:**
- `tests/cases/README.md` (new)
- `tests/parity/parity-report.mjs`
- `tests/nuget/Apiconvert.Core.CaseRunner/Program.cs`
**Steps:**
1. Document required files per case (`input.*`, `rules.json`, `output.*`).
2. Define naming conventions and supported extensions.
3. Explain how .NET and Node runners consume cases.
4. Add guidance for adding new behavior cases.
5. Include parity command and expected artifacts.
6. Link case README from root README and contributing docs.
**Acceptance Criteria:**
- Case contract is documented in repo.
- New contributor can add a valid case without opening runner code.
- Documentation matches current runner behavior.

### [A19] Add case manifest and validator script for `tests/cases`
**Why:** A manifest-based contract prevents silent drift and incomplete case directories.
**Scope:**
- `tests/cases/cases.manifest.json` (new)
- `scripts/validate-cases.sh` (new)
- `.github/workflows/build-and-test.yml`
**Steps:**
1. Create manifest containing case ids, formats, and scenario tags.
2. Implement validator checking required files and extension consistency.
3. Validate manifest entries map to existing directories.
4. Add validator to CI before parity gate.
5. Document update steps in case README.
**Acceptance Criteria:**
- CI fails when case folder is missing required files.
- Manifest and filesystem stay synchronized.
- Case additions require explicit manifest entry.

### [A20] Add dedicated adoption-focused case set and docs references
**Why:** Existing cases are broad but not grouped into beginner-to-advanced progression.
**Scope:**
- `tests/cases/*` (selected new folders)
- `docs/recipes/*.md`
- `docs/getting-started/first-conversion.md`
**Steps:**
1. Define 5–8 “adoption path” cases (basic map, defaults, branch, array, xml/query).
2. Add new case directories with deterministic input/output.
3. Tag these in case manifest as `adoption`.
4. Reference each case in docs with direct links.
5. Ensure both runtimes pass new cases.
**Acceptance Criteria:**
- Adoption-tagged cases exist and pass parity gate.
- Docs reference real case directories, not synthetic examples.
- Case order provides increasing complexity.

### [A21] Add schema sync integrity check between root and npm package
**Why:** `src/apiconvert-core/scripts/sync-schemas.mjs` copies schema files, but integrity should be CI-enforced.
**Scope:**
- `src/apiconvert-core/scripts/sync-schemas.mjs`
- `scripts/check-schema-sync.sh` (new)
- `.github/workflows/build-and-test.yml`
**Steps:**
1. Add script comparing hash/tree of `schemas/rules` and `src/apiconvert-core/schemas/rules`.
2. Fail with clear remediation message (`npm --prefix src/apiconvert-core run sync-schemas`).
3. Add script to CI after checkout.
4. Add local developer command in README/contributing docs.
5. Validate script on intentionally desynced branch.
**Acceptance Criteria:**
- CI fails when root and npm schema directories diverge.
- Failure message includes one-command fix.
- Sync workflow is documented for contributors.

### [A22] Add cross-runtime snapshot test for diagnostics parity
**Why:** Output parity alone is not enough; diagnostics/warnings parity drives trust in production adoption.
**Scope:**
- `tests/nuget/Apiconvert.Core.Tests/*`
- `tests/npm/apiconvert-core-tests/src/*`
- `tests/parity/parity-report.mjs`
**Steps:**
1. Select representative failing and warning-heavy cases.
2. Extend parity report comparison docs for diagnostics categories.
3. Add tests in each runtime asserting deterministic diagnostics ordering.
4. Update parity reporting to include diagnostic diff summary.
5. Add docs explaining expected parity boundaries.
**Acceptance Criteria:**
- Both runtimes assert diagnostic shape/order for shared scenarios.
- Parity report includes diagnostics mismatch visibility.
- CI catches diagnostic regressions.

### [A23] Add “new feature parity checklist” template for PRs
**Why:** Adoption suffers when one runtime gets features first without clear parity intent.
**Scope:**
- `.github/pull_request_template.md` (new)
- `internal-docs/contributing/index.md`
**Steps:**
1. Create PR template with required checkboxes for .NET runtime, Node runtime, schema, shared cases, docs.
2. Add “if not applicable, explain why” sections.
3. Include links to parity gate and schema versioning docs.
4. Require evidence of tests run for both runtimes.
5. Reference template in contributing docs.
**Acceptance Criteria:**
- New PRs open with parity checklist.
- Template explicitly covers both runtimes and shared cases.
- Maintainers can reject parity-incomplete changes using template criteria.

## Packaging & Releases

### [A24] Add consumer-facing `CHANGELOG.md` with Keep a Changelog format
**Why:** Release transparency is a key adoption trust signal for package consumers.
**Scope:**
- `CHANGELOG.md` (new)
- `.github/workflows/create-release-tag.yml`
- `.github/workflows/publish-packages.yml`
**Steps:**
1. Add `CHANGELOG.md` with sections for Added/Changed/Fixed/Removed.
2. Backfill recent released versions from tags and notable changes.
3. Add release workflow step that checks current version has changelog entry.
4. Document changelog update requirement in PR template/contributing docs.
5. Link changelog from README and package READMEs.
**Acceptance Criteria:**
- Repo contains a maintained changelog.
- Release flow fails if version entry is missing.
- README and package READMEs link to changelog.

### [A25] Auto-generate GitHub Release notes from changelog and parity summary
**Why:** Tag-only publishing lacks public release narrative and adoption confidence details.
**Scope:**
- `.github/workflows/publish-packages.yml`
- `tests/parity/parity-summary.json`
- `CHANGELOG.md`
**Steps:**
1. Add workflow step to parse current version section from changelog.
2. Append parity summary metrics to release notes body.
3. Create or update GitHub Release for tag `vX.Y.Z`.
4. Include links to NuGet/npm package pages and docs site.
5. Validate workflow on dry-run/test tag.
**Acceptance Criteria:**
- Each published tag has a GitHub Release with structured notes.
- Release notes include parity counts and package links.
- Release notes source is deterministic from changelog + artifacts.

### [A26] Align npm package metadata for discoverability and consistency
**Why:** Current npm metadata is minimal; better keywords, homepage, bugs, and funding improve discoverability.
**Scope:**
- `src/apiconvert-core/package.json`
- `src/apiconvert-core/README.md`
**Steps:**
1. Add `homepage`, `bugs`, and `keywords` fields with conversion-focused terms.
2. Validate `repository` and docs URLs are accurate.
3. Add `files`/exports sanity check for published package shape.
4. Add `npm pack --dry-run` check in CI for package contents.
5. Ensure README references CLI and schema paths accurately.
**Acceptance Criteria:**
- npm package metadata includes homepage, bugs, and enriched keywords.
- Dry-run pack output contains expected dist/bin/schemas/readme files.
- Package metadata matches repo URLs and naming conventions.

### [A27] Improve NuGet package metadata and README discoverability
**Why:** NuGet consumers evaluate package quality from metadata and rendered README.
**Scope:**
- `src/Apiconvert.Core/Apiconvert.Core.csproj`
- `src/Apiconvert.Core/README.md`
**Steps:**
1. Expand package tags with schema/parity/deterministic keywords.
2. Confirm repository, project URL, and license metadata are coherent.
3. Add package icon and documentation URL fields if desired.
4. Validate README renders correctly in NuGet (links and code blocks).
5. Add pack validation step to inspect `.nupkg` metadata.
**Acceptance Criteria:**
- NuGet metadata fields are complete and consumer-oriented.
- NuGet README renders with working links.
- Pack validation confirms metadata and included README/license.

## CI & Quality Gates

### [A28] Split CI into named jobs and publish test/parity summaries
**Why:** A monolithic CI job makes failures harder to triage and slows contributor feedback loops.
**Scope:**
- `.github/workflows/build-and-test.yml`
- `.github/workflows/publish-packages.yml`
**Steps:**
1. Split workflow into jobs: `dotnet`, `node`, `docs`, `parity`, `schema`.
2. Keep dependency graph explicit (`parity` after runtime tests).
3. Upload test and parity summaries as artifacts.
4. Add concise GitHub step summary output for key pass/fail metrics.
5. Ensure required checks map to branch protection.
**Acceptance Criteria:**
- CI shows separate job statuses for each domain.
- Parity and test summaries are attached as artifacts.
- Failure location is immediately identifiable by job name.

### [A29] Add deterministic docs quality gate for orphan pages and dead links
**Why:** Docs growth without governance creates hidden adoption regressions.
**Scope:**
- `scripts/check-doc-links.sh`
- `tests/docs/docs-content-quality.smoke.mjs`
- `.github/workflows/build-and-test.yml`
**Steps:**
1. Extend docs quality checks to detect unlinked docs pages.
2. Validate runtime selector blocks are balanced on dual-runtime pages.
3. Fail CI on broken internal anchors and orphan pages.
4. Add actionable failure output with file paths.
5. Document docs quality gate behavior in contributor docs.
**Acceptance Criteria:**
- CI fails on orphan or dead-link docs regressions.
- Failure output identifies exact page/link issues.
- Contributors can run quality checks locally.

## Community & Contribution

### [A30] Add CONTRIBUTING, issue templates, discussion guidance, and code of conduct
**Why:** Adoption at scale requires clear contribution and support channels beyond code quality.
**Scope:**
- `CONTRIBUTING.md` (new)
- `CODE_OF_CONDUCT.md` (new)
- `.github/ISSUE_TEMPLATE/bug_report.yml` (new)
- `.github/ISSUE_TEMPLATE/feature_request.yml` (new)
- `.github/ISSUE_TEMPLATE/config.yml` (new)
- `.github/DISCUSSION_TEMPLATE/usage.yml` (new, optional)
- `README.md`
**Steps:**
1. Create `CONTRIBUTING.md` with setup, parity expectations, and docs/testing commands.
2. Add `CODE_OF_CONDUCT.md` using a standard template.
3. Add bug template requiring runtime, rules sample, input/output, expected behavior.
4. Add feature template requiring use case, parity impact, and schema implications.
5. Configure issue template links to docs and discussions.
6. Add README section for support channels and contribution entrypoint.
7. Link contributing docs to internal maintainer docs where relevant.
**Acceptance Criteria:**
- Repository has visible contribution and behavior guidelines.
- New issues are structured with data needed for triage.
- README links to contribution and support resources.
**Notes:** Keep external contributor docs in root files; keep maintainer-only process details in `internal-docs/`.
