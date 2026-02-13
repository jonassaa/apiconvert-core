import { normalizeWritePath } from "./core-utils";
import {
  type ArrayRule,
  type BranchRule,
  DataFormat,
  type ConversionRules,
  type FieldRule,
  type RuleNode,
  type ValueSource
} from "./types";

export function normalizeConversionRules(raw: unknown): ConversionRules {
  if (raw && typeof raw === "object" && !Array.isArray(raw)) {
    if (isConversionRules(raw)) {
      return normalizeRules(raw);
    }
    return emptyRules();
  }

  if (typeof raw === "string") {
    try {
      const parsed = JSON.parse(raw);
      return normalizeConversionRules(parsed);
    } catch {
      return emptyRules();
    }
  }

  return emptyRules();
}

function normalizeRules(rules: ConversionRules): ConversionRules {
  const validationErrors: string[] = [];
  return {
    inputFormat: rules.inputFormat ?? DataFormat.Json,
    outputFormat: rules.outputFormat ?? DataFormat.Json,
    rules: normalizeRuleNodes(rules.rules ?? [], "rules", validationErrors),
    validationErrors
  };
}

function emptyRules(validationErrors: string[] = []): ConversionRules {
  return {
    inputFormat: DataFormat.Json,
    outputFormat: DataFormat.Json,
    rules: [],
    validationErrors
  };
}

function normalizeValueSource(source: ValueSource | null | undefined): ValueSource {
  const normalized = source ?? { type: "path" };
  const expression = normalized.expression?.trim();
  return {
    ...normalized,
    paths: normalized.paths ?? [],
    expression: expression && expression.length > 0 ? expression : null,
    trueSource: normalized.trueSource ? normalizeValueSource(normalized.trueSource) : null,
    falseSource: normalized.falseSource ? normalizeValueSource(normalized.falseSource) : null,
    elseIf: (normalized.elseIf ?? []).map((branch) => {
      const branchExpression = branch.expression?.trim();
      return {
        expression: branchExpression && branchExpression.length > 0 ? branchExpression : null,
        source: branch.source ? normalizeValueSource(branch.source) : null,
        value: branch.value ?? null
      };
    })
  };
}

function isConversionRules(value: unknown): value is ConversionRules {
  if (!value || typeof value !== "object" || Array.isArray(value)) {
    return false;
  }
  const record = value as Record<string, unknown>;
  return "rules" in record || "inputFormat" in record || "outputFormat" in record;
}

function normalizeRuleNodes(
  nodes: RuleNode[],
  path: string,
  validationErrors: string[]
): RuleNode[] {
  const normalized: RuleNode[] = [];

  nodes.forEach((node, index) => {
    const nodePath = `${path}[${index}]`;
    const kind = normalizeNodeKind(node);
    if (kind == null) {
      validationErrors.push(`${nodePath}: kind is required.`);
      return;
    }

    if (!isSupportedRuleKind(kind)) {
      validationErrors.push(`${nodePath}: unsupported kind '${node?.kind ?? ""}'.`);
      return;
    }

    switch (kind) {
      case "field":
        normalized.push(normalizeFieldRuleNode(node as FieldRule, nodePath, validationErrors));
        return;
      case "array":
        normalized.push(normalizeArrayRuleNode(node as ArrayRule, nodePath, validationErrors));
        return;
      default:
        normalized.push(normalizeBranchRuleNode(node as BranchRule, nodePath, validationErrors));
    }
  });

  return normalized;
}

function normalizeNodeKind(node: RuleNode): string | null {
  const normalizedKind = (node?.kind ?? "").trim().toLowerCase();
  return normalizedKind.length > 0 ? normalizedKind : null;
}

function isSupportedRuleKind(kind: string): kind is "field" | "array" | "branch" {
  return kind === "field" || kind === "array" || kind === "branch";
}

function normalizeFieldRuleNode(
  field: FieldRule,
  nodePath: string,
  validationErrors: string[]
): FieldRule {
  const outputPaths = normalizeOutputPaths(field.outputPaths ?? []);
  if (outputPaths.length === 0) {
    validationErrors.push(`${nodePath}: outputPaths is required.`);
  }

  return {
    kind: "field",
    outputPaths,
    source: normalizeValueSource(field.source),
    defaultValue: field.defaultValue ?? ""
  };
}

function normalizeArrayRuleNode(
  array: ArrayRule,
  nodePath: string,
  validationErrors: string[]
): ArrayRule {
  const outputPaths = normalizeOutputPaths(array.outputPaths ?? []);
  if (!array.inputPath || array.inputPath.trim().length === 0) {
    validationErrors.push(`${nodePath}: inputPath is required.`);
  }
  if (outputPaths.length === 0) {
    validationErrors.push(`${nodePath}: outputPaths is required.`);
  }

  return {
    kind: "array",
    inputPath: (array.inputPath ?? "").trim(),
    outputPaths,
    coerceSingle: array.coerceSingle ?? false,
    itemRules: normalizeRuleNodes(array.itemRules ?? [], `${nodePath}.itemRules`, validationErrors)
  };
}

function normalizeBranchRuleNode(
  branch: BranchRule,
  nodePath: string,
  validationErrors: string[]
): BranchRule {
  const expression = branch.expression?.trim() || null;
  if (!expression) {
    validationErrors.push(`${nodePath}: expression is required.`);
  }

  const elseIf = (branch.elseIf ?? []).map((entry, elseIfIndex) => {
    const elseIfPath = `${nodePath}.elseIf[${elseIfIndex}]`;
    const elseIfExpression = entry.expression?.trim() || null;
    if (!elseIfExpression) {
      validationErrors.push(`${elseIfPath}: expression is required.`);
    }
    return {
      expression: elseIfExpression,
      then: normalizeRuleNodes(entry.then ?? [], `${elseIfPath}.then`, validationErrors)
    };
  });

  return {
    kind: "branch",
    expression,
    then: normalizeRuleNodes(branch.then ?? [], `${nodePath}.then`, validationErrors),
    elseIf,
    else: normalizeRuleNodes(branch.else ?? [], `${nodePath}.else`, validationErrors)
  };
}

function normalizeOutputPaths(paths: string[]): string[] {
  return [...new Set(paths
    .filter((path) => !!path && path.trim().length > 0)
    .map((path) => normalizeWritePath(path))
    .filter((path) => path.length > 0)
  )];
}
