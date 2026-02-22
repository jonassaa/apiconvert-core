import assert from "node:assert/strict";
import test from "node:test";
import {
  StreamErrorMode,
  StreamInputKind,
  streamConversion,
  streamJsonArrayConversion
} from "@apiconvert/core";

test("streamJsonArrayConversion converts async iterable input item-by-item", async () => {
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

  async function* items() {
    yield { name: "Ada" };
    yield { name: "Bob" };
  }

  const outputs: unknown[] = [];
  for await (const result of streamJsonArrayConversion(items(), rules)) {
    assert.deepEqual(result.errors, []);
    outputs.push(result.output);
  }

  assert.deepEqual(outputs, [{ user: { name: "Ada" } }, { user: { name: "Bob" } }]);
});

test("streamConversion converts NDJSON input", async () => {
  const rules = {
    inputFormat: "json",
    outputFormat: "json",
    rules: [
      {
        kind: "field",
        outputPaths: ["user.id"],
        source: { type: "path", path: "id" }
      }
    ]
  };

  const outputs: unknown[] = [];
  for await (const result of streamConversion(
    '{"id":"a1"}\n{"id":"b2"}\n',
    rules,
    {
      inputKind: StreamInputKind.Ndjson,
      errorMode: StreamErrorMode.ContinueWithReport
    }
  )) {
    assert.deepEqual(result.errors, []);
    outputs.push(result.output);
  }

  assert.deepEqual(outputs, [{ user: { id: "a1" } }, { user: { id: "b2" } }]);
});

test("streamConversion converts query-line input", async () => {
  const rules = {
    inputFormat: "query",
    outputFormat: "json",
    rules: [
      {
        kind: "field",
        outputPaths: ["user.name"],
        source: { type: "path", path: "name" }
      }
    ]
  };

  const outputs: unknown[] = [];
  for await (const result of streamConversion(
    "name=Ada\nname=Bob\n",
    rules,
    {
      inputKind: StreamInputKind.QueryLines,
      errorMode: StreamErrorMode.ContinueWithReport
    }
  )) {
    assert.deepEqual(result.errors, []);
    outputs.push(result.output);
  }

  assert.deepEqual(outputs, [{ user: { name: "Ada" } }, { user: { name: "Bob" } }]);
});

test("streamConversion converts XML element items from configured path", async () => {
  const rules = {
    inputFormat: "xml",
    outputFormat: "json",
    rules: [
      {
        kind: "field",
        outputPaths: ["user.name"],
        source: { type: "path", path: "name" }
      }
    ]
  };

  const xml = `
    <customers>
      <customer><name>Ada</name></customer>
      <customer><name>Bob</name></customer>
    </customers>
  `;

  const outputs: unknown[] = [];
  for await (const result of streamConversion(
    xml,
    rules,
    {
      inputKind: StreamInputKind.XmlElements,
      xmlItemPath: "customers.customer",
      errorMode: StreamErrorMode.ContinueWithReport
    }
  )) {
    assert.deepEqual(result.errors, []);
    outputs.push(result.output);
  }

  assert.deepEqual(outputs, [{ user: { name: "Ada" } }, { user: { name: "Bob" } }]);
});

test("streamConversion failFast throws on malformed item", async () => {
  const rules = {
    inputFormat: "json",
    outputFormat: "json",
    rules: [
      {
        kind: "field",
        outputPaths: ["user.id"],
        source: { type: "path", path: "id" }
      }
    ]
  };

  await assert.rejects(async () => {
    for await (const _ of streamConversion(
      '{"id":"ok"}\nnot-json\n',
      rules,
      {
        inputKind: StreamInputKind.Ndjson,
        errorMode: StreamErrorMode.FailFast
      }
    )) {
      // no-op
    }
  }, /failed to parse NDJSON line/);
});

test("streamConversion continueWithReport yields item-level errors", async () => {
  const rules = {
    inputFormat: "json",
    outputFormat: "json",
    rules: [
      {
        kind: "field",
        outputPaths: ["user.id"],
        source: { type: "path", path: "id" }
      }
    ]
  };

  const results: Array<{ errors: string[] }> = [];
  for await (const result of streamConversion(
    '{"id":"ok"}\nnot-json\n{"id":"next"}\n',
    rules,
    {
      inputKind: StreamInputKind.Ndjson,
      errorMode: StreamErrorMode.ContinueWithReport
    }
  )) {
    results.push({ errors: result.errors });
  }

  assert.equal(results.length, 3);
  assert.deepEqual(results[0].errors, []);
  assert.equal(results[1].errors.length, 1);
  assert.match(results[1].errors[0], /stream\[1\]/);
  assert.deepEqual(results[2].errors, []);
});
