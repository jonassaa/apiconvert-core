# Intent and Boundaries

Apiconvert.Core is conversion logic only.

## Intent

- Rule-driven API transformation
- Deterministic, side-effect-free execution
- Parity between .NET and TypeScript runtimes

## Out of scope

- API gateway and proxy features
- Auth, HTTP, persistence, orchestration, UI concerns
- External I/O inside conversion execution paths

## Decision checks

- Does this belong in conversion engine scope?
- Can behavior be declared in rules rather than custom integration code?
- Is determinism preserved?
- Is cross-runtime parity maintained?

