import { DataFormat, TransformType, type ConversionRules, type RuleNode, type ValueSource } from "./types";
import { normalizeConversionRules } from "./rules-normalizer";

export interface FormatConversionRulesOptions {
  pretty?: boolean;
}

export function formatConversionRules(rawRules: unknown, options: FormatConversionRulesOptions = {}): string {
  const rules = normalizeConversionRules(rawRules);
  const canonical = canonicalizeRules(rules);
  const pretty = options.pretty ?? true;
  return JSON.stringify(canonical, null, pretty ? 2 : 0);
}

function canonicalizeRules(rules: ConversionRules): Record<string, unknown> {
  const result: Record<string, unknown> = {};

  if ((rules.inputFormat ?? DataFormat.Json) !== DataFormat.Json) {
    result.inputFormat = rules.inputFormat;
  }

  if ((rules.outputFormat ?? DataFormat.Json) !== DataFormat.Json) {
    result.outputFormat = rules.outputFormat;
  }

  result.rules = (rules.rules ?? []).map(canonicalizeRule);
  return result;
}

function canonicalizeRule(rule: RuleNode): Record<string, unknown> {
  if (rule.kind === "field") {
    const result: Record<string, unknown> = {
      kind: "field",
      outputPaths: [...(rule.outputPaths ?? [])],
      source: canonicalizeSource(rule.source)
    };

    if (rule.defaultValue != null && rule.defaultValue.length > 0) {
      result.defaultValue = rule.defaultValue;
    }

    return result;
  }

  if (rule.kind === "array") {
    const result: Record<string, unknown> = {
      kind: "array",
      inputPath: rule.inputPath,
      outputPaths: [...(rule.outputPaths ?? [])],
      itemRules: (rule.itemRules ?? []).map(canonicalizeRule)
    };

    if (rule.coerceSingle) {
      result.coerceSingle = true;
    }

    return result;
  }

  const result: Record<string, unknown> = {
    kind: "branch",
    expression: rule.expression ?? "",
    then: (rule.then ?? []).map(canonicalizeRule)
  };

  if ((rule.elseIf ?? []).length > 0) {
    result.elseIf = (rule.elseIf ?? []).map((entry) => ({
      expression: entry.expression ?? "",
      then: (entry.then ?? []).map(canonicalizeRule)
    }));
  }

  if ((rule.else ?? []).length > 0) {
    result.else = (rule.else ?? []).map(canonicalizeRule);
  }

  return result;
}

function canonicalizeSource(source: ValueSource): Record<string, unknown> {
  const result: Record<string, unknown> = {
    type: source.type
  };

  if (source.path != null && source.path.length > 0) {
    result.path = source.path;
  }

  if (source.paths != null && source.paths.length > 0) {
    result.paths = [...source.paths];
  }

  if (source.value != null) {
    result.value = source.value;
  }

  if (source.expression != null && source.expression.length > 0) {
    result.expression = source.expression;
  }

  if (source.trueValue != null) {
    result.trueValue = source.trueValue;
  }

  if (source.falseValue != null) {
    result.falseValue = source.falseValue;
  }

  if (source.trueSource != null) {
    result.trueSource = canonicalizeSource(source.trueSource);
  }

  if (source.falseSource != null) {
    result.falseSource = canonicalizeSource(source.falseSource);
  }

  if (source.elseIf != null && source.elseIf.length > 0) {
    result.elseIf = source.elseIf.map((entry) => {
      const branch: Record<string, unknown> = {
        expression: entry.expression ?? ""
      };
      if (entry.source != null) {
        branch.source = canonicalizeSource(entry.source);
      }
      if (entry.value != null) {
        branch.value = entry.value;
      }
      return branch;
    });
  }

  if (source.conditionOutput != null) {
    result.conditionOutput = source.conditionOutput;
  }

  if (source.mergeMode != null) {
    result.mergeMode = source.mergeMode;
  }

  if (source.separator != null) {
    result.separator = source.separator;
  }

  if (source.tokenIndex != null) {
    result.tokenIndex = source.tokenIndex;
  }

  if (source.trimAfterSplit != null) {
    result.trimAfterSplit = source.trimAfterSplit;
  }

  if (source.transform != null) {
    result.transform = source.transform;
  }

  if (source.customTransform != null && source.customTransform.length > 0) {
    result.customTransform = source.customTransform;
  }

  if (source.type === "merge" && source.mergeMode == null) {
    result.mergeMode = "concat";
  }

  if (source.type === "condition" && source.conditionOutput == null) {
    result.conditionOutput = "branch";
  }

  if (source.type === "transform" && source.transform == null && source.customTransform == null) {
    result.transform = TransformType.ToLowerCase;
  }

  return result;
}
