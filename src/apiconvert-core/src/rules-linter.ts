import { normalizeConversionRules } from "./rules-normalizer";
import {
  type ConversionRulesLintResult,
  RuleLintSeverity,
  type RuleNode,
  type RuleLintDiagnostic
} from "./types";

export function lintConversionRules(rawRules: unknown): ConversionRulesLintResult {
  const rules = normalizeConversionRules(rawRules);
  const diagnostics: RuleLintDiagnostic[] = [];

  for (const validationError of rules.validationErrors ?? []) {
    diagnostics.push({
      code: "ACV-LINT-001",
      severity: RuleLintSeverity.Error,
      rulePath: "rules",
      message: validationError,
      suggestion: "Fix schema/normalization errors before running conversion."
    });
  }

  const outputWriters = new Map<string, string>();
  analyzeRuleNodes(diagnostics, outputWriters, rules.rules ?? [], "rules");

  return {
    diagnostics,
    hasErrors: diagnostics.some((entry) => entry.severity === RuleLintSeverity.Error)
  };
}

function analyzeRuleNodes(
  diagnostics: RuleLintDiagnostic[],
  outputWriters: Map<string, string>,
  nodes: RuleNode[],
  path: string
): void {
  for (let index = 0; index < nodes.length; index += 1) {
    const node = nodes[index];
    const nodePath = `${path}[${index}]`;

    if (node.kind === "field") {
      analyzeOutputPaths(diagnostics, outputWriters, node.outputPaths ?? [], nodePath);
      if (node.source.type === "path" && !node.defaultValue) {
        diagnostics.push({
          code: "ACV-LINT-002",
          severity: RuleLintSeverity.Warning,
          rulePath: nodePath,
          message: `${nodePath}: source.type=path without defaultValue can produce null/empty writes when input is missing.`,
          suggestion: "Set defaultValue when missing input is expected, or keep as-is if null propagation is intentional."
        });
      }
      continue;
    }

    if (node.kind === "array") {
      analyzeOutputPaths(diagnostics, outputWriters, node.outputPaths ?? [], nodePath);
      analyzeRuleNodes(diagnostics, outputWriters, node.itemRules ?? [], `${nodePath}.itemRules`);
      continue;
    }

    const literal = parseBooleanLiteral(node.expression ?? null);
    if (literal === true && ((node.elseIf?.length ?? 0) > 0 || (node.else?.length ?? 0) > 0)) {
      diagnostics.push({
        code: "ACV-LINT-003",
        severity: RuleLintSeverity.Warning,
        rulePath: nodePath,
        message: `${nodePath}: expression is always true; elseIf/else branches are unreachable.`,
        suggestion: "Remove unreachable branches or replace expression with a non-literal condition."
      });
    } else if (
      literal === false &&
      (node.elseIf?.length ?? 0) === 0 &&
      (node.else?.length ?? 0) === 0 &&
      (node.then?.length ?? 0) > 0
    ) {
      diagnostics.push({
        code: "ACV-LINT-004",
        severity: RuleLintSeverity.Warning,
        rulePath: nodePath,
        message: `${nodePath}: expression is always false and no else/elseIf branch exists.`,
        suggestion: "Add else/elseIf handling or remove the branch."
      });
    }

    analyzeRuleNodes(diagnostics, outputWriters, node.then ?? [], `${nodePath}.then`);
    (node.elseIf ?? []).forEach((branch, branchIndex) => {
      analyzeRuleNodes(
        diagnostics,
        outputWriters,
        branch.then ?? [],
        `${nodePath}.elseIf[${branchIndex}].then`
      );
    });
    analyzeRuleNodes(diagnostics, outputWriters, node.else ?? [], `${nodePath}.else`);
  }
}

function analyzeOutputPaths(
  diagnostics: RuleLintDiagnostic[],
  outputWriters: Map<string, string>,
  outputPaths: string[],
  nodePath: string
): void {
  for (const outputPath of outputPaths) {
    const firstWriter = outputWriters.get(outputPath);
    if (firstWriter) {
      diagnostics.push({
        code: "ACV-LINT-005",
        severity: RuleLintSeverity.Warning,
        rulePath: nodePath,
        message: `${nodePath}: outputPath '${outputPath}' is also written by ${firstWriter}.`,
        suggestion: "Use unique output paths or configure collisionPolicy explicitly for intentional overlaps."
      });
      continue;
    }

    outputWriters.set(outputPath, nodePath);
  }
}

function parseBooleanLiteral(expression: string | null): boolean | null {
  if (!expression) {
    return null;
  }

  const normalized = expression.trim().toLowerCase();
  if (normalized === "true") {
    return true;
  }
  if (normalized === "false") {
    return false;
  }

  return null;
}
