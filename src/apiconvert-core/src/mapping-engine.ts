import { normalizeConversionRules } from "./rules-normalizer";
import { executeRules } from "./rule-executor";
import {
  ConversionDiagnosticSeverity,
  OutputCollisionPolicy,
  type ApplyConversionOptions,
  type ConversionResult,
  type ConversionRules
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
  const rules = normalizeConversionRules(rawRules);

  for await (const item of toAsyncIterable(items)) {
    yield applyConversionWithRules(item, rules, options);
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
