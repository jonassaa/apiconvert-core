#!/usr/bin/env node
import { execFileSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const repoRoot = path.resolve(__dirname, "../..");
const casesRoot = path.resolve(repoRoot, "tests/cases");
const reportPath = resolveReportPath(process.argv.slice(2));

const dotnetResults = runDotnetCaseRunner();
const npmResults = await runNpmCaseRunner();
const report = buildParityReport(dotnetResults, npmResults);

fs.mkdirSync(path.dirname(reportPath), { recursive: true });
fs.writeFileSync(reportPath, JSON.stringify(report, null, 2));

console.log(`Parity report written to ${path.relative(repoRoot, reportPath)}`);
console.log(`Cases: ${report.totalCases}, mismatches: ${report.mismatches.length}`);
if (report.mismatches.length > 0) {
  process.exitCode = 1;
}

function resolveReportPath(args) {
  const reportFlag = args.indexOf("--report");
  if (reportFlag >= 0 && args[reportFlag + 1]) {
    return path.resolve(process.cwd(), args[reportFlag + 1]);
  }
  return path.resolve(repoRoot, "tests/parity/parity-report.json");
}

function runDotnetCaseRunner() {
  const projectPath = path.resolve(repoRoot, "tests/nuget/Apiconvert.Core.CaseRunner/Apiconvert.Core.CaseRunner.csproj");
  const output = execFileSync(
    "dotnet",
    ["run", "--project", projectPath, "--", casesRoot],
    { cwd: repoRoot, encoding: "utf8", maxBuffer: 20 * 1024 * 1024 }
  );

  const entries = JSON.parse(output);
  const normalizedEntries = entries.map((entry) => ({
    caseName: entry.caseName ?? entry.CaseName,
    outputText: entry.outputText ?? entry.OutputText ?? "",
    errors: entry.errors ?? entry.Errors ?? [],
    warnings: entry.warnings ?? entry.Warnings ?? []
  }));
  return new Map(normalizedEntries.map((entry) => [entry.caseName, entry]));
}

async function runNpmCaseRunner() {
  const distEntry = path.resolve(repoRoot, "src/apiconvert-core/dist/index.js");
  if (!fs.existsSync(distEntry)) {
    throw new Error("Missing src/apiconvert-core/dist/index.js. Run npm build for @apiconvert/core first.");
  }

  const api = await import(pathToFileURL(distEntry).href);
  const caseDirectories = fs
    .readdirSync(casesRoot, { withFileTypes: true })
    .filter((entry) => entry.isDirectory())
    .map((entry) => path.join(casesRoot, entry.name))
    .sort((a, b) => a.localeCompare(b));

  const results = new Map();
  for (const caseDirectory of caseDirectories) {
    const caseName = path.basename(caseDirectory);
    try {
      const rulesPath = path.join(caseDirectory, "rules.json");
      const inputPath = findSingleFile(caseDirectory, "input", caseName);
      const outputPath = findSingleFile(caseDirectory, "output", caseName);

      const rulesText = fs.readFileSync(rulesPath, "utf8");
      const inputText = fs.readFileSync(inputPath, "utf8");

      const inputFormat = extensionToFormat(path.extname(inputPath).slice(1), api);
      const outputFormat = extensionToFormat(path.extname(outputPath).slice(1), api);

      const rules = api.normalizeConversionRules(rulesText);
      const inputValue = inputFormat ? parsePayloadOrThrow(inputText, inputFormat, api) : inputText;
      const conversion = api.applyConversion(inputValue, rules);

      const outputText = outputFormat
        ? api.formatPayload(conversion.output, outputFormat, outputFormat === api.DataFormat.Xml)
        : conversion.output == null
          ? ""
          : String(conversion.output);

      results.set(caseName, {
        caseName,
        outputText: normalizeOutput(outputText, path.extname(outputPath).slice(1)),
        errors: conversion.errors,
        warnings: conversion.warnings
      });
    } catch (error) {
      results.set(caseName, {
        caseName,
        outputText: "",
        errors: [`runner failure: ${error instanceof Error ? error.message : String(error)}`],
        warnings: []
      });
    }
  }

  return results;
}

function parsePayloadOrThrow(text, format, api) {
  const parsed = api.parsePayload(text, format);
  if (parsed.error) {
    throw new Error(parsed.error);
  }
  return parsed.value;
}

function extensionToFormat(ext, api) {
  const normalized = ext.toLowerCase();
  if (normalized === "json") return api.DataFormat.Json;
  if (normalized === "xml") return api.DataFormat.Xml;
  if (normalized === "txt") return api.DataFormat.Query;
  return null;
}

function findSingleFile(directory, basename, caseName) {
  const matches = fs
    .readdirSync(directory)
    .filter((entry) => entry.startsWith(`${basename}.`))
    .map((entry) => path.join(directory, entry));

  if (matches.length !== 1) {
    throw new Error(`Case '${caseName}' must contain exactly one ${basename}.* file.`);
  }

  return matches[0];
}

function normalizeOutput(text, extension) {
  const normalized = text.replace(/\r\n/g, "\n").trim();
  if (extension === "json") {
    return stableStringify(JSON.parse(normalized || "{}"));
  }
  return normalized;
}

function stableStringify(value) {
  if (Array.isArray(value)) {
    return `[${value.map((item) => stableStringify(item)).join(",")}]`;
  }

  if (value && typeof value === "object") {
    const entries = Object.keys(value)
      .sort()
      .map((key) => `"${key}":${stableStringify(value[key])}`);
    return `{${entries.join(",")}}`;
  }

  return JSON.stringify(value);
}

function buildParityReport(dotnetResults, npmResults) {
  const caseNames = [...new Set([...dotnetResults.keys(), ...npmResults.keys()])].sort((a, b) => a.localeCompare(b));
  const mismatches = [];

  for (const caseName of caseNames) {
    const dotnet = dotnetResults.get(caseName) ?? { caseName, outputText: "", errors: ["missing .NET result"], warnings: [] };
    const npm = npmResults.get(caseName) ?? { caseName, outputText: "", errors: ["missing npm result"], warnings: [] };

    const diffFields = [];
    if (canonicalizeOutput(dotnet.outputText) !== canonicalizeOutput(npm.outputText)) diffFields.push("outputText");
    if (stableStringify(dotnet.errors ?? []) !== stableStringify(npm.errors ?? [])) diffFields.push("errors");
    if (stableStringify(dotnet.warnings ?? []) !== stableStringify(npm.warnings ?? [])) diffFields.push("warnings");

    if (diffFields.length > 0) {
      mismatches.push({ caseName, diffFields, dotnet, npm });
    }
  }

  return {
    generatedAtUtc: new Date().toISOString(),
    totalCases: caseNames.length,
    matchingCases: caseNames.length - mismatches.length,
    mismatches
  };
}

function canonicalizeOutput(outputText) {
  const text = (outputText ?? "").trim();
  if (!text) {
    return "";
  }

  try {
    return stableStringify(JSON.parse(text));
  } catch {
    return text;
  }
}
