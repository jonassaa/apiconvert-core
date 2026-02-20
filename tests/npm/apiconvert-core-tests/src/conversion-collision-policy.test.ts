import assert from "node:assert/strict";
import test from "node:test";
import {
  applyConversion,
  normalizeConversionRules,
  OutputCollisionPolicy
} from "@apiconvert/core";

test("default collision policy keeps last write", () => {
  const result = convertWithCollisionRules();

  assert.equal(result.errors.length, 0);
  assert.deepEqual(result.output, { name: "third" });
});

test("firstWriteWins keeps the first write", () => {
  const result = convertWithCollisionRules({ collisionPolicy: OutputCollisionPolicy.FirstWriteWins });

  assert.equal(result.errors.length, 0);
  assert.deepEqual(result.output, { name: "first" });
});

test("error policy reports each collision and keeps first write", () => {
  const result = convertWithCollisionRules({ collisionPolicy: OutputCollisionPolicy.Error });

  assert.equal(result.errors.length, 2);
  assert.match(result.errors[0], /rules\[1\]/);
  assert.match(result.errors[1], /rules\[2\]/);
  assert.equal(result.errors.every((error) => /already written by rules\[0\]/.test(error)), true);
  assert.deepEqual(result.output, { name: "first" });
});

function convertWithCollisionRules(options?: { collisionPolicy?: OutputCollisionPolicy }) {
  const rules = normalizeConversionRules({
    inputFormat: "json",
    outputFormat: "json",
    rules: [
      {
        kind: "field",
        outputPaths: ["name"],
        source: { type: "constant", value: "first" }
      },
      {
        kind: "field",
        outputPaths: ["name"],
        source: { type: "constant", value: "second" }
      },
      {
        kind: "field",
        outputPaths: ["name"],
        source: { type: "constant", value: "third" }
      }
    ]
  });

  return applyConversion({}, rules, options);
}
