import {
  getWritePaths,
  parsePrimitive,
  setValueByPath
} from "./core-utils";
import { tryEvaluateConditionExpression } from "./condition-expression";
import { resolvePathValue, resolveSourceValue } from "./source-resolver";
import {
  type ArrayRule,
  type BranchRule,
  type FieldRule,
  type RuleNode
} from "./types";

export function executeRules(
  root: unknown,
  item: unknown,
  rules: RuleNode[],
  output: Record<string, unknown>,
  errors: string[],
  warnings: string[],
  path: string,
  depth: number
): void {
  if (depth > 64) {
    errors.push(`${path}: rule recursion limit exceeded.`);
    return;
  }

  rules.forEach((rule, index) => {
    executeRule(root, item, rule, output, errors, warnings, `${path}[${index}]`, depth);
  });
}

function executeRule(
  root: unknown,
  item: unknown,
  rule: RuleNode,
  output: Record<string, unknown>,
  errors: string[],
  warnings: string[],
  path: string,
  depth: number
): void {
  if (rule.kind === "field") {
    executeFieldRule(root, item, rule, output, errors, path);
    return;
  }

  if (rule.kind === "array") {
    executeArrayRule(root, item, rule, output, errors, warnings, path, depth);
    return;
  }

  if (rule.kind === "branch") {
    executeBranchRule(root, item, rule, output, errors, warnings, path, depth);
    return;
  }

  errors.push(`${path}: unsupported kind '${(rule as { kind?: string }).kind ?? ""}'.`);
}

function executeFieldRule(
  root: unknown,
  item: unknown,
  rule: FieldRule,
  output: Record<string, unknown>,
  errors: string[],
  path: string
): void {
  const writePaths = getWritePaths(rule);
  if (writePaths.length === 0) {
    errors.push(`${path}: outputPaths is required.`);
    return;
  }

  let value = resolveSourceValue(root, item, rule.source, errors, `${path}.source`);
  if ((value == null || value === "") && rule.defaultValue) {
    value = parsePrimitive(rule.defaultValue);
  }

  writePaths.forEach((writePath) => {
    setValueByPath(output, writePath, value);
  });
}

function executeArrayRule(
  root: unknown,
  item: unknown,
  rule: ArrayRule,
  output: Record<string, unknown>,
  errors: string[],
  warnings: string[],
  path: string,
  depth: number
): void {
  const value = resolveSourceValue(root, item, { type: "path", path: rule.inputPath ?? "" }, errors, path);
  let items = Array.isArray(value) ? value : null;
  if (!items && rule.coerceSingle && value != null) {
    items = [value];
  }

  if (!items) {
    if (value == null) {
      warnings.push(`Array mapping skipped: inputPath "${rule.inputPath}" not found (${path}).`);
    } else {
      errors.push(`${path}: input path did not resolve to an array (${rule.inputPath}).`);
    }
    return;
  }

  const writePaths = getWritePaths(rule);
  if (writePaths.length === 0) {
    errors.push(`${path}: outputPaths is required.`);
    return;
  }

  const mappedItems: unknown[] = [];
  items.forEach((arrayItem) => {
    const itemOutput: Record<string, unknown> = {};
    executeRules(root, arrayItem, rule.itemRules ?? [], itemOutput, errors, warnings, `${path}.itemRules`, depth + 1);
    mappedItems.push(itemOutput);
  });

  writePaths.forEach((writePath) => {
    setValueByPath(output, writePath, mappedItems);
  });
}

function executeBranchRule(
  root: unknown,
  item: unknown,
  rule: BranchRule,
  output: Record<string, unknown>,
  errors: string[],
  warnings: string[],
  path: string,
  depth: number
): void {
  const matched = evaluateRuleCondition(root, item, rule.expression, errors, path, "branch expression");
  if (matched) {
    executeRules(root, item, rule.then ?? [], output, errors, warnings, `${path}.then`, depth + 1);
    return;
  }

  const elseIfBranches = rule.elseIf ?? [];
  for (let index = 0; index < elseIfBranches.length; index += 1) {
    const elseIf = elseIfBranches[index];
    const branchPath = `${path}.elseIf[${index}]`;
    const elseIfMatched = evaluateRuleCondition(
      root,
      item,
      elseIf.expression,
      errors,
      branchPath,
      "branch expression"
    );

    if (!elseIfMatched) {
      continue;
    }

    executeRules(root, item, elseIf.then ?? [], output, errors, warnings, `${branchPath}.then`, depth + 1);
    return;
  }

  if ((rule.else ?? []).length > 0) {
    executeRules(root, item, rule.else ?? [], output, errors, warnings, `${path}.else`, depth + 1);
  }
}

function evaluateRuleCondition(
  root: unknown,
  item: unknown,
  expression: string | null | undefined,
  errors: string[],
  errorContext: string,
  label: string
): boolean {
  if (!expression || expression.trim().length === 0) {
    errors.push(`${errorContext}: ${label} is required.`);
    return false;
  }

  const evaluation = tryEvaluateConditionExpression(expression, (path) => resolvePathValue(root, item, path));
  if (!evaluation.ok) {
    errors.push(`${errorContext}: invalid ${label} "${expression}": ${evaluation.error}`);
    return false;
  }

  return evaluation.value;
}
