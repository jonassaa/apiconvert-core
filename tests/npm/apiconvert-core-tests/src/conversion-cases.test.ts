import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";

const supportedExtensions = new Set(["json", "xml", "txt"]);

interface CaseFileSet {
  caseName: string;
  rulesPath: string;
  inputPath: string;
  outputPath: string;
}

test("conversion cases", async (t) => {
  const casesRoot = locateCasesRoot();
  const caseDirectories = fs
    .readdirSync(casesRoot, { withFileTypes: true })
    .filter((entry) => entry.isDirectory())
    .map((entry) => path.join(casesRoot, entry.name))
    .sort((a, b) => a.localeCompare(b));

  assert.ok(caseDirectories.length > 0, "No conversion cases found under tests/cases.");

  for (const caseDirectory of caseDirectories) {
    const caseFiles = loadCaseFiles(caseDirectory);
    await t.test(caseFiles.caseName, async () => {
      const rulesText = fs.readFileSync(caseFiles.rulesPath, "utf8");
      const inputText = fs.readFileSync(caseFiles.inputPath, "utf8");
      const expectedText = fs.readFileSync(caseFiles.outputPath, "utf8");

      const inputExt = path.extname(caseFiles.inputPath).slice(1);
      const outputExt = path.extname(caseFiles.outputPath).slice(1);

      const result = await runConversion({
        rulesText,
        inputText,
        inputExtension: inputExt,
        outputExtension: outputExt
      });

      if (result.status === "skipped") {
        t.skip(result.reason);
        return;
      }

      const normalizedActual = normalizeOutput(result.outputText, outputExt);
      const normalizedExpected = normalizeOutput(expectedText, outputExt);

      assert.equal(normalizedActual, normalizedExpected);
    });
  }
});

function locateCasesRoot(): string {
  const baseDir = __dirname;
  const candidate = path.resolve(baseDir, "../../../../tests/cases");
  if (fs.existsSync(candidate)) {
    return candidate;
  }

  let current = baseDir;
  while (true) {
    const testsRoot = path.join(current, "tests", "cases");
    if (fs.existsSync(testsRoot)) {
      return testsRoot;
    }
    const next = path.dirname(current);
    if (next === current) {
      break;
    }
    current = next;
  }

  throw new Error("Could not locate tests/cases. Ensure the repository contains shared cases.");
}

function loadCaseFiles(caseDirectory: string): CaseFileSet {
  const caseName = path.basename(caseDirectory);
  const rulesPath = path.join(caseDirectory, "rules.json");

  if (!fs.existsSync(rulesPath)) {
    throw new Error(`Case '${caseName}' is missing rules.json.`);
  }

  const inputPath = findSingleFile(caseDirectory, "input", caseName);
  const outputPath = findSingleFile(caseDirectory, "output", caseName);

  ensureExtensionSupported(inputPath, caseName);
  ensureExtensionSupported(outputPath, caseName);

  return { caseName, rulesPath, inputPath, outputPath };
}

function findSingleFile(directory: string, basename: string, caseName: string): string {
  const matches = fs
    .readdirSync(directory)
    .filter((file) => file.startsWith(`${basename}.`))
    .map((file) => path.join(directory, file))
    .filter((file) => fs.statSync(file).isFile());

  if (matches.length === 0) {
    throw new Error(`Case '${caseName}' must include ${basename}.*.`);
  }

  if (matches.length > 1) {
    throw new Error(`Case '${caseName}' must include exactly one ${basename}.* file.`);
  }

  return matches[0];
}

function ensureExtensionSupported(filePath: string, caseName: string): void {
  const extension = path.extname(filePath).slice(1).toLowerCase();
  if (!supportedExtensions.has(extension)) {
    throw new Error(`Case '${caseName}' uses unsupported extension '.${extension}'.`);
  }
}

function normalizeOutput(text: string, extension: string): string {
  const normalized = text.replace(/\r\n/g, "\n").trim();
  if (extension === "json") {
    const parsed = JSON.parse(normalized || "{}");
    return stableStringify(parsed);
  }
  return normalized;
}

function stableStringify(value: unknown): string {
  if (Array.isArray(value)) {
    return `[${value.map((item) => stableStringify(item)).join(",")}]`;
  }

  if (value && typeof value === "object") {
    const record = value as Record<string, unknown>;
    const keys = Object.keys(record).sort();
    const entries = keys.map((key) => `"${key}":${stableStringify(record[key])}`);
    return `{${entries.join(",")}}`;
  }

  return JSON.stringify(value);
}

type ConversionRunResult =
  | { status: "ok"; outputText: string }
  | { status: "skipped"; reason: string };

async function runConversion(args: {
  rulesText: string;
  inputText: string;
  inputExtension: string;
  outputExtension: string;
}): Promise<ConversionRunResult> {
  try {
    const api = await import("@apiconvert/core");
    if (typeof api.runConversionCase === "function") {
      try {
        const outputText = await api.runConversionCase(args);
        return { status: "ok", outputText };
      } catch (error) {
        if (error instanceof Error && error.message.includes("runConversionCase is not implemented")) {
          return {
            status: "skipped",
            reason: "runConversionCase is not implemented in @apiconvert/core yet."
          };
        }
        throw error;
      }
    }
  } catch (error) {
    if (error instanceof Error && error.message.includes("Cannot find module")) {
      return {
        status: "skipped",
        reason: "@apiconvert/core is not available; conversion engine not implemented yet."
      };
    }
    throw error;
  }

  return {
    status: "skipped",
    reason: "runConversionCase is not implemented in @apiconvert/core yet."
  };
}
