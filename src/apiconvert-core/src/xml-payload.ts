import { XMLBuilder, XMLParser } from "fast-xml-parser";

import { isRecord, parsePrimitive } from "./core-utils";

export function parseXml(text: string): Record<string, unknown> {
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

export function formatXml(value: unknown, pretty: boolean): string {
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
