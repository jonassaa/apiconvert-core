import { isRecord, sanitizeForJson } from "./core-utils";

export function parseQueryString(text: string): Record<string, unknown> {
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

export function formatQueryString(value: unknown): string {
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
