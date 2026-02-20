import assert from "node:assert/strict";
import test from "node:test";
import { applyConversion, normalizeConversionRules } from "@apiconvert/core";

test("custom transform uses provided runtime registry", () => {
  const rules = normalizeConversionRules({
    inputFormat: "json",
    outputFormat: "json",
    rules: [
      {
        kind: "field",
        outputPaths: ["user.code"],
        source: {
          type: "transform",
          path: "name",
          customTransform: "reverse"
        }
      }
    ]
  });

  const result = applyConversion(
    { name: "Ada" },
    rules,
    {
      transforms: {
        reverse: (value) => String(value ?? "").split("").reverse().join("")
      }
    }
  );

  assert.deepEqual(result.errors, []);
  assert.deepEqual(result.output, { user: { code: "adA" } });
});

test("custom transform missing registry entry emits error", () => {
  const rules = normalizeConversionRules({
    inputFormat: "json",
    outputFormat: "json",
    rules: [
      {
        kind: "field",
        outputPaths: ["user.code"],
        source: {
          type: "transform",
          path: "name",
          customTransform: "reverse"
        }
      }
    ]
  });

  const result = applyConversion({ name: "Ada" }, rules);

  assert.ok(result.errors.some((error) => error.includes("custom transform 'reverse' is not registered")));
});
