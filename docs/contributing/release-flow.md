# Release Flow

Publishing is tag-driven:

1. create release tag workflow (`patch`, `minor`, `major`)
2. workflow creates `vX.Y.Z`
3. package publish pipeline releases NuGet and npm artifacts
4. docs pipeline publishes matching version docs
