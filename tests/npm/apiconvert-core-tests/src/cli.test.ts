import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { execFileSync } from "node:child_process";
import test from "node:test";

const repoRoot = path.resolve(__dirname, "../../../..");
const cliPath = path.join(repoRoot, "src", "apiconvert-core", "bin", "apiconvert.js");

test("CLI validate/lint/convert smoke", () => {
  const caseDir = path.join(repoRoot, "tests", "cases", "basic-json");
  const rulesPath = path.join(caseDir, "rules.json");
  const inputPath = path.join(caseDir, "input.json");
  const expectedOutputPath = path.join(caseDir, "output.json");

  const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), "apiconvert-cli-"));
  const outputPath = path.join(tempDir, "out.json");
  const bundledPath = path.join(tempDir, "bundled.rules.json");
  const benchInputPath = path.join(tempDir, "bench.ndjson");
  fs.writeFileSync(benchInputPath, `${JSON.stringify({ name: "Ada" })}\n${JSON.stringify({ name: "Lin" })}\n`);

  execFileSync(process.execPath, [cliPath, "rules", "validate", rulesPath], { encoding: "utf8" });
  execFileSync(process.execPath, [cliPath, "rules", "lint", rulesPath], { encoding: "utf8" });
  execFileSync(
    process.execPath,
    [cliPath, "rules", "doctor", "--rules", rulesPath, "--input", inputPath, "--format", "json"],
    { encoding: "utf8" }
  );
  execFileSync(
    process.execPath,
    [cliPath, "rules", "compatibility", "--rules", rulesPath, "--target", "1.0.0"],
    { encoding: "utf8" }
  );
  execFileSync(
    process.execPath,
    [cliPath, "rules", "bundle", "--rules", rulesPath, "--out", bundledPath],
    { encoding: "utf8" }
  );
  execFileSync(
    process.execPath,
    [
      cliPath,
      "convert",
      "--rules",
      rulesPath,
      "--input",
      inputPath,
      "--output",
      outputPath
    ],
    { encoding: "utf8" }
  );
  const benchmarkOutput = execFileSync(
    process.execPath,
    [cliPath, "benchmark", "--rules", rulesPath, "--input", benchInputPath, "--iterations", "2"],
    { encoding: "utf8" }
  );

  const expected = JSON.stringify(JSON.parse(fs.readFileSync(expectedOutputPath, "utf8")));
  const actual = JSON.stringify(JSON.parse(fs.readFileSync(outputPath, "utf8")));
  const benchmark = JSON.parse(benchmarkOutput) as { totalRuns: number };
  assert.equal(actual, expected);
  assert.ok(fs.existsSync(bundledPath));
  assert.equal(benchmark.totalRuns, 4);
});
