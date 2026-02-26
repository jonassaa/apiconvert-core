import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import test from "node:test";
import {
  StreamErrorMode,
  StreamInputKind,
  bundleConversionRules,
  normalizeConversionRules,
  streamConversion,
  validateConversionRules
} from "@apiconvert/core";

test("normalizeConversionRules and validateConversionRules cover edge validation branches", () => {
  assert.ok((normalizeConversionRules(42).validationErrors ?? []).some((entry) => entry.includes("expected")));
  assert.ok(
    (normalizeConversionRules('{"notRules":true}').validationErrors ?? []).some((entry) =>
      entry.includes("expected conversion rules object")
    )
  );

  const validation = validateConversionRules({
    inputFormat: "yaml",
    outputFormat: "csv",
    fragments: {
      "  ": {},
      bad: "nope",
      a: { use: "b", kind: "field", outputPaths: ["x"], source: { type: "path", path: "x" } },
      b: { use: "a", kind: "field", outputPaths: ["y"], source: { type: "path", path: "y" } }
    },
    rules: [
      null,
      { kind: "unknown" },
      {
        kind: "field",
        outputPaths: [],
        as: "toUpperCase",
        source: {
          type: "condition",
          expression: "",
          elseIf: "bad",
          conditionOutput: "bad",
          mergeMode: "bad",
          transform: "bad",
          separator: "",
          tokenIndex: "x"
        }
      },
      {
        kind: "field",
        outputPath: "x",
        source: { type: "transform", transform: "split", path: "name", separator: "" }
      },
      {
        kind: "array",
        inputPath: "",
        outputPaths: [],
        itemRules: "bad"
      },
      {
        kind: "branch",
        expression: "",
        then: "bad",
        elseIf: [{ then: "bad" }, "bad"],
        else: "bad"
      },
      {
        kind: "map",
        entries: [{ to: "out.value", from: "name" }, "bad"]
      },
      { use: "unknown-fragment" },
      { use: "a" }
    ]
  });

  assert.equal(validation.isValid, false);
  assert.ok(validation.errors.some((entry) => entry.includes("unsupported format")));
  assert.ok(validation.errors.some((entry) => entry.includes("fragment name is required")));
  assert.ok(validation.errors.some((entry) => entry.includes("must be an object")));
  assert.ok(validation.errors.some((entry) => entry.includes("introduces a cycle")));
  assert.ok(validation.errors.some((entry) => entry.includes("unsupported transform")));
  assert.ok(validation.errors.some((entry) => entry.includes("unsupported condition output mode")));
  assert.ok(validation.errors.some((entry) => entry.includes("unsupported merge mode")));
  assert.ok(validation.errors.some((entry) => entry.includes("itemRules: must be an array")));
  assert.ok(validation.errors.some((entry) => entry.includes("then: must be an array")));
  assert.ok(validation.errors.some((entry) => entry.includes("unknown fragment")));
});

test("bundleConversionRules covers include errors and duplicate include traversal", () => {
  const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), "apiconvert-bundle-"));
  const entry = path.join(tempDir, "entry.rules.json");
  const shared = path.join(tempDir, "shared.rules.json");
  const nested = path.join(tempDir, "nested.rules.json");
  const invalidJson = path.join(tempDir, "invalid.rules.json");
  const nonObject = path.join(tempDir, "non-object.rules.json");

  fs.writeFileSync(
    entry,
    JSON.stringify({
      include: ["shared.rules.json", "nested.rules.json", "shared.rules.json"],
      rules: [{ kind: "field", outputPaths: ["entry"], source: { type: "constant", value: "entry" } }]
    })
  );
  fs.writeFileSync(
    shared,
    JSON.stringify({
      rules: [{ kind: "field", outputPaths: ["shared"], source: { type: "constant", value: "shared" } }]
    })
  );
  fs.writeFileSync(nested, JSON.stringify({ include: ["shared.rules.json"], rules: [] }));
  fs.writeFileSync(invalidJson, "{invalid");
  fs.writeFileSync(nonObject, JSON.stringify([1, 2, 3]));

  const bundled = bundleConversionRules(entry);
  const outputPaths = (bundled.rules ?? []).flatMap((rule) =>
    "outputPaths" in rule && Array.isArray(rule.outputPaths) ? rule.outputPaths : []
  );
  assert.equal(outputPaths.includes("shared"), true);
  assert.equal(outputPaths.includes("entry"), true);
  assert.equal(outputPaths.filter((entryPath) => entryPath === "shared").length, 1);

  fs.writeFileSync(entry, JSON.stringify({ include: "bad", rules: [] }));
  assert.throws(() => bundleConversionRules(entry), /include must be an array/);

  fs.writeFileSync(entry, JSON.stringify({ include: ["missing.rules.json"], rules: [] }));
  assert.throws(() => bundleConversionRules(entry), /file not found/);

  assert.throws(() => bundleConversionRules(invalidJson), /Invalid JSON/);
  assert.throws(() => bundleConversionRules(nonObject), /must contain a JSON object/);
});

test("streamConversion covers parse, path, and fail-fast branches", async () => {
  const rules = {
    rules: [{ kind: "field", outputPaths: ["name"], source: { type: "path", path: "name" } }]
  };

  const results: Array<{ errors: string[] }> = [];
  for await (const item of streamConversion(
    "{bad json",
    rules,
    { inputKind: StreamInputKind.JsonArray, errorMode: StreamErrorMode.ContinueWithReport }
  )) {
    results.push({ errors: item.errors });
  }
  assert.equal(results.length, 1);
  assert.ok(results[0].errors[0].includes("failed to parse JSON array stream"));

  const notArray: Array<{ errors: string[] }> = [];
  for await (const item of streamConversion(
    '{"name":"Ada"}',
    rules,
    { inputKind: StreamInputKind.JsonArray, errorMode: StreamErrorMode.ContinueWithReport }
  )) {
    notArray.push({ errors: item.errors });
  }
  assert.equal(notArray.length, 1);
  assert.ok(notArray[0].errors[0].includes("top-level JSON value must be an array"));

  const ndjson: Array<{ errors: string[] }> = [];
  for await (const item of streamConversion(
    ['{"name":"Ada"}\n', '{bad}\n'],
    rules,
    { inputKind: StreamInputKind.Ndjson, errorMode: StreamErrorMode.ContinueWithReport }
  )) {
    ndjson.push({ errors: item.errors });
  }
  assert.equal(ndjson.length, 2);
  assert.equal(ndjson[0].errors.length, 0);
  assert.ok(ndjson[1].errors[0].includes("failed to parse NDJSON line"));

  const queryLines: Array<{ errors: string[] }> = [];
  for await (const item of streamConversion(
    ["a=%E0%A4%A"],
    rules,
    { inputKind: StreamInputKind.QueryLines, errorMode: StreamErrorMode.ContinueWithReport }
  )) {
    queryLines.push({ errors: item.errors });
  }
  assert.equal(queryLines.length, 1);
  assert.ok(queryLines[0].errors.length > 0);
  assert.ok(queryLines[0].errors.join(" ").includes("failed to parse query record"));

  await assert.rejects(
    async () => {
      for await (const _ of streamConversion("<root><item/></root>", rules, {
        inputKind: StreamInputKind.XmlElements
      })) {
      }
    },
    /requires streamOptions\.xmlItemPath/
  );

  const xmlErrors: Array<{ errors: string[] }> = [];
  for await (const item of streamConversion(
    "<root><item></root>",
    rules,
    {
      inputKind: StreamInputKind.XmlElements,
      xmlItemPath: "root.item",
      errorMode: StreamErrorMode.ContinueWithReport
    }
  )) {
    xmlErrors.push({ errors: item.errors });
  }
  assert.equal(xmlErrors.length, 1);
  assert.ok(Array.isArray(xmlErrors[0].errors));

  await assert.rejects(
    async () => {
      for await (const _ of streamConversion(
        "<root><item><name>Ada</name></item></root>",
        rules,
        {
          inputKind: StreamInputKind.XmlElements,
          xmlItemPath: "   "
        }
      )) {
      }
    },
    /requires streamOptions\.xmlItemPath/
  );

  await assert.rejects(
    async () => {
      for await (const _ of streamConversion(
        [{ name: "Ada" }],
        {
          rules: [{ kind: "branch", expression: "path(name) is 'Ada'", then: [] }]
        },
        { inputKind: StreamInputKind.JsonArray, errorMode: StreamErrorMode.FailFast }
      )) {
      }
    },
    /conversion failed/
  );
});
