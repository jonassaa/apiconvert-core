import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { execFileSync, spawnSync } from "node:child_process";
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
  const formattedPath = path.join(tempDir, "formatted.rules.json");
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
    [cliPath, "rules", "format", "--rules", rulesPath, "--out", formattedPath],
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
  const formatted = JSON.parse(fs.readFileSync(formattedPath, "utf8")) as {
    inputFormat?: string;
    outputFormat?: string;
    rules: unknown[];
  };
  const benchmark = JSON.parse(benchmarkOutput) as { totalRuns: number };
  assert.equal(actual, expected);
  assert.equal(formatted.inputFormat, undefined);
  assert.equal(formatted.outputFormat, undefined);
  assert.ok(Array.isArray(formatted.rules));
  assert.ok(fs.existsSync(bundledPath));
  assert.equal(benchmark.totalRuns, 4);
});

test("CLI prints usage and non-zero exit for unknown or missing command", () => {
  const noArgs = spawnSync(process.execPath, [cliPath], { encoding: "utf8" });
  assert.notEqual(noArgs.status, 0);
  assert.match(noArgs.stderr, /Usage:/);

  const unknown = spawnSync(process.execPath, [cliPath, "unknown"], { encoding: "utf8" });
  assert.notEqual(unknown.status, 0);
  assert.match(unknown.stderr, /Usage:/);
});

test("CLI surfaces option validation and runtime parsing failures", () => {
  const caseDir = path.join(repoRoot, "tests", "cases", "basic-json");
  const rulesPath = path.join(caseDir, "rules.json");
  const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), "apiconvert-cli-errors-"));
  const invalidJsonPath = path.join(tempDir, "invalid.json");
  const invalidNdjsonPath = path.join(tempDir, "invalid.ndjson");
  const queryInputPath = path.join(tempDir, "query.txt");
  const outPath = path.join(tempDir, "out.json");

  fs.writeFileSync(invalidJsonPath, "{ invalid");
  fs.writeFileSync(invalidNdjsonPath, '{"ok":1}\n{bad}\n');
  fs.writeFileSync(queryInputPath, "a=1&b=2");

  const missingRules = spawnSync(process.execPath, [cliPath, "rules", "lint"], { encoding: "utf8" });
  assert.notEqual(missingRules.status, 0);
  assert.match(missingRules.stderr, /Missing rules file path/);

  const doctorWithUnknownFormat = spawnSync(
    process.execPath,
    [cliPath, "rules", "doctor", "--rules", rulesPath, "--input", queryInputPath, "--format", "yaml"],
    { encoding: "utf8" }
  );
  assert.notEqual(doctorWithUnknownFormat.status, 0);
  assert.match(`${doctorWithUnknownFormat.stderr}${doctorWithUnknownFormat.stdout}`, /Failed to parse sample input/);

  const badIterations = spawnSync(
    process.execPath,
    [cliPath, "benchmark", "--rules", rulesPath, "--input", invalidNdjsonPath, "--iterations", "0"],
    { encoding: "utf8" }
  );
  assert.notEqual(badIterations.status, 0);
  assert.match(badIterations.stderr, /--iterations must be a positive number/);

  const badNdjson = spawnSync(
    process.execPath,
    [cliPath, "benchmark", "--rules", rulesPath, "--input", invalidNdjsonPath],
    { encoding: "utf8" }
  );
  assert.notEqual(badNdjson.status, 0);
  assert.match(badNdjson.stderr, /Invalid NDJSON at line 2/);

  const badInput = spawnSync(
    process.execPath,
    [cliPath, "convert", "--rules", rulesPath, "--input", invalidJsonPath, "--output", outPath],
    { encoding: "utf8" }
  );
  assert.notEqual(badInput.status, 0);
  assert.match(badInput.stderr, /Failed to parse input/);
});
