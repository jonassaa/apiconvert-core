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
