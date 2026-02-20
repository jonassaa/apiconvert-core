import assert from "node:assert/strict";
import test from "node:test";
import {
  DataFormat,
  RuleDoctorFindingSeverity,
  runRuleDoctor
} from "@apiconvert/core";

test("runRuleDoctor returns deterministic validation/lint/runtime order", () => {
  const rawRules = {
    inputFormat: DataFormat.Json,
    outputFormat: DataFormat.Json,
    rules: [
      {
        kind: "field",
        outputPaths: ["customer.name", "customer.name"],
        source: { type: "path", path: "user.name" }
      }
    ]
  };

  const report = runRuleDoctor(rawRules, {
    sampleInputText: JSON.stringify({ user: { name: "Ada" } }),
    inputFormat: DataFormat.Json
  });

  assert.equal(report.findings[0].stage, "lint");
  assert.equal(report.findings[0].code, "ACV-LINT-002");
  assert.equal(report.hasErrors, false);
  assert.ok(report.safeFixPreview.length > 0);
});

test("runRuleDoctor includes parse errors as ACV-DOCTOR-001", () => {
  const rules = {
    inputFormat: DataFormat.Json,
    outputFormat: DataFormat.Json,
    rules: []
  };

  const report = runRuleDoctor(rules, {
    sampleInputText: "{not-json}",
    inputFormat: DataFormat.Json
  });

  const parseFinding = report.findings.find((entry) => entry.code === "ACV-DOCTOR-001");
  assert.ok(parseFinding);
  assert.equal(parseFinding?.severity, RuleDoctorFindingSeverity.Error);
  assert.equal(report.hasErrors, true);
});
