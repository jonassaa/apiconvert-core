import { sanitizeForJson } from "./core-utils";
import { formatQueryString, parseQueryString } from "./query-payload";
import { formatXml, parseXml } from "./xml-payload";
import { DataFormat } from "./types";

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

function parseJson(text: string): unknown {
  return JSON.parse(text);
}
