import { getValueByPath, parsePrimitive, toNumber } from "./core-utils";
import { tryEvaluateConditionExpression } from "./condition-expression";
import {
  ConditionOutputMode,
  ConversionDiagnosticSeverity,
  MergeMode,
  TransformType,
  type ConversionDiagnostic,
  type ValueSource
} from "./types";

export function resolveSourceValue(
  root: unknown,
  item: unknown,
  source: ValueSource,
  errors: string[],
  diagnostics: ConversionDiagnostic[],
  transforms: Record<string, (value: unknown) => unknown>,
  errorContext: string,
  depth = 0
): unknown {
  if (depth > 32) {
    addError(diagnostics, "ACV-RUN-800", errorContext, `${errorContext}: condition/source recursion limit exceeded.`);
    return null;
  }

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
      if (source.transform) {
        return resolveTransform(baseValue, source.transform);
      }

      if (source.customTransform) {
        const transform = transforms[source.customTransform];
        if (!transform) {
          addError(
            diagnostics,
            "ACV-RUN-201",
            errorContext,
            `${errorContext}: custom transform '${source.customTransform}' is not registered.`
          );
          return null;
        }

        try {
          return transform(baseValue);
        } catch (error) {
          addError(
            diagnostics,
            "ACV-RUN-202",
            errorContext,
            `${errorContext}: custom transform '${source.customTransform}' failed: ${error instanceof Error ? error.message : String(error)}.`
          );
          return null;
        }
      }

      return resolveTransform(baseValue, TransformType.ToLowerCase);
    }
    case "condition":
      return resolveConditionSourceValue(root, item, source, errors, diagnostics, transforms, errorContext, depth);
    default:
      addError(
        diagnostics,
        "ACV-RUN-203",
        errorContext,
        `${errorContext}: unsupported source type '${String((source as { type?: unknown }).type ?? "")}'.`
      );
      return null;
  }
}

function resolveConditionSourceValue(
  root: unknown,
  item: unknown,
  source: ValueSource,
  errors: string[],
  diagnostics: ConversionDiagnostic[],
  transforms: Record<string, (value: unknown) => unknown>,
  errorContext: string,
  depth: number
): unknown {
  const matched = evaluateCondition(
    root,
    item,
    source.expression,
    errors,
    diagnostics,
    errorContext,
    "condition expression"
  );

  if (source.conditionOutput === ConditionOutputMode.Match) {
    return matched;
  }

  if (matched) {
    return resolveConditionBranchValue(
      root,
      item,
      source.trueSource ?? null,
      source.trueValue ?? null,
      errors,
      diagnostics,
      transforms,
      `${errorContext} true branch`,
      depth
    );
  }

  const elseIfBranches = source.elseIf ?? [];
  for (let index = 0; index < elseIfBranches.length; index += 1) {
    const branch = elseIfBranches[index];
    const elseIfMatched = evaluateCondition(
      root,
      item,
      branch.expression,
      errors,
      diagnostics,
      `${errorContext} elseIf[${index}]`,
      "elseIf expression"
    );
    if (!elseIfMatched) {
      continue;
    }

    return resolveConditionBranchValue(
      root,
      item,
      branch.source ?? null,
      branch.value ?? null,
      errors,
      diagnostics,
      transforms,
      `${errorContext} elseIf[${index}] branch`,
      depth
    );
  }

  return resolveConditionBranchValue(
    root,
    item,
    source.falseSource ?? null,
    source.falseValue ?? null,
    errors,
    diagnostics,
    transforms,
    `${errorContext} false branch`,
    depth
  );
}

function evaluateCondition(
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

function resolveConditionBranchValue(
  root: unknown,
  item: unknown,
  source: ValueSource | null,
  value: string | null,
  errors: string[],
  diagnostics: ConversionDiagnostic[],
  transforms: Record<string, (value: unknown) => unknown>,
  errorContext: string,
  depth: number
): unknown {
  if (source) {
    return resolveSourceValue(root, item, source, errors, diagnostics, transforms, errorContext, depth + 1);
  }

  return parsePrimitive(value ?? "");
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

export function resolvePathValue(root: unknown, item: unknown, path: string): unknown {
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

function resolveTransform(value: unknown, transform: TransformType | string): unknown {
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

function addError(
  diagnostics: ConversionDiagnostic[],
  code: string,
  rulePath: string,
  message: string
): void {
  diagnostics.push({ code, rulePath, message, severity: ConversionDiagnosticSeverity.Error });
}
