import { XMLBuilder, XMLParser } from "fast-xml-parser";

export type MappingSourceType = "path" | "constant" | "transform";
export type TransformType =
  | "toLowerCase"
  | "toUpperCase"
  | "number"
  | "boolean"
  | "concat";

export type MappingRow = {
  outputPath: string;
  sourceType: MappingSourceType;
  sourceValue: string;
  transformType?: TransformType;
  defaultValue?: string;
};

export type MappingConfig = {
  rows: MappingRow[];
};

export type DataFormat = "json" | "xml" | "query";

export type ConditionOperator =
  | "exists"
  | "equals"
  | "notEquals"
  | "includes"
  | "gt"
  | "lt";

export type ConditionRule = {
  path: string;
  operator: ConditionOperator;
  value?: string;
};

export type ValueSource =
  | { type: "path"; path: string }
  | { type: "constant"; value: string }
  | { type: "transform"; path: string; transform: TransformType }
  | {
      type: "condition";
      condition: ConditionRule;
      trueValue: string;
      falseValue: string;
    };

export type FieldRule = {
  outputPath: string;
  source: ValueSource;
  defaultValue: string;
};

export type ArrayRule = {
  inputPath: string;
  outputPath: string;
  itemMappings: FieldRule[];
  coerceSingle: boolean;
};

export type ConversionRules = {
  version: 2;
  inputFormat: DataFormat;
  outputFormat: DataFormat;
  fieldMappings: FieldRule[];
  arrayMappings: ArrayRule[];
};

export type MappingResult = {
  output: unknown;
  errors: string[];
};

type JsonValue =
  | Record<string, unknown>
  | unknown[]
  | string
  | number
  | boolean
  | null;

const xmlParser = new XMLParser({
  ignoreAttributes: false,
  attributeNamePrefix: "@_",
  allowBooleanAttributes: true,
  parseTagValue: true,
  parseAttributeValue: true,
  trimValues: true,
});

const xmlBuilderPretty = new XMLBuilder({
  ignoreAttributes: false,
  attributeNamePrefix: "@_",
  format: true,
});

const xmlBuilderCompact = new XMLBuilder({
  ignoreAttributes: false,
  attributeNamePrefix: "@_",
  format: false,
});

function isRecord(value: unknown): value is Record<string, unknown> {
  return !!value && typeof value === "object" && !Array.isArray(value);
}

type QueryPathSegment = string | number;

function parseQueryKey(key: string): QueryPathSegment[] {
  const parts: QueryPathSegment[] = [];
  const matcher = /([^[.\]]+)|\[(.*?)\]/g;
  let match: RegExpExecArray | null;
  while ((match = matcher.exec(key)) !== null) {
    const token = match[1] ?? match[2] ?? "";
    if (token === "") continue;
    if (/^\d+$/.test(token)) {
      parts.push(Number(token));
    } else {
      parts.push(token);
    }
  }
  return parts.length ? parts : [key];
}

function setQueryValue(
  target: Record<string, unknown>,
  path: QueryPathSegment[],
  value: string
) {
  let current: unknown = target;
  let parent: Record<string, unknown> | unknown[] | null = null;
  let parentKey: QueryPathSegment | null = null;

  function ensureObjectContainer(
    valueToCheck: unknown
  ): Record<string, unknown> {
    if (isRecord(valueToCheck)) return valueToCheck;
    const next: Record<string, unknown> = {};
    if (parent && parentKey !== null) {
      if (Array.isArray(parent)) {
        parent[parentKey as number] = next;
      } else {
        parent[parentKey as string] = next;
      }
    }
    return next;
  }

  function ensureArrayContainer(valueToCheck: unknown): unknown[] {
    if (Array.isArray(valueToCheck)) return valueToCheck;
    const next: unknown[] = [];
    if (parent && parentKey !== null) {
      if (Array.isArray(parent)) {
        parent[parentKey as number] = next;
      } else {
        parent[parentKey as string] = next;
      }
    }
    return next;
  }

  path.forEach((segment, index) => {
    const isLast = index === path.length - 1;
    const nextSegment = path[index + 1];
    if (typeof segment === "number") {
      const arrayContainer = ensureArrayContainer(current);
      if (isLast) {
        const existing = arrayContainer[segment];
        if (existing === undefined) {
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
      const existing = arrayContainer[segment];
      const shouldBeArray = typeof nextSegment === "number";
      if (
        existing === undefined ||
        (shouldBeArray && !Array.isArray(existing)) ||
        (!shouldBeArray && !isRecord(existing))
      ) {
        arrayContainer[segment] = shouldBeArray ? [] : {};
      }
      current = arrayContainer[segment];
      return;
    }

    const objectContainer = ensureObjectContainer(current);
    if (isLast) {
      const existing = objectContainer[segment];
      if (existing === undefined) {
        objectContainer[segment] = value;
      } else if (Array.isArray(existing)) {
        existing.push(value);
      } else {
        objectContainer[segment] = [existing, value];
      }
      return;
    }
    parent = objectContainer;
    parentKey = segment;
    const existing = objectContainer[segment];
    const shouldBeArray = typeof nextSegment === "number";
    if (
      existing === undefined ||
      (shouldBeArray && !Array.isArray(existing)) ||
      (!shouldBeArray && !isRecord(existing))
    ) {
      objectContainer[segment] = shouldBeArray ? [] : {};
    }
    current = objectContainer[segment];
  });
}

function parseQueryString(text: string): Record<string, unknown> {
  const trimmed = text.trim();
  if (!trimmed) return {};
  const search = trimmed.startsWith("?") ? trimmed.slice(1) : trimmed;
  const params = new URLSearchParams(search);
  const result: Record<string, unknown> = {};
  params.forEach((value, key) => {
    const path = parseQueryKey(key);
    setQueryValue(result, path, value);
  });
  return result;
}

function addQueryPair(
  pairs: Array<[string, string]>,
  key: string,
  value: unknown
) {
  if (value === undefined) return;
  if (value === null) {
    pairs.push([key, ""]);
    return;
  }
  if (Array.isArray(value)) {
    value.forEach((item) => {
      if (isRecord(item) || Array.isArray(item)) {
        pairs.push([key, JSON.stringify(item)]);
      } else if (item === null || item === undefined) {
        pairs.push([key, ""]);
      } else {
        pairs.push([key, String(item)]);
      }
    });
    return;
  }
  if (isRecord(value)) {
    const keys = Object.keys(value).sort();
    keys.forEach((childKey) => {
      addQueryPair(
        pairs,
        key ? `${key}.${childKey}` : childKey,
        value[childKey]
      );
    });
    return;
  }
  if (!key) return;
  pairs.push([key, String(value)]);
}

function formatQueryString(value: unknown): string {
  if (!isRecord(value)) {
    throw new Error("Query output must be an object.");
  }
  const pairs: Array<[string, string]> = [];
  const keys = Object.keys(value).sort();
  keys.forEach((key) => {
    addQueryPair(pairs, key, value[key]);
  });
  const params = new URLSearchParams();
  pairs.forEach(([key, val]) => params.append(key, val));
  return params.toString();
}

function getValueByPath(input: unknown, path: string): unknown {
  if (!path) return undefined;
  const parts = path.split(".").map((part) => part.trim());
  let current: JsonValue | undefined = input as JsonValue;
  for (const part of parts) {
    if (current == null) return undefined;
    const arrayMatch = part.match(/^(\w+)\[(\d+)\]$/);
    if (arrayMatch) {
      const [, key, index] = arrayMatch;
      if (!isRecord(current)) return undefined;
      const next = current[key];
      if (!Array.isArray(next)) return undefined;
      current = next[Number(index)] as JsonValue;
    } else if (/^\d+$/.test(part)) {
      if (!Array.isArray(current)) return undefined;
      current = current[Number(part)] as JsonValue;
    } else {
      if (!isRecord(current)) return undefined;
      current = current[part] as JsonValue;
    }
  }
  return current;
}

function setValueByPath(
  target: Record<string, unknown>,
  path: string,
  value: unknown
) {
  const parts = path.split(".").map((part) => part.trim());
  let current: Record<string, unknown> = target;
  parts.forEach((part, index) => {
    const isLast = index === parts.length - 1;
    if (isLast) {
      current[part] = value;
      return;
    }
    if (!isRecord(current[part])) {
      current[part] = {} as Record<string, unknown>;
    }
    current = current[part] as Record<string, unknown>;
  });
}

function parsePrimitive(value: string): unknown {
  if (value === "true") return true;
  if (value === "false") return false;
  if (value === "null") return null;
  if (value === "undefined") return undefined;
  if (!Number.isNaN(Number(value)) && value.trim() !== "") return Number(value);
  return value;
}

export function normalizeConversionRules(
  raw: unknown
): ConversionRules {
  if (raw && typeof raw === "object") {
    if (
      "version" in raw &&
      (raw as ConversionRules).version === 2 &&
      "fieldMappings" in raw
    ) {
      const typed = raw as ConversionRules;
      const fieldMappings = (typed.fieldMappings ?? []).map((rule) => ({
        ...rule,
        defaultValue: rule.defaultValue ?? "",
      }));
      const arrayMappings = (typed.arrayMappings ?? []).map((mapping) => ({
        ...mapping,
        coerceSingle: mapping.coerceSingle ?? false,
        itemMappings: mapping.itemMappings.map((rule) => ({
          ...rule,
          defaultValue: rule.defaultValue ?? "",
        })),
      }));
      return {
        version: 2,
        inputFormat: typed.inputFormat ?? "json",
        outputFormat: typed.outputFormat ?? "json",
        fieldMappings,
        arrayMappings,
      };
    }
    if ("rows" in raw) {
      const legacy = raw as MappingConfig;
      return {
        version: 2,
        inputFormat: "json",
        outputFormat: "json",
        fieldMappings: (legacy.rows ?? []).map((row) => {
          if (row.sourceType === "constant") {
            return {
              outputPath: row.outputPath,
              source: { type: "constant", value: row.sourceValue },
              defaultValue: row.defaultValue ?? "",
            };
          }
          if (row.sourceType === "transform") {
            return {
              outputPath: row.outputPath,
              source: {
                type: "transform",
                path: row.sourceValue,
                transform: row.transformType ?? "toLowerCase",
              },
              defaultValue: row.defaultValue ?? "",
            };
          }
          return {
            outputPath: row.outputPath,
            source: { type: "path", path: row.sourceValue },
            defaultValue: row.defaultValue ?? "",
          };
        }),
        arrayMappings: [],
      };
    }
  }

  return {
    version: 2,
    inputFormat: "json",
    outputFormat: "json",
    fieldMappings: [],
    arrayMappings: [],
  };
}

function resolvePathValue(
  root: unknown,
  item: unknown | null,
  path: string
): unknown {
  if (!path) return undefined;
  if (path === "$") return root;
  if (path.startsWith("$.")) {
    return getValueByPath(root, path.slice(2));
  }
  if (path.startsWith("$[")) {
    return getValueByPath(root, path.slice(1));
  }
  if (item !== null && item !== undefined) {
    return getValueByPath(item, path);
  }
  return getValueByPath(root, path);
}

function resolveTransform(value: unknown, transform: TransformType): unknown {
  switch (transform) {
    case "toLowerCase":
      return typeof value === "string" ? value.toLowerCase() : value;
    case "toUpperCase":
      return typeof value === "string" ? value.toUpperCase() : value;
    case "number":
      return value == null || value === "" ? value : Number(value);
    case "boolean":
      if (typeof value === "boolean") return value;
      if (typeof value === "string") {
        return ["true", "1", "yes", "y"].includes(value.toLowerCase());
      }
      return Boolean(value);
    case "concat": {
      const tokens = String(value ?? "")
        .split(",")
        .map((token) => token.trim())
        .filter(Boolean);
      return tokens
        .map((token) => {
          if (token.startsWith("const:")) return token.replace("const:", "");
          return token;
        })
        .join("");
    }
    default:
      return value;
  }
}

function evaluateCondition(
  root: unknown,
  item: unknown | null,
  condition: ConditionRule
): boolean {
  const value = resolvePathValue(root, item, condition.path);
  const compareValue =
    condition.value !== undefined
      ? parsePrimitive(condition.value)
      : undefined;
  switch (condition.operator) {
    case "exists":
      return value !== undefined && value !== null;
    case "equals":
      return value === compareValue;
    case "notEquals":
      return value !== compareValue;
    case "includes":
      if (typeof value === "string" && typeof compareValue === "string") {
        return value.includes(compareValue);
      }
      if (Array.isArray(value)) {
        return value.includes(compareValue as never);
      }
      return false;
    case "gt":
      return Number(value) > Number(compareValue);
    case "lt":
      return Number(value) < Number(compareValue);
    default:
      return false;
  }
}

function resolveSourceValue(
  root: unknown,
  item: unknown | null,
  source: ValueSource
): unknown {
  switch (source.type) {
    case "constant":
      return parsePrimitive(source.value);
    case "path":
      return resolvePathValue(root, item, source.path);
    case "transform": {
      const baseValue = resolvePathValue(root, item, source.path);
      if (source.transform === "concat") {
        const tokens = source.path
          .split(",")
          .map((token) => token.trim())
          .filter(Boolean);
        return tokens
          .map((token) => {
            if (token.startsWith("const:")) return token.replace("const:", "");
            return resolvePathValue(root, item, token) ?? "";
          })
          .join("");
      }
      return resolveTransform(baseValue, source.transform);
    }
    case "condition": {
      const matched = evaluateCondition(root, item, source.condition);
      const resolved = matched
        ? parsePrimitive(source.trueValue)
        : parsePrimitive(source.falseValue);
      return resolved;
    }
    default:
      return undefined;
  }
}

function applyFieldMappings(
  root: unknown,
  item: unknown | null,
  mappings: FieldRule[],
  output: Record<string, unknown>,
  errors: string[],
  label: string
) {
  mappings.forEach((rule, index) => {
    if (!rule.outputPath) {
      errors.push(`${label} ${index + 1}: output path is required.`);
      return;
    }
    let value = resolveSourceValue(root, item, rule.source);
    if ((value === undefined || value === null || value === "") && rule.defaultValue) {
      value = parsePrimitive(rule.defaultValue);
    }
    setValueByPath(output, rule.outputPath, value);
  });
}

export function applyConversion(
  input: unknown,
  rawRules: ConversionRules | MappingConfig | null
): MappingResult {
  const rules = normalizeConversionRules(rawRules);
  if (!rules.fieldMappings.length && !rules.arrayMappings.length) {
    return { output: input ?? {}, errors: [] };
  }

  const output: Record<string, unknown> = {};
  const errors: string[] = [];

  applyFieldMappings(input, null, rules.fieldMappings, output, errors, "Field");

  rules.arrayMappings.forEach((arrayRule, index) => {
    const value = getValueByPath(input, arrayRule.inputPath);
    const items = Array.isArray(value)
      ? value
      : arrayRule.coerceSingle && value !== undefined
      ? [value]
      : null;

    if (!items) {
      errors.push(
        `Array ${index + 1}: input path did not resolve to an array (${arrayRule.inputPath}).`
      );
      return;
    }

    const mappedItems = items.map((item) => {
      const itemOutput: Record<string, unknown> = {};
      applyFieldMappings(
        input,
        item,
        arrayRule.itemMappings,
        itemOutput,
        errors,
        `Array ${index + 1} item`
      );
      return itemOutput;
    });

    if (!arrayRule.outputPath) {
      errors.push(`Array ${index + 1}: output path is required.`);
      return;
    }

    setValueByPath(output, arrayRule.outputPath, mappedItems);
  });

  return { output, errors };
}

export function parsePayload(text: string, format: DataFormat): {
  value: unknown;
  error?: string;
} {
  try {
    if (format === "xml") {
      return { value: xmlParser.parse(text) };
    }
    if (format === "query") {
      return { value: parseQueryString(text) };
    }
    return { value: JSON.parse(text) };
  } catch (error) {
    return {
      value: null,
      error: error instanceof Error ? error.message : "Invalid payload",
    };
  }
}

export function formatPayload(value: unknown, format: DataFormat, pretty = false): string {
  if (format === "xml") {
    const builder = pretty ? xmlBuilderPretty : xmlBuilderCompact;
    return builder.build(value ?? {});
  }
  if (format === "query") {
    return formatQueryString(value ?? {});
  }
  return JSON.stringify(value ?? {}, null, pretty ? 2 : 0);
}

export function applyMapping(
  input: unknown,
  mapping: MappingConfig | ConversionRules | null
): MappingResult {
  return applyConversion(input, mapping);
}
