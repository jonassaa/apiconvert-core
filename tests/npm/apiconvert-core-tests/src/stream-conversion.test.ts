import assert from "node:assert/strict";
import test from "node:test";
import { streamJsonArrayConversion } from "@apiconvert/core";

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
