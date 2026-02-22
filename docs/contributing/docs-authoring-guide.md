# Docs Authoring Guide

## Runtime-tagged content

Use runtime classes only for runtime-specific text/code:

- `<div class="runtime-dotnet">...</div>`
- `<div class="runtime-typescript">...</div>`

Keep shared conceptual text untagged.

## Parity authoring rules

- if behavior differs, explain why
- keep equivalent examples for both runtimes
- update shared case references when examples change behavior

## Versioning

- deploy versioned docs via `mike`
- keep `latest` aligned with active release branch/tag stream
