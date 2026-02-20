import assert from "node:assert/strict";
import test from "node:test";
import {
  applyConversion,
  normalizeConversionRules,
  normalizeConversionRulesStrict,
  validateConversionRules
} from "@apiconvert/core";

test("invalid rules JSON reports validation errors", () => {
  const rules = normalizeConversionRules("{bad json");
  assert.ok((rules.validationErrors ?? []).length > 0);
  assert.match((rules.validationErrors ?? [])[0], /invalid JSON/i);
});

test("strict normalization throws on validation errors", () => {
  assert.throws(
    () =>
      normalizeConversionRulesStrict({
        rules: [
          {
            kind: "field",
            outputPaths: ["out.value"],
            source: { type: "unknown", path: "name" }
          }
        ]
      }),
    /Invalid conversion rules/
  );
});

test("validateConversionRules surfaces unsupported source types", () => {
  const validation = validateConversionRules({
    rules: [
      {
        kind: "field",
        outputPaths: ["out.value"],
        source: { type: "unsupported" }
      }
    ]
  });

  assert.equal(validation.isValid, false);
  assert.ok(validation.errors.some((entry) => entry.includes("unsupported source type")));
});

test("applyConversion carries rule validation errors into result", () => {
  const result = applyConversion(
    { name: "Ada" },
    {
      rules: [
        {
          kind: "field",
          outputPaths: ["out.value"],
          source: { type: "unsupported", path: "name" }
        }
      ]
    }
  );

  assert.ok(result.errors.some((entry) => entry.includes("unsupported source type")));
});
