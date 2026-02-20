import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import test from "node:test";

const repoRoot = path.resolve(__dirname, "../../../..");
const parityGatePath = path.join(repoRoot, "tests", "parity", "parity-gate.mjs");
const parityReportPath = path.join(repoRoot, "tests", "parity", "parity-report.json");

test("parity gate entrypoint supports dry-run", () => {
  const summaryPath = path.join(os.tmpdir(), `parity-summary-${Date.now()}.json`);
  const output = execFileSync(
    process.execPath,
    [parityGatePath, "--dry-run", "--summary", summaryPath],
    { encoding: "utf8" }
  );

  const parsed = JSON.parse(output) as { mode: string; summaryPath: string };
  assert.equal(parsed.mode, "dry-run");
  assert.equal(path.resolve(parsed.summaryPath), path.resolve(summaryPath));
});

test("parity report schema contains stable required fields", () => {
  const report = JSON.parse(fs.readFileSync(parityReportPath, "utf8")) as {
    generatedAtUtc?: string;
    totalCases?: number;
    matchingCases?: number;
    mismatches?: Array<{ caseName?: string; diffFields?: string[] }>;
  };

  assert.equal(typeof report.generatedAtUtc, "string");
  assert.equal(typeof report.totalCases, "number");
  assert.equal(typeof report.matchingCases, "number");
  assert.ok(Array.isArray(report.mismatches));

  for (const mismatch of report.mismatches ?? []) {
    assert.equal(typeof mismatch.caseName, "string");
    assert.ok(Array.isArray(mismatch.diffFields));
  }
});
