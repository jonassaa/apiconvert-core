# GitHub Issue Draft: TypeScript Package Consumer Gaps (Expert Review)

## Suggested Title
`TS package: close consumer adoption gaps (ESM support, value typing, true streaming)`

## Suggested Labels
- `npm-runtime`
- `enhancement`
- `dx`

## Body
### Summary
A recent expert consumer review of `@apiconvert/core` identified several adoption blockers and DX gaps for production Node/TypeScript users.

### Top gaps (P0)
1. **Dual module support missing (ESM + CJS)**
   - Package is currently CJS-only and should publish conditional exports for both `import` and `require`.
2. **Value typing too string-constrained**
   - Rule value-bearing fields are typed as `string | null` in several places, which hurts TS ergonomics for booleans/numbers/objects.
3. **Streaming is not fully incremental for NDJSON/query/XML**
   - Non-array stream modes buffer full input text before item parse/emission, causing memory pressure on large inputs.

### Additional gaps (P1/P2)
- Harmonize top-level error handling modes (`throw` vs `report`).
- Improve CLI argument UX and machine-friendly output switches.
- Add API/type contract snapshot tests for semver safety.
- Improve npm README link robustness and add production-oriented guidance.

### Requested actions
- [ ] Add dual ESM+CJS build + conditional exports.
- [ ] Widen value-bearing TS rule fields to JSON-compatible value types.
- [ ] Implement bounded-memory incremental stream parsing for NDJSON/query (and improve XML element extraction strategy).
- [ ] Add consumer contract tests (API/type snapshots).
- [ ] Update docs for production usage and module-mode compatibility.

### Source
See full report: `reviews/ts-package-consumer-review.md`
