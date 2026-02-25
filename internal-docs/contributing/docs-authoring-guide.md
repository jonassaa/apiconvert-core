# Docs Authoring Guide

## Runtime-tagged content

Use runtime classes only for runtime-specific text/code:

- `<div class="runtime-dotnet">...</div>`
- `<div class="runtime-typescript">...</div>`

Keep shared conceptual text untagged.

Do not wrap an entire page in only one runtime block. Every page should remain useful when either runtime is selected.

## Parity authoring rules

- if behavior differs, explain why
- keep equivalent examples for both runtimes
- update shared case references when examples change behavior

## Content quality baseline

- include a runnable or copyable snippet where practical
- include explicit next-step links for onboarding pages
- reference diagnostics/parity pages for behavior-changing examples

## Publishing

- docs deploy from GitHub Actions using VitePress build output
- keep changes to docs and `.vitepress` config in the same PR when behavior/layout changes

