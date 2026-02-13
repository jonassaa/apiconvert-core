import assert from "node:assert/strict";
import test from "node:test";
import { applyConversion, normalizeConversionRules } from "@apiconvert/core";

test("condition expressions evaluate aliases and grouping", () => {
  const input = {
    name: "Ada",
    count: 5,
    flag: "yes"
  };

  const rules = normalizeConversionRules({
    version: 2,
    inputFormat: "json",
    outputFormat: "json",
    rules: [
      {
        kind: "field",
        outputPaths: ["exists"],
        source: {
          type: "condition",
          expression: "exists(path(name))",
          trueValue: "y",
          falseValue: "n"
        }
      },
      {
        kind: "field",
        outputPaths: ["eqAlias"],
        source: {
          type: "condition",
          expression: "path(flag) eq 'yes'",
          trueValue: "y",
          falseValue: "n"
        }
      },
      {
        kind: "field",
        outputPaths: ["grouped"],
        source: {
          type: "condition",
          expression: "path(count) gte 3 and not (path(count) lt 5)",
          trueValue: "y",
          falseValue: "n"
        }
      }
    ]
  });

  const result = applyConversion(input, rules);
  assert.equal(result.errors.length, 0);

  const output = result.output as Record<string, unknown>;
  assert.equal(output.exists, "y");
  assert.equal(output.eqAlias, "y");
  assert.equal(output.grouped, "y");
});

test("branch rules execute then / elseIf / else", () => {
  const input = { score: 72 };
  const rules = normalizeConversionRules({
    version: 2,
    inputFormat: "json",
    outputFormat: "json",
    rules: [
      {
        kind: "branch",
        expression: "path(score) >= 90",
        then: [
          {
            kind: "field",
            outputPaths: ["grade"],
            source: { type: "constant", value: "A" }
          }
        ],
        elseIf: [
          {
            expression: "path(score) >= 80",
            then: [
              {
                kind: "field",
                outputPaths: ["grade"],
                source: { type: "constant", value: "B" }
              }
            ]
          },
          {
            expression: "path(score) >= 70",
            then: [
              {
                kind: "field",
                outputPaths: ["grade"],
                source: { type: "constant", value: "C" }
              }
            ]
          }
        ],
        else: [
          {
            kind: "field",
            outputPaths: ["grade"],
            source: { type: "constant", value: "F" }
          }
        ]
      }
    ]
  });

  const result = applyConversion(input, rules);
  assert.equal(result.errors.length, 0);
  const output = result.output as Record<string, unknown>;
  assert.equal(output.grade, "C");
});

test("branch expression invalid adds error", () => {
  const input = { name: "Ada" };
  const rules = normalizeConversionRules({
    version: 2,
    inputFormat: "json",
    outputFormat: "json",
    rules: [
      {
        kind: "branch",
        expression: "path(name) ==",
        then: [
          {
            kind: "field",
            outputPaths: ["match"],
            source: { type: "constant", value: "yes" }
          }
        ],
        else: [
          {
            kind: "field",
            outputPaths: ["match"],
            source: { type: "constant", value: "no" }
          }
        ]
      }
    ]
  });

  const result = applyConversion(input, rules);
  assert.equal(result.errors.length, 1);
  assert.match(result.errors[0], /invalid branch expression/);

  const output = result.output as Record<string, unknown>;
  assert.equal(output.match, "no");
});

test("legacy keys are rejected", () => {
  const result = applyConversion(
    { name: "Ada" },
    {
      version: 2,
      inputFormat: "json",
      outputFormat: "json",
      fieldMappings: [
        {
          outputPath: "name",
          source: { type: "path", path: "name" }
        }
      ]
    }
  );

  assert.ok(result.errors.length >= 1);
  assert.ok(result.errors.some((entry) => /legacy property 'fieldMappings' is not supported/.test(entry)));
});

test("rule recursion overflow is deterministic", () => {
  const deepBranch = (): Record<string, unknown> => ({
    kind: "branch",
    expression: "true",
    then: [] as unknown[]
  });

  const root = deepBranch();
  let cursor = root;
  for (let index = 0; index < 70; index += 1) {
    const next = deepBranch();
    (cursor.then as unknown[]).push(next);
    cursor = next;
  }

  const result = applyConversion({}, { version: 2, inputFormat: "json", outputFormat: "json", rules: [root] });
  assert.ok(result.errors.some((entry) => entry.includes("rule recursion limit exceeded")));
});
