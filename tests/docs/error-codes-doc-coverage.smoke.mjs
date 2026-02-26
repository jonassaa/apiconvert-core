import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { join } from "node:path";

const root = process.cwd();
const docPath = join(root, "docs", "troubleshooting", "error-codes.md");
const runtimeFiles = [
  join(root, "src", "Apiconvert.Core", "Converters", "MappingExecutor.cs"),
  join(root, "src", "Apiconvert.Core", "Converters", "MappingExecutor.RuleHandlers.cs"),
  join(root, "src", "Apiconvert.Core", "Converters", "MappingExecutor.SourceResolvers.cs"),
  join(root, "src", "Apiconvert.Core", "Converters", "ConversionEngine.cs"),
  join(root, "src", "apiconvert-core", "src", "mapping-engine.ts"),
  join(root, "src", "apiconvert-core", "src", "rule-executor.ts"),
  join(root, "src", "apiconvert-core", "src", "source-resolver.ts")
];

function extractCodes(text) {
  return new Set(text.match(/ACV-(?:RUN|STR)-\d{3}/g) ?? []);
}

const docText = readFileSync(docPath, "utf8");
const tableMatch = docText.match(
  /<!--\s*ACV-CODES-TABLE-START\s*-->([\s\S]*?)<!--\s*ACV-CODES-TABLE-END\s*-->/
);

assert.ok(tableMatch, "Missing ACV-CODES-TABLE markers in docs/troubleshooting/error-codes.md.");

const docCodes = extractCodes(tableMatch[1]);
assert.ok(docCodes.size > 0, "No ACV-RUN/ACV-STR codes found inside the documentation table.");

const runtimeCodes = new Set();
for (const file of runtimeFiles) {
  const source = readFileSync(file, "utf8");
  for (const code of extractCodes(source)) {
    runtimeCodes.add(code);
  }
}

const missingInDocs = [...runtimeCodes].filter((code) => !docCodes.has(code)).sort();
const unknownInDocs = [...docCodes].filter((code) => !runtimeCodes.has(code)).sort();

assert.deepEqual(
  missingInDocs,
  [],
  `Missing conversion codes in docs/troubleshooting/error-codes.md: ${missingInDocs.join(", ")}`
);

assert.deepEqual(
  unknownInDocs,
  [],
  `Documented conversion codes not found in runtime sources: ${unknownInDocs.join(", ")}`
);

console.log(`Error code docs coverage smoke test passed (${docCodes.size} codes).`);
