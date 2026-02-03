"use client";

import type { ReactNode } from "react";
import { useEffect, useId, useMemo, useState } from "react";
import { ChevronDown, Loader2 } from "lucide-react";
import {
  applyConversion,
  formatPayload,
  normalizeConversionRules,
  parsePayload,
  type ArrayRule,
  type ConditionOperator,
  type ConversionRules,
  type DataFormat,
  type FieldRule,
  type TransformType,
  type ValueSource,
} from "@/lib/mapping/engine";
import { apiFetch } from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { SubmitButton } from "@/components/app/SubmitButton";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { TooltipIcon } from "@/components/app/TooltipIcon";

const transformOptions: TransformType[] = [
  "toLowerCase",
  "toUpperCase",
  "number",
  "boolean",
  "concat",
];

const conditionOperators: ConditionOperator[] = [
  "exists",
  "equals",
  "notEquals",
  "includes",
  "gt",
  "lt",
];

const emptyFieldRule: FieldRule = {
  outputPath: "",
  source: { type: "path", path: "" },
  defaultValue: "",
};

function defaultSample(format: DataFormat) {
  if (format === "xml") {
    return "<root>\n  <customer>\n    <email>sample@acme.io</email>\n  </customer>\n</root>";
  }
  if (format === "query") {
    return "customer.email=sample@acme.io&plan=premium";
  }
  return JSON.stringify({ customer: { email: "sample@acme.io" } }, null, 2);
}

function buildSourceForType(type: ValueSource["type"]): ValueSource {
  if (type === "constant") {
    return { type: "constant", value: "" };
  }
  if (type === "transform") {
    return { type: "transform", path: "", transform: "toLowerCase" };
  }
  if (type === "condition") {
    return {
      type: "condition",
      condition: { path: "", operator: "exists", value: "" },
      trueValue: "true",
      falseValue: "false",
    };
  }
  return { type: "path", path: "" };
}

function FieldMappingsEditor({
  title,
  mappings,
  onChange,
}: {
  title: string;
  mappings: FieldRule[];
  onChange: (next: FieldRule[]) => void;
}) {
  function updateRule(index: number, next: Partial<FieldRule>) {
    onChange(
      mappings.map((rule, ruleIndex) =>
        ruleIndex === index ? { ...rule, ...next } : rule
      )
    );
  }

  function updateSource(index: number, next: ValueSource) {
    onChange(
      mappings.map((rule, ruleIndex) =>
        ruleIndex === index ? { ...rule, source: next } : rule
      )
    );
  }

  function addRule() {
    onChange([...mappings, { ...emptyFieldRule }]);
  }

  function removeRule(index: number) {
    onChange(mappings.filter((_, ruleIndex) => ruleIndex !== index));
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>{title}</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid gap-3">
          {mappings.map((rule, index) => {
            const source = rule.source;
            return (
              <div
                key={index}
                className="grid gap-3 rounded-xl border border-border/70 bg-card p-4"
              >
                <div className="grid gap-3 lg:grid-cols-5">
                  <div className="flex items-center gap-2">
                    <Input
                      placeholder="output.path"
                      value={rule.outputPath}
                      aria-label="Output path"
                      className="flex-1"
                      onChange={(event) =>
                        updateRule(index, { outputPath: event.target.value })
                      }
                    />
                    <TooltipIcon text="Destination field path to write to." />
                  </div>
                  <div className="flex items-center gap-2">
                    <Select
                      value={source.type}
                      onValueChange={(value) =>
                        updateSource(
                          index,
                          buildSourceForType(value as ValueSource["type"])
                        )
                      }
                    >
                      <SelectTrigger className="flex-1">
                        <SelectValue placeholder="Source type" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="path">Path</SelectItem>
                        <SelectItem value="constant">Constant</SelectItem>
                        <SelectItem value="transform">Transform</SelectItem>
                        <SelectItem value="condition">Condition</SelectItem>
                      </SelectContent>
                    </Select>
                    <TooltipIcon text="Choose how to derive the output value." />
                  </div>
                  {source.type === "path" ? (
                    <div className="flex items-center gap-2">
                      <Input
                        placeholder="input.path"
                        value={source.path}
                        aria-label="Source path"
                        className="flex-1"
                        onChange={(event) =>
                          updateSource(index, {
                            ...source,
                            path: event.target.value,
                          })
                        }
                      />
                      <TooltipIcon text="Source field path from the input payload." />
                    </div>
                  ) : null}
                  {source.type === "constant" ? (
                    <div className="flex items-center gap-2">
                      <Input
                        placeholder="const value"
                        value={source.value}
                        aria-label="Constant value"
                        className="flex-1"
                        onChange={(event) =>
                          updateSource(index, {
                            ...source,
                            value: event.target.value,
                          })
                        }
                      />
                      <TooltipIcon text="Literal value to use for the output field." />
                    </div>
                  ) : null}
                  {source.type === "transform" ? (
                    <>
                      <div className="flex items-center gap-2">
                      <Input
                        placeholder="input.path or concat tokens"
                        value={source.path}
                        aria-label="Transform source"
                        className="flex-1"
                        onChange={(event) =>
                          updateSource(index, {
                            ...source,
                            path: event.target.value,
                            })
                          }
                        />
                        <TooltipIcon text="Source path or concat tokens for the transform." />
                      </div>
                      <div className="flex items-center gap-2">
                        <Select
                          value={source.transform}
                          onValueChange={(value) =>
                            updateSource(index, {
                              ...source,
                              transform: value as TransformType,
                            })
                          }
                        >
                          <SelectTrigger className="flex-1">
                            <SelectValue placeholder="Transform" />
                          </SelectTrigger>
                          <SelectContent>
                            {transformOptions.map((option) => (
                              <SelectItem key={option} value={option}>
                                {option}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                        <TooltipIcon text="Transform to apply to the source value." />
                      </div>
                    </>
                  ) : null}
                  {source.type === "condition" ? (
                    <>
                      <div className="flex items-center gap-2">
                      <Input
                        placeholder="input.path"
                        value={source.condition.path}
                        aria-label="Condition path"
                        className="flex-1"
                        onChange={(event) =>
                          updateSource(index, {
                            ...source,
                            condition: {
                                ...source.condition,
                                path: event.target.value,
                              },
                            })
                          }
                        />
                        <TooltipIcon text="Input path to evaluate for the condition." />
                      </div>
                      <div className="flex items-center gap-2">
                        <Select
                          value={source.condition.operator}
                          onValueChange={(value) =>
                            updateSource(index, {
                              ...source,
                              condition: {
                                ...source.condition,
                                operator: value as ConditionOperator,
                              },
                            })
                          }
                        >
                          <SelectTrigger className="flex-1">
                            <SelectValue placeholder="Operator" />
                          </SelectTrigger>
                          <SelectContent>
                            {conditionOperators.map((option) => (
                              <SelectItem key={option} value={option}>
                                {option}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                        <TooltipIcon text="Condition operator to apply." />
                      </div>
                      {source.condition.operator !== "exists" ? (
                        <div className="flex items-center gap-2">
                          <Input
                            placeholder="compare value"
                            value={source.condition.value ?? ""}
                            aria-label="Condition compare value"
                            className="flex-1"
                            onChange={(event) =>
                              updateSource(index, {
                                ...source,
                                condition: {
                                  ...source.condition,
                                  value: event.target.value,
                                },
                              })
                            }
                          />
                          <TooltipIcon text="Value to compare against for the condition." />
                        </div>
                      ) : null}
                      <div className="flex items-center gap-2">
                      <Input
                        placeholder="true value"
                        value={source.trueValue}
                        aria-label="Condition true value"
                        className="flex-1"
                        onChange={(event) =>
                          updateSource(index, {
                            ...source,
                            trueValue: event.target.value,
                            })
                          }
                        />
                        <TooltipIcon text="Value used when the condition passes." />
                      </div>
                      <div className="flex items-center gap-2">
                      <Input
                        placeholder="false value"
                        value={source.falseValue}
                        aria-label="Condition false value"
                        className="flex-1"
                        onChange={(event) =>
                          updateSource(index, {
                            ...source,
                            falseValue: event.target.value,
                            })
                          }
                        />
                        <TooltipIcon text="Value used when the condition fails." />
                      </div>
                    </>
                  ) : null}
                  <div className="flex items-center gap-2">
                    <Input
                      placeholder="default (optional)"
                      value={rule.defaultValue}
                      aria-label="Default value"
                      className="flex-1"
                      onChange={(event) =>
                        updateRule(index, { defaultValue: event.target.value })
                      }
                    />
                    <TooltipIcon text="Fallback value if the source is missing." />
                  </div>
                </div>
                <div className="flex justify-end">
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    onClick={() => removeRule(index)}
                  >
                    Remove
                  </Button>
                </div>
              </div>
            );
          })}
        </div>
        <Button type="button" variant="secondary" onClick={addRule}>
          Add rule
        </Button>
      </CardContent>
    </Card>
  );
}

function normalizeConditionValue(mappings: FieldRule[]): FieldRule[] {
  return mappings.map((rule) => {
    if (rule.source.type !== "condition") return rule;
    return {
      ...rule,
      source: {
        ...rule.source,
        condition: {
          ...rule.source.condition,
          value: rule.source.condition.value ?? "",
        },
        falseValue: rule.source.falseValue ?? "",
      },
    };
  });
}

function normalizeDefaultValue(mappings: FieldRule[]): FieldRule[] {
  return mappings.map((rule) => ({
    ...rule,
    defaultValue: rule.defaultValue ?? "",
  }));
}

type DiffLine = {
  type: "context" | "remove" | "add";
  value: string;
};

function buildLineDiff(expected: string, actual: string): DiffLine[] {
  const expectedLines = expected.split("\n");
  const actualLines = actual.split("\n");
  const maxLength = Math.max(expectedLines.length, actualLines.length);
  const lines: DiffLine[] = [];

  for (let index = 0; index < maxLength; index += 1) {
    const expectedLine = expectedLines[index];
    const actualLine = actualLines[index];
    if (expectedLine === actualLine) {
      lines.push({ type: "context", value: expectedLine ?? "" });
      continue;
    }
    if (expectedLine !== undefined) {
      lines.push({ type: "remove", value: expectedLine });
    }
    if (actualLine !== undefined) {
      lines.push({ type: "add", value: actualLine });
    }
  }

  return lines;
}

function normalizePayloadForDiff(value: string, format: DataFormat) {
  const trimmed = value.trim();
  if (!trimmed) {
    return "";
  }
  if (format !== "json") {
    return trimmed;
  }
  try {
    return JSON.stringify(JSON.parse(trimmed), null, 2);
  } catch {
    return trimmed;
  }
}

function CollapsibleCard({
  title,
  children,
  defaultOpen = true,
}: {
  title: ReactNode;
  children: ReactNode;
  defaultOpen?: boolean;
}) {
  const [isOpen, setIsOpen] = useState(defaultOpen);
  const contentId = useId();

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between gap-3">
        <CardTitle>{title}</CardTitle>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          aria-expanded={isOpen}
          aria-controls={contentId}
          onClick={() => setIsOpen((prev) => !prev)}
          className="h-8 px-2 text-xs"
        >
          {isOpen ? "Hide" : "Show"}
          <ChevronDown
            className={`ml-1 h-4 w-4 transition-transform ${
              isOpen ? "rotate-180" : ""
            }`}
          />
        </Button>
      </CardHeader>
      {isOpen ? <CardContent id={contentId}>{children}</CardContent> : null}
    </Card>
  );
}

export function ConversionEditor({
  initialRules,
  initialInputSample,
  initialOutputSample,
  action,
  onSubmit,
}: {
  initialRules: ConversionRules | null;
  initialInputSample?: string | null;
  initialOutputSample?: string | null;
  action?: (formData: FormData) => void;
  onSubmit?: (formData: FormData) => void | Promise<void>;
}) {
  const normalized = normalizeConversionRules(initialRules);
  const [inputFormat, setInputFormat] = useState<DataFormat>(
    normalized.inputFormat
  );
  const [outputFormat, setOutputFormat] = useState<DataFormat>(
    normalized.outputFormat
  );
  const [sampleInput, setSampleInput] = useState(() =>
    initialInputSample ?? defaultSample(normalized.inputFormat)
  );
  const [sampleOutput, setSampleOutput] = useState(() =>
    initialOutputSample ?? defaultSample(normalized.outputFormat)
  );
  const [fieldMappings, setFieldMappings] = useState<FieldRule[]>(
    normalized.fieldMappings.length
      ? normalizeDefaultValue(normalizeConditionValue(normalized.fieldMappings))
      : [emptyFieldRule]
  );
  const [arrayMappings, setArrayMappings] = useState<ArrayRule[]>(
    normalized.arrayMappings.map((mapping) => ({
      ...mapping,
      coerceSingle: mapping.coerceSingle ?? false,
      itemMappings: normalizeDefaultValue(
        normalizeConditionValue(mapping.itemMappings)
      ),
    }))
  );
  const [previewOutput, setPreviewOutput] = useState("{}");
  const [previewErrors, setPreviewErrors] = useState<string[]>([]);
  const [rulesJson, setRulesJson] = useState(() =>
    JSON.stringify(normalized, null, 2)
  );
  const [rulesJsonDirty, setRulesJsonDirty] = useState(false);
  const [rulesJsonError, setRulesJsonError] = useState<string | null>(null);
  const [generateError, setGenerateError] = useState<string | null>(null);
  const [isGenerating, setIsGenerating] = useState(false);
  const useDotnetPreview = process.env.NEXT_PUBLIC_CONVERSION_ENGINE === "dotnet";

  const normalizedSampleOutput = useMemo(
    () => normalizePayloadForDiff(sampleOutput, outputFormat),
    [sampleOutput, outputFormat]
  );
  const normalizedPreviewOutput = useMemo(
    () => normalizePayloadForDiff(previewOutput, outputFormat),
    [previewOutput, outputFormat]
  );
  const diffLines = useMemo(
    () => buildLineDiff(normalizedSampleOutput, normalizedPreviewOutput),
    [normalizedSampleOutput, normalizedPreviewOutput]
  );
  const isDiffEmpty =
    normalizedSampleOutput.trim() === normalizedPreviewOutput.trim();

  const rules: ConversionRules = useMemo(
    () => ({
      version: 2,
      inputFormat,
      outputFormat,
      fieldMappings,
      arrayMappings,
    }),
    [arrayMappings, fieldMappings, inputFormat, outputFormat]
  );

  useEffect(() => {
    if (!rulesJsonDirty) {
      setRulesJson(JSON.stringify(rules, null, 2));
    }
  }, [rules, rulesJsonDirty]);

  async function runPreview() {
    if (!useDotnetPreview) {
      const parsed = parsePayload(sampleInput, inputFormat);
      if (parsed.error) {
        setPreviewErrors([
          inputFormat === "xml"
            ? "Invalid XML input."
            : inputFormat === "query"
            ? "Invalid query input."
            : "Invalid JSON input.",
        ]);
        setPreviewOutput("{}");
        return;
      }
      const result = applyConversion(parsed.value, rules);
      try {
        const formatted = formatPayload(result.output, outputFormat, true);
        setPreviewErrors(result.errors);
        setPreviewOutput(formatted);
      } catch (error) {
        const message =
          error instanceof Error ? error.message : "Failed to format output.";
        setPreviewErrors([...result.errors, message]);
        setPreviewOutput("");
      }
      return;
    }

    try {
      const data = await apiFetch<{ output: string; errors: string[] }>(
        "/api/conversion",
        {
          method: "POST",
          body: { payload: sampleInput, rules, pretty: true },
        }
      );
      setPreviewErrors(data.errors ?? []);
      setPreviewOutput(data.output ?? "");
    } catch (error) {
      setPreviewErrors([
        error instanceof Error ? error.message : "Failed to generate preview.",
      ]);
      setPreviewOutput("");
    }
  }

  useEffect(() => {
    const timeout = setTimeout(() => {
      void runPreview();
    }, 200);
    return () => clearTimeout(timeout);
  }, [inputFormat, outputFormat, rules, sampleInput, sampleOutput]);

  async function generateRules() {
    setIsGenerating(true);
    setGenerateError(null);
    try {
      const data = await apiFetch<{ rules: ConversionRules }>(
        "/api/conversion-rules/generate",
        {
          method: "POST",
          body: {
            inputFormat,
            outputFormat,
            inputSample: sampleInput,
            outputSample: sampleOutput,
          },
        }
      );
      const normalizedRules = normalizeConversionRules(data.rules);
      setInputFormat(normalizedRules.inputFormat);
      setOutputFormat(normalizedRules.outputFormat);
      setFieldMappings(
        normalizedRules.fieldMappings.length
          ? normalizeDefaultValue(
              normalizeConditionValue(normalizedRules.fieldMappings)
            )
          : [emptyFieldRule]
      );
      setArrayMappings(
        normalizedRules.arrayMappings.map((mapping) => ({
          ...mapping,
          coerceSingle: mapping.coerceSingle ?? false,
          itemMappings: normalizeDefaultValue(
            normalizeConditionValue(mapping.itemMappings)
          ),
        }))
      );
      setRulesJsonDirty(false);
      setRulesJsonError(null);
    } catch (error) {
      setGenerateError(
        error instanceof Error ? error.message : "Failed to generate rules"
      );
    } finally {
      setIsGenerating(false);
    }
  }

  function applyRulesJson() {
    try {
      const parsed = JSON.parse(rulesJson);
      const normalizedRules = normalizeConversionRules(parsed);
      setInputFormat(normalizedRules.inputFormat);
      setOutputFormat(normalizedRules.outputFormat);
      setFieldMappings(
        normalizedRules.fieldMappings.length
          ? normalizeDefaultValue(
              normalizeConditionValue(normalizedRules.fieldMappings)
            )
          : [emptyFieldRule]
      );
      setArrayMappings(
        normalizedRules.arrayMappings.map((mapping) => ({
          ...mapping,
          coerceSingle: mapping.coerceSingle ?? false,
          itemMappings: normalizeDefaultValue(
            normalizeConditionValue(mapping.itemMappings)
          ),
        }))
      );
      setRulesJsonDirty(false);
      setRulesJsonError(null);
    } catch {
      setRulesJsonError("Rules JSON is invalid.");
    }
  }

  function discardRulesJson() {
    setRulesJson(JSON.stringify(rules, null, 2));
    setRulesJsonDirty(false);
    setRulesJsonError(null);
  }

  function addArrayMapping() {
    setArrayMappings((prev) => [
      ...prev,
      {
        inputPath: "",
        outputPath: "",
        coerceSingle: true,
        itemMappings: [emptyFieldRule],
      },
    ]);
  }

  function updateArrayMapping(index: number, next: Partial<ArrayRule>) {
    setArrayMappings((prev) =>
      prev.map((rule, ruleIndex) =>
        ruleIndex === index ? { ...rule, ...next } : rule
      )
    );
  }

  function removeArrayMapping(index: number) {
    setArrayMappings((prev) => prev.filter((_, ruleIndex) => ruleIndex !== index));
  }

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    if (!onSubmit) return;
    event.preventDefault();
    const formData = new FormData(event.currentTarget);
    onSubmit(formData);
  };

  return (
    <form
      action={onSubmit ? undefined : action}
      onSubmit={onSubmit ? handleSubmit : undefined}
      className="space-y-6 pb-16"
    >
      <input
        type="hidden"
        name="mapping_json"
        value={JSON.stringify(rules)}
        readOnly
      />

      <div className="flex flex-wrap gap-4">
        <div className="min-w-[220px]">
          <div className="flex items-center gap-2 text-sm font-medium">
            <span>Input format</span>
            <TooltipIcon text="Format of the inbound payload." />
          </div>
          <Select
            value={inputFormat}
            onValueChange={(value) => setInputFormat(value as DataFormat)}
          >
            <SelectTrigger className="mt-2">
              <SelectValue placeholder="Input format" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="json">JSON</SelectItem>
              <SelectItem value="xml">XML</SelectItem>
              <SelectItem value="query">Query parameters</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="min-w-[220px]">
          <div className="flex items-center gap-2 text-sm font-medium">
            <span>Output format</span>
            <TooltipIcon text="Format produced after conversion." />
          </div>
          <Select
            value={outputFormat}
            onValueChange={(value) => setOutputFormat(value as DataFormat)}
          >
            <SelectTrigger className="mt-2">
              <SelectValue placeholder="Output format" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="json">JSON</SelectItem>
              <SelectItem value="xml">XML</SelectItem>
              <SelectItem value="query">Query parameters</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="flex items-end gap-2">
          <Button
            type="button"
            onClick={generateRules}
            disabled={isGenerating}
            aria-busy={isGenerating}
          >
            {isGenerating ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Generating...
              </>
            ) : (
              "Generate rules with AI"
            )}
          </Button>
        </div>
        <div className="ml-auto flex items-end">
          <SubmitButton type="submit" pendingLabel="Saving...">
            Save rules
          </SubmitButton>
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <CollapsibleCard
          title={
            <div className="flex items-center gap-2">
              <span>Example input</span>
              <TooltipIcon text="Sample payload in the selected input format." />
            </div>
          }
        >
          <Textarea
            rows={12}
            name="input_sample"
            value={sampleInput}
            aria-label="Example input"
            onChange={(event) => setSampleInput(event.target.value)}
            className="font-mono text-xs"
          />
        </CollapsibleCard>
        <CollapsibleCard
          title={
            <div className="flex items-center gap-2">
              <span>Expected output</span>
              <TooltipIcon text="Desired output for the sample input." />
            </div>
          }
        >
          <div className="space-y-2">
            <Textarea
              rows={12}
              name="output_sample"
              value={sampleOutput}
              aria-label="Expected output"
              onChange={(event) => setSampleOutput(event.target.value)}
              className="font-mono text-xs"
            />
            {generateError ? (
              <p className="text-sm text-destructive">{generateError}</p>
            ) : null}
          </div>
        </CollapsibleCard>
      </div>

      <CollapsibleCard
        title={
          <div className="flex items-center gap-2">
            <span>Preview output</span>
            <TooltipIcon text="Diff between expected output and previewed result." />
          </div>
        }
      >
        <div className="space-y-2">
          <div className="rounded-lg border border-border/70 bg-muted/40 p-3 font-mono text-xs">
            {isDiffEmpty ? (
              <div className="space-y-2 whitespace-pre-wrap break-words">
                <p className="text-sm text-muted-foreground">
                  No differences detected.
                </p>
                <div className="text-muted-foreground">{previewOutput || " "}</div>
              </div>
            ) : (
              <div className="space-y-1 whitespace-pre-wrap break-words">
                {diffLines.map((line, index) => (
                  <div
                    key={`${line.type}-${index}`}
                    className={
                      line.type === "add"
                        ? "text-emerald-600"
                        : line.type === "remove"
                        ? "text-rose-600"
                        : "text-muted-foreground"
                    }
                  >
                    <span className="mr-2 select-none">
                      {line.type === "add"
                        ? "+"
                        : line.type === "remove"
                        ? "-"
                        : " "}
                    </span>
                    <span>{line.value || " "}</span>
                  </div>
                ))}
              </div>
            )}
          </div>
          {previewErrors.length ? (
            <p className="text-sm text-destructive">{previewErrors.join(" ")}</p>
          ) : null}
        </div>
      </CollapsibleCard>

      <div className="space-y-4">
        <FieldMappingsEditor
          title="Field rules"
          mappings={fieldMappings}
          onChange={setFieldMappings}
        />

        <Card>
          <CardHeader>
            <CardTitle>Array rules</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {arrayMappings.length ? (
              <div className="space-y-6">
                {arrayMappings.map((rule, index) => (
                  <div
                    key={index}
                    className="space-y-4 rounded-xl border border-border/70 bg-card p-4"
                  >
                    <div className="grid gap-3 lg:grid-cols-3">
                      <div className="flex items-center gap-2">
                        <Input
                          placeholder="input.array"
                          value={rule.inputPath}
                          className="flex-1"
                          onChange={(event) =>
                            updateArrayMapping(index, {
                              inputPath: event.target.value,
                            })
                          }
                        />
                        <TooltipIcon text="Input path that resolves to the array." />
                      </div>
                      <div className="flex items-center gap-2">
                        <Input
                          placeholder="output.items"
                          value={rule.outputPath}
                          className="flex-1"
                          onChange={(event) =>
                            updateArrayMapping(index, {
                              outputPath: event.target.value,
                            })
                          }
                        />
                        <TooltipIcon text="Output path to write array items to." />
                      </div>
                      <div className="flex items-center gap-2 text-sm">
                        <label className="flex items-center gap-2">
                          <input
                            type="checkbox"
                            checked={rule.coerceSingle ?? false}
                            onChange={(event) =>
                              updateArrayMapping(index, {
                                coerceSingle: event.target.checked,
                              })
                            }
                            className="h-4 w-4 rounded border-border"
                          />
                          Coerce single item
                        </label>
                        <TooltipIcon text="Treat a single object as a one-item array." />
                      </div>
                    </div>
                    <FieldMappingsEditor
                      title="Item rules"
                      mappings={rule.itemMappings}
                      onChange={(next) =>
                        updateArrayMapping(index, { itemMappings: next })
                      }
                    />
                    <div className="flex justify-end">
                      <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        onClick={() => removeArrayMapping(index)}
                      >
                        Remove array
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <p className="text-sm text-muted-foreground">
                No array rules yet.
              </p>
            )}
            <Button type="button" variant="secondary" onClick={addArrayMapping}>
              Add array rule
            </Button>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <span>Rules JSON</span>
              <TooltipIcon text="Advanced: edit the conversion rules JSON directly." />
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <Textarea
              rows={12}
              value={rulesJson}
              onChange={(event) => {
                setRulesJson(event.target.value);
                setRulesJsonDirty(true);
              }}
              className="font-mono text-xs"
            />
            {rulesJsonError ? (
              <p className="text-sm text-destructive">{rulesJsonError}</p>
            ) : null}
            <div className="flex flex-wrap gap-2">
              <Button type="button" variant="secondary" onClick={applyRulesJson}>
                Apply JSON
              </Button>
              <Button type="button" variant="outline" onClick={discardRulesJson}>
                Discard changes
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>

    </form>
  );
}
