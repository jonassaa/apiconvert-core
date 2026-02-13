import { normalizeConversionRules } from "./rules-normalizer";
import { executeRules } from "./rule-executor";
import { type ConversionResult } from "./types";

export function applyConversion(input: unknown, rawRules: unknown): ConversionResult {
  const rules = normalizeConversionRules(rawRules);
  const nodes = rules.rules ?? [];
  const errors = [...(rules.validationErrors ?? [])];

  if (nodes.length === 0) {
    return { output: input ?? {}, errors, warnings: [] };
  }

  const output: Record<string, unknown> = {};
  const warnings: string[] = [];

  executeRules(input, null, nodes, output, errors, warnings, "rules", 0);

  return { output, errors, warnings };
}
