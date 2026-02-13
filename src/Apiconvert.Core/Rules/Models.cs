using System.Text.Json.Serialization;

namespace Apiconvert.Core.Rules;

/// <summary>
/// Supported payload formats.
/// </summary>
public enum DataFormat
{
    /// <summary>
    /// JSON format.
    /// </summary>
    Json,
    /// <summary>
    /// XML format.
    /// </summary>
    Xml,
    /// <summary>
    /// URL query-string format.
    /// </summary>
    Query
}

/// <summary>
/// Built-in value transformations.
/// </summary>
public enum TransformType
{
    /// <summary>
    /// Lowercase the value.
    /// </summary>
    ToLowerCase,
    /// <summary>
    /// Uppercase the value.
    /// </summary>
    ToUpperCase,
    /// <summary>
    /// Convert the value to a number.
    /// </summary>
    Number,
    /// <summary>
    /// Convert the value to a boolean.
    /// </summary>
    Boolean,
    /// <summary>
    /// Concatenate multiple values.
    /// </summary>
    Concat,
    /// <summary>
    /// Split a string into tokens and return a token by index.
    /// </summary>
    Split
}

/// <summary>
/// Strategies for combining multiple input values.
/// </summary>
public enum MergeMode
{
    /// <summary>
    /// Concatenate values as text.
    /// </summary>
    Concat,
    /// <summary>
    /// Return the first non-empty value.
    /// </summary>
    FirstNonEmpty,
    /// <summary>
    /// Return all values as an array.
    /// </summary>
    Array
}

/// <summary>
/// Output behavior for condition sources.
/// </summary>
public enum ConditionOutputMode
{
    /// <summary>
    /// Return the selected branch value.
    /// </summary>
    Branch,
    /// <summary>
    /// Return the boolean expression match result.
    /// </summary>
    Match
}

/// <summary>
/// Else-if branch for condition sources.
/// </summary>
public sealed record ConditionElseIfBranch
{
    /// <summary>
    /// Condition expression for this else-if branch.
    /// </summary>
    [JsonPropertyName("expression")]
    public string? Expression { get; init; }

    /// <summary>
    /// Nested source used when this else-if matches.
    /// </summary>
    [JsonPropertyName("source")]
    public ValueSource? Source { get; init; }

    /// <summary>
    /// Legacy literal used when this else-if matches.
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; init; }
}

/// <summary>
/// Describes where a field value is sourced from.
/// </summary>
public sealed record ValueSource
{
    /// <summary>
    /// Source type (for example, "path" or "value").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "path";

    /// <summary>
    /// Input path for the source type "path".
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; init; }

    /// <summary>
    /// Input paths for multi-source merge.
    /// </summary>
    [JsonPropertyName("paths")]
    public List<string> Paths { get; init; } = new();

    /// <summary>
    /// Literal value for the source type "value".
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; init; }

    /// <summary>
    /// Optional transformation to apply to the value.
    /// </summary>
    [JsonPropertyName("transform")]
    public TransformType? Transform { get; init; }

    /// <summary>
    /// Optional condition expression for source type "condition".
    /// </summary>
    [JsonPropertyName("expression")]
    public string? Expression { get; init; }

    /// <summary>
    /// Value used when the condition is true.
    /// </summary>
    [JsonPropertyName("trueValue")]
    public string? TrueValue { get; init; }

    /// <summary>
    /// Value used when the condition is false.
    /// </summary>
    [JsonPropertyName("falseValue")]
    public string? FalseValue { get; init; }

    /// <summary>
    /// Source used when the condition is true.
    /// </summary>
    [JsonPropertyName("trueSource")]
    public ValueSource? TrueSource { get; init; }

    /// <summary>
    /// Source used when the condition is false.
    /// </summary>
    [JsonPropertyName("falseSource")]
    public ValueSource? FalseSource { get; init; }

    /// <summary>
    /// Else-if branches evaluated when the primary expression is false.
    /// </summary>
    [JsonPropertyName("elseIf")]
    public List<ConditionElseIfBranch> ElseIf { get; init; } = new();

    /// <summary>
    /// Output behavior for condition sources.
    /// </summary>
    [JsonPropertyName("conditionOutput")]
    public ConditionOutputMode? ConditionOutput { get; init; }

    /// <summary>
    /// Merge mode when source type is "merge".
    /// </summary>
    [JsonPropertyName("mergeMode")]
    public MergeMode? MergeMode { get; init; }

    /// <summary>
    /// Delimiter used by concat merge mode and split transform.
    /// </summary>
    [JsonPropertyName("separator")]
    public string? Separator { get; init; }

    /// <summary>
    /// Token index used by split transform.
    /// </summary>
    [JsonPropertyName("tokenIndex")]
    public int? TokenIndex { get; init; }

    /// <summary>
    /// Trim tokens after split transform. Defaults to true when omitted.
    /// </summary>
    [JsonPropertyName("trimAfterSplit")]
    public bool? TrimAfterSplit { get; init; }
}

/// <summary>
/// Else-if branch for top-level branch rules.
/// </summary>
public sealed record BranchElseIfRule
{
    /// <summary>
    /// Condition expression for this else-if branch.
    /// </summary>
    [JsonPropertyName("expression")]
    public string? Expression { get; init; }

    /// <summary>
    /// Rules executed when the branch matches.
    /// </summary>
    [JsonPropertyName("then")]
    public List<RuleNode> Then { get; init; } = new();
}

/// <summary>
/// Recursive conversion rule node.
/// </summary>
public sealed record RuleNode
{
    /// <summary>
    /// Rule discriminator. Supported values are "field", "array", and "branch".
    /// </summary>
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = string.Empty;

    /// <summary>
    /// Output paths for this rule.
    /// </summary>
    [JsonPropertyName("outputPaths")]
    public List<string> OutputPaths { get; init; } = new();

    /// <summary>
    /// Source definition for field rules.
    /// </summary>
    [JsonPropertyName("source")]
    public ValueSource? Source { get; init; }

    /// <summary>
    /// Default value when the field source is missing.
    /// </summary>
    [JsonPropertyName("defaultValue")]
    public string DefaultValue { get; init; } = string.Empty;

    /// <summary>
    /// Input array path for array rules.
    /// </summary>
    [JsonPropertyName("inputPath")]
    public string InputPath { get; init; } = string.Empty;

    /// <summary>
    /// Item rules for array rules.
    /// </summary>
    [JsonPropertyName("itemRules")]
    public List<RuleNode> ItemRules { get; init; } = new();

    /// <summary>
    /// Whether a single value should be coerced into an array.
    /// </summary>
    [JsonPropertyName("coerceSingle")]
    public bool CoerceSingle { get; init; }

    /// <summary>
    /// Branch expression for branch rules.
    /// </summary>
    [JsonPropertyName("expression")]
    public string? Expression { get; init; }

    /// <summary>
    /// Rules executed when the branch expression matches.
    /// </summary>
    [JsonPropertyName("then")]
    public List<RuleNode> Then { get; init; } = new();

    /// <summary>
    /// Else-if branches evaluated in order.
    /// </summary>
    [JsonPropertyName("elseIf")]
    public List<BranchElseIfRule> ElseIf { get; init; } = new();

    /// <summary>
    /// Rules executed when no branch condition matches.
    /// </summary>
    [JsonPropertyName("else")]
    public List<RuleNode> Else { get; init; } = new();
}

/// <summary>
/// Canonical rules model used by the conversion engine.
/// </summary>
public sealed record ConversionRules
{
    /// <summary>
    /// Rules version.
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; init; } = 2;

    /// <summary>
    /// Input payload format.
    /// </summary>
    [JsonPropertyName("inputFormat")]
    public DataFormat InputFormat { get; init; } = DataFormat.Json;

    /// <summary>
    /// Output payload format.
    /// </summary>
    [JsonPropertyName("outputFormat")]
    public DataFormat OutputFormat { get; init; } = DataFormat.Json;

    /// <summary>
    /// Ordered, recursive conversion rules.
    /// </summary>
    [JsonPropertyName("rules")]
    public List<RuleNode> Rules { get; init; } = new();

    /// <summary>
    /// Validation errors produced during normalization.
    /// </summary>
    [JsonIgnore]
    public List<string> ValidationErrors { get; init; } = new();
}

/// <summary>
/// Result of applying conversion rules.
/// </summary>
public sealed record ConversionResult
{
    /// <summary>
    /// Converted output payload.
    /// </summary>
    public object? Output { get; init; }

    /// <summary>
    /// Errors encountered during conversion.
    /// </summary>
    public List<string> Errors { get; init; } = new();

    /// <summary>
    /// Non-fatal warnings encountered during conversion.
    /// </summary>
    public List<string> Warnings { get; init; } = new();
}
