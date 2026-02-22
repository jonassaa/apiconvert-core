# Custom Transforms

Use runtime-registered deterministic functions and reference them via `source.customTransform`.

## Guidelines

- keep transform functions pure
- do not perform I/O
- keep output deterministic for same input
