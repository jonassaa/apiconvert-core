#!/usr/bin/env node
import { execFileSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const repoRoot = path.resolve(__dirname, "../..");

const args = parseArgs(process.argv.slice(2));
const reportPath = path.resolve(repoRoot, args.report);
const summaryPath = path.resolve(repoRoot, args.summary);

if (args.dryRun) {
  console.log(JSON.stringify({ mode: "dry-run", reportPath, summaryPath, maxMismatches: args.maxMismatches }, null, 2));
  process.exit(0);
}

execFileSync(process.execPath, [path.join(__dirname, "parity-report.mjs"), "--report", reportPath], {
  cwd: repoRoot,
  stdio: "inherit"
});

const report = JSON.parse(fs.readFileSync(reportPath, "utf8"));
validateReportSchema(report);

const mismatchCount = report.mismatches.length;
const summary = {
  generatedAtUtc: new Date().toISOString(),
  reportPath: path.relative(repoRoot, reportPath),
  totalCases: report.totalCases,
  matchingCases: report.matchingCases,
  mismatchCount,
  maxMismatches: args.maxMismatches,
  passed: mismatchCount <= args.maxMismatches
};

fs.mkdirSync(path.dirname(summaryPath), { recursive: true });
fs.writeFileSync(summaryPath, JSON.stringify(summary, null, 2));
console.log(`Parity summary written to ${path.relative(repoRoot, summaryPath)}`);

if (!summary.passed) {
  process.exitCode = 1;
}

function parseArgs(tokens) {
  const options = {
    report: "tests/parity/parity-report.json",
    summary: "tests/parity/parity-summary.json",
    maxMismatches: 0,
    dryRun: false
  };

  for (let i = 0; i < tokens.length; i += 1) {
    const token = tokens[i];
    if (token === "--dry-run") {
      options.dryRun = true;
      continue;
    }

    if (token === "--report") {
      options.report = requireValue(tokens, i, "--report");
      i += 1;
      continue;
    }

    if (token === "--summary") {
      options.summary = requireValue(tokens, i, "--summary");
      i += 1;
      continue;
    }

    if (token === "--max-mismatches") {
      const raw = requireValue(tokens, i, "--max-mismatches");
      const value = Number(raw);
      if (!Number.isFinite(value) || value < 0) {
        throw new Error("--max-mismatches must be a non-negative number.");
      }
      options.maxMismatches = value;
      i += 1;
      continue;
    }

    throw new Error(`Unsupported argument: ${token}`);
  }

  return options;
}

function requireValue(tokens, index, flagName) {
  const value = tokens[index + 1];
  if (!value) {
    throw new Error(`Missing value for ${flagName}.`);
  }
  return value;
}

function validateReportSchema(report) {
  if (!report || typeof report !== "object") {
    throw new Error("Parity report must be an object.");
  }

  if (typeof report.generatedAtUtc !== "string") {
    throw new Error("Parity report is missing generatedAtUtc.");
  }

  if (typeof report.totalCases !== "number" || typeof report.matchingCases !== "number") {
    throw new Error("Parity report is missing totalCases/matchingCases numeric fields.");
  }

  if (!Array.isArray(report.mismatches)) {
    throw new Error("Parity report is missing mismatches array.");
  }

  for (const mismatch of report.mismatches) {
    if (!mismatch || typeof mismatch !== "object") {
      throw new Error("Mismatch entry must be an object.");
    }

    if (typeof mismatch.caseName !== "string" || !Array.isArray(mismatch.diffFields)) {
      throw new Error("Mismatch entry must contain caseName and diffFields.");
    }
  }
}
