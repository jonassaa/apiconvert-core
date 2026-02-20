# Skills Index

This directory contains repository-local Codex skills.

## Available skills

- `apiconvert-core-power-consumer`
  - Purpose: Evaluate package adoption readiness from a consumer perspective and produce high-value feature requests/RFCs.
  - Entry point: `skills/apiconvert-core-power-consumer/SKILL.md`
- `apiconvert-feature-delivery`
  - Purpose: Deliver Apiconvert feature changes end-to-end across .NET and TypeScript with parity and deterministic tests.
  - Entry point: `skills/apiconvert-feature-delivery/SKILL.md`
- `apiconvert-dotnet-api-review`
  - Purpose: Run a deep .NET package/API design review with concrete file/symbol findings and actionable recommendations.
  - Entry point: `skills/apiconvert-dotnet-api-review/SKILL.md`
- `apiconvert-node-api-review`
  - Purpose: Run a deep Node/TypeScript package/API design review with concrete file/symbol findings and actionable recommendations.
  - Entry point: `skills/apiconvert-node-api-review/SKILL.md`
- `apiconvert-production-consumer-review`
  - Purpose: Evaluate production adoption readiness from a customer perspective for `TARGET=.NET` or `TARGET=Node`.
  - Entry point: `skills/apiconvert-production-consumer-review/SKILL.md`

## Structure convention

Each skill lives in its own folder:

- `SKILL.md` (required)
- optional supporting content such as `references/`, `scripts/`, `assets/`, `templates/`, and `agents/`

Keep support files co-located with the skill and reference them using relative paths from the skill folder.

## Task management convention

All Apiconvert skills should follow the repository Notion tracking contract from `AGENTS.md`:

- Read tracker config from env vars (`APICONVERT_NOTION_DATA_SOURCE_ID`, optional `APICONVERT_NOTION_DATABASE_URL`, `APICONVERT_CODEX_INSTANCE_ID`).
- Ask the user before writing tasks when required env vars are missing.
- Use stable `Codex Task ID` values in format `<APICONVERT_CODEX_INSTANCE_ID>:<short-task-slug>`.
- Keep one actively worked task in `In Progress`; keep queued items in `Backlog`/`Ready`.
- Use explicit `Priority` (`P0`-`P3`, default `P2`) and best-fit `Area`.
- Add `parity` tag when work impacts both .NET and TypeScript behavior.
