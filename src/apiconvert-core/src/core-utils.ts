export function isRecord(value: unknown): value is Record<string, unknown> {
  return !!value && typeof value === "object" && !Array.isArray(value);
}

export function sanitizeForJson(value: unknown): unknown {
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

export function parsePrimitive(value: string): unknown {
  if (value === "true") return true;
  if (value === "false") return false;
  if (value === "null") return null;
  if (value === "undefined") return null;
  const parsed = Number.parseFloat(value);
  if (!Number.isNaN(parsed) && isFinite(parsed)) return parsed;
  return value;
}

export function toNumber(value: unknown): number {
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

export function getValueByPath(input: unknown, path: string): unknown {
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

export function setValueByPath(target: Record<string, unknown>, path: string, value: unknown): void {
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

export function normalizeWritePath(path: string): string {
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

export function getWritePaths(rule: { outputPaths?: string[] | null }): string[] {
  return Array.from(new Set((rule.outputPaths ?? [])
    .filter((path) => !!path && path.trim().length > 0)
    .map((path) => normalizeWritePath(path))
    .filter((path) => !!path && path.trim().length > 0)
  ));
}
