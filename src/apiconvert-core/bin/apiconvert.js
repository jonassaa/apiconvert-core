#!/usr/bin/env node
"use strict";

const fs = require("node:fs");
const path = require("node:path");
const core = require("../dist/index.js");

async function main() {
  const args = process.argv.slice(2);
  if (args.length === 0) {
    printUsage();
    process.exitCode = 1;
    return;
  }

  const [command, subcommand, ...rest] = args;
  if (command === "rules" && subcommand === "validate") {
    await handleValidate(rest);
    return;
  }

  if (command === "rules" && subcommand === "lint") {
    await handleLint(rest);
    return;
  }

  if (command === "rules" && subcommand === "doctor") {
    await handleDoctor(rest);
    return;
  }

  if (command === "rules" && subcommand === "compatibility") {
    await handleCompatibility(rest);
    return;
  }

  if (command === "rules" && subcommand === "bundle") {
    await handleBundle(rest);
    return;
  }

  if (command === "convert") {
    await handleConvert([subcommand, ...rest].filter(Boolean));
    return;
  }

  if (command === "benchmark") {
    await handleBenchmark([subcommand, ...rest].filter(Boolean));
    return;
  }

  printUsage();
  process.exitCode = 1;
}

async function handleValidate(args) {
  const [rulesPath] = args;
  if (!rulesPath) {
    throw new Error("Missing rules file path. Usage: apiconvert rules validate <rules.json>");
  }

  const rawRules = fs.readFileSync(rulesPath, "utf8");
  const validation = core.validateConversionRules(rawRules);
  console.log(JSON.stringify({ isValid: validation.isValid, errors: validation.errors }, null, 2));
  if (!validation.isValid) {
    process.exitCode = 1;
  }
}

async function handleLint(args) {
  const [rulesPath] = args;
  if (!rulesPath) {
    throw new Error("Missing rules file path. Usage: apiconvert rules lint <rules.json>");
  }

  const rawRules = fs.readFileSync(rulesPath, "utf8");
  const lint = core.lintConversionRules(rawRules);
  console.log(JSON.stringify(lint, null, 2));
  if (lint.hasErrors) {
    process.exitCode = 1;
  }
}

async function handleConvert(args) {
  const options = parseFlags(args);
  const rulesPath = requireFlag(options, "rules");
  const inputPath = requireFlag(options, "input");
  const outputPath = requireFlag(options, "output");

  const rawRules = fs.readFileSync(rulesPath, "utf8");
  const rules = core.normalizeConversionRules(rawRules);
  const inputText = fs.readFileSync(inputPath, "utf8");

  const inputFormat = extensionToFormat(path.extname(inputPath), rules.inputFormat);
  const outputFormat = extensionToFormat(path.extname(outputPath), rules.outputFormat);

  const parsed = core.parsePayload(inputText, inputFormat);
  if (parsed.error) {
    throw new Error(`Failed to parse input: ${parsed.error}`);
  }

  const result = core.applyConversion(parsed.value, rules);
  if (result.errors.length > 0) {
    console.error(JSON.stringify({ errors: result.errors, warnings: result.warnings }, null, 2));
    process.exitCode = 1;
    return;
  }

  const outputText = core.formatPayload(result.output, outputFormat, outputFormat === core.DataFormat.Xml);
  fs.writeFileSync(outputPath, outputText);
  console.log(JSON.stringify({ outputPath, warnings: result.warnings }, null, 2));
}

async function handleDoctor(args) {
  const options = parseFlags(args);
  const rulesPath = requireFlag(options, "rules");
  const inputPath = options.input;
  const formatFlag = options.format;

  const rawRules = fs.readFileSync(rulesPath, "utf8");

  let sampleInputText = null;
  let inputFormat = null;
  if (inputPath) {
    sampleInputText = fs.readFileSync(inputPath, "utf8");
    inputFormat = extensionToFormat(path.extname(inputPath), null);
  }

  if (formatFlag) {
    inputFormat = extensionToFormat(`.${formatFlag}`, null);
    if (!inputFormat) {
      throw new Error("Unsupported --format value. Use json, xml, or query.");
    }
  }

  const report = core.runRuleDoctor(rawRules, {
    sampleInputText,
    inputFormat,
    applySafeFixes: false
  });

  console.log(JSON.stringify(report, null, 2));
  if (report.hasErrors) {
    process.exitCode = 1;
  }
}

async function handleCompatibility(args) {
  const options = parseFlags(args);
  const rulesPath = requireFlag(options, "rules");
  const targetVersion = requireFlag(options, "target");

  const rawRules = fs.readFileSync(rulesPath, "utf8");
  const report = core.checkRulesCompatibility(rawRules, { targetVersion });
  console.log(JSON.stringify(report, null, 2));
  if (!report.isCompatible) {
    process.exitCode = 1;
  }
}

async function handleBundle(args) {
  const options = parseFlags(args);
  const rulesPath = requireFlag(options, "rules");
  const outputPath = requireFlag(options, "out");

  const bundled = core.bundleConversionRules(rulesPath);
  fs.writeFileSync(outputPath, JSON.stringify(bundled, null, 2));
  console.log(JSON.stringify({ outputPath, validationErrors: bundled.validationErrors ?? [] }, null, 2));
}

async function handleBenchmark(args) {
  const options = parseFlags(args);
  const rulesPath = requireFlag(options, "rules");
  const inputPath = requireFlag(options, "input");
  const iterations = options.iterations ? Number(options.iterations) : 100;
  if (!Number.isFinite(iterations) || iterations <= 0) {
    throw new Error("--iterations must be a positive number.");
  }

  const rawRules = fs.readFileSync(rulesPath, "utf8");
  const inputItems = parseNdjson(fs.readFileSync(inputPath, "utf8"));
  const report = core.profileConversionPlan(rawRules, inputItems, { iterations });
  console.log(JSON.stringify(report, null, 2));
}

function parseFlags(args) {
  const options = {};
  for (let i = 0; i < args.length; i += 1) {
    const token = args[i];
    if (!token.startsWith("--")) {
      continue;
    }
    options[token.slice(2)] = args[i + 1];
    i += 1;
  }
  return options;
}

function requireFlag(options, name) {
  const value = options[name];
  if (!value) {
    throw new Error(`Missing --${name} option.`);
  }
  return value;
}

function extensionToFormat(extension, fallbackFormat) {
  const normalized = extension.toLowerCase();
  if (normalized === ".json") return core.DataFormat.Json;
  if (normalized === ".xml") return core.DataFormat.Xml;
  if (normalized === ".txt") return core.DataFormat.Query;
  return fallbackFormat ?? core.DataFormat.Json;
}

function printUsage() {
  console.error([
    "Usage:",
    "  apiconvert rules validate <rules.json>",
    "  apiconvert rules lint <rules.json>",
    "  apiconvert rules doctor --rules <rules.json> [--input <sample.ext>] [--format json|xml|query]",
    "  apiconvert rules compatibility --rules <rules.json> --target <version>",
    "  apiconvert rules bundle --rules <entry.rules.json> --out <bundled.rules.json>",
    "  apiconvert convert --rules <rules.json> --input <input.ext> --output <output.ext>",
    "  apiconvert benchmark --rules <rules.json> --input <samples.ndjson> [--iterations <n>]"
  ].join("\n"));
}

function parseNdjson(text) {
  const lines = text
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter((line) => line.length > 0);

  return lines.map((line, index) => {
    try {
      return JSON.parse(line);
    } catch (error) {
      throw new Error(`Invalid NDJSON at line ${index + 1}: ${error instanceof Error ? error.message : String(error)}`);
    }
  });
}

main().catch((error) => {
  console.error(error instanceof Error ? error.message : String(error));
  process.exitCode = 1;
});
