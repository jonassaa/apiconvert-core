import assert from "node:assert/strict";
import test from "node:test";
import { lintConversionRules, RuleLintSeverity } from "@apiconvert/core";

test("lintConversionRules reports normalization errors", () => {
  const result = lintConversionRules("{ not-valid-json }");

  assert.equal(result.hasErrors, true);
  assert.ok(
    result.diagnostics.some(
      (entry) => entry.code === "ACV-LINT-001" && entry.severity === RuleLintSeverity.Error
    )
  );
});

test("lintConversionRules reports duplicate outputs and unreachable branches", () => {
  const rawRules = {
    inputFormat: "json",
    outputFormat: "json",
    rules: [
      {
        kind: "field",
        outputPaths: ["meta.name"],
        source: { type: "path", path: "name" }
      },
      {
        kind: "field",
        outputPaths: ["meta.name"],
        source: { type: "path", path: "nickname" }
      },
      {
        kind: "branch",
        expression: "true",
        then: [
          {
            kind: "field",
            outputPaths: ["meta.flag"],
            source: { type: "constant", value: "Y" }
          }
        ],
        else: [
          {
            kind: "field",
            outputPaths: ["meta.flag"],
            source: { type: "constant", value: "N" }
          }
        ]
      }
    ]
  };

  const result = lintConversionRules(rawRules);

  assert.ok(result.diagnostics.some((entry) => entry.code === "ACV-LINT-002" && entry.rulePath === "rules[0]"));
  assert.ok(result.diagnostics.some((entry) => entry.code === "ACV-LINT-005" && entry.rulePath === "rules[1]"));
  assert.ok(result.diagnostics.some((entry) => entry.code === "ACV-LINT-003" && entry.rulePath === "rules[2]"));
  assert.ok(result.diagnostics.every((entry) => entry.suggestion.length > 0));
});
