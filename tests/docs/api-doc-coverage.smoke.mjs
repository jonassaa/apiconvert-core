import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { join } from "node:path";

const root = process.cwd();
const runtimeApiDocPath = join(root, "docs", "guides", "runtime-api.md");

const DOC_DOTNET_START = "<!-- API-INVENTORY-DOTNET-START -->";
const DOC_DOTNET_END = "<!-- API-INVENTORY-DOTNET-END -->";
const DOC_TYPESCRIPT_START = "<!-- API-INVENTORY-TYPESCRIPT-START -->";
const DOC_TYPESCRIPT_END = "<!-- API-INVENTORY-TYPESCRIPT-END -->";

const DOTNET_CONTRACT_TYPES = new Set([
  "ConversionRulesGenerationRequest",
  "IConversionRulesGenerator"
]);

const DOTNET_MODEL_TYPES = new Set([
  "ConversionResult",
  "ConversionDiagnostic",
  "ConversionTraceEntry",
  "RuleLintSeverity",
  "RuleLintDiagnostic",
  "RuleDoctorFindingSeverity",
  "RuleDoctorFinding",
  "RuleDoctorReport",
  "RulesCompatibilityDiagnostic",
  "RulesCompatibilityReport",
  "RuleBundleOptions",
  "ConversionProfileOptions",
  "ConversionLatencyProfile",
  "ConversionProfileReport"
]);

function normalizeLineSet(lines) {
  return new Set(
    lines
      .map((line) => line.trim())
      .filter((line) => line.length > 0)
      .sort((a, b) => a.localeCompare(b))
  );
}

function splitTopLevelCommas(text) {
  const parts = [];
  let depthAngle = 0;
  let depthParen = 0;
  let current = "";

  for (const ch of text) {
    if (ch === "<") depthAngle += 1;
    if (ch === ">") depthAngle = Math.max(0, depthAngle - 1);
    if (ch === "(") depthParen += 1;
    if (ch === ")") depthParen = Math.max(0, depthParen - 1);

    if (ch === "," && depthAngle === 0 && depthParen === 0) {
      parts.push(current);
      current = "";
      continue;
    }

    current += ch;
  }

  if (current.trim().length > 0) {
    parts.push(current);
  }

  return parts;
}

function normalizeParameterType(rawParameter) {
  let value = rawParameter.trim();
  if (value.length === 0) {
    return null;
  }

  while (value.startsWith("[")) {
    const closeIndex = value.indexOf("]");
    if (closeIndex === -1) {
      break;
    }
    value = value.slice(closeIndex + 1).trim();
  }

  const equalsIndex = value.indexOf("=");
  if (equalsIndex !== -1) {
    value = value.slice(0, equalsIndex).trim();
  }

  const tokens = value.split(/\s+/).filter(Boolean);
  if (tokens.length <= 1) {
    return tokens[0] ?? null;
  }

  const withoutName = tokens.slice(0, -1).join(" ");
  return withoutName.replace(/\s+/g, " ").trim();
}

function extractParameterTypes(paramText) {
  const trimmed = paramText.trim();
  if (trimmed.length === 0) {
    return [];
  }

  return splitTopLevelCommas(trimmed)
    .map((part) => normalizeParameterType(part))
    .filter((entry) => entry !== null);
}

function readMethodParameterText(text, openParenIndex) {
  let depth = 0;
  let cursor = openParenIndex;
  let collected = "";

  while (cursor < text.length) {
    const ch = text[cursor];
    if (ch === "(") {
      depth += 1;
      if (depth > 1) {
        collected += ch;
      }
    } else if (ch === ")") {
      depth -= 1;
      if (depth === 0) {
        return collected;
      }
      collected += ch;
    } else {
      collected += ch;
    }
    cursor += 1;
  }

  throw new Error("Failed to find closing parenthesis while parsing method signature.");
}

function extractInventoryBlock(text, startMarker, endMarker, runtimeLabel) {
  const start = text.indexOf(startMarker);
  const end = text.indexOf(endMarker);

  assert.ok(start !== -1, `Missing ${startMarker} in runtime API docs.`);
  assert.ok(end !== -1, `Missing ${endMarker} in runtime API docs.`);
  assert.ok(end > start, `Invalid ${runtimeLabel} inventory marker ordering.`);

  const block = text.slice(start + startMarker.length, end);
  const symbols = [];
  for (const line of block.split(/\r?\n/)) {
    const match = line.match(/-\s*`([^`]+)`/);
    if (match) {
      symbols.push(match[1]);
    }
  }

  return normalizeLineSet(symbols);
}

function extractDotnetMethodSymbols(text, typeName) {
  const symbols = new Set();
  const methodPattern = /public\s+static\s+.*?([A-Za-z_][A-Za-z0-9_]*)\s*\(/g;

  for (const match of text.matchAll(methodPattern)) {
    const methodName = match[1];
    const openParenIndex = (match.index ?? 0) + match[0].lastIndexOf("(");
    const paramText = readMethodParameterText(text, openParenIndex);
    const normalizedParams = extractParameterTypes(paramText);
    symbols.add(`dotnet:${typeName}.${methodName}(${normalizedParams.join(",")})`);
  }

  return symbols;
}

function extractConversionPlanSymbols(text) {
  const symbols = new Set();

  const propertyPattern = /public\s+[A-Za-z_][A-Za-z0-9_<>?,\s]*\s+([A-Za-z_][A-Za-z0-9_]*)\s*\{\s*get;\s*\}/g;
  for (const match of text.matchAll(propertyPattern)) {
    symbols.add(`dotnet:ConversionPlan.${match[1]}`);
  }

  const methodPattern = /public\s+.*?([A-Za-z_][A-Za-z0-9_]*)\s*\(/g;
  for (const match of text.matchAll(methodPattern)) {
    const methodName = match[1];
    if (methodName === "ConversionPlan" || methodName === "get" || methodName === "set") {
      continue;
    }
    const openParenIndex = (match.index ?? 0) + match[0].lastIndexOf("(");
    const paramText = readMethodParameterText(text, openParenIndex);
    const normalizedParams = extractParameterTypes(paramText);
    symbols.add(`dotnet:ConversionPlan.${methodName}(${normalizedParams.join(",")})`);
  }

  return symbols;
}

function extractNamedTypes(text) {
  const names = new Set();
  const typePattern = /public\s+(?:sealed\s+)?(?:record|class|interface|enum)\s+([A-Za-z_][A-Za-z0-9_]*)/g;

  for (const match of text.matchAll(typePattern)) {
    names.add(match[1]);
  }

  return names;
}

function extractTypescriptIndexExports(text) {
  const symbols = new Set();

  const exportBlockPattern = /export\s*\{([\s\S]*?)\};/g;
  for (const blockMatch of text.matchAll(exportBlockPattern)) {
    const block = blockMatch[1]
      .split("\n")
      .map((line) => line.trim())
      .join(" ");

    for (const symbol of block.split(",").map((entry) => entry.trim()).filter(Boolean)) {
      symbols.add(`typescript:${symbol}`);
    }
  }

  const functionPattern = /export\s+async\s+function\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(/g;
  for (const match of text.matchAll(functionPattern)) {
    symbols.add(`typescript:${match[1]}`);
  }

  return symbols;
}

function extractTypescriptTypeExports(text) {
  const symbols = new Set();
  const typePattern = /export\s+(?:enum|interface|type|const)\s+([A-Za-z_][A-Za-z0-9_]*)/g;

  for (const match of text.matchAll(typePattern)) {
    symbols.add(`typescript:${match[1]}`);
  }

  return symbols;
}

function compareSymbolSets(runtimeLabel, runtimeSymbols, documentedSymbols) {
  const missingInDocs = [...runtimeSymbols].filter((symbol) => !documentedSymbols.has(symbol)).sort();
  const staleInDocs = [...documentedSymbols].filter((symbol) => !runtimeSymbols.has(symbol)).sort();

  const failures = [];
  if (missingInDocs.length > 0) {
    failures.push(`Missing ${runtimeLabel} symbols in docs:\n- ${missingInDocs.join("\n- ")}`);
  }

  if (staleInDocs.length > 0) {
    failures.push(`Stale ${runtimeLabel} symbols documented but not in runtime:\n- ${staleInDocs.join("\n- ")}`);
  }

  return failures;
}

const runtimeApiDocText = readFileSync(runtimeApiDocPath, "utf8");
const documentedDotnetSymbols = extractInventoryBlock(
  runtimeApiDocText,
  DOC_DOTNET_START,
  DOC_DOTNET_END,
  ".NET"
);
const documentedTypescriptSymbols = extractInventoryBlock(
  runtimeApiDocText,
  DOC_TYPESCRIPT_START,
  DOC_TYPESCRIPT_END,
  "TypeScript"
);

const conversionEngineText = readFileSync(join(root, "src", "Apiconvert.Core", "Converters", "ConversionEngine.cs"), "utf8");
const conversionPlanText = readFileSync(join(root, "src", "Apiconvert.Core", "Converters", "ConversionPlan.cs"), "utf8");
const generationContractsText = readFileSync(join(root, "src", "Apiconvert.Core", "Contracts", "GenerationModels.cs"), "utf8");
const dotnetModelsText = readFileSync(join(root, "src", "Apiconvert.Core", "Rules", "Models.cs"), "utf8");

const runtimeDotnetSymbols = new Set([
  ...extractDotnetMethodSymbols(conversionEngineText, "ConversionEngine"),
  ...extractConversionPlanSymbols(conversionPlanText)
]);

for (const typeName of extractNamedTypes(generationContractsText)) {
  if (DOTNET_CONTRACT_TYPES.has(typeName)) {
    runtimeDotnetSymbols.add(`dotnet:type:${typeName}`);
  }
}

for (const typeName of extractNamedTypes(dotnetModelsText)) {
  if (DOTNET_MODEL_TYPES.has(typeName)) {
    runtimeDotnetSymbols.add(`dotnet:type:${typeName}`);
  }
}

const typescriptIndexText = readFileSync(join(root, "src", "apiconvert-core", "src", "index.ts"), "utf8");
const typescriptTypesText = readFileSync(join(root, "src", "apiconvert-core", "src", "types.ts"), "utf8");

const runtimeTypescriptSymbols = new Set([
  ...extractTypescriptIndexExports(typescriptIndexText),
  ...extractTypescriptTypeExports(typescriptTypesText)
]);

const failures = [
  ...compareSymbolSets(".NET", runtimeDotnetSymbols, documentedDotnetSymbols),
  ...compareSymbolSets("TypeScript", runtimeTypescriptSymbols, documentedTypescriptSymbols)
];

assert.equal(failures.length, 0, failures.join("\n\n"));
console.log(
  `API docs coverage smoke test passed (.NET=${runtimeDotnetSymbols.size}, TypeScript=${runtimeTypescriptSymbols.size}).`
);
