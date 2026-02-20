import { applyConversionWithRules } from "./mapping-engine";
import { normalizeConversionRules } from "./rules-normalizer";
import type {
  ApplyConversionOptions,
  CompiledConversionPlan,
  ConversionResult,
  ConversionRules
} from "./types";

export function compileConversionPlan(rawRules: unknown): CompiledConversionPlan {
  const rules = normalizeConversionRules(rawRules);
  const cacheKey = computeRulesCacheKey(rules);

  return {
    rules,
    cacheKey,
    apply(input: unknown, options: ApplyConversionOptions = {}): ConversionResult {
      return applyConversionWithRules(input, rules, options);
    }
  };
}

export function computeRulesCacheKey(rawRules: unknown): string {
  const rules = normalizeConversionRules(rawRules);
  const canonical = stableStringify(rules);
  return fnv1a64(canonical);
}

function stableStringify(value: unknown): string {
  if (Array.isArray(value)) {
    return `[${value.map((item) => stableStringify(item)).join(",")}]`;
  }

  if (value && typeof value === "object") {
    const record = value as Record<string, unknown>;
    const keys = Object.keys(record).sort();
    const entries = keys.map((key) => `${JSON.stringify(key)}:${stableStringify(record[key])}`);
    return `{${entries.join(",")}}`;
  }

  return JSON.stringify(value);
}

function fnv1a64(input: string): string {
  let hash = 0xcbf29ce484222325n;
  const prime = 0x100000001b3n;

  for (let i = 0; i < input.length; i += 1) {
    hash ^= BigInt(input.charCodeAt(i));
    hash = (hash * prime) & 0xffffffffffffffffn;
  }

  return hash.toString(16).padStart(16, "0");
}
