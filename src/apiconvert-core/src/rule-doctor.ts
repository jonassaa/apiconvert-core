import { applyConversion } from "./mapping-engine";
import { parsePayload } from "./payload-converter";
import { lintConversionRules } from "./rules-linter";
import { normalizeConversionRules } from "./rules-normalizer";
import {
  DataFormat,
  RuleDoctorFindingSeverity,
  type RuleDoctorFinding,
  type RuleDoctorOptions,
  type RuleDoctorReport
} from "./types";

export function runRuleDoctor(rawRules: unknown, options: RuleDoctorOptions = {}): RuleDoctorReport {
  const normalizedRules = normalizeConversionRules(rawRules);
  const lint = lintConversionRules(rawRules);
  const findings: RuleDoctorFinding[] = [];

  for (const validationError of normalizedRules.validationErrors ?? []) {
    findings.push({
      code: "ACV-LINT-001",
      stage: "validation",
      severity: RuleDoctorFindingSeverity.Error,
      rulePath: "rules",
      message: validationError,
      suggestion: "Fix schema/normalization errors before conversion."
    });
  }

  for (const diagnostic of lint.diagnostics) {
    findings.push({
      code: diagnostic.code,
      stage: "lint",
      severity:
        diagnostic.severity === "error"
          ? RuleDoctorFindingSeverity.Error
          : RuleDoctorFindingSeverity.Warning,
      rulePath: diagnostic.rulePath,
      message: diagnostic.message,
      suggestion: diagnostic.suggestion
    });
  }

  const format = options.inputFormat ?? DataFormat.Json;
  const inputText = options.sampleInputText;
  if (typeof inputText === "string" && inputText.length > 0) {
    const parsed = parsePayload(inputText, format);
    if (parsed.error) {
      findings.push({
        code: "ACV-DOCTOR-001",
        stage: "runtime",
        severity: RuleDoctorFindingSeverity.Error,
        rulePath: "runtime.input",
        message: `Failed to parse sample input: ${parsed.error}`,
        suggestion: "Pass sample input matching --format (json/xml/query)."
      });
    } else {
      const conversion = applyConversion(parsed.value, normalizedRules);
      for (const warning of conversion.warnings) {
        findings.push({
          code: "ACV-DOCTOR-010",
          stage: "runtime",
          severity: RuleDoctorFindingSeverity.Warning,
          rulePath: "runtime",
          message: warning,
          suggestion: "Adjust rules or input sample to avoid runtime warnings."
        });
      }

      for (const error of conversion.errors) {
        findings.push({
          code: "ACV-DOCTOR-011",
          stage: "runtime",
          severity: RuleDoctorFindingSeverity.Error,
          rulePath: "runtime",
          message: error,
          suggestion: "Fix rule source paths, transforms, or branch expressions."
        });
      }
    }
  } else {
    findings.push({
      code: "ACV-DOCTOR-100",
      stage: "runtime",
      severity: RuleDoctorFindingSeverity.Info,
      rulePath: "runtime",
      message: "Runtime checks skipped (no sample input provided).",
      suggestion: "Provide --input to include conversion-time diagnostics."
    });
  }

  const safeFixPreview = lint.diagnostics
    .map((diagnostic) => `${diagnostic.rulePath}: ${diagnostic.suggestion}`)
    .filter((entry, index, all) => all.indexOf(entry) === index);

  return {
    findings,
    hasErrors: findings.some((finding) => finding.severity === RuleDoctorFindingSeverity.Error),
    canApplySafeFixes: false,
    safeFixPreview
  };
}
