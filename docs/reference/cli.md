# CLI Reference (`apiconvert`)

The npm package ships a CLI for local validation, diagnostics, conversion, and benchmarking.

## Install

```bash
npm install @apiconvert/core
```

The binary is available as `apiconvert` from `node_modules/.bin` (or via `npx apiconvert`).

## Rules commands

```bash
apiconvert rules validate rules.json
apiconvert rules lint rules.json
apiconvert rules doctor --rules rules.json --input sample.json --format json
apiconvert rules compatibility --rules rules.json --target 1.0.0
apiconvert rules bundle --rules entry.rules.json --out bundled.rules.json
apiconvert rules format --rules rules.json --out formatted.rules.json
```

## Conversion and performance

```bash
apiconvert convert --rules rules.json --input input.json --output out.json
apiconvert benchmark --rules rules.json --input samples.ndjson --iterations 1000
```

## Notes

- `convert` infers format from file extension (`.json`, `.xml`, `.txt`).
- `benchmark` expects NDJSON samples.
- `rules doctor` can use `--format json|xml|query` when the input extension is ambiguous.
