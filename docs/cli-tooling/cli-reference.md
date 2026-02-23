# CLI Reference

The npm package ships `apiconvert` for local validation and conversion workflows.

## Rules commands

```bash
apiconvert rules validate rules.json
apiconvert rules lint rules.json
apiconvert rules doctor --rules rules.json --input sample.json --format json
apiconvert rules compatibility --rules rules.json --target 1.0.0
apiconvert rules bundle --rules entry.rules.json --out bundled.rules.json
```

## Convert and benchmark

```bash
apiconvert convert --rules rules.json --input input.json --output out.json
apiconvert benchmark --rules rules.json --input samples.ndjson --iterations 1000
```

