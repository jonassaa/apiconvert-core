# Repository Guidelines

## Project Structure & Module Organization
This is a multi-project solution. Current layout:

- `src/Apiconvert.Client/`: Next.js App Router client (app, components, lib, public, configs).
- `src/Apiconvert.Api/`: .NET API (controllers only).
- `src/Apiconvert.Api/Infrastructure/`: .NET Infrastructure integrations (merged into API project).
- `src/Apiconvert.Apphost/`: .NET Aspire AppHost for local orchestration.
- `src/Apiconvert.Client/tests/`: unit tests (Vitest).
- `tests/Apiconvert.E2e.Tests/`: Playwright E2E tests.
- `tests/Apiconvert.*.Tests/`: .NET test projects.
- `docs/`: notes, decisions, and documentation.
- `supabase/`: local Supabase config/migrations.

If you introduce a new top-level folder, document it here with a brief purpose.

## Build, Test, and Development Commands
Use these exact commands:

- `npm run dev`: start the local dev server on port 3123.
- `npm run build`: production build.
- `npm start`: run the production server.
- `npm run lint`: run ESLint.
- `npm test`: run Vitest (single run).
- `npm run e2e`: run Playwright E2E tests (starts dev server).
- `npm run e2e:headed`: run Playwright with UI visible.
- `npm run e2e:ui`: run Playwright UI mode.

## Coding Style & Naming Conventions
- Language: TypeScript/TSX.
- Indentation: 2 spaces.
- Naming: `camelCase` for variables/functions, `PascalCase` for components/types, `kebab-case` for files.
- Prefer role/label-based selectors in E2E tests (`getByRole`, `getByLabel`).

## Testing Guidelines
- Unit tests: Vitest.
  - Location: `src/Apiconvert.Client/tests/*.test.ts`.
  - Run all: `npm test`.
- E2E tests: Playwright.
  - Location: `tests/Apiconvert.E2e.Tests/**/*.spec.ts`.
  - Run all: `npm run e2e`.

## Commit & Pull Request Guidelines
- Commit messages: clear, imperative, and scoped (e.g., "Add converter log table").
- Keep changes focused; avoid mixing refactors with features.
- PRs should include: summary, testing notes, and any relevant screenshots/API examples.

## Task Tracking (Agent Instructions)
- Feature work should be tracked in GitHub Issues.
- Show the proposed issue before creating it.
- Add a task description to each issue explaining the implementation plan.
- Before starting an issue: ensure a clean git status, create a branch off the latest `main`.
- Before starting an issue: Assign `jonassaa`.
- When complete: open a PR, review/fix issues, squash-merge, and close the issue.

## Git Configuration
- Do not edit `.git/` files directly.
- Perform git operations via the command line only.

## Security & Configuration
- Never commit secrets.
- Use `.env.example` for required env vars.
- Document setup steps in `docs/` and link them here if needed (see `docs/local-dev.md`).

## Validation Checklist
Before finishing work:
1. Run the app and confirm there are no errors.
2. `npm run build`
3. `npm test`
4. `npm run e2e`

## Authentication (Local Dev)
- Username: `asdf@asdf.com`
- Password: `P@ssw0rd`

## Review
When reviewing a PR:
Prioritize correctness, security, data integrity, and user-facing regressions.
Check for missing edge cases, error handling, and loading/empty states.
Look for unintended API changes and breaking UI behavior.
Verify tests cover the new behavior; suggest additions if coverage is thin.
Ensure E2E selectors are role/label-based and stable.
Confirm `npm run build`, `npm test`, and `npm run e2e` pass locally.
Call out performance risks (N+1 queries, large client bundles, blocking calls).
Note any follow-ups (docs, migrations, env vars) explicitly.

## Database migrations
Never edit existing database migrations that are applied when doing edits in the database. Always add new migration in order to change the database.
