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

export interface ConditionElseIfBranch {
  expression?: string | null;
  source?: ValueSource | null;
  value?: string | null;
}

export interface ValueSource {
  type: string;
  path?: string | null;
  paths?: string[] | null;
  value?: string | null;
  transform?: TransformType | null;
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
}

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
