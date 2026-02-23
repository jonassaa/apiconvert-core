# Release Flow

Publishing is tag-driven with lockstep versions.

## Process

1. Trigger release tag workflow (`patch`, `minor`, `major`).
2. Workflow creates `vX.Y.Z`.
3. Publish pipeline releases NuGet and npm artifacts.
4. Docs pipeline publishes matching version docs.

## Pre-release checks

- .NET tests pass
- npm tests pass
- parity checks pass
- docs updated for API/behavior changes

