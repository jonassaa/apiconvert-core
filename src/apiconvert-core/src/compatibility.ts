import { normalizeConversionRules } from "./rules-normalizer";
import {
  RuleDoctorFindingSeverity,
  type RulesCompatibilityOptions,
  type RulesCompatibilityReport
} from "./types";

const SUPPORTED_VERSION_MIN = "1.0.0";
const SUPPORTED_VERSION_MAX = "1.0.0";

export function checkRulesCompatibility(
  rawRules: unknown,
  options: RulesCompatibilityOptions
): RulesCompatibilityReport {
  const diagnostics: RulesCompatibilityReport["diagnostics"] = [];
  const normalized = normalizeConversionRules(rawRules);

  const targetVersion = options.targetVersion?.trim() ?? "";
  const target = parseSemver(targetVersion);
  if (!target) {
    diagnostics.push({
      code: "ACV-COMP-001",
      severity: RuleDoctorFindingSeverity.Error,
      message: `Invalid target version '${targetVersion}'. Expected <major>.<minor>.<patch>.`,
      suggestion: "Pass a semantic version like 1.0.0."
    });
  }

  const schemaVersion = readSchemaVersion(rawRules);
  const parsedSchemaVersion = schemaVersion ? parseSemver(schemaVersion) : null;

  if (!schemaVersion) {
    diagnostics.push({
      code: "ACV-COMP-002",
      severity: RuleDoctorFindingSeverity.Warning,
      message: "Rules do not declare schemaVersion. Compatibility is conservative.",
      suggestion: "Add schemaVersion to your rules pack for strict compatibility checks."
    });
  } else if (!parsedSchemaVersion) {
    diagnostics.push({
      code: "ACV-COMP-005",
      severity: RuleDoctorFindingSeverity.Error,
      message: `Invalid schemaVersion '${schemaVersion}' in rules.`,
      suggestion: "Set schemaVersion using semantic version format (for example, 1.0.0)."
    });
  }

  const min = parseSemver(SUPPORTED_VERSION_MIN)!;
  const max = parseSemver(SUPPORTED_VERSION_MAX)!;

  if (target) {
    if (compareSemver(target, min) < 0 || compareSemver(target, max) > 0) {
      diagnostics.push({
        code: "ACV-COMP-003",
        severity: RuleDoctorFindingSeverity.Error,
        message: `Target runtime version ${targetVersion} is outside supported range ${SUPPORTED_VERSION_MIN} - ${SUPPORTED_VERSION_MAX}.`,
        suggestion: `Use a target version within ${SUPPORTED_VERSION_MIN} - ${SUPPORTED_VERSION_MAX}.`
      });
    }

    if (parsedSchemaVersion && compareSemver(parsedSchemaVersion, target) > 0) {
      diagnostics.push({
        code: "ACV-COMP-004",
        severity: RuleDoctorFindingSeverity.Error,
        message: `Rules schemaVersion ${schemaVersion} requires runtime >= ${schemaVersion}, but target is ${targetVersion}.`,
        suggestion: "Upgrade target runtime or use rules compatible with the target version."
      });
    }
  }

  for (const validationError of normalized.validationErrors ?? []) {
    diagnostics.push({
      code: "ACV-COMP-006",
      severity: RuleDoctorFindingSeverity.Warning,
      message: `Rules normalization warning: ${validationError}`,
      suggestion: "Resolve rules validation issues before relying on compatibility checks."
    });
  }

  return {
    targetVersion,
    schemaVersion,
    supportedRangeMin: SUPPORTED_VERSION_MIN,
    supportedRangeMax: SUPPORTED_VERSION_MAX,
    isCompatible: diagnostics.every((entry) => entry.severity !== RuleDoctorFindingSeverity.Error),
    diagnostics
  };
}

function readSchemaVersion(rawRules: unknown): string | null {
  if (typeof rawRules === "string") {
    try {
      return readSchemaVersion(JSON.parse(rawRules));
    } catch {
      return null;
    }
  }

  if (!rawRules || typeof rawRules !== "object" || Array.isArray(rawRules)) {
    return null;
  }

  const value = (rawRules as { schemaVersion?: unknown }).schemaVersion;
  return typeof value === "string" && value.trim().length > 0 ? value.trim() : null;
}

function parseSemver(value: string): { major: number; minor: number; patch: number } | null {
  const match = /^(\d+)\.(\d+)\.(\d+)$/.exec(value);
  if (!match) {
    return null;
  }

  return {
    major: Number(match[1]),
    minor: Number(match[2]),
    patch: Number(match[3])
  };
}

function compareSemver(
  left: { major: number; minor: number; patch: number },
  right: { major: number; minor: number; patch: number }
): number {
  if (left.major !== right.major) return left.major - right.major;
  if (left.minor !== right.minor) return left.minor - right.minor;
  return left.patch - right.patch;
}
