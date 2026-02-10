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
  Concat = "concat"
}

export enum ConditionOperator {
  Exists = "exists",
  Equals = "equals",
  NotEquals = "notEquals",
  Includes = "includes",
  Gt = "gt",
  Lt = "lt"
}

export interface ConditionRule {
  path: string;
  operator: ConditionOperator;
  value?: string | null;
}

export interface ValueSource {
  type: string;
  path?: string | null;
  value?: string | null;
  transform?: TransformType | null;
  condition?: ConditionRule | null;
  trueValue?: string | null;
  falseValue?: string | null;
}

export interface FieldRule {
  outputPath: string;
  source: ValueSource;
  defaultValue?: string | null;
}

export interface ArrayRule {
  inputPath: string;
  outputPath: string;
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
    return { output: input ?? {}, errors: [] };
  }

  const output: Record<string, unknown> = {};
  const errors: string[] = [];

  applyFieldMappings(input, null, fieldMappings, output, errors, "Field");

  arrayMappings.forEach((arrayRule, index) => {
    const value = resolvePathValue(input, null, arrayRule.inputPath ?? "");
    let items = Array.isArray(value) ? value : null;
    if (!items && arrayRule.coerceSingle && value != null) {
      items = [value];
    }

    if (!items) {
      errors.push(
        `Array ${index + 1}: input path did not resolve to an array (${arrayRule.inputPath}).`
      );
      return;
    }

    const mappedItems: unknown[] = [];
    items.forEach((item) => {
      const itemOutput: Record<string, unknown> = {};
      applyFieldMappings(input, item, arrayRule.itemMappings ?? [], itemOutput, errors, `Array ${index + 1} item`);
      mappedItems.push(itemOutput);
    });

    if (!arrayRule.outputPath || arrayRule.outputPath.trim().length === 0) {
      errors.push(`Array ${index + 1}: output path is required.`);
      return;
    }

    const outputPath = normalizeWritePath(arrayRule.outputPath);
    if (!outputPath || outputPath.trim().length === 0) {
      errors.push(`Array ${index + 1}: output path is required.`);
      return;
    }

    setValueByPath(output, outputPath, mappedItems);
  });

  return { output, errors };
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
      source: rule.source ?? { type: "path" },
      defaultValue: rule.defaultValue ?? ""
    })),
    arrayMappings: (rules.arrayMappings ?? []).map((mapping) => ({
      inputPath: mapping.inputPath,
      outputPath: mapping.outputPath,
      coerceSingle: mapping.coerceSingle ?? false,
      itemMappings: (mapping.itemMappings ?? []).map((rule) => ({
        outputPath: rule.outputPath,
        source: rule.source ?? { type: "path" },
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
    if (!rule.outputPath || rule.outputPath.trim().length === 0) {
      errors.push(`${label} ${index + 1}: output path is required.`);
      return;
    }

    let value = resolveSourceValue(root, item, rule.source);
    if ((value == null || value === "") && rule.defaultValue) {
      value = parsePrimitive(rule.defaultValue);
    }
    setValueByPath(output, rule.outputPath, value);
  });
}

function resolveSourceValue(root: unknown, item: unknown, source: ValueSource): unknown {
  switch (source.type) {
    case "constant":
      return parsePrimitive(source.value ?? "");
    case "path":
      return resolvePathValue(root, item, source.path ?? "");
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

      const baseValue = resolvePathValue(root, item, source.path ?? "");
      return resolveTransform(baseValue, source.transform ?? TransformType.ToLowerCase);
    }
    case "condition": {
      if (!source.condition) {
        return null;
      }
      const matched = evaluateCondition(root, item, source.condition);
      const resolved = matched ? source.trueValue : source.falseValue;
      return parsePrimitive(resolved ?? "");
    }
    default:
      return null;
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

function evaluateCondition(root: unknown, item: unknown, condition: ConditionRule): boolean {
  const value = resolvePathValue(root, item, condition.path);
  const compareValue = condition.value != null ? parsePrimitive(condition.value) : null;
  switch (condition.operator) {
    case ConditionOperator.Exists:
      return value != null;
    case ConditionOperator.Equals:
      return Object.is(value, compareValue);
    case ConditionOperator.NotEquals:
      return !Object.is(value, compareValue);
    case ConditionOperator.Includes:
      return includesValue(value, compareValue);
    case ConditionOperator.Gt:
      return toNumber(value) > toNumber(compareValue);
    case ConditionOperator.Lt:
      return toNumber(value) < toNumber(compareValue);
    default:
      return false;
  }
}

function includesValue(value: unknown, compareValue: unknown): boolean {
  if (typeof value === "string" && typeof compareValue === "string") {
    return value.includes(compareValue);
  }
  if (Array.isArray(value)) {
    return value.some((item) => Object.is(item, compareValue));
  }
  return false;
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
