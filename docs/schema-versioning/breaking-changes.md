# Breaking Change Policy

Breaking schema/runtime behavior requires a major version bump and migration guidance.

## Required actions

1. Bump major version.
2. Update versioned schema artifacts.
3. Add migration documentation with before/after examples.
4. Add shared cases for old/new behavior boundaries.
5. Validate parity across runtimes before release.

## Typical breaking examples

- Changing required rule fields
- Changing built-in transform semantics
- Changing diagnostic output expected by CI automation

