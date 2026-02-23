# Shared Cases

`tests/cases` is the canonical cross-runtime behavior suite.

## Standard case structure

- `rules.json`
- Input artifact(s), for example `input.json`
- Expected output artifact(s), for example `output.json`
- Optional notes describing edge intent

## Authoring guidelines

- Keep each case focused on one behavior concern.
- Prefer deterministic fixtures without external dependencies.
- Add edge cases for nulls, missing paths, and collision behavior.

