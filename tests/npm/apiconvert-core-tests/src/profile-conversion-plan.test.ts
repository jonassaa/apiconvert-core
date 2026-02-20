import assert from "node:assert/strict";
import test from "node:test";
import { profileConversionPlan } from "@apiconvert/core";

test("profileConversionPlan returns report with deterministic shape", () => {
  const rules = {
    inputFormat: "json",
    outputFormat: "json",
    rules: [
      {
        kind: "field",
        outputPaths: ["user.name"],
        source: { type: "path", path: "name" }
      }
    ]
  };

  const report = profileConversionPlan(rules, [{ name: "Ada" }, { name: "Lin" }], {
    iterations: 3,
    warmupIterations: 1
  });

  assert.equal(report.totalRuns, 6);
  assert.equal(report.iterations, 3);
  assert.equal(report.warmupIterations, 1);
  assert.ok(report.planCacheKey.length > 0);
  assert.ok(report.latencyMs.p50 >= 0);
});
