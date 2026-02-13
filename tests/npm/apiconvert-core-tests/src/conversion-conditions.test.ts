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
    fieldMappings: [
      {
        outputPath: "exists",
        source: {
          type: "condition",
          expression: "exists(path(name))",
          trueValue: "y",
          falseValue: "n"
        }
      },
      {
        outputPath: "eqAlias",
        source: {
          type: "condition",
          expression: "path(flag) eq 'yes'",
          trueValue: "y",
          falseValue: "n"
        }
      },
      {
        outputPath: "notEqAlias",
        source: {
          type: "condition",
          expression: "path(name) not eq 'Bob'",
          trueValue: "y",
          falseValue: "n"
        }
      },
      {
        outputPath: "membership",
        source: {
          type: "condition",
          expression: "path(name) in ['Ada', 'John']",
          trueValue: "y",
          falseValue: "n"
        }
      },
      {
        outputPath: "grouped",
        source: {
          type: "condition",
          expression: "path(count) gte 3 and not (path(count) lt 5)",
          trueValue: "y",
          falseValue: "n"
        }
      }
    ],
    arrayMappings: []
  });

  const result = applyConversion(input, rules);
  assert.equal(result.errors.length, 0);

  const output = result.output as Record<string, unknown>;
  assert.equal(output.exists, "y");
  assert.equal(output.eqAlias, "y");
  assert.equal(output.notEqAlias, "y");
  assert.equal(output.membership, "y");
  assert.equal(output.grouped, "y");
});

test("condition expression can read root path inside item mapping", () => {
  const input = {
    meta: { source: "api" },
    items: [{ value: "x" }]
  };

  const rules = normalizeConversionRules({
    version: 2,
    inputFormat: "json",
    outputFormat: "json",
    fieldMappings: [],
    arrayMappings: [
      {
        inputPath: "items",
        outputPath: "items",
        itemMappings: [
          {
            outputPath: "match",
            source: {
              type: "condition",
              expression: "path($.meta.source) == 'api' && path(value) == 'x'",
              trueValue: "yes",
              falseValue: "no"
            }
          }
        ]
      }
    ]
  });

  const result = applyConversion(input, rules);
  assert.equal(result.errors.length, 0);

  const output = result.output as Record<string, unknown>;
  const items = output.items as Array<Record<string, unknown>>;
  assert.equal(items[0].match, "yes");
});

test("invalid condition expression adds error and returns false branch", () => {
  const input = { name: "Ada" };

  const rules = normalizeConversionRules({
    version: 2,
    inputFormat: "json",
    outputFormat: "json",
    fieldMappings: [
      {
        outputPath: "match",
        source: {
          type: "condition",
          expression: "path(name) ==",
          trueValue: "yes",
          falseValue: "no"
        }
      }
    ],
    arrayMappings: []
  });

  const result = applyConversion(input, rules);
  assert.equal(result.errors.length, 1);
  assert.match(result.errors[0], /invalid condition expression/);

  const output = result.output as Record<string, unknown>;
  assert.equal(output.match, "no");
});

test("legacy condition object fails hard cut", () => {
  const input = { name: "Ada" };

  const rules = normalizeConversionRules({
    version: 2,
    inputFormat: "json",
    outputFormat: "json",
    fieldMappings: [
      {
        outputPath: "match",
        source: {
          type: "condition",
          condition: { path: "name", operator: "equals", value: "Ada" },
          trueValue: "yes",
          falseValue: "no"
        }
      }
    ],
    arrayMappings: []
  });

  const result = applyConversion(input, rules);
  assert.equal(result.errors.length, 1);
  assert.match(result.errors[0], /condition expression is required/);

  const output = result.output as Record<string, unknown>;
  assert.equal(output.match, "no");
});

test("condition supports nested sources and elseIf chain", () => {
  const input = { score: 72, profile: { level: "silver" } };

  const rules = normalizeConversionRules({
    version: 2,
    inputFormat: "json",
    outputFormat: "json",
    fieldMappings: [
      {
        outputPath: "grade",
        source: {
          type: "condition",
          expression: "path(score) >= 90",
          trueSource: { type: "constant", value: "A" },
          elseIf: [
            {
              expression: "path(score) >= 80",
              value: "B"
            },
            {
              expression: "path(score) >= 70",
              source: { type: "constant", value: "C" }
            }
          ],
          falseSource: { type: "constant", value: "F" }
        }
      },
      {
        outputPath: "tier",
        source: {
          type: "condition",
          expression: "path(profile.level) == 'gold'",
          trueSource: { type: "constant", value: "priority" },
          falseSource: {
            type: "condition",
            expression: "path(profile.level) == 'silver'",
            trueSource: { type: "constant", value: "standard" },
            falseValue: "basic"
          }
        }
      }
    ],
    arrayMappings: []
  });

  const result = applyConversion(input, rules);
  assert.equal(result.errors.length, 0);

  const output = result.output as Record<string, unknown>;
  assert.equal(output.grade, "C");
  assert.equal(output.tier, "standard");
});

test("condition output mode match returns boolean", () => {
  const input = { count: 5 };

  const rules = normalizeConversionRules({
    version: 2,
    inputFormat: "json",
    outputFormat: "json",
    fieldMappings: [
      {
        outputPath: "isLarge",
        source: {
          type: "condition",
          expression: "path(count) > 3",
          conditionOutput: "match",
          trueValue: "yes",
          falseValue: "no"
        }
      }
    ],
    arrayMappings: []
  });

  const result = applyConversion(input, rules);
  assert.equal(result.errors.length, 0);

  const output = result.output as Record<string, unknown>;
  assert.equal(output.isLarge, true);
});

test("invalid elseIf expression adds error and falls through", () => {
  const input = { score: 65 };

  const rules = normalizeConversionRules({
    version: 2,
    inputFormat: "json",
    outputFormat: "json",
    fieldMappings: [
      {
        outputPath: "grade",
        source: {
          type: "condition",
          expression: "path(score) > 90",
          trueValue: "A",
          elseIf: [
            {
              expression: "path(score) ==",
              value: "B"
            }
          ],
          falseValue: "F"
        }
      }
    ],
    arrayMappings: []
  });

  const result = applyConversion(input, rules);
  assert.equal(result.errors.length, 1);
  assert.match(result.errors[0], /invalid elseIf expression/);

  const output = result.output as Record<string, unknown>;
  assert.equal(output.grade, "F");
});
