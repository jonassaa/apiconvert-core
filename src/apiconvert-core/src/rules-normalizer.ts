import { normalizeWritePath } from "./core-utils";
import {
  type ArrayRule,
  type BranchRule,
  ConditionOutputMode,
  type ConditionElseIfBranch,
  DataFormat,
  MergeMode,
  TransformType,
  type ConversionRules,
  type ConversionRulesValidationResult,
  type FieldRule,
  type RuleNode,
  type ValueSource
} from "./types";

const SUPPORTED_RULE_KINDS = new Set(["field", "array", "branch", "map"]);
const SUPPORTED_SOURCE_TYPES = new Set(["path", "transform", "condition", "constant", "merge"]);

export function normalizeConversionRules(raw: unknown): ConversionRules {
  if (raw && typeof raw === "object" && !Array.isArray(raw)) {
    if (isConversionRules(raw)) {
      return normalizeRules(raw);
    }
    return emptyRules(["root: expected conversion rules object with rules/inputFormat/outputFormat."]);
  }

  if (typeof raw === "string") {
    try {
      const parsed = JSON.parse(raw);
      return normalizeConversionRules(parsed);
    } catch {
      return emptyRules(["root: invalid JSON in rules payload."]);
    }
  }

  return emptyRules(["root: expected conversion rules object or JSON string."]);
}

export function normalizeConversionRulesStrict(raw: unknown): ConversionRules {
  const rules = normalizeConversionRules(raw);
  if ((rules.validationErrors ?? []).length > 0) {
    throw new Error(`Invalid conversion rules: ${(rules.validationErrors ?? []).join("; ")}`);
  }
  return rules;
}

export function validateConversionRules(raw: unknown): ConversionRulesValidationResult {
  const rules = normalizeConversionRules(raw);
  const errors = [...(rules.validationErrors ?? [])];
  return {
    rules,
    errors,
    isValid: errors.length === 0
  };
}

function normalizeRules(rules: ConversionRules): ConversionRules {
  const validationErrors: string[] = [];
  const inputFormat = normalizeFormat(rules.inputFormat, "inputFormat", validationErrors);
  const outputFormat = normalizeFormat(rules.outputFormat, "outputFormat", validationErrors);
  const fragments = normalizeFragments(rules.fragments, validationErrors);
  const fragmentResolver = new FragmentResolver(fragments, validationErrors);

  const rawRules = rules.rules;
  if (rawRules != null && !Array.isArray(rawRules)) {
    validationErrors.push("rules: must be an array.");
  }

  return {
    inputFormat,
    outputFormat,
    rules: normalizeRuleNodes(
      Array.isArray(rawRules) ? rawRules : [],
      "rules",
      validationErrors,
      fragmentResolver
    ),
    validationErrors
  };
}

function normalizeFormat(
  value: DataFormat | undefined,
  fieldName: "inputFormat" | "outputFormat",
  validationErrors: string[]
): DataFormat {
  if (value == null) {
    return DataFormat.Json;
  }

  if (value === DataFormat.Json || value === DataFormat.Xml || value === DataFormat.Query) {
    return value;
  }

  validationErrors.push(`${fieldName}: unsupported format '${String(value)}'.`);
  return DataFormat.Json;
}

function emptyRules(validationErrors: string[] = []): ConversionRules {
  return {
    inputFormat: DataFormat.Json,
    outputFormat: DataFormat.Json,
    rules: [],
    validationErrors
  };
}

function normalizeValueSource(
  source: unknown,
  path: string,
  validationErrors: string[]
): ValueSource {
  const sourceRecord = toRecord(source);
  const rawType = typeof sourceRecord?.type === "string" ? sourceRecord.type.trim() : "";
  const normalizedType = rawType.toLowerCase();

  if (!normalizedType) {
    validationErrors.push(`${path}.type: is required.`);
  } else if (!SUPPORTED_SOURCE_TYPES.has(normalizedType)) {
    validationErrors.push(`${path}.type: unsupported source type '${rawType}'.`);
  }

  const expression = normalizeString(sourceRecord?.expression);
  if (normalizedType === "condition" && !expression) {
    validationErrors.push(`${path}.expression: is required for condition source.`);
  }

  const separator = normalizeNullableString(sourceRecord?.separator);
  if (sourceRecord?.transform === "split" && separator === "") {
    validationErrors.push(`${path}.separator: must be non-empty when using split transform.`);
  }

  const normalized: ValueSource = {
    type: SUPPORTED_SOURCE_TYPES.has(normalizedType) ? (normalizedType as ValueSource["type"]) : "path",
    path: normalizeNullableString(sourceRecord?.path),
    paths: normalizeStringArray(sourceRecord?.paths),
    value: normalizeNullableString(sourceRecord?.value),
    transform: normalizeTransform(sourceRecord?.transform, `${path}.transform`, validationErrors),
    expression,
    trueValue: normalizeNullableString(sourceRecord?.trueValue),
    falseValue: normalizeNullableString(sourceRecord?.falseValue),
    trueSource: sourceRecord?.trueSource
      ? normalizeValueSource(sourceRecord.trueSource, `${path}.trueSource`, validationErrors)
      : null,
    falseSource: sourceRecord?.falseSource
      ? normalizeValueSource(sourceRecord.falseSource, `${path}.falseSource`, validationErrors)
      : null,
    elseIf: normalizeConditionElseIf(sourceRecord?.elseIf, `${path}.elseIf`, validationErrors),
    conditionOutput: normalizeConditionOutput(
      sourceRecord?.conditionOutput,
      `${path}.conditionOutput`,
      validationErrors
    ),
    mergeMode: normalizeMergeMode(sourceRecord?.mergeMode, `${path}.mergeMode`, validationErrors),
    customTransform: normalizeNullableString(sourceRecord?.customTransform),
    separator,
    tokenIndex: typeof sourceRecord?.tokenIndex === "number" ? sourceRecord.tokenIndex : null,
    trimAfterSplit:
      typeof sourceRecord?.trimAfterSplit === "boolean" ? sourceRecord.trimAfterSplit : null
  };

  return normalized;
}

function normalizeConditionElseIf(
  input: unknown,
  path: string,
  validationErrors: string[]
): ConditionElseIfBranch[] {
  if (input == null) {
    return [];
  }

  if (!Array.isArray(input)) {
    validationErrors.push(`${path}: must be an array when provided.`);
    return [];
  }

  const normalized: ConditionElseIfBranch[] = [];
  input.forEach((entry, index) => {
    const entryPath = `${path}[${index}]`;
    const record = toRecord(entry);
    if (!record) {
      validationErrors.push(`${entryPath}: must be an object.`);
      return;
    }

    const expression = normalizeString(record.expression);
    if (!expression) {
      validationErrors.push(`${entryPath}.expression: is required.`);
    }

    normalized.push({
      expression,
      source: record.source ? normalizeValueSource(record.source, `${entryPath}.source`, validationErrors) : null,
      value: normalizeNullableString(record.value)
    });
  });

  return normalized;
}

function normalizeTransform(
  input: unknown,
  path: string,
  validationErrors: string[]
): ValueSource["transform"] {
  if (input == null) {
    return null;
  }

  if (input === TransformType.ToLowerCase) return TransformType.ToLowerCase;
  if (input === TransformType.ToUpperCase) return TransformType.ToUpperCase;
  if (input === TransformType.Number) return TransformType.Number;
  if (input === TransformType.Boolean) return TransformType.Boolean;
  if (input === TransformType.Concat) return TransformType.Concat;
  if (input === TransformType.Split) return TransformType.Split;

  validationErrors.push(`${path}: unsupported transform '${String(input)}'.`);
  return null;
}

function normalizeConditionOutput(
  input: unknown,
  path: string,
  validationErrors: string[]
): ValueSource["conditionOutput"] {
  if (input == null) {
    return null;
  }

  if (input === ConditionOutputMode.Branch) return ConditionOutputMode.Branch;
  if (input === ConditionOutputMode.Match) return ConditionOutputMode.Match;

  validationErrors.push(`${path}: unsupported condition output mode '${String(input)}'.`);
  return null;
}

function normalizeMergeMode(
  input: unknown,
  path: string,
  validationErrors: string[]
): ValueSource["mergeMode"] {
  if (input == null) {
    return null;
  }

  if (input === MergeMode.Concat) return MergeMode.Concat;
  if (input === MergeMode.FirstNonEmpty) return MergeMode.FirstNonEmpty;
  if (input === MergeMode.Array) return MergeMode.Array;

  validationErrors.push(`${path}: unsupported merge mode '${String(input)}'.`);
  return null;
}

function isConversionRules(value: unknown): value is ConversionRules {
  if (!value || typeof value !== "object" || Array.isArray(value)) {
    return false;
  }
  const record = value as Record<string, unknown>;
  return "rules" in record || "inputFormat" in record || "outputFormat" in record;
}

function normalizeRuleNodes(
  nodes: unknown[],
  path: string,
  validationErrors: string[],
  fragmentResolver: FragmentResolver
): RuleNode[] {
  const normalized: RuleNode[] = [];

  nodes.forEach((node, index) => {
    const nodePath = `${path}[${index}]`;
    const nodeRecord = toRecord(node);
    if (!nodeRecord) {
      validationErrors.push(`${nodePath}: rule must be an object.`);
      return;
    }

    const resolvedRecord = fragmentResolver.resolveUse(nodeRecord, nodePath);
    const kind = normalizeNodeKind(resolvedRecord.kind);
    if (kind == null) {
      validationErrors.push(`${nodePath}: kind is required.`);
      return;
    }

    if (!SUPPORTED_RULE_KINDS.has(kind)) {
      validationErrors.push(`${nodePath}: unsupported kind '${String(nodeRecord.kind ?? "")}'.`);
      return;
    }

    switch (kind) {
      case "field":
        normalized.push(normalizeFieldRuleNode(resolvedRecord, nodePath, validationErrors));
        return;
      case "array":
        normalized.push(normalizeArrayRuleNode(resolvedRecord, nodePath, validationErrors, fragmentResolver));
        return;
      case "map":
        normalized.push(...normalizeMapRuleNode(nodeRecord, nodePath, validationErrors));
        return;
      default:
        normalized.push(normalizeBranchRuleNode(resolvedRecord, nodePath, validationErrors, fragmentResolver));
    }
  });

  return normalized;
}

function normalizeNodeKind(kind: unknown): string | null {
  if (typeof kind !== "string") {
    return null;
  }
  const normalizedKind = kind.trim().toLowerCase();
  return normalizedKind.length > 0 ? normalizedKind : null;
}

function normalizeFieldRuleNode(
  field: Record<string, unknown>,
  nodePath: string,
  validationErrors: string[]
): FieldRule {
  const outputAliases = [...readStringOrStringArray(field.to)];
  if (typeof field.outputPath === "string") {
    outputAliases.push(field.outputPath);
  }

  const outputPaths = normalizeOutputPaths([
    ...(Array.isArray(field.outputPaths) ? field.outputPaths : []),
    ...outputAliases
  ]);
  if (outputPaths.length === 0) {
    validationErrors.push(`${nodePath}: outputPaths is required.`);
  }

  return {
    kind: "field",
    outputPaths,
    source: resolveFieldSource(field, nodePath, validationErrors),
    defaultValue: normalizeNullableString(field.defaultValue)
  };
}

function normalizeMapRuleNode(
  mapRule: Record<string, unknown>,
  nodePath: string,
  validationErrors: string[]
): FieldRule[] {
  const entries = mapRule.entries;
  if (!Array.isArray(entries) || entries.length === 0) {
    validationErrors.push(`${nodePath}.entries: is required for map rule.`);
    return [];
  }

  return entries
    .map((entry, index) => {
      const entryPath = `${nodePath}.entries[${index}]`;
      const entryRecord = toRecord(entry);
      if (!entryRecord) {
        validationErrors.push(`${entryPath}: must be an object.`);
        return null;
      }

      return normalizeFieldRuleNode(entryRecord, entryPath, validationErrors);
    })
    .filter((entry): entry is FieldRule => entry != null);
}

function resolveFieldSource(
  field: Record<string, unknown>,
  nodePath: string,
  validationErrors: string[]
): ValueSource {
  if (field.source != null) {
    return normalizeValueSource(field.source, `${nodePath}.source`, validationErrors);
  }

  const from = normalizeString(field.from);
  if (field.const !== undefined) {
    return normalizeValueSource(
      { type: "constant", value: normalizeConstAlias(field.const) },
      `${nodePath}.source`,
      validationErrors
    );
  }

  const transformAlias = normalizeString(field.as);
  if (transformAlias) {
    if (!from) {
      validationErrors.push(`${nodePath}: from is required when using as.`);
      return normalizeValueSource({ type: "path" }, `${nodePath}.source`, validationErrors);
    }

    return normalizeValueSource(
      { type: "transform", path: from, transform: transformAlias },
      `${nodePath}.source`,
      validationErrors
    );
  }

  if (from) {
    return normalizeValueSource({ type: "path", path: from }, `${nodePath}.source`, validationErrors);
  }

  return normalizeValueSource(field.source, `${nodePath}.source`, validationErrors);
}

function normalizeConstAlias(value: unknown): string {
  if (value == null) {
    return "";
  }
  if (typeof value === "string") {
    return value;
  }
  if (typeof value === "number" || typeof value === "boolean") {
    return String(value);
  }
  return JSON.stringify(value);
}

function readStringOrStringArray(value: unknown): string[] {
  if (typeof value === "string") {
    return [value];
  }

  if (!Array.isArray(value)) {
    return [];
  }

  return value.filter((entry): entry is string => typeof entry === "string");
}

function normalizeArrayRuleNode(
  array: Record<string, unknown>,
  nodePath: string,
  validationErrors: string[],
  fragmentResolver: FragmentResolver
): ArrayRule {
  const outputPaths = normalizeOutputPaths(array.outputPaths);
  const inputPath = normalizeString(array.inputPath) ?? "";
  if (!inputPath) {
    validationErrors.push(`${nodePath}: inputPath is required.`);
  }
  if (outputPaths.length === 0) {
    validationErrors.push(`${nodePath}: outputPaths is required.`);
  }

  if (array.itemRules != null && !Array.isArray(array.itemRules)) {
    validationErrors.push(`${nodePath}.itemRules: must be an array.`);
  }

  if (array.itemRules == null) {
    validationErrors.push(`${nodePath}.itemRules: is required.`);
  }

  return {
    kind: "array",
    inputPath,
    outputPaths,
    coerceSingle: array.coerceSingle === true,
    itemRules: normalizeRuleNodes(
      Array.isArray(array.itemRules) ? array.itemRules : [],
      `${nodePath}.itemRules`,
      validationErrors,
      fragmentResolver
    )
  };
}

function normalizeBranchRuleNode(
  branch: Record<string, unknown>,
  nodePath: string,
  validationErrors: string[],
  fragmentResolver: FragmentResolver
): BranchRule {
  const expression = normalizeString(branch.expression);
  if (!expression) {
    validationErrors.push(`${nodePath}: expression is required.`);
  }

  if (branch.then != null && !Array.isArray(branch.then)) {
    validationErrors.push(`${nodePath}.then: must be an array.`);
  }

  if (branch.then == null) {
    validationErrors.push(`${nodePath}.then: is required.`);
  }

  const elseIfInput = branch.elseIf;
  if (elseIfInput != null && !Array.isArray(elseIfInput)) {
    validationErrors.push(`${nodePath}.elseIf: must be an array.`);
  }

  const elseIf = (Array.isArray(elseIfInput) ? elseIfInput : []).map((entry, elseIfIndex) => {
    const elseIfPath = `${nodePath}.elseIf[${elseIfIndex}]`;
    const elseIfRecord = toRecord(entry);
    if (!elseIfRecord) {
      validationErrors.push(`${elseIfPath}: must be an object.`);
      return { expression: null, then: [] };
    }

    const elseIfExpression = normalizeString(elseIfRecord.expression);
    if (!elseIfExpression) {
      validationErrors.push(`${elseIfPath}: expression is required.`);
    }

    if (elseIfRecord.then != null && !Array.isArray(elseIfRecord.then)) {
      validationErrors.push(`${elseIfPath}.then: must be an array.`);
    }

    if (elseIfRecord.then == null) {
      validationErrors.push(`${elseIfPath}.then: is required.`);
    }

    return {
      expression: elseIfExpression,
      then: normalizeRuleNodes(
        Array.isArray(elseIfRecord.then) ? elseIfRecord.then : [],
        `${elseIfPath}.then`,
        validationErrors,
        fragmentResolver
      )
    };
  });

  if (branch.else != null && !Array.isArray(branch.else)) {
    validationErrors.push(`${nodePath}.else: must be an array.`);
  }

  return {
    kind: "branch",
    expression,
    then: normalizeRuleNodes(
      Array.isArray(branch.then) ? branch.then : [],
      `${nodePath}.then`,
      validationErrors,
      fragmentResolver
    ),
    elseIf,
    else: normalizeRuleNodes(
      Array.isArray(branch.else) ? branch.else : [],
      `${nodePath}.else`,
      validationErrors,
      fragmentResolver
    )
  };
}

function normalizeFragments(
  fragments: ConversionRules["fragments"],
  validationErrors: string[]
): Record<string, Record<string, unknown>> {
  if (fragments == null) {
    return {};
  }

  const record = toRecord(fragments);
  if (!record) {
    validationErrors.push("fragments: must be an object when provided.");
    return {};
  }

  const normalized: Record<string, Record<string, unknown>> = {};
  Object.entries(record).forEach(([key, value]) => {
    const name = key.trim();
    if (!name) {
      validationErrors.push("fragments: fragment name is required.");
      return;
    }
    if (normalized[name]) {
      validationErrors.push(`fragments.${name}: duplicate fragment name.`);
      return;
    }

    const fragmentRecord = toRecord(value);
    if (!fragmentRecord) {
      validationErrors.push(`fragments.${name}: must be an object.`);
      return;
    }

    normalized[name] = fragmentRecord;
  });

  return normalized;
}

class FragmentResolver {
  private fragments: Record<string, Record<string, unknown>>;
  private cache: Record<string, Record<string, unknown>> = {};
  private resolving = new Set<string>();
  private validationErrors: string[];

  constructor(fragments: Record<string, Record<string, unknown>>, validationErrors: string[]) {
    this.fragments = fragments;
    this.validationErrors = validationErrors;
  }

  resolveUse(node: Record<string, unknown>, nodePath: string): Record<string, unknown> {
    const use = normalizeString(node.use);
    if (!use) {
      return node;
    }

    const fragment = this.resolveFragment(use, `${nodePath}.use`);
    if (!fragment) {
      const { use: _use, ...rest } = node;
      return rest;
    }

    const merged = mergeRuleRecords(fragment, node);
    const { use: _use, ...rest } = merged;
    return rest;
  }

  private resolveFragment(name: string, path: string): Record<string, unknown> | null {
    const cached = this.cache[name];
    if (cached) {
      return cached;
    }

    const fragment = this.fragments[name];
    if (!fragment) {
      this.validationErrors.push(`${path}: unknown fragment '${name}'.`);
      return null;
    }

    if (this.resolving.has(name)) {
      this.validationErrors.push(`${path}: fragment '${name}' introduces a cycle.`);
      return null;
    }

    this.resolving.add(name);
    let resolved = fragment;
    if (normalizeString(fragment.use)) {
      resolved = this.resolveUse(fragment, `fragments.${name}`);
    }
    this.resolving.delete(name);

    const { use: _use, ...rest } = resolved;
    this.cache[name] = rest;
    return rest;
  }
}

function mergeRuleRecords(
  fragment: Record<string, unknown>,
  overrideNode: Record<string, unknown>
): Record<string, unknown> {
  return {
    ...fragment,
    ...overrideNode,
    kind: pickString(overrideNode.kind, fragment.kind),
    outputPaths: pickArray(overrideNode.outputPaths, fragment.outputPaths),
    source: toRecord(overrideNode.source) ?? fragment.source,
    defaultValue: pickNonEmptyString(overrideNode.defaultValue, fragment.defaultValue),
    inputPath: pickString(overrideNode.inputPath, fragment.inputPath),
    itemRules: pickArray(overrideNode.itemRules, fragment.itemRules),
    coerceSingle: overrideNode.coerceSingle === true || fragment.coerceSingle === true,
    expression: pickString(overrideNode.expression, fragment.expression),
    then: pickArray(overrideNode.then, fragment.then),
    elseIf: pickArray(overrideNode.elseIf, fragment.elseIf),
    else: pickArray(overrideNode.else, fragment.else)
  };
}

function pickString(overrideValue: unknown, baseValue: unknown): unknown {
  const override = normalizeString(overrideValue);
  if (override) {
    return override;
  }
  return baseValue;
}

function pickNonEmptyString(overrideValue: unknown, baseValue: unknown): unknown {
  if (typeof overrideValue === "string" && overrideValue.length > 0) {
    return overrideValue;
  }
  return baseValue;
}

function pickArray(overrideValue: unknown, baseValue: unknown): unknown {
  if (Array.isArray(overrideValue) && overrideValue.length > 0) {
    return overrideValue;
  }
  return baseValue;
}

function normalizeOutputPaths(paths: unknown): string[] {
  if (!Array.isArray(paths)) {
    return [];
  }

  return [
    ...new Set(
      paths
        .filter((path) => typeof path === "string" && path.trim().length > 0)
        .map((path) => normalizeWritePath(path))
        .filter((path) => path.length > 0)
    )
  ];
}

function normalizeString(input: unknown): string | null {
  if (typeof input !== "string") {
    return null;
  }
  const trimmed = input.trim();
  return trimmed.length > 0 ? trimmed : null;
}

function normalizeNullableString(input: unknown): string | null {
  if (input == null) {
    return null;
  }
  return typeof input === "string" ? input : String(input);
}

function normalizeStringArray(input: unknown): string[] {
  if (!Array.isArray(input)) {
    return [];
  }

  return input.filter((entry): entry is string => typeof entry === "string");
}

function toRecord(value: unknown): Record<string, unknown> | null {
  if (!value || typeof value !== "object" || Array.isArray(value)) {
    return null;
  }
  return value as Record<string, unknown>;
}
