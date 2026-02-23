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

## Publishing

- docs deploy from GitHub Actions using VitePress build output
- keep changes to docs and `.vitepress` config in the same PR when behavior/layout changes
