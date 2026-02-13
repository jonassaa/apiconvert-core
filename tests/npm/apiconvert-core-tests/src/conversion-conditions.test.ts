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
