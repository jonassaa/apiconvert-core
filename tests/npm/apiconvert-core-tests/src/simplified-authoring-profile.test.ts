import assert from "node:assert/strict";
import test from "node:test";
import { applyConversion, normalizeConversionRules } from "@apiconvert/core";

test("field aliases normalize to canonical behavior", () => {
  const rules = normalizeConversionRules({
    rules: [
      { kind: "field", from: "name", to: "user.name" },
      { kind: "field", to: ["meta.source"], const: "crm" },
      { kind: "field", from: "email", to: ["user.emailLower"], as: "toLowerCase" }
    ]
  });

  assert.equal(rules.rules?.length, 3);
  const first = rules.rules?.[0] as { source: { type: string; path: string }; outputPaths: string[] };
  const third = rules.rules?.[2] as { source: { type: string; transform: string } };

  assert.equal(first.source.type, "path");
  assert.equal(first.source.path, "name");
  assert.equal(first.outputPaths[0], "user.name");
  assert.equal(third.source.type, "transform");
  assert.equal(third.source.transform, "toLowerCase");
  assert.deepEqual(rules.validationErrors, []);
});

test("map rule expands entries into field mappings", () => {
  const result = applyConversion(
    { id: "123", name: "Ada" },
    {
      rules: [
        {
          kind: "map",
          entries: [
            { from: "id", to: "user.id" },
            { from: "name", to: "user.name" }
          ]
        }
      ]
    }
  );

  assert.deepEqual(result.errors, []);
  assert.deepEqual(result.warnings, []);
  assert.deepEqual(result.output, { user: { id: "123", name: "Ada" } });
});
