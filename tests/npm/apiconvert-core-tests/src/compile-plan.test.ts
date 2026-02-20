import assert from "node:assert/strict";
import test from "node:test";
import { applyConversion, compileConversionPlan, computeRulesCacheKey } from "@apiconvert/core";

test("computeRulesCacheKey is stable for equivalent input", () => {
  const rawRules = {
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

  const key1 = computeRulesCacheKey(rawRules);
  const key2 = computeRulesCacheKey(rawRules);

  assert.equal(key1, key2);
});

test("compileConversionPlan reuses normalized rules and keeps output parity", () => {
  const rawRules = {
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

  const plan = compileConversionPlan(rawRules);
  const direct = applyConversion({ name: "Ada" }, rawRules);
  const viaPlan = plan.apply({ name: "Ada" });

  assert.equal(plan.cacheKey, computeRulesCacheKey(rawRules));
  assert.deepEqual(viaPlan.output, direct.output);
  assert.deepEqual(viaPlan.errors, direct.errors);
  assert.deepEqual(viaPlan.warnings, direct.warnings);
});
