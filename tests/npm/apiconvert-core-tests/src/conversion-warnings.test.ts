import assert from "node:assert/strict";
import test from "node:test";
import { applyConversion, normalizeConversionRules } from "@apiconvert/core";

test("missing array input path yields warning and no errors", () => {
  const input = { name: "Ada" };
  const rules = normalizeConversionRules({
    version: 2,
    inputFormat: "json",
    outputFormat: "json",
    fieldMappings: [
      {
        outputPath: "user.name",
        source: { type: "path", path: "name" }
      }
    ],
    arrayMappings: [
      {
        inputPath: "items",
        outputPath: "lines",
        itemMappings: [
          {
            outputPath: "id",
            source: { type: "path", path: "id" }
          }
        ]
      }
    ]
  });

  const result = applyConversion(input, rules);

  assert.equal(result.errors.length, 0);
  assert.equal(result.warnings.length, 1);
  assert.equal(
    result.warnings[0],
    'Array mapping skipped: inputPath "items" not found (arrayMappings[0]).'
  );

  const output = result.output as Record<string, unknown>;
  assert.deepEqual(output.user, { name: "Ada" });
  assert.equal("lines" in output, false);
});

test("non-array value at array input path remains an error", () => {
  const input = { items: "not-an-array" };
  const rules = normalizeConversionRules({
    version: 2,
    inputFormat: "json",
    outputFormat: "json",
    fieldMappings: [],
    arrayMappings: [
      {
        inputPath: "items",
        outputPath: "lines",
        itemMappings: []
      }
    ]
  });

  const result = applyConversion(input, rules);

  assert.equal(result.errors.length, 1);
  assert.equal(result.warnings.length, 0);
  assert.match(result.errors[0], /input path did not resolve to an array/);
});
