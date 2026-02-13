import { XMLBuilder, XMLParser } from "fast-xml-parser";

export enum DataFormat {
  Json = "json",
  Xml = "xml",
  Query = "query"
}

export enum TransformType {
  ToLowerCase = "toLowerCase",
  ToUpperCase = "toUpperCase",
  Number = "number",
  Boolean = "boolean",
  Concat = "concat",
  Split = "split"
}

export enum MergeMode {
  Concat = "concat",
  FirstNonEmpty = "firstNonEmpty",
  Array = "array"
}

export interface ValueSource {
  type: string;
  path?: string | null;
  paths?: string[] | null;
  value?: string | null;
  transform?: TransformType | null;
  expression?: string | null;
  trueValue?: string | null;
  falseValue?: string | null;
  mergeMode?: MergeMode | null;
  separator?: string | null;
  tokenIndex?: number | null;
  trimAfterSplit?: boolean | null;
}

export interface FieldRule {
  outputPath?: string;
  outputPaths?: string[] | null;
  source: ValueSource;
  defaultValue?: string | null;
}

export interface ArrayRule {
  inputPath: string;
  outputPath?: string;
  outputPaths?: string[] | null;
  itemMappings: FieldRule[];
  coerceSingle?: boolean;
}

export interface ConversionRules {
  version?: number;
  inputFormat?: DataFormat;
  outputFormat?: DataFormat;
  fieldMappings?: FieldRule[];
  arrayMappings?: ArrayRule[];
}

export interface LegacyMappingRow {
  outputPath: string;
  sourceType?: string | null;
  sourceValue?: string | null;
  transformType?: TransformType | null;
  defaultValue?: string | null;
}

export interface LegacyMappingConfig {
  version?: number;
  rows: LegacyMappingRow[];
}

export interface ConversionResult {
  output?: unknown;
  errors: string[];
  warnings: string[];
}

export interface ConversionRulesGenerationRequest {
  inputFormat: DataFormat;
  outputFormat: DataFormat;
  inputSample: string;
  outputSample: string;
  model?: string | null;
}

export interface ConversionRulesGenerator {
  generate(
    request: ConversionRulesGenerationRequest,
    options?: { signal?: AbortSignal }
  ): Promise<ConversionRules>;
}

export const rulesSchemaPath = "/schemas/rules/rules.schema.json";

export function normalizeConversionRules(raw: unknown): ConversionRules {
  if (raw && typeof raw === "object" && !Array.isArray(raw)) {
    if (isConversionRules(raw)) {
      return normalizeRules(raw);
    }
    if (isLegacyConfig(raw)) {
      return normalizeLegacyRules(raw);
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

export function applyConversion(input: unknown, rawRules: unknown): ConversionResult {
  const rules = normalizeConversionRules(rawRules);
  const fieldMappings = rules.fieldMappings ?? [];
  const arrayMappings = rules.arrayMappings ?? [];

  if (fieldMappings.length === 0 && arrayMappings.length === 0) {
    return { output: input ?? {}, errors: [], warnings: [] };
  }

  const output: Record<string, unknown> = {};
  const errors: string[] = [];
  const warnings: string[] = [];

  applyFieldMappings(input, null, fieldMappings, output, errors, "Field");

  arrayMappings.forEach((arrayRule, index) => {
    const value = resolvePathValue(input, null, arrayRule.inputPath ?? "");
    let items = Array.isArray(value) ? value : null;
    if (!items && arrayRule.coerceSingle && value != null) {
      items = [value];
    }

    if (!items) {
      if (value == null) {
        warnings.push(
          `Array mapping skipped: inputPath "${arrayRule.inputPath}" not found (arrayMappings[${index}]).`
        );
      } else {
        errors.push(
          `Array ${index + 1}: input path did not resolve to an array (${arrayRule.inputPath}).`
        );
      }
      return;
    }

    const mappedItems: unknown[] = [];
    items.forEach((item) => {
      const itemOutput: Record<string, unknown> = {};
      applyFieldMappings(input, item, arrayRule.itemMappings ?? [], itemOutput, errors, `Array ${index + 1} item`);
      mappedItems.push(itemOutput);
    });

    const arrayWritePaths = getArrayWritePaths(arrayRule);
    if (arrayWritePaths.length === 0) {
      errors.push(`Array ${index + 1}: output path is required.`);
      return;
    }

    arrayWritePaths.forEach((outputPath) => {
      setValueByPath(output, outputPath, mappedItems);
    });
  });

  return { output, errors, warnings };
}

export function parsePayload(text: string, format: DataFormat): { value: unknown; error?: string } {
  try {
    switch (format) {
      case DataFormat.Xml:
        return { value: parseXml(text) };
      case DataFormat.Query:
        return { value: parseQueryString(text) };
      default:
        return { value: parseJson(text) };
    }
  } catch (error) {
    return { value: null, error: error instanceof Error ? error.message : String(error) };
  }
}

export function formatPayload(value: unknown, format: DataFormat, pretty: boolean): string {
  switch (format) {
    case DataFormat.Xml:
      return formatXml(value, pretty);
    case DataFormat.Query:
      return formatQueryString(value);
    default:
      return JSON.stringify(sanitizeForJson(value ?? {}), null, pretty ? 2 : 0);
  }
}

export async function runConversionCase(args: {
  rulesText: string;
  inputText: string;
  inputExtension: string;
  outputExtension: string;
}): Promise<string> {
  const rules = normalizeConversionRules(args.rulesText);
  const inputFormat = extensionToFormat(args.inputExtension);
  const outputFormat = extensionToFormat(args.outputExtension);

  const inputValue = inputFormat ? parsePayloadOrThrow(args.inputText, inputFormat) : args.inputText;
  const result = applyConversion(inputValue, rules);

  if (result.errors.length > 0) {
    throw new Error(`Conversion errors: ${result.errors.join("; ")}`);
  }

  if (!outputFormat) {
    return result.output == null ? "" : String(result.output);
  }

  return formatPayload(result.output, outputFormat, outputFormat === DataFormat.Xml);
}

function parsePayloadOrThrow(text: string, format: DataFormat): unknown {
  const { value, error } = parsePayload(text, format);
  if (error) {
    throw new Error(error);
  }
  return value;
}

function extensionToFormat(extension: string): DataFormat | null {
  const ext = extension.toLowerCase();
  if (ext === "json") return DataFormat.Json;
  if (ext === "xml") return DataFormat.Xml;
  if (ext === "txt") return DataFormat.Query;
  return null;
}

function normalizeRules(rules: ConversionRules): ConversionRules {
  return {
    version: rules.version ?? 2,
    inputFormat: rules.inputFormat ?? DataFormat.Json,
    outputFormat: rules.outputFormat ?? DataFormat.Json,
    fieldMappings: (rules.fieldMappings ?? []).map((rule) => ({
      outputPath: rule.outputPath,
      outputPaths: rule.outputPaths ?? [],
      source: normalizeValueSource(rule.source),
      defaultValue: rule.defaultValue ?? ""
    })),
    arrayMappings: (rules.arrayMappings ?? []).map((mapping) => ({
      inputPath: mapping.inputPath,
      outputPath: mapping.outputPath,
      outputPaths: mapping.outputPaths ?? [],
      coerceSingle: mapping.coerceSingle ?? false,
      itemMappings: (mapping.itemMappings ?? []).map((rule) => ({
        outputPath: rule.outputPath,
        outputPaths: rule.outputPaths ?? [],
        source: normalizeValueSource(rule.source),
        defaultValue: rule.defaultValue ?? ""
      }))
    }))
  };
}

function normalizeLegacyRules(legacy: LegacyMappingConfig): ConversionRules {
  const rows = legacy.rows ?? [];
  const fieldMappings = rows.map((row) => {
    const sourceType = row.sourceType ?? "path";
    let source: ValueSource;
    if (sourceType === "constant") {
      source = { type: "constant", value: row.sourceValue ?? "" };
    } else if (sourceType === "transform") {
      source = {
        type: "transform",
        path: row.sourceValue ?? "",
        transform: row.transformType ?? TransformType.ToLowerCase
      };
    } else {
      source = { type: "path", path: row.sourceValue ?? "" };
    }
    return {
      outputPath: row.outputPath,
      source,
      defaultValue: row.defaultValue ?? ""
    };
  });

  return {
    version: 2,
    inputFormat: DataFormat.Json,
    outputFormat: DataFormat.Json,
    fieldMappings,
    arrayMappings: []
  };
}

function emptyRules(): ConversionRules {
  return {
    version: 2,
    inputFormat: DataFormat.Json,
    outputFormat: DataFormat.Json,
    fieldMappings: [],
    arrayMappings: []
  };
}

function normalizeValueSource(source: ValueSource | null | undefined): ValueSource {
  const normalized = source ?? { type: "path" };
  const expression = normalized.expression?.trim();
  return {
    ...normalized,
    paths: normalized.paths ?? [],
    expression: expression && expression.length > 0 ? expression : null
  };
}

function isConversionRules(value: unknown): value is ConversionRules {
  if (!value || typeof value !== "object" || Array.isArray(value)) {
    return false;
  }
  const record = value as Record<string, unknown>;
  return record.version === 2 || "fieldMappings" in record || "arrayMappings" in record;
}

function isLegacyConfig(value: unknown): value is LegacyMappingConfig {
  if (!value || typeof value !== "object" || Array.isArray(value)) {
    return false;
  }
  const record = value as Record<string, unknown>;
  return Array.isArray(record.rows);
}

function applyFieldMappings(
  root: unknown,
  item: unknown,
  mappings: FieldRule[],
  output: Record<string, unknown>,
  errors: string[],
  label: string
): void {
  mappings.forEach((rule, index) => {
    const writePaths = getWritePaths(rule);
    if (writePaths.length === 0) {
      errors.push(`${label} ${index + 1}: output path is required.`);
      return;
    }

    let value = resolveSourceValue(root, item, rule.source, errors, `${label} ${index + 1}`);
    if ((value == null || value === "") && rule.defaultValue) {
      value = parsePrimitive(rule.defaultValue);
    }
    writePaths.forEach((writePath) => {
      setValueByPath(output, writePath, value);
    });
  });
}

function resolveSourceValue(
  root: unknown,
  item: unknown,
  source: ValueSource,
  errors: string[],
  errorContext: string
): unknown {
  switch (source.type) {
    case "constant":
      return parsePrimitive(source.value ?? "");
    case "path":
      return resolvePathValue(root, item, source.path ?? "");
    case "merge":
      return resolveMergeSourceValue(root, item, source);
    case "transform": {
      if (source.transform === TransformType.Concat) {
        const tokens = (source.path ?? "")
          .split(",")
          .map((token) => token.trim())
          .filter((token) => token.length > 0);
        let result = "";
        for (const token of tokens) {
          if (token.toLowerCase().startsWith("const:")) {
            result += token.slice("const:".length);
            continue;
          }
          const resolved = resolvePathValue(root, item, token);
          result += resolved == null ? "" : String(resolved);
        }
        return result;
      }

      if (source.transform === TransformType.Split) {
        const baseValue = resolvePathValue(root, item, source.path ?? "");
        if (baseValue == null) {
          return null;
        }

        const separator = source.separator ?? " ";
        if (separator.length === 0) {
          return null;
        }
        let tokenIndex = source.tokenIndex ?? 0;
        const trimAfterSplit = source.trimAfterSplit ?? true;
        const rawTokens = String(baseValue).split(separator).filter((token) => token.length > 0);
        const tokens = trimAfterSplit
          ? rawTokens.map((token) => token.trim()).filter((token) => token.length > 0)
          : rawTokens;
        if (tokenIndex < 0) {
          tokenIndex = tokens.length + tokenIndex;
        }
        if (tokenIndex < 0 || tokenIndex >= tokens.length) {
          return null;
        }

        return tokens[tokenIndex];
      }

      const baseValue = resolvePathValue(root, item, source.path ?? "");
      return resolveTransform(baseValue, source.transform ?? TransformType.ToLowerCase);
    }
    case "condition": {
      if (!source.expression || source.expression.trim().length === 0) {
        errors.push(`${errorContext}: condition expression is required.`);
        return parsePrimitive(source.falseValue ?? "");
      }

      const evaluation = tryEvaluateConditionExpression(root, item, source.expression);
      const matched = evaluation.ok ? evaluation.value : false;
      if (!evaluation.ok) {
        errors.push(
          `${errorContext}: invalid condition expression "${source.expression}": ${evaluation.error}`
        );
      }

      const resolved = matched ? source.trueValue : source.falseValue;
      return parsePrimitive(resolved ?? "");
    }
    default:
      return null;
  }
}

function resolveMergeSourceValue(root: unknown, item: unknown, source: ValueSource): unknown {
  const values = (source.paths ?? [])
    .filter((path) => !!path && path.trim().length > 0)
    .map((path) => resolvePathValue(root, item, path));

  const mode = source.mergeMode ?? MergeMode.Concat;
  switch (mode) {
    case MergeMode.FirstNonEmpty:
      return values.find((value) => value != null && (typeof value !== "string" || value.length > 0)) ?? null;
    case MergeMode.Array:
      return values;
    default:
      return values.map((value) => (value == null ? "" : String(value))).join(source.separator ?? "");
  }
}

function resolvePathValue(root: unknown, item: unknown, path: string): unknown {
  if (!path || path.trim().length === 0) return null;
  if (path === "$") return root;
  if (path.startsWith("$.", 0)) {
    return getValueByPath(root, path.slice(2));
  }
  if (path.startsWith("$[", 0)) {
    return getValueByPath(root, path.slice(1));
  }
  if (item != null) {
    return getValueByPath(item, path);
  }
  return getValueByPath(root, path);
}

function tryEvaluateConditionExpression(
  root: unknown,
  item: unknown,
  expression: string
): { ok: true; value: boolean } | { ok: false; error: string } {
  try {
    const parser = new ConditionExpressionParser(expression);
    const ast = parser.parse();
    return { ok: true, value: toBoolean(evaluateConditionExpressionNode(ast, root, item)) };
  } catch (error) {
    return { ok: false, error: error instanceof Error ? error.message : String(error) };
  }
}

function evaluateConditionExpressionNode(
  node: ConditionExpressionNode,
  root: unknown,
  item: unknown
): unknown {
  switch (node.kind) {
    case "literal":
      return node.value;
    case "path":
      return resolvePathValue(root, item, node.path);
    case "array":
      return node.items.map((entry) => evaluateConditionExpressionNode(entry, root, item));
    case "exists":
      return evaluateConditionExpressionNode(node.argument, root, item) != null;
    case "unary":
      return !toBoolean(evaluateConditionExpressionNode(node.operand, root, item));
    case "binary":
      return evaluateConditionBinaryNode(node, root, item);
    default:
      throw new Error("Unsupported condition expression node.");
  }
}

function evaluateConditionBinaryNode(
  node: Extract<ConditionExpressionNode, { kind: "binary" }>,
  root: unknown,
  item: unknown
): unknown {
  if (node.operator === "&&") {
    return (
      toBoolean(evaluateConditionExpressionNode(node.left, root, item)) &&
      toBoolean(evaluateConditionExpressionNode(node.right, root, item))
    );
  }

  if (node.operator === "||") {
    return (
      toBoolean(evaluateConditionExpressionNode(node.left, root, item)) ||
      toBoolean(evaluateConditionExpressionNode(node.right, root, item))
    );
  }

  const left = evaluateConditionExpressionNode(node.left, root, item);
  const right = evaluateConditionExpressionNode(node.right, root, item);

  switch (node.operator) {
    case "==":
      return Object.is(left, right);
    case "!=":
      return !Object.is(left, right);
    case ">":
      return toNumber(left) > toNumber(right);
    case ">=":
      return toNumber(left) >= toNumber(right);
    case "<":
      return toNumber(left) < toNumber(right);
    case "<=":
      return toNumber(left) <= toNumber(right);
    case "in":
      return Array.isArray(right) ? right.some((entry) => Object.is(left, entry)) : false;
    default:
      throw new Error(`Unsupported condition operator '${node.operator}'.`);
  }
}

function toBoolean(value: unknown): boolean {
  if (value == null) return false;
  if (typeof value === "boolean") return value;
  if (typeof value === "string") return value.length > 0;
  return true;
}

type ConditionExpressionNode =
  | { kind: "literal"; value: unknown }
  | { kind: "path"; path: string }
  | { kind: "array"; items: ConditionExpressionNode[] }
  | { kind: "exists"; argument: ConditionExpressionNode }
  | { kind: "unary"; operand: ConditionExpressionNode }
  | {
      kind: "binary";
      operator: "==" | "!=" | ">" | ">=" | "<" | "<=" | "in" | "&&" | "||";
      left: ConditionExpressionNode;
      right: ConditionExpressionNode;
    };

type ConditionTokenType =
  | "identifier"
  | "number"
  | "string"
  | "operator"
  | "leftParen"
  | "rightParen"
  | "leftBracket"
  | "rightBracket"
  | "comma"
  | "end";

interface ConditionToken {
  type: ConditionTokenType;
  text: string;
  position: number;
}

class ConditionExpressionParser {
  private readonly tokens: ConditionToken[];
  private index = 0;

  constructor(private readonly expression: string) {
    this.tokens = tokenizeConditionExpression(expression);
  }

  parse(): ConditionExpressionNode {
    const node = this.parseOr();
    this.expect("end");
    return node;
  }

  private parseOr(): ConditionExpressionNode {
    let left = this.parseAnd();
    while (this.matchOperator("||") || this.matchIdentifier("or")) {
      left = { kind: "binary", operator: "||", left, right: this.parseAnd() };
    }
    return left;
  }

  private parseAnd(): ConditionExpressionNode {
    let left = this.parseComparison();
    while (this.matchOperator("&&") || this.matchIdentifier("and")) {
      left = { kind: "binary", operator: "&&", left, right: this.parseComparison() };
    }
    return left;
  }

  private parseComparison(): ConditionExpressionNode {
    let left = this.parseUnary();

    while (true) {
      const operator = this.parseComparisonOperator();
      if (!operator) {
        return left;
      }

      const right = this.parseUnary();
      if (operator === "in" && right.kind !== "array") {
        throw this.error("Right-hand side of 'in' must be an array literal.");
      }
      left = { kind: "binary", operator, left, right };
    }
  }

  private parseComparisonOperator():
    | "=="
    | "!="
    | ">"
    | ">="
    | "<"
    | "<="
    | "in"
    | null {
    if (this.matchOperator("==")) return "==";
    if (this.matchOperator("!=")) return "!=";
    if (this.matchOperator(">=")) return ">=";
    if (this.matchOperator("<=")) return "<=";
    if (this.matchOperator(">")) return ">";
    if (this.matchOperator("<")) return "<";
    if (this.matchIdentifier("eq")) return "==";
    if (this.matchNotEqAlias()) return "!=";
    if (this.matchIdentifier("gt")) return ">";
    if (this.matchIdentifier("gte")) return ">=";
    if (this.matchIdentifier("lt")) return "<";
    if (this.matchIdentifier("lte")) return "<=";
    if (this.matchIdentifier("in")) return "in";
    return null;
  }

  private matchNotEqAlias(): boolean {
    if (!this.peekIdentifier("not")) return false;
    const next = this.peek(1);
    if (next.type !== "identifier" || next.text.toLowerCase() !== "eq") {
      return false;
    }
    this.index += 2;
    return true;
  }

  private parseUnary(): ConditionExpressionNode {
    if (this.matchOperator("!") || this.matchIdentifier("not")) {
      return { kind: "unary", operand: this.parseUnary() };
    }
    return this.parsePrimary();
  }

  private parsePrimary(): ConditionExpressionNode {
    if (this.match("leftParen")) {
      const node = this.parseOr();
      this.expect("rightParen");
      return node;
    }

    if (this.match("leftBracket")) {
      const items: ConditionExpressionNode[] = [];
      if (!this.match("rightBracket")) {
        do {
          items.push(this.parseOr());
        } while (this.match("comma"));
        this.expect("rightBracket");
      }
      return { kind: "array", items };
    }

    if (this.current.type === "number") {
      const text = this.current.text;
      this.index += 1;
      const number = Number.parseFloat(text);
      if (Number.isNaN(number) || !isFinite(number)) {
        throw this.error(`Invalid number literal '${text}'.`);
      }
      return { kind: "literal", value: number };
    }

    if (this.current.type === "string") {
      const text = this.current.text;
      this.index += 1;
      return { kind: "literal", value: text };
    }

    if (this.current.type === "identifier") {
      const identifier = this.current.text;
      this.index += 1;
      const lowered = identifier.toLowerCase();

      if (lowered === "true") return { kind: "literal", value: true };
      if (lowered === "false") return { kind: "literal", value: false };
      if (lowered === "null") return { kind: "literal", value: null };
      if (lowered === "path") return this.parsePathCall();
      if (lowered === "exists") return this.parseExistsCall();

      throw this.error(`Unexpected identifier '${identifier}'. Use path(...) for value references.`);
    }

    throw this.error(`Unexpected token '${this.current.text}'.`);
  }

  private parsePathCall(): ConditionExpressionNode {
    this.expect("leftParen");
    if (this.current.type !== "identifier" && this.current.type !== "string") {
      throw this.error("path(...) requires a path reference.");
    }
    const path = this.current.text;
    this.index += 1;
    this.expect("rightParen");
    return { kind: "path", path };
  }

  private parseExistsCall(): ConditionExpressionNode {
    this.expect("leftParen");
    const argument = this.parseOr();
    this.expect("rightParen");
    return { kind: "exists", argument };
  }

  private get current(): ConditionToken {
    return this.peek(0);
  }

  private peek(offset: number): ConditionToken {
    const cursor = this.index + offset;
    return cursor >= this.tokens.length ? this.tokens[this.tokens.length - 1] : this.tokens[cursor];
  }

  private match(type: ConditionTokenType): boolean {
    if (this.current.type !== type) return false;
    this.index += 1;
    return true;
  }

  private expect(type: ConditionTokenType): void {
    if (!this.match(type)) {
      throw this.error(`Expected ${type} but found '${this.current.text}'.`);
    }
  }

  private matchOperator(operator: string): boolean {
    if (this.current.type !== "operator" || this.current.text !== operator) return false;
    this.index += 1;
    return true;
  }

  private matchIdentifier(identifier: string): boolean {
    if (!this.peekIdentifier(identifier)) return false;
    this.index += 1;
    return true;
  }

  private peekIdentifier(identifier: string): boolean {
    return (
      this.current.type === "identifier" && this.current.text.toLowerCase() === identifier.toLowerCase()
    );
  }

  private error(message: string): Error {
    return new Error(`${message} (position ${this.current.position}).`);
  }
}

function tokenizeConditionExpression(expression: string): ConditionToken[] {
  const tokens: ConditionToken[] = [];
  let index = 0;

  while (index < expression.length) {
    const current = expression[index];
    if (/\s/.test(current)) {
      index += 1;
      continue;
    }

    if (current === "'") {
      const parsed = readQuotedConditionString(expression, index);
      tokens.push({ type: "string", text: parsed.value, position: index });
      index = parsed.nextIndex;
      continue;
    }

    if (/\d/.test(current) || (current === "." && index + 1 < expression.length && /\d/.test(expression[index + 1]))) {
      const start = index;
      index += 1;
      while (index < expression.length && /[\d.]/.test(expression[index])) {
        index += 1;
      }
      tokens.push({ type: "number", text: expression.slice(start, index), position: start });
      continue;
    }

    const twoChar = expression.slice(index, index + 2);
    if (["&&", "||", "==", "!=", ">=", "<="].includes(twoChar)) {
      tokens.push({ type: "operator", text: twoChar, position: index });
      index += 2;
      continue;
    }

    if (["!", ">", "<"].includes(current)) {
      tokens.push({ type: "operator", text: current, position: index });
      index += 1;
      continue;
    }

    if (current === "(") {
      tokens.push({ type: "leftParen", text: current, position: index });
      index += 1;
      continue;
    }

    if (current === ")") {
      tokens.push({ type: "rightParen", text: current, position: index });
      index += 1;
      continue;
    }

    if (current === "[") {
      tokens.push({ type: "leftBracket", text: current, position: index });
      index += 1;
      continue;
    }

    if (current === "]") {
      tokens.push({ type: "rightBracket", text: current, position: index });
      index += 1;
      continue;
    }

    if (current === ",") {
      tokens.push({ type: "comma", text: current, position: index });
      index += 1;
      continue;
    }

    if (isConditionIdentifierStart(current)) {
      const start = index;
      index += 1;
      while (index < expression.length && isConditionIdentifierPart(expression[index])) {
        index += 1;
      }
      tokens.push({ type: "identifier", text: expression.slice(start, index), position: start });
      continue;
    }

    throw new Error(`Unexpected character '${current}' at position ${index}.`);
  }

  tokens.push({ type: "end", text: "", position: expression.length });
  return tokens;
}

function isConditionIdentifierStart(value: string): boolean {
  return /[A-Za-z_$]/.test(value);
}

function isConditionIdentifierPart(value: string): boolean {
  return /[A-Za-z0-9_.$[\]]/.test(value);
}

function readQuotedConditionString(
  expression: string,
  startIndex: number
): { value: string; nextIndex: number } {
  let output = "";
  let cursor = startIndex + 1;

  while (cursor < expression.length) {
    const current = expression[cursor];
    if (current === "'") {
      return { value: output, nextIndex: cursor + 1 };
    }

    if (current === "\\" && cursor + 1 < expression.length) {
      cursor += 1;
      output += expression[cursor];
      cursor += 1;
      continue;
    }

    output += current;
    cursor += 1;
  }

  throw new Error(`Unterminated string literal at position ${startIndex}.`);
}

function resolveTransform(value: unknown, transform: TransformType): unknown {
  switch (transform) {
    case TransformType.ToLowerCase:
      return typeof value === "string" ? value.toLowerCase() : value;
    case TransformType.ToUpperCase:
      return typeof value === "string" ? value.toUpperCase() : value;
    case TransformType.Number:
      return value == null || value === "" ? value : toNumber(value);
    case TransformType.Boolean:
      if (typeof value === "boolean") return value;
      if (typeof value === "string") {
        return ["true", "1", "yes", "y"].includes(value.toLowerCase());
      }
      return value != null;
    default:
      return value;
  }
}

function getValueByPath(input: unknown, path: string): unknown {
  if (input == null || !path || path.trim().length === 0) return null;
  const parts = path.split(".").map((part) => part.trim());
  let current: unknown = input;
  for (const part of parts) {
    if (current == null) return null;

    const arrayMatch = part.match(/^(\w+)\[(\d+)\]$/);
    if (arrayMatch) {
      const key = arrayMatch[1];
      const index = Number.parseInt(arrayMatch[2], 10);
      if (!isRecord(current) || !(key in current)) return null;
      const next = (current as Record<string, unknown>)[key];
      if (!Array.isArray(next) || index >= next.length) return null;
      current = next[index];
      continue;
    }

    if (/^\d+$/.test(part)) {
      const index = Number.parseInt(part, 10);
      if (!Array.isArray(current) || index >= current.length) return null;
      current = current[index];
      continue;
    }

    if (!isRecord(current) || !(part in current)) return null;
    current = (current as Record<string, unknown>)[part];
  }
  return current;
}

function setValueByPath(target: Record<string, unknown>, path: string, value: unknown): void {
  const parts = path.split(".").map((part) => part.trim());
  let current: Record<string, unknown> = target;
  for (let index = 0; index < parts.length; index += 1) {
    const part = parts[index];
    const isLast = index === parts.length - 1;
    if (isLast) {
      current[part] = value;
      return;
    }
    const next = current[part];
    if (!isRecord(next)) {
      current[part] = {};
    }
    current = current[part] as Record<string, unknown>;
  }
}

function normalizeWritePath(path: string): string {
  if (!path || path.trim().length === 0) {
    return path;
  }

  if (path.startsWith("$.", 0)) {
    return path.slice(2);
  }

  if (path === "$") {
    return "";
  }

  return path;
}

function getWritePaths(rule: FieldRule): string[] {
  const paths: string[] = [];

  if (rule.outputPath && rule.outputPath.trim().length > 0) {
    paths.push(rule.outputPath);
  }

  (rule.outputPaths ?? []).forEach((path) => {
    if (path && path.trim().length > 0) {
      paths.push(path);
    }
  });

  return Array.from(new Set(paths));
}

function getArrayWritePaths(rule: ArrayRule): string[] {
  const paths: string[] = [];

  if (rule.outputPath && rule.outputPath.trim().length > 0) {
    const normalized = normalizeWritePath(rule.outputPath);
    if (normalized && normalized.trim().length > 0) {
      paths.push(normalized);
    }
  }

  (rule.outputPaths ?? []).forEach((path) => {
    if (path && path.trim().length > 0) {
      const normalized = normalizeWritePath(path);
      if (normalized && normalized.trim().length > 0) {
        paths.push(normalized);
      }
    }
  });

  return Array.from(new Set(paths));
}

function parsePrimitive(value: string): unknown {
  if (value === "true") return true;
  if (value === "false") return false;
  if (value === "null") return null;
  if (value === "undefined") return null;
  const parsed = Number.parseFloat(value);
  if (!Number.isNaN(parsed) && isFinite(parsed)) return parsed;
  return value;
}

function toNumber(value: unknown): number {
  if (value == null) return Number.NaN;
  if (typeof value === "number") return value;
  if (typeof value === "string") {
    const parsed = Number.parseFloat(value);
    return Number.isNaN(parsed) ? Number.NaN : parsed;
  }
  if (typeof value === "boolean") return value ? 1 : 0;
  const coerced = Number(value);
  return Number.isNaN(coerced) ? Number.NaN : coerced;
}

function parseJson(text: string): unknown {
  return JSON.parse(text);
}

function parseXml(text: string): Record<string, unknown> {
  const parser = new XMLParser({
    ignoreAttributes: false,
    attributeNamePrefix: "@_",
    textNodeName: "#text",
    trimValues: false,
    parseTagValue: false,
    parseAttributeValue: false
  });

  const parsed = parser.parse(text) as Record<string, unknown>;
  return normalizeXmlValue(parsed) as Record<string, unknown>;
}

function formatXml(value: unknown, pretty: boolean): string {
  if (!isRecord(value) || Object.keys(value).length === 0) {
    return new XMLBuilder({
      ignoreAttributes: false,
      attributeNamePrefix: "@_",
      textNodeName: "#text",
      format: pretty,
      indentBy: "  "
    }).build({ root: null });
  }

  const builder = new XMLBuilder({
    ignoreAttributes: false,
    attributeNamePrefix: "@_",
    textNodeName: "#text",
    format: pretty,
    indentBy: "  "
  });

  if (!isRecord(value) || Object.keys(value).length === 0) {
    return builder.build({ root: null });
  }

  const keys = Object.keys(value);
  if (keys.length === 1) {
    const rootKey = keys[0];
    return builder.build({ [rootKey]: value[rootKey] });
  }

  return builder.build({ root: value });
}

function normalizeXmlValue(value: unknown): unknown {
  if (Array.isArray(value)) {
    return value.map((item) => normalizeXmlValue(item));
  }

  if (isRecord(value)) {
    const normalized: Record<string, unknown> = {};
    for (const [key, child] of Object.entries(value)) {
      if (key === "#text" && typeof child === "string") {
        normalized[key] = parsePrimitive(child.trim());
        continue;
      }
      if (typeof child === "string") {
        normalized[key] = parsePrimitive(child.trim());
        continue;
      }
      normalized[key] = normalizeXmlValue(child);
    }
    return normalized;
  }

  if (typeof value === "string") {
    return parsePrimitive(value.trim());
  }

  return value;
}

function parseQueryString(text: string): Record<string, unknown> {
  const trimmed = text.trim();
  if (!trimmed) return {};
  const query = trimmed.startsWith("?") ? trimmed.slice(1) : trimmed;
  const result: Record<string, unknown> = {};
  const pairs = query.split("&").filter((pair) => pair.length > 0);

  for (const pair of pairs) {
    const splitIndex = pair.indexOf("=");
    const rawKey = splitIndex >= 0 ? pair.slice(0, splitIndex) : pair;
    const rawValue = splitIndex >= 0 ? pair.slice(splitIndex + 1) : "";
    const key = decodeURIComponent(rawKey.replace(/\+/g, " "));
    const value = decodeURIComponent(rawValue.replace(/\+/g, " "));
    const path = parseQueryKey(key);
    setQueryValue(result, path, value);
  }

  return result;
}

function formatQueryString(value: unknown): string {
  if (!isRecord(value)) {
    throw new Error("Query output must be an object.");
  }

  const pairs: Array<{ key: string; value: string }> = [];
  const keys = Object.keys(value).sort((a, b) => a.localeCompare(b));
  keys.forEach((key) => {
    addQueryPair(pairs, key, value[key]);
  });

  return pairs
    .map((pair) => `${encodeURIComponent(pair.key)}=${encodeURIComponent(pair.value)}`)
    .join("&");
}

function parseQueryKey(key: string): Array<string | number> {
  const parts: Array<string | number> = [];
  const matcher = /([^\[.\]]+)|\[(.*?)\]/g;
  let match: RegExpExecArray | null;
  while ((match = matcher.exec(key)) !== null) {
    const token = match[1] ?? match[2] ?? "";
    if (!token) continue;
    const number = Number.parseInt(token, 10);
    if (!Number.isNaN(number) && number.toString() === token) {
      parts.push(number);
    } else {
      parts.push(token);
    }
  }
  if (parts.length === 0) {
    parts.push(key);
  }
  return parts;
}

function setQueryValue(target: Record<string, unknown>, path: Array<string | number>, value: string): void {
  let current: unknown = target;
  let parent: unknown = null;
  let parentKey: string | number | null = null;

  const ensureObjectContainer = (valueToCheck: unknown): Record<string, unknown> => {
    if (isRecord(valueToCheck)) return valueToCheck;
    const next: Record<string, unknown> = {};
    if (parent != null && parentKey != null) {
      if (Array.isArray(parent)) {
        parent[parentKey as number] = next;
      } else if (isRecord(parent)) {
        parent[parentKey.toString()] = next;
      }
    }
    return next;
  };

  const ensureArrayContainer = (valueToCheck: unknown): unknown[] => {
    if (Array.isArray(valueToCheck)) return valueToCheck;
    const next: unknown[] = [];
    if (parent != null && parentKey != null) {
      if (Array.isArray(parent)) {
        parent[parentKey as number] = next;
      } else if (isRecord(parent)) {
        parent[parentKey.toString()] = next;
      }
    }
    return next;
  };

  path.forEach((segment, index) => {
    const isLast = index === path.length - 1;
    const nextSegment = index + 1 < path.length ? path[index + 1] : null;

    if (typeof segment === "number") {
      const arrayContainer = ensureArrayContainer(current);
      if (isLast) {
        const existing = arrayContainer[segment];
        if (existing == null) {
          ensureListSize(arrayContainer, segment + 1);
          arrayContainer[segment] = value;
        } else if (Array.isArray(existing)) {
          existing.push(value);
        } else {
          arrayContainer[segment] = [existing, value];
        }
        return;
      }

      parent = arrayContainer;
      parentKey = segment;
      ensureListSize(arrayContainer, segment + 1);
      const existingValue = arrayContainer[segment];
      const shouldBeArray = typeof nextSegment === "number";
      if (
        existingValue == null ||
        (shouldBeArray && !Array.isArray(existingValue)) ||
        (!shouldBeArray && !isRecord(existingValue))
      ) {
        arrayContainer[segment] = shouldBeArray ? [] : {};
      }
      current = arrayContainer[segment];
      return;
    }

    const objectContainer = ensureObjectContainer(current);
    const key = segment;
    if (isLast) {
      const existing = objectContainer[key];
      if (existing === undefined) {
        objectContainer[key] = value;
      } else if (Array.isArray(existing)) {
        existing.push(value);
      } else {
        objectContainer[key] = [existing, value];
      }
      return;
    }

    parent = objectContainer;
    parentKey = key;
    const next = objectContainer[key];
    const shouldBeArray = typeof nextSegment === "number";
    if (next == null || (shouldBeArray && !Array.isArray(next)) || (!shouldBeArray && !isRecord(next))) {
      objectContainer[key] = shouldBeArray ? [] : {};
    }
    current = objectContainer[key];
  });
}

function ensureListSize(list: unknown[], size: number): void {
  while (list.length < size) {
    list.push(null);
  }
}

function addQueryPair(pairs: Array<{ key: string; value: string }>, key: string, value: unknown): void {
  if (value == null) {
    pairs.push({ key, value: "" });
    return;
  }

  if (Array.isArray(value)) {
    value.forEach((item) => {
      if (isRecord(item) || Array.isArray(item)) {
        pairs.push({ key, value: JSON.stringify(sanitizeForJson(item)) });
      } else if (item == null) {
        pairs.push({ key, value: "" });
      } else {
        pairs.push({ key, value: String(item) });
      }
    });
    return;
  }

  if (isRecord(value)) {
    const keys = Object.keys(value).sort((a, b) => a.localeCompare(b));
    keys.forEach((childKey) => {
      const nextKey = key.length === 0 ? childKey : `${key}.${childKey}`;
      addQueryPair(pairs, nextKey, value[childKey]);
    });
    return;
  }

  if (!key) return;
  pairs.push({ key, value: String(value) });
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return !!value && typeof value === "object" && !Array.isArray(value);
}

function sanitizeForJson(value: unknown): unknown {
  if (Array.isArray(value)) {
    return value.map((item) => sanitizeForJson(item));
  }

  if (isRecord(value)) {
    const result: Record<string, unknown> = {};
    Object.entries(value).forEach(([key, child]) => {
      if (child === null) {
        return;
      }
      result[key] = sanitizeForJson(child);
    });
    return result;
  }

  return value;
}
