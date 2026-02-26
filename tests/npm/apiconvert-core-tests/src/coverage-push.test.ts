import assert from "node:assert/strict";
import test from "node:test";
import {
  DataFormat,
  applyConversion,
  checkRulesCompatibility,
  formatConversionRules,
  formatPayload,
  lintConversionRules,
  parsePayload,
  runRuleDoctor,
  validateConversionRules
} from "@apiconvert/core";

test("compatibility reports invalid target/schema and out-of-range versions", () => {
  const report = checkRulesCompatibility(
    {
      schemaVersion: "bad",
      inputFormat: "bad-format",
      outputFormat: "json",
      rules: []
    },
    { targetVersion: "nope" }
  );

  assert.equal(report.isCompatible, false);
  assert.ok(report.diagnostics.some((entry) => entry.code === "ACV-COMP-001"));
  assert.ok(report.diagnostics.some((entry) => entry.code === "ACV-COMP-005"));
  assert.ok(report.diagnostics.some((entry) => entry.code === "ACV-COMP-006"));

  const rangeReport = checkRulesCompatibility(
    {
      schemaVersion: "1.0.0",
      rules: []
    },
    { targetVersion: "0.9.0" }
  );
  assert.ok(rangeReport.diagnostics.some((entry) => entry.code === "ACV-COMP-003"));
});

test("runRuleDoctor emits runtime-skipped info and runtime diagnostics from conversion", () => {
  const skipped = runRuleDoctor({ rules: [] }, {});
  assert.ok(skipped.findings.some((entry) => entry.code === "ACV-DOCTOR-100"));

  const report = runRuleDoctor(
    {
      inputFormat: DataFormat.Json,
      outputFormat: DataFormat.Json,
      rules: [
        {
          kind: "array",
          inputPath: "items",
          outputPaths: ["items"],
          itemRules: []
        },
        {
          kind: "field",
          outputPaths: ["name.custom"],
          source: { type: "transform", path: "name", customTransform: "missing" }
        }
      ]
    },
    {
      sampleInputText: JSON.stringify({ name: "Ada" }),
      inputFormat: DataFormat.Json
    }
  );

  assert.ok(report.findings.some((entry) => entry.code === "ACV-DOCTOR-010"));
  assert.ok(report.findings.some((entry) => entry.code === "ACV-DOCTOR-011"));
});

test("formatConversionRules canonicalizes branch/array/source defaults", () => {
  const formatted = formatConversionRules(
    {
      inputFormat: "xml",
      outputFormat: "query",
      rules: [
        {
          kind: "field",
          outputPaths: ["user.flag"],
          defaultValue: "fallback",
          source: {
            type: "condition",
            expression: "path(name) == 'Ada'",
            trueValue: "T",
            falseValue: "F",
            elseIf: [{ expression: "true", value: "E" }]
          }
        },
        {
          kind: "field",
          outputPaths: ["user.value"],
          source: { type: "merge", paths: ["first", "second"] }
        },
        {
          kind: "field",
          outputPaths: ["user.lower"],
          source: { type: "transform", path: "name" }
        },
        {
          kind: "array",
          inputPath: "items",
          outputPaths: ["items"],
          coerceSingle: true,
          itemRules: [{ kind: "field", outputPaths: ["value"], source: { type: "path", path: "x" } }]
        },
        {
          kind: "branch",
          expression: "false",
          then: [{ kind: "field", outputPaths: ["then.value"], source: { type: "constant", value: "t" } }],
          elseIf: [{ expression: "true", then: [] }],
          else: [{ kind: "field", outputPaths: ["else.value"], source: { type: "constant", value: "e" } }]
        }
      ]
    },
    { pretty: false }
  );

  const parsed = JSON.parse(formatted) as {
    inputFormat: string;
    outputFormat: string;
    rules: Array<{ kind: string; source?: { type: string; conditionOutput?: string; mergeMode?: string; transform?: string } }>;
  };
  assert.equal(parsed.inputFormat, "xml");
  assert.equal(parsed.outputFormat, "query");
  assert.equal(parsed.rules[0].source?.conditionOutput, "branch");
  assert.equal(parsed.rules[1].source?.mergeMode, "concat");
  assert.equal(parsed.rules[2].source?.transform, "toLowerCase");
});

test("query payload parse/format covers nested, repeated and encoded values", () => {
  const parsed = parsePayload(
    "?a=1&a=2&arr[0]=x&arr[1]=y&obj.name=A&obj.name=B&space=hello+world&empty=",
    DataFormat.Query
  );
  assert.equal(parsed.error == null, true);
  assert.deepEqual(parsed.value, {
    a: ["1", "2"],
    arr: ["x", "y"],
    obj: { name: ["A", "B"] },
    space: "hello world",
    empty: ""
  });

  const text = formatPayload(
    {
      q: "search",
      nested: { one: 1, two: true },
      list: ["x", null, { z: 2 }]
    },
    DataFormat.Query,
    false
  );
  assert.ok(text.includes("q=search"));
  assert.ok(text.includes("nested.one=1"));
  assert.ok(text.includes("nested.two=true"));
  assert.ok(text.includes("list=x"));
});

test("source resolver paths exercise condition, merge and transform variants via applyConversion", () => {
  const result = applyConversion(
    {
      name: "Ada Lovelace",
      first: "",
      second: "fallback",
      tags: ["one", "two"]
    },
    {
      rules: [
        {
          kind: "field",
          outputPaths: ["full.concat"],
          source: { type: "transform", transform: "concat", path: "name,const:!" }
        },
        {
          kind: "field",
          outputPaths: ["full.lastName"],
          source: { type: "transform", transform: "split", path: "name", separator: " ", tokenIndex: -1 }
        },
        {
          kind: "field",
          outputPaths: ["merged.first"],
          source: { type: "merge", paths: ["first", "second"], mergeMode: "firstNonEmpty" }
        },
        {
          kind: "field",
          outputPaths: ["merged.array"],
          source: { type: "merge", paths: ["first", "second"], mergeMode: "array" }
        },
        {
          kind: "field",
          outputPaths: ["cond.match"],
          source: { type: "condition", expression: "path(name) == 'Ada Lovelace'", conditionOutput: "match" }
        },
        {
          kind: "field",
          outputPaths: ["cond.branch"],
          source: {
            type: "condition",
            expression: "false",
            elseIf: [{ expression: "path(name) == 'Ada Lovelace'", source: { type: "path", path: "name" } }],
            falseValue: "no"
          }
        }
      ]
    }
  );

  assert.equal(result.errors.length, 0);
  assert.deepEqual(result.output, {
    full: { concat: "Ada Lovelace!", lastName: "Lovelace" },
    merged: { first: "fallback", array: ["", "fallback"] },
    cond: { match: true, branch: "Ada Lovelace" }
  });
});

test("normalizer and linter cover aliases, fragments and always-false branch warning", () => {
  const validation = validateConversionRules({
    fragments: {
      base: { kind: "field", outputPaths: ["value"], source: { type: "path", path: "a" }, use: "nested" },
      nested: { kind: "field", outputPaths: ["value"], source: { type: "path", path: "b" }, use: "base" }
    },
    rules: [
      { kind: "field", to: "user.name", from: "name" },
      { kind: "field", outputPath: "user.const", const: 7 },
      { kind: "field", outputPaths: ["user.trim"], from: "name", as: "toUpperCase" },
      { kind: "map", entries: [{ to: "user.map", from: "name" }, "bad-entry"] },
      { use: "base" },
      { use: "unknown-fragment" }
    ]
  });

  assert.equal(validation.isValid, false);
  assert.ok(validation.errors.some((entry) => entry.includes("unknown fragment")));
  assert.ok(validation.errors.some((entry) => entry.includes("introduces a cycle")));
  assert.ok(validation.errors.some((entry) => entry.includes("must be an object")));

  const lint = lintConversionRules({
    rules: [
      {
        kind: "branch",
        expression: "false",
        then: [{ kind: "field", outputPaths: ["never"], source: { type: "constant", value: "x" } }]
      }
    ]
  });
  assert.ok(lint.diagnostics.some((entry) => entry.code === "ACV-LINT-004"));
});
