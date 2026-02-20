import { compileConversionPlan } from "./conversion-plan";
import type {
  CompiledConversionPlan,
  ConversionLatencyProfile,
  ConversionProfileOptions,
  ConversionProfileReport
} from "./types";

export function profileConversionPlan(
  planOrRules: CompiledConversionPlan | unknown,
  inputs: unknown[],
  options: ConversionProfileOptions = {}
): ConversionProfileReport {
  if (!Array.isArray(inputs) || inputs.length === 0) {
    throw new Error("profileConversionPlan requires at least one input item.");
  }

  const iterations = Math.max(1, Math.trunc(options.iterations ?? 100));
  const warmupIterations = Math.max(0, Math.trunc(options.warmupIterations ?? 10));

  const compileStart = nowMs();
  const plan = isCompiledPlan(planOrRules) ? planOrRules : compileConversionPlan(planOrRules);
  const compileMs = isCompiledPlan(planOrRules) ? 0 : nowMs() - compileStart;

  const totalRuns = iterations * inputs.length;
  for (let i = 0; i < warmupIterations; i += 1) {
    for (const input of inputs) {
      plan.apply(input);
    }
  }

  const runDurations: number[] = [];
  for (let i = 0; i < iterations; i += 1) {
    for (const input of inputs) {
      const start = nowMs();
      plan.apply(input);
      runDurations.push(nowMs() - start);
    }
  }

  return {
    planCacheKey: plan.cacheKey,
    compileMs,
    warmupIterations,
    iterations,
    totalRuns,
    latencyMs: computeLatencyProfile(runDurations)
  };
}

function isCompiledPlan(value: unknown): value is CompiledConversionPlan {
  if (!value || typeof value !== "object") {
    return false;
  }

  const maybePlan = value as Partial<CompiledConversionPlan>;
  return typeof maybePlan.cacheKey === "string" && typeof maybePlan.apply === "function";
}

function computeLatencyProfile(values: number[]): ConversionLatencyProfile {
  const sorted = [...values].sort((a, b) => a - b);
  const sum = sorted.reduce((acc, value) => acc + value, 0);

  return {
    min: percentile(sorted, 0),
    p50: percentile(sorted, 50),
    p95: percentile(sorted, 95),
    p99: percentile(sorted, 99),
    max: percentile(sorted, 100),
    mean: sorted.length > 0 ? sum / sorted.length : 0
  };
}

function percentile(sortedValues: number[], pct: number): number {
  if (sortedValues.length === 0) {
    return 0;
  }

  const index = Math.min(
    sortedValues.length - 1,
    Math.max(0, Math.ceil((pct / 100) * sortedValues.length) - 1)
  );
  return sortedValues[index];
}

function nowMs(): number {
  return Number(process.hrtime.bigint()) / 1_000_000;
}
