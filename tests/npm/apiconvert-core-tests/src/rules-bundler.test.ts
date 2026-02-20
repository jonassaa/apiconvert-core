import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import test from "node:test";
import { bundleConversionRules } from "@apiconvert/core";

test("bundleConversionRules merges includes deterministically", () => {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), "apiconvert-bundle-"));
  const sharedPath = path.join(root, "shared.rules.json");
  const entryPath = path.join(root, "entry.rules.json");

  fs.writeFileSync(
    sharedPath,
    JSON.stringify({
      inputFormat: "json",
      outputFormat: "json",
      rules: [
        {
          kind: "field",
          outputPaths: ["customer.id"],
          source: { type: "path", path: "id" }
        }
      ]
    })
  );

  fs.writeFileSync(
    entryPath,
    JSON.stringify({
      include: ["./shared.rules.json"],
      inputFormat: "json",
      outputFormat: "json",
      rules: [
        {
          kind: "field",
          outputPaths: ["customer.name"],
          source: { type: "path", path: "name" }
        }
      ]
    })
  );

  const bundled = bundleConversionRules(entryPath);
  assert.equal(bundled.rules?.length, 2);
  assert.equal((bundled.rules?.[0] as { outputPaths: string[] }).outputPaths[0], "customer.id");
  assert.equal((bundled.rules?.[1] as { outputPaths: string[] }).outputPaths[0], "customer.name");
});

test("bundleConversionRules detects circular includes", () => {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), "apiconvert-bundle-cycle-"));
  const aPath = path.join(root, "a.rules.json");
  const bPath = path.join(root, "b.rules.json");

  fs.writeFileSync(aPath, JSON.stringify({ include: ["./b.rules.json"], rules: [] }));
  fs.writeFileSync(bPath, JSON.stringify({ include: ["./a.rules.json"], rules: [] }));

  assert.throws(() => bundleConversionRules(aPath), /Circular include detected/);
});
