import fs from "node:fs";
import path from "node:path";
import { normalizeConversionRules } from "./rules-normalizer";
import type { ConversionRules, RuleBundleOptions } from "./types";

type RawRulesFile = {
  include?: unknown;
  inputFormat?: unknown;
  outputFormat?: unknown;
  rules?: unknown;
};

export function bundleConversionRules(entryRulesPath: string, options: RuleBundleOptions = {}): ConversionRules {
  const baseDirectory = path.resolve(options.baseDirectory ?? path.dirname(entryRulesPath));
  const entryPath = path.resolve(baseDirectory, entryRulesPath);
  const visited = new Set<string>();

  return bundleFile(entryPath, visited, []);
}

function bundleFile(filePath: string, visited: Set<string>, chain: string[]): ConversionRules {
  const normalizedFilePath = path.resolve(filePath);
  if (chain.includes(normalizedFilePath)) {
    const circular = [...chain, normalizedFilePath].map((entry) => path.basename(entry)).join(" -> ");
    throw new Error(`Circular include detected: ${circular}`);
  }

  if (visited.has(normalizedFilePath)) {
    return normalizeConversionRules({ inputFormat: "json", outputFormat: "json", rules: [] });
  }

  visited.add(normalizedFilePath);
  const raw = loadRawRulesFile(normalizedFilePath);

  const includes = normalizeIncludes(raw.include);
  const currentChain = [...chain, normalizedFilePath];
  const includedRules: ConversionRules[] = includes.map((includePath) => {
    const resolvedIncludePath = path.resolve(path.dirname(normalizedFilePath), includePath);
    return bundleFile(resolvedIncludePath, visited, currentChain);
  });

  const localRules = normalizeConversionRules({
    inputFormat: raw.inputFormat,
    outputFormat: raw.outputFormat,
    rules: raw.rules
  });

  const mergedRules = includedRules.flatMap((entry) => entry.rules ?? []).concat(localRules.rules ?? []);
  const inheritedFormat = includedRules.find((entry) => entry.inputFormat != null)?.inputFormat;
  const inheritedOutputFormat = includedRules.find((entry) => entry.outputFormat != null)?.outputFormat;

  return {
    inputFormat: localRules.inputFormat ?? inheritedFormat,
    outputFormat: localRules.outputFormat ?? inheritedOutputFormat,
    rules: mergedRules,
    validationErrors: [
      ...(includedRules.flatMap((entry) => entry.validationErrors ?? [])),
      ...(localRules.validationErrors ?? [])
    ]
  };
}

function loadRawRulesFile(filePath: string): RawRulesFile {
  if (!fs.existsSync(filePath)) {
    throw new Error(`Included rules file not found: ${filePath}`);
  }

  const text = fs.readFileSync(filePath, "utf8");
  let parsed: unknown;
  try {
    parsed = JSON.parse(text);
  } catch (error) {
    throw new Error(
      `Invalid JSON in rules file ${filePath}: ${error instanceof Error ? error.message : String(error)}`
    );
  }

  if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) {
    throw new Error(`Rules file must contain a JSON object: ${filePath}`);
  }

  return parsed as RawRulesFile;
}

function normalizeIncludes(includeValue: unknown): string[] {
  if (includeValue == null) {
    return [];
  }

  if (!Array.isArray(includeValue)) {
    throw new Error("include must be an array of relative file paths.");
  }

  return includeValue
    .filter((entry): entry is string => typeof entry === "string")
    .map((entry) => entry.trim())
    .filter((entry) => entry.length > 0)
    .sort((a, b) => a.localeCompare(b));
}
