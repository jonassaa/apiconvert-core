import { normalizeConversionRules } from "./rules-normalizer";
import { executeRules } from "./rule-executor";
import {
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
  const errors = [...(rules.validationErrors ?? [])];
  const collisionPolicy = options.collisionPolicy ?? OutputCollisionPolicy.LastWriteWins;
  const transforms = options.transforms ?? {};
  const trace = options.explain ? [] : null;

  if (nodes.length === 0) {
    return { output: input ?? {}, errors, warnings: [], trace: trace ?? [] };
  }

  const output: Record<string, unknown> = {};
  const warnings: string[] = [];

  executeRules(
    input,
    null,
    nodes,
    output,
    errors,
    warnings,
    new Map<string, string>(),
    collisionPolicy,
    transforms,
    trace,
    "rules",
    0
  );

  return { output, errors, warnings, trace: trace ?? [] };
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
