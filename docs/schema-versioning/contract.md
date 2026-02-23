# Schema Compatibility Contract

The rules schema is a cross-runtime contract. Any change must preserve compatibility unless explicitly versioned.

## Contract scope

- Rule node structure and required fields
- Source and transform semantics
- Validation behavior and error surfaces
- Compatibility expectations for diagnostics consumed by automation

## Required checks for contract changes

1. Update schema artifacts in `schemas/rules`.
2. Implement behavior in both runtimes.
3. Add or update shared cases for changed behavior.
4. Run parity checks before merge.
5. Document migration notes for consumer-facing changes.

