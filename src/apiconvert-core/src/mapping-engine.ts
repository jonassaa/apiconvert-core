import { normalizeConversionRules } from "./rules-normalizer";
import { executeRules } from "./rule-executor";
import { parseQueryString } from "./query-payload";
import { parseXml } from "./xml-payload";
import {
  ConversionDiagnosticSeverity,
  OutputCollisionPolicy,
  type ApplyConversionOptions,
  StreamErrorMode,
  StreamInputKind,
  type ConversionDiagnostic,
  type ConversionResult,
  type ConversionRules,
  type StreamConversionOptions
} from "./types";

export function applyConversion(
  input: unknown,
  rawRules: unknown,
  options: ApplyConversionOptions = {}
): ConversionResult {
  const rules = normalizeConversionRules(rawRules);
  return applyConversionWithRules(input, rules, options);
}

export function applyConversionWithRules(
  input: unknown,
  rules: ConversionRules,
  options: ApplyConversionOptions = {}
): ConversionResult {
  const nodes = rules.rules ?? [];
  const diagnostics = (rules.validationErrors ?? []).map((message) => ({
    code: "ACV-RUN-000",
    rulePath: "rules",
    message,
    severity: ConversionDiagnosticSeverity.Error
  }));
  const errors = diagnostics
    .filter((entry) => entry.severity === ConversionDiagnosticSeverity.Error)
    .map((entry) => entry.message);
  const collisionPolicy = options.collisionPolicy ?? OutputCollisionPolicy.LastWriteWins;
  const transforms = options.transforms ?? {};
  const trace = options.explain ? [] : null;

  if (nodes.length === 0) {
    return { output: input ?? {}, errors, warnings: [], trace: trace ?? [], diagnostics };
  }

  const output: Record<string, unknown> = {};

  executeRules(
    input,
    null,
    nodes,
    output,
    errors,
    diagnostics,
    new Map<string, string>(),
    collisionPolicy,
    transforms,
    trace,
    "rules",
    0
  );

  return {
    output,
    errors: diagnostics
      .filter((entry) => entry.severity === ConversionDiagnosticSeverity.Error)
      .map((entry) => entry.message),
    warnings: diagnostics
      .filter((entry) => entry.severity === ConversionDiagnosticSeverity.Warning)
      .map((entry) => entry.message),
    trace: trace ?? [],
    diagnostics
  };
}

export async function* streamJsonArrayConversion(
  items: Iterable<unknown> | AsyncIterable<unknown>,
  rawRules: unknown,
  options: ApplyConversionOptions = {}
): AsyncGenerator<ConversionResult> {
  yield* streamConversion(
    items,
    rawRules,
    {
      inputKind: StreamInputKind.JsonArray,
      errorMode: StreamErrorMode.ContinueWithReport
    },
    options
  );
}

export async function* streamConversion(
  input:
    | Iterable<unknown>
    | AsyncIterable<unknown>
    | string
    | Iterable<string | Uint8Array>
    | AsyncIterable<string | Uint8Array>,
  rawRules: unknown,
  streamOptions: StreamConversionOptions = {},
  options: ApplyConversionOptions = {}
): AsyncGenerator<ConversionResult> {
  const rules = normalizeConversionRules(rawRules);
  const inputKind = streamOptions.inputKind ?? StreamInputKind.JsonArray;
  const errorMode = streamOptions.errorMode ?? StreamErrorMode.FailFast;
  let index = 0;

  for await (const entry of readItems(input, inputKind, streamOptions.xmlItemPath ?? null)) {
    if (entry.error) {
      if (errorMode === StreamErrorMode.FailFast) {
        throw new Error(`stream[${index}]: ${entry.error}`);
      }
      yield createStreamErrorResult(index, entry.error);
      index += 1;
      continue;
    }

    const result = applyConversionWithRules(entry.value, rules, options);
    if (errorMode === StreamErrorMode.FailFast && result.errors.length > 0) {
      throw new Error(`stream[${index}]: conversion failed: ${result.errors[0]}`);
    }

    if (errorMode === StreamErrorMode.ContinueWithReport) {
      yield addStreamContext(result, index);
    } else {
      yield result;
    }

    index += 1;
  }
}

async function* toAsyncIterable(
  values: Iterable<unknown> | AsyncIterable<unknown>
): AsyncGenerator<unknown> {
  if (Symbol.asyncIterator in Object(values)) {
    for await (const value of values as AsyncIterable<unknown>) {
      yield value;
    }
    return;
  }

  for (const value of values as Iterable<unknown>) {
    yield value;
  }
}

async function* readItems(
  input:
    | Iterable<unknown>
    | AsyncIterable<unknown>
    | string
    | Iterable<string | Uint8Array>
    | AsyncIterable<string | Uint8Array>,
  inputKind: StreamInputKind,
  xmlItemPath: string | null
): AsyncGenerator<{ value: unknown; error?: string }> {
  if (inputKind === StreamInputKind.JsonArray) {
    if (typeof input === "string") {
      let parsed: unknown;
      try {
        parsed = JSON.parse(input);
      } catch (error) {
        yield { value: null, error: `failed to parse JSON array stream: ${toErrorMessage(error)}` };
        return;
      }
      if (!Array.isArray(parsed)) {
        yield { value: null, error: "failed to parse JSON array stream: top-level JSON value must be an array." };
        return;
      }
      for (const item of parsed) {
        yield { value: item };
      }
      return;
    }

    for await (const item of toAsyncIterable(input as Iterable<unknown> | AsyncIterable<unknown>)) {
      yield { value: item };
    }
    return;
  }

  const text = await readAllText(input as string | Iterable<string | Uint8Array> | AsyncIterable<string | Uint8Array>);

  if (inputKind === StreamInputKind.Ndjson) {
    const lines = splitNonEmptyLines(text);
    for (const line of lines) {
      try {
        yield { value: JSON.parse(line) };
      } catch (error) {
        yield { value: null, error: `failed to parse NDJSON line: ${toErrorMessage(error)}` };
      }
    }
    return;
  }

  if (inputKind === StreamInputKind.QueryLines) {
    const lines = splitNonEmptyLines(text);
    for (const line of lines) {
      try {
        yield { value: parseQueryString(line) };
      } catch (error) {
        yield { value: null, error: `failed to parse query record: ${toErrorMessage(error)}` };
      }
    }
    return;
  }

  if (!xmlItemPath || xmlItemPath.trim().length === 0) {
    throw new Error("XmlElements streaming requires streamOptions.xmlItemPath.");
  }

  let parsedXml: unknown;
  try {
    parsedXml = parseXml(text);
  } catch (error) {
    yield { value: null, error: `failed to parse XML stream: ${toErrorMessage(error)}` };
    return;
  }

  const segments = xmlItemPath.split(".").map((segment) => segment.trim()).filter((segment) => segment.length > 0);
  if (segments.length === 0) {
    throw new Error("XmlElements streaming requires a non-empty streamOptions.xmlItemPath.");
  }

  for (const item of selectXmlItems(parsedXml, segments)) {
    yield { value: item };
  }
}

async function readAllText(
  input: string | Iterable<string | Uint8Array> | AsyncIterable<string | Uint8Array>
): Promise<string> {
  if (typeof input === "string") {
    return input;
  }

  const decoder = new TextDecoder();
  let text = "";
  if (Symbol.asyncIterator in Object(input)) {
    for await (const chunk of input as AsyncIterable<string | Uint8Array>) {
      text += typeof chunk === "string" ? chunk : decoder.decode(chunk, { stream: true });
    }
  } else {
    for (const chunk of input as Iterable<string | Uint8Array>) {
      text += typeof chunk === "string" ? chunk : decoder.decode(chunk, { stream: true });
    }
  }

  text += decoder.decode();
  return text;
}

function splitNonEmptyLines(text: string): string[] {
  return text
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter((line) => line.length > 0);
}

function selectXmlItems(root: unknown, pathSegments: string[]): unknown[] {
  let current = [root];
  for (const segment of pathSegments) {
    const next: unknown[] = [];
    for (const item of current) {
      if (!isRecord(item)) {
        continue;
      }

      const value = item[segment];
      if (Array.isArray(value)) {
        next.push(...value);
      } else if (value !== undefined) {
        next.push(value);
      }
    }
    current = next;
  }
  return current;
}

function addStreamContext(result: ConversionResult, index: number): ConversionResult {
  const prefix = `stream[${index}]`;
  const diagnostics: ConversionDiagnostic[] = result.diagnostics.map((diagnostic) => ({
    ...diagnostic,
    rulePath: `${prefix}.${diagnostic.rulePath}`,
    message: `${prefix}: ${diagnostic.message}`
  }));

  return {
    ...result,
    errors: result.errors.map((error) => `${prefix}: ${error}`),
    warnings: result.warnings.map((warning) => `${prefix}: ${warning}`),
    diagnostics
  };
}

function createStreamErrorResult(index: number, error: string): ConversionResult {
  const message = `stream[${index}]: ${error}`;
  return {
    output: {},
    errors: [message],
    warnings: [],
    trace: [],
    diagnostics: [
      {
        code: "ACV-STR-001",
        rulePath: `stream[${index}]`,
        message,
        severity: ConversionDiagnosticSeverity.Error
      }
    ]
  };
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return value != null && typeof value === "object" && !Array.isArray(value);
}

function toErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}
