# Version Pinning

Use immutable schema versions when reproducibility matters.

## Paths

- Immutable: `schemas/rules/vX.Y.Z/schema.json`
- Latest moving alias: `schemas/rules/current/schema.json`
- Legacy alias: `schemas/rules/rules.schema.json`

## Guidance

- Pin immutable versions in long-lived integrations.
- Use `current` only where continuous tracking is intentional.
- Record pinned version in service configuration and change notes.

