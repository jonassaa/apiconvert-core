export enum DataFormat {
  Json = "json",
  Xml = "xml",
  Query = "query"
}

export enum TransformType {
  ToLowerCase = "toLowerCase",
  ToUpperCase = "toUpperCase",
  Number = "number",
  Boolean = "boolean",
  Concat = "concat",
  Split = "split"
}

export enum MergeMode {
  Concat = "concat",
  FirstNonEmpty = "firstNonEmpty",
  Array = "array"
}

export enum ConditionOutputMode {
  Branch = "branch",
  Match = "match"
}

export enum OutputCollisionPolicy {
  LastWriteWins = "lastWriteWins",
  FirstWriteWins = "firstWriteWins",
  Error = "error"
}

export interface ApplyConversionOptions {
  collisionPolicy?: OutputCollisionPolicy | null;
  explain?: boolean | null;
  transforms?: Record<string, (value: unknown) => unknown> | null;
}

export enum RuleLintSeverity {
  Warning = "warning",
  Error = "error"
}

export interface RuleLintDiagnostic {
  code: string;
  severity: RuleLintSeverity;
  rulePath: string;
  message: string;
  suggestion: string;
}

export interface ConversionTraceEntry {
  rulePath: string;
  ruleKind: string;
  decision: string;
  sourceValue?: unknown;
  outputPaths: string[];
  expression?: string;
  warning?: string;
  error?: string;
}

export interface ConditionElseIfBranch {
  expression: string | null;
  source?: ValueSource | null;
  value?: string | null;
}

interface ValueSourceBase {
  path?: string | null;
  paths?: string[] | null;
  value?: string | null;
  expression?: string | null;
  trueValue?: string | null;
  falseValue?: string | null;
  trueSource?: ValueSource | null;
  falseSource?: ValueSource | null;
  elseIf?: ConditionElseIfBranch[] | null;
  conditionOutput?: ConditionOutputMode | null;
  mergeMode?: MergeMode | null;
  separator?: string | null;
  tokenIndex?: number | null;
  trimAfterSplit?: boolean | null;
  transform?: TransformType | null;
  customTransform?: string | null;
}

export interface PathValueSource extends ValueSourceBase {
  type: "path";
}

export interface ConstantValueSource extends ValueSourceBase {
  type: "constant";
}

export interface TransformValueSource extends ValueSourceBase {
  type: "transform";
  transform?: TransformType | null;
}

export interface MergeValueSource extends ValueSourceBase {
  type: "merge";
}

export interface ConditionValueSource extends ValueSourceBase {
  type: "condition";
}

export type ValueSource =
  | PathValueSource
  | ConstantValueSource
  | TransformValueSource
  | MergeValueSource
  | ConditionValueSource;

export interface FieldRule {
  kind: "field";
  outputPaths?: string[] | null;
  source: ValueSource;
  defaultValue?: string | null;
}

export interface ArrayRule {
  kind: "array";
  inputPath: string;
  outputPaths?: string[] | null;
  itemRules?: RuleNode[] | null;
  coerceSingle?: boolean;
}

export interface BranchElseIfRule {
  expression?: string | null;
  then?: RuleNode[] | null;
}

export interface BranchRule {
  kind: "branch";
  expression?: string | null;
  then?: RuleNode[] | null;
  elseIf?: BranchElseIfRule[] | null;
  else?: RuleNode[] | null;
}

export type RuleNode = FieldRule | ArrayRule | BranchRule;

export interface ConversionRules {
  inputFormat?: DataFormat;
  outputFormat?: DataFormat;
  rules?: RuleNode[] | null;
  validationErrors?: string[];
}

export interface ConversionResult {
  output?: unknown;
  errors: string[];
  warnings: string[];
  trace?: ConversionTraceEntry[];
}

export interface CompiledConversionPlan {
  rules: ConversionRules;
  cacheKey: string;
  apply(input: unknown, options?: ApplyConversionOptions): ConversionResult;
}

export interface ConversionRulesValidationResult {
  rules: ConversionRules;
  isValid: boolean;
  errors: string[];
}

export interface ConversionRulesLintResult {
  diagnostics: RuleLintDiagnostic[];
  hasErrors: boolean;
}

export enum RuleDoctorFindingSeverity {
  Info = "info",
  Warning = "warning",
  Error = "error"
}

export interface RuleDoctorFinding {
  code: string;
  stage: "validation" | "lint" | "runtime";
  severity: RuleDoctorFindingSeverity;
  rulePath: string;
  message: string;
  suggestion?: string;
}

export interface RuleDoctorOptions {
  sampleInputText?: string | null;
  inputFormat?: DataFormat | null;
  applySafeFixes?: boolean | null;
}

export interface RuleDoctorReport {
  findings: RuleDoctorFinding[];
  hasErrors: boolean;
  canApplySafeFixes: boolean;
  safeFixPreview: string[];
}

export interface RulesCompatibilityOptions {
  targetVersion: string;
}

export interface RulesCompatibilityDiagnostic {
  code: string;
  severity: RuleDoctorFindingSeverity;
  message: string;
  suggestion: string;
}

export interface RulesCompatibilityReport {
  targetVersion: string;
  schemaVersion: string | null;
  supportedRangeMin: string;
  supportedRangeMax: string;
  isCompatible: boolean;
  diagnostics: RulesCompatibilityDiagnostic[];
}

export interface ConversionRulesGenerationRequest {
  inputFormat: DataFormat;
  outputFormat: DataFormat;
  inputSample: string;
  outputSample: string;
  model?: string | null;
}

export interface ConversionRulesGenerator {
  generate(
    request: ConversionRulesGenerationRequest,
    options?: { signal?: AbortSignal }
  ): Promise<ConversionRules>;
}

export const rulesSchemaPath = "/schemas/rules/rules.schema.json";
export const rulesSchemaVersion = "1.0.0";
export const rulesSchemaVersionedPath = `/schemas/rules/v${rulesSchemaVersion}/schema.json`;
