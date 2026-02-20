import assert from "node:assert/strict";
import test from "node:test";
import { applyConversion, normalizeConversionRules } from "@apiconvert/core";

test("explain disabled returns empty trace", () => {
  const rules = normalizeConversionRules({
    rules: [
      {
        kind: "field",
        outputPaths: ["name"],
        source: { type: "constant", value: "Ada" }
      }
    ]
  });

  const result = applyConversion({}, rules);

  assert.equal(result.errors.length, 0);
  assert.deepEqual(result.trace, []);
});

test("explain enabled emits deterministic trace timeline", () => {
  const input = {
    name: "Ada",
    score: 72,
    items: [{ id: "A1" }, { id: "B2" }]
  };

  const rules = normalizeConversionRules({
    inputFormat: "json",
    outputFormat: "json",
    rules: [
      {
        kind: "field",
        outputPaths: ["profile.name"],
        source: { type: "path", path: "name" }
      },
      {
        kind: "branch",
        expression: "path(score) >= 80",
        then: [
          {
            kind: "field",
            outputPaths: ["profile.grade"],
            source: { type: "constant", value: "B" }
          }
        ],
        else: [
          {
            kind: "field",
            outputPaths: ["profile.grade"],
            source: { type: "constant", value: "C" }
          }
        ]
      },
      {
        kind: "array",
        inputPath: "items",
        outputPaths: ["lines"],
        itemRules: [
          {
            kind: "field",
            outputPaths: ["id"],
            source: { type: "path", path: "id" }
          }
        ]
      }
    ]
  });

  const result = applyConversion(input, rules, { explain: true });
  const trace = result.trace ?? [];

  assert.equal(result.errors.length, 0);
  assert.equal(trace.length, 6);

  assert.equal(trace[0].rulePath, "rules[0]");
  assert.equal(trace[0].ruleKind, "field");
  assert.equal(trace[0].decision, "applied");

  assert.equal(trace[1].rulePath, "rules[1]");
  assert.equal(trace[1].ruleKind, "branch");
  assert.equal(trace[1].decision, "else");

  assert.equal(trace[2].rulePath, "rules[1].else[0]");
  assert.equal(trace[2].ruleKind, "field");
  assert.equal(trace[2].decision, "applied");

  assert.equal(trace[3].rulePath, "rules[2].itemRules[0]");
  assert.equal(trace[3].ruleKind, "field");
  assert.equal(trace[3].decision, "applied");

  assert.equal(trace[4].rulePath, "rules[2].itemRules[0]");
  assert.equal(trace[4].ruleKind, "field");
  assert.equal(trace[4].decision, "applied");

  assert.equal(trace[5].rulePath, "rules[2]");
  assert.equal(trace[5].ruleKind, "array");
  assert.equal(trace[5].decision, "mapped");
  assert.deepEqual(trace[5].outputPaths, ["lines"]);
});
