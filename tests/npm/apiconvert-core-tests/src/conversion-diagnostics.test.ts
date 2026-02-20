import assert from "node:assert/strict";
import test from "node:test";
import { applyConversion, OutputCollisionPolicy } from "@apiconvert/core";

test("runtime diagnostics include warning for missing array source path", () => {
  const result = applyConversion(
    { name: "Ada" },
    {
      rules: [
        {
          kind: "array",
          inputPath: "items",
          outputPaths: ["items"],
          itemRules: []
        }
      ]
    }
  );

  const diagnostic = result.diagnostics.find((entry) => entry.code === "ACV-RUN-101");
  assert.ok(diagnostic);
  assert.equal(diagnostic?.rulePath, "rules[0]");
  assert.equal(result.warnings.includes(diagnostic?.message ?? ""), true);
});

test("runtime diagnostics include branch expression failure", () => {
  const result = applyConversion(
    { name: "Ada" },
    {
      rules: [
        {
          kind: "branch",
          expression: "path(name) is 'Ada'",
          then: []
        }
      ]
    }
  );

  const diagnostic = result.diagnostics.find((entry) => entry.code === "ACV-RUN-302");
  assert.ok(diagnostic);
  assert.equal(diagnostic?.rulePath, "rules[0]");
  assert.equal(result.errors.includes(diagnostic?.message ?? ""), true);
});

test("runtime diagnostics include output collision and transform failures", () => {
  const collision = applyConversion(
    { name: "Ada" },
    {
      rules: [
        { kind: "field", outputPaths: ["user.name"], source: { type: "path", path: "name" } },
        { kind: "field", outputPaths: ["user.name"], source: { type: "constant", value: "override" } }
      ]
    },
    { collisionPolicy: OutputCollisionPolicy.Error }
  );

  const collisionDiagnostic = collision.diagnostics.find((entry) => entry.code === "ACV-RUN-103");
  assert.ok(collisionDiagnostic);
  assert.equal(collision.errors.includes(collisionDiagnostic?.message ?? ""), true);

  const transform = applyConversion(
    { name: "Ada" },
    {
      rules: [
        {
          kind: "field",
          outputPaths: ["user.custom"],
          source: { type: "transform", path: "name", customTransform: "reverse" }
        }
      ]
    }
  );

  const transformDiagnostic = transform.diagnostics.find((entry) => entry.code === "ACV-RUN-201");
  assert.ok(transformDiagnostic);
  assert.equal(transform.errors.includes(transformDiagnostic?.message ?? ""), true);
});
