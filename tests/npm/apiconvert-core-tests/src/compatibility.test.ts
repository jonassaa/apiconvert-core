import assert from "node:assert/strict";
import test from "node:test";
import { checkRulesCompatibility } from "@apiconvert/core";

test("checkRulesCompatibility reports missing schemaVersion as warning", () => {
  const report = checkRulesCompatibility(
    {
      inputFormat: "json",
      outputFormat: "json",
      rules: []
    },
    { targetVersion: "1.0.0" }
  );

  assert.equal(report.isCompatible, true);
  assert.equal(report.diagnostics[0].code, "ACV-COMP-002");
});

test("checkRulesCompatibility fails when schemaVersion exceeds target", () => {
  const report = checkRulesCompatibility(
    {
      schemaVersion: "1.1.0",
      inputFormat: "json",
      outputFormat: "json",
      rules: []
    },
    { targetVersion: "1.0.0" }
  );

  assert.equal(report.isCompatible, false);
  assert.ok(report.diagnostics.some((entry) => entry.code === "ACV-COMP-004"));
});
