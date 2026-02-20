import {
  getWritePaths,
  parsePrimitive,
  setValueByPath
} from "./core-utils";
import { tryEvaluateConditionExpression } from "./condition-expression";
import { resolvePathValue, resolveSourceValue } from "./source-resolver";
import {
  ConversionDiagnosticSeverity,
  OutputCollisionPolicy,
  type ArrayRule,
  type BranchRule,
  type ConversionDiagnostic,
  type ConversionTraceEntry,
  type FieldRule,
  type RuleNode
} from "./types";

export function executeRules(
  root: unknown,
  item: unknown,
  rules: RuleNode[],
  output: Record<string, unknown>,
  errors: string[],
  diagnostics: ConversionDiagnostic[],
  writeOwners: Map<string, string>,
  collisionPolicy: OutputCollisionPolicy,
  transforms: Record<string, (value: unknown) => unknown>,
  trace: ConversionTraceEntry[] | null,
  path: string,
  depth: number
): void {
  if (depth > 64) {
    addError(diagnostics, "ACV-RUN-900", path, `${path}: rule recursion limit exceeded.`);
    return;
  }

  rules.forEach((rule, index) => {
    executeRule(
      root,
      item,
      rule,
      output,
      errors,
      diagnostics,
      writeOwners,
      collisionPolicy,
      transforms,
      trace,
      `${path}[${index}]`,
      depth
    );
  });
}

function executeRule(
  root: unknown,
  item: unknown,
  rule: RuleNode,
  output: Record<string, unknown>,
  errors: string[],
  diagnostics: ConversionDiagnostic[],
  writeOwners: Map<string, string>,
  collisionPolicy: OutputCollisionPolicy,
  transforms: Record<string, (value: unknown) => unknown>,
  trace: ConversionTraceEntry[] | null,
  path: string,
  depth: number
): void {
  if (rule.kind === "field") {
    executeFieldRule(root, item, rule, output, errors, diagnostics, writeOwners, collisionPolicy, transforms, trace, path);
    return;
  }

  if (rule.kind === "array") {
    executeArrayRule(root, item, rule, output, errors, diagnostics, writeOwners, collisionPolicy, transforms, trace, path, depth);
    return;
  }

  if (rule.kind === "branch") {
    executeBranchRule(root, item, rule, output, errors, diagnostics, writeOwners, collisionPolicy, transforms, trace, path, depth);
    return;
  }

  const error = `${path}: unsupported kind '${(rule as { kind?: string }).kind ?? ""}'.`;
  addError(diagnostics, "ACV-RUN-901", path, error);
  addTrace(trace, path, (rule as { kind?: string }).kind ?? "", "unsupported", { error });
}

function executeFieldRule(
  root: unknown,
  item: unknown,
  rule: FieldRule,
  output: Record<string, unknown>,
  errors: string[],
  diagnostics: ConversionDiagnostic[],
  writeOwners: Map<string, string>,
  collisionPolicy: OutputCollisionPolicy,
  transforms: Record<string, (value: unknown) => unknown>,
  trace: ConversionTraceEntry[] | null,
  path: string
): void {
  const writePaths = getWritePaths(rule);
  if (writePaths.length === 0) {
    const error = `${path}: outputPaths is required.`;
    addError(diagnostics, "ACV-RUN-100", path, error);
    addTrace(trace, path, "field", "invalid", { error });
    return;
  }

  let value = resolveSourceValue(root, item, rule.source, errors, diagnostics, transforms, `${path}.source`);
  if ((value == null || value === "") && rule.defaultValue) {
    value = parsePrimitive(rule.defaultValue);
  }

  writePaths.forEach((writePath) => {
    writeValue(output, writeOwners, errors, diagnostics, collisionPolicy, path, writePath, value);
  });

  addTrace(trace, path, "field", "applied", { sourceValue: value, outputPaths: writePaths });
}

function executeArrayRule(
  root: unknown,
  item: unknown,
  rule: ArrayRule,
  output: Record<string, unknown>,
  errors: string[],
  diagnostics: ConversionDiagnostic[],
  writeOwners: Map<string, string>,
  collisionPolicy: OutputCollisionPolicy,
  transforms: Record<string, (value: unknown) => unknown>,
  trace: ConversionTraceEntry[] | null,
  path: string,
  depth: number
): void {
  const value = resolveSourceValue(root, item, { type: "path", path: rule.inputPath ?? "" }, errors, diagnostics, transforms, path);
  let items = Array.isArray(value) ? value : null;
  if (!items && rule.coerceSingle && value != null) {
    items = [value];
  }

  if (!items) {
    if (value == null) {
      const warning = `Array mapping skipped: inputPath "${rule.inputPath}" not found (${path}).`;
      addWarning(diagnostics, "ACV-RUN-101", path, warning);
      addTrace(trace, path, "array", "skipped", { sourceValue: value, warning });
    } else {
      const error = `${path}: input path did not resolve to an array (${rule.inputPath}).`;
      addError(diagnostics, "ACV-RUN-102", path, error);
      addTrace(trace, path, "array", "error", { sourceValue: value, error });
    }
    return;
  }

  const writePaths = getWritePaths(rule);
  if (writePaths.length === 0) {
    const error = `${path}: outputPaths is required.`;
    addError(diagnostics, "ACV-RUN-100", path, error);
    addTrace(trace, path, "array", "invalid", { sourceValue: value, error });
    return;
  }

  const mappedItems: unknown[] = [];
  items.forEach((arrayItem) => {
    const itemOutput: Record<string, unknown> = {};
    executeRules(
      root,
      arrayItem,
      rule.itemRules ?? [],
      itemOutput,
      errors,
      diagnostics,
      new Map<string, string>(),
      collisionPolicy,
      transforms,
      trace,
      `${path}.itemRules`,
      depth + 1
    );
    mappedItems.push(itemOutput);
  });

  writePaths.forEach((writePath) => {
    writeValue(output, writeOwners, errors, diagnostics, collisionPolicy, path, writePath, mappedItems);
  });

  addTrace(trace, path, "array", "mapped", { sourceValue: value, outputPaths: writePaths });
}

function executeBranchRule(
  root: unknown,
  item: unknown,
  rule: BranchRule,
  output: Record<string, unknown>,
  errors: string[],
  diagnostics: ConversionDiagnostic[],
  writeOwners: Map<string, string>,
  collisionPolicy: OutputCollisionPolicy,
  transforms: Record<string, (value: unknown) => unknown>,
  trace: ConversionTraceEntry[] | null,
  path: string,
  depth: number
): void {
  const matched = evaluateRuleCondition(root, item, rule.expression, errors, diagnostics, path, "branch expression");
  if (matched) {
    addTrace(trace, path, "branch", "then", { sourceValue: true, expression: rule.expression ?? undefined });
    executeRules(root, item, rule.then ?? [], output, errors, diagnostics, writeOwners, collisionPolicy, transforms, trace, `${path}.then`, depth + 1);
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
      diagnostics,
      branchPath,
      "branch expression"
    );

    if (!elseIfMatched) {
      continue;
    }

    addTrace(trace, path, "branch", `elseIf[${index}]`, { sourceValue: true, expression: elseIf.expression ?? undefined });
    executeRules(
      root,
      item,
      elseIf.then ?? [],
      output,
      errors,
      diagnostics,
      writeOwners,
      collisionPolicy,
      transforms,
      trace,
      `${branchPath}.then`,
      depth + 1
    );
    return;
  }

  if ((rule.else ?? []).length > 0) {
    addTrace(trace, path, "branch", "else", { sourceValue: false, expression: rule.expression ?? undefined });
    executeRules(root, item, rule.else ?? [], output, errors, diagnostics, writeOwners, collisionPolicy, transforms, trace, `${path}.else`, depth + 1);
    return;
  }

  addTrace(trace, path, "branch", "noMatch", { sourceValue: false, expression: rule.expression ?? undefined });
}

function writeValue(
  output: Record<string, unknown>,
  writeOwners: Map<string, string>,
  errors: string[],
  diagnostics: ConversionDiagnostic[],
  collisionPolicy: OutputCollisionPolicy,
  rulePath: string,
  outputPath: string,
  value: unknown
): void {
  const firstWriterPath = writeOwners.get(outputPath);
  if (!firstWriterPath) {
    writeOwners.set(outputPath, rulePath);
    setValueByPath(output, outputPath, value);
    return;
  }

  if (collisionPolicy === OutputCollisionPolicy.LastWriteWins) {
    writeOwners.set(outputPath, rulePath);
    setValueByPath(output, outputPath, value);
    return;
  }

  if (collisionPolicy === OutputCollisionPolicy.Error) {
    addError(
      diagnostics,
      "ACV-RUN-103",
      rulePath,
      `${rulePath}: output collision at "${outputPath}" (already written by ${firstWriterPath}).`
    );
  }
}

function evaluateRuleCondition(
  root: unknown,
  item: unknown,
  expression: string | null | undefined,
  errors: string[],
  diagnostics: ConversionDiagnostic[],
  errorContext: string,
  label: string
): boolean {
  if (!expression || expression.trim().length === 0) {
    addError(diagnostics, "ACV-RUN-301", errorContext, `${errorContext}: ${label} is required.`);
    return false;
  }

  const evaluation = tryEvaluateConditionExpression(expression, (path) => resolvePathValue(root, item, path));
  if (!evaluation.ok) {
    addError(
      diagnostics,
      "ACV-RUN-302",
      errorContext,
      `${errorContext}: invalid ${label} "${expression}": ${evaluation.error}`
    );
    return false;
  }

  return evaluation.value;
}

function addTrace(
  trace: ConversionTraceEntry[] | null,
  rulePath: string,
  ruleKind: string,
  decision: string,
  details: {
    sourceValue?: unknown;
    outputPaths?: string[];
    expression?: string;
    warning?: string;
    error?: string;
  } = {}
): void {
  if (!trace) {
    return;
  }

  trace.push({
    rulePath,
    ruleKind,
    decision,
    sourceValue: details.sourceValue,
    outputPaths: details.outputPaths ?? [],
    expression: details.expression,
    warning: details.warning,
    error: details.error
  });
}

function addError(
  diagnostics: ConversionDiagnostic[],
  code: string,
  rulePath: string,
  message: string
): void {
  diagnostics.push({ code, rulePath, message, severity: ConversionDiagnosticSeverity.Error });
}

function addWarning(
  diagnostics: ConversionDiagnostic[],
  code: string,
  rulePath: string,
  message: string
): void {
  diagnostics.push({ code, rulePath, message, severity: ConversionDiagnosticSeverity.Warning });
}
