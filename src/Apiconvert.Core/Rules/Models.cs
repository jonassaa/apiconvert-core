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
    Concat
}

/// <summary>
/// Operators for conditional checks.
/// </summary>
public enum ConditionOperator
{
    /// <summary>
    /// Value exists.
    /// </summary>
    Exists,
    /// <summary>
    /// Value equals.
    /// </summary>
    Equals,
    /// <summary>
    /// Value does not equal.
    /// </summary>
    NotEquals,
    /// <summary>
    /// Value includes the operand.
    /// </summary>
    Includes,
    /// <summary>
    /// Value is greater than the operand.
    /// </summary>
    Gt,
    /// <summary>
    /// Value is less than the operand.
    /// </summary>
    Lt
}

/// <summary>
/// Represents a conditional rule applied to a value source.
/// </summary>
public sealed record ConditionRule
{
    /// <summary>
    /// Input path used for the condition.
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// Operator used for the condition.
    /// </summary>
    [JsonPropertyName("operator")]
    public ConditionOperator Operator { get; init; }

    /// <summary>
    /// Operand value for the condition.
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
    /// Optional condition that selects between true/false values.
    /// </summary>
    [JsonPropertyName("condition")]
    public ConditionRule? Condition { get; init; }

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
}

/// <summary>
/// Defines a mapping from a value source to an output path.
/// </summary>
public sealed record FieldRule
{
    /// <summary>
    /// Output path for the mapped value.
    /// </summary>
    [JsonPropertyName("outputPath")]
    public string OutputPath { get; init; } = string.Empty;

    /// <summary>
    /// Source definition for the mapped value.
    /// </summary>
    [JsonPropertyName("source")]
    public ValueSource Source { get; init; } = new();

    /// <summary>
    /// Default value when the source is missing.
    /// </summary>
    [JsonPropertyName("defaultValue")]
    public string DefaultValue { get; init; } = string.Empty;
}

/// <summary>
/// Defines an array mapping from input to output.
/// </summary>
public sealed record ArrayRule
{
    /// <summary>
    /// Input array path.
    /// </summary>
    [JsonPropertyName("inputPath")]
    public string InputPath { get; init; } = string.Empty;

    /// <summary>
    /// Output array path.
    /// </summary>
    [JsonPropertyName("outputPath")]
    public string OutputPath { get; init; } = string.Empty;

    /// <summary>
    /// Field mappings applied to each array item.
    /// </summary>
    [JsonPropertyName("itemMappings")]
    public List<FieldRule> ItemMappings { get; init; } = new();

    /// <summary>
    /// Whether a single item should be coerced into an array.
    /// </summary>
    [JsonPropertyName("coerceSingle")]
    public bool CoerceSingle { get; init; }
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
    /// Field mappings for scalar values.
    /// </summary>
    [JsonPropertyName("fieldMappings")]
    public List<FieldRule> FieldMappings { get; init; } = new();

    /// <summary>
    /// Array mappings.
    /// </summary>
    [JsonPropertyName("arrayMappings")]
    public List<ArrayRule> ArrayMappings { get; init; } = new();
}

/// <summary>
/// Legacy mapping row used for older rule formats.
/// </summary>
public sealed record LegacyMappingRow
{
    /// <summary>
    /// Output path for the mapped value.
    /// </summary>
    [JsonPropertyName("outputPath")]
    public string OutputPath { get; init; } = string.Empty;

    /// <summary>
    /// Source type for the legacy mapping.
    /// </summary>
    [JsonPropertyName("sourceType")]
    public string SourceType { get; init; } = "path";

    /// <summary>
    /// Source value for the legacy mapping.
    /// </summary>
    [JsonPropertyName("sourceValue")]
    public string SourceValue { get; init; } = string.Empty;

    /// <summary>
    /// Optional transform type for the legacy mapping.
    /// </summary>
    [JsonPropertyName("transformType")]
    public TransformType? TransformType { get; init; }

    /// <summary>
    /// Default value when the source is missing.
    /// </summary>
    [JsonPropertyName("defaultValue")]
    public string? DefaultValue { get; init; }
}

/// <summary>
/// Legacy mapping configuration wrapper.
/// </summary>
public sealed record LegacyMappingConfig
{
    /// <summary>
    /// Legacy mapping rows.
    /// </summary>
    [JsonPropertyName("rows")]
    public List<LegacyMappingRow> Rows { get; init; } = new();
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
}
