import { normalizeConversionRules } from "./rules-normalizer";
import { executeRules } from "./rule-executor";
import {
  OutputCollisionPolicy,
  type ApplyConversionOptions,
  type ConversionResult
} from "./types";

export function applyConversion(
  input: unknown,
  rawRules: unknown,
  options: ApplyConversionOptions = {}
): ConversionResult {
  const rules = normalizeConversionRules(rawRules);
  const nodes = rules.rules ?? [];
  const errors = [...(rules.validationErrors ?? [])];
  const collisionPolicy = options.collisionPolicy ?? OutputCollisionPolicy.LastWriteWins;

  if (nodes.length === 0) {
    return { output: input ?? {}, errors, warnings: [] };
  }

  const output: Record<string, unknown> = {};
  const warnings: string[] = [];

  executeRules(input, null, nodes, output, errors, warnings, new Map<string, string>(), collisionPolicy, "rules", 0);

  return { output, errors, warnings };
}
