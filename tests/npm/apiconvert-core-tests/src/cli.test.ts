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

  execFileSync(process.execPath, [cliPath, "rules", "validate", rulesPath], { encoding: "utf8" });
  execFileSync(process.execPath, [cliPath, "rules", "lint", rulesPath], { encoding: "utf8" });
  execFileSync(
    process.execPath,
    [cliPath, "rules", "doctor", "--rules", rulesPath, "--input", inputPath, "--format", "json"],
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

  const expected = JSON.stringify(JSON.parse(fs.readFileSync(expectedOutputPath, "utf8")));
  const actual = JSON.stringify(JSON.parse(fs.readFileSync(outputPath, "utf8")));
  assert.equal(actual, expected);
});
