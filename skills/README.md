# Skills Index

This directory contains repository-local Codex skills.

## Available skills

- `apiconvert-core-power-consumer`
  - Purpose: Evaluate package adoption readiness from a consumer perspective and produce high-value feature requests/RFCs.
  - Entry point: `skills/apiconvert-core-power-consumer/SKILL.md`
- `apiconvert-feature-delivery`
  - Purpose: Deliver Apiconvert feature changes end-to-end across .NET and TypeScript with parity and deterministic tests.
  - Entry point: `skills/apiconvert-feature-delivery/SKILL.md`
- `apiconvert-rules-generator`
  - Purpose: Generate rules JSON from sample input/output payloads aligned to the latest rules schema.
  - Entry point: `skills/apiconvert-rules-generator/SKILL.md`

## Structure convention

Each skill lives in its own folder:

- `SKILL.md` (required)
- optional supporting content such as `references/`, `scripts/`, `assets/`, `templates/`, and `agents/`

Keep support files co-located with the skill and reference them using relative paths from the skill folder.
