import { applyConversion } from "./mapping-engine";
import { formatPayload, parsePayload } from "./payload-converter";
import { normalizeConversionRules } from "./rules-normalizer";
import { DataFormat } from "./types";

export * from "./types";
export { applyConversion, formatPayload, normalizeConversionRules, parsePayload };

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
