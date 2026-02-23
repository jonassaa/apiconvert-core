# What Is Apiconvert.Core?

Apiconvert.Core is a deterministic, rule-driven engine for transforming API payloads. It ships in two runtimes:

- `.NET` package: `Apiconvert.Core`
- `TypeScript/Node` package: `@apiconvert/core`

Both runtimes implement the same rule model and are validated against shared parity cases.

## What problem it solves

Instead of writing one-off mapping code for each integration, you define transformations as JSON rules:

- map fields from input to output
- branch on conditions
- transform values (case, number, boolean, split, concat)
- map arrays with nested item rules
- convert between `json`, `xml`, and `query` payload formats

This keeps transformation logic declarative, testable, and versionable.

## What this package does not do

Apiconvert.Core does not include HTTP handling, auth, persistence, UI, or orchestration. It only performs in-memory conversion and diagnostics.

## Start here

- New to the package: [Getting started](../getting-started/index.md)
- First end-to-end walkthrough: [First conversion](../getting-started/first-conversion.md)
- Rule authoring source of truth: [Rules schema reference](../reference/rules-schema.md)
- Runtime API reference: [Runtime APIs](../guides/runtime-api.md)
