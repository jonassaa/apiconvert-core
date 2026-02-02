using System.Text.Json.Serialization;

namespace Apiconvert.Core.Rules;

public enum DataFormat
{
    Json,
    Xml,
    Query
}

public enum TransformType
{
    ToLowerCase,
    ToUpperCase,
    Number,
    Boolean,
    Concat
}

public enum ConditionOperator
{
    Exists,
    Equals,
    NotEquals,
    Includes,
    Gt,
    Lt
}

public sealed record ConditionRule
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("operator")]
    public ConditionOperator Operator { get; init; }

    [JsonPropertyName("value")]
    public string? Value { get; init; }
}

public sealed record ValueSource
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "path";

    [JsonPropertyName("path")]
    public string? Path { get; init; }

    [JsonPropertyName("value")]
    public string? Value { get; init; }

    [JsonPropertyName("transform")]
    public TransformType? Transform { get; init; }

    [JsonPropertyName("condition")]
    public ConditionRule? Condition { get; init; }

    [JsonPropertyName("trueValue")]
    public string? TrueValue { get; init; }

    [JsonPropertyName("falseValue")]
    public string? FalseValue { get; init; }
}

public sealed record FieldRule
{
    [JsonPropertyName("outputPath")]
    public string OutputPath { get; init; } = string.Empty;

    [JsonPropertyName("source")]
    public ValueSource Source { get; init; } = new();

    [JsonPropertyName("defaultValue")]
    public string DefaultValue { get; init; } = string.Empty;
}

public sealed record ArrayRule
{
    [JsonPropertyName("inputPath")]
    public string InputPath { get; init; } = string.Empty;

    [JsonPropertyName("outputPath")]
    public string OutputPath { get; init; } = string.Empty;

    [JsonPropertyName("itemMappings")]
    public List<FieldRule> ItemMappings { get; init; } = new();

    [JsonPropertyName("coerceSingle")]
    public bool CoerceSingle { get; init; }
}

public sealed record ConversionRules
{
    [JsonPropertyName("version")]
    public int Version { get; init; } = 2;

    [JsonPropertyName("inputFormat")]
    public DataFormat InputFormat { get; init; } = DataFormat.Json;

    [JsonPropertyName("outputFormat")]
    public DataFormat OutputFormat { get; init; } = DataFormat.Json;

    [JsonPropertyName("fieldMappings")]
    public List<FieldRule> FieldMappings { get; init; } = new();

    [JsonPropertyName("arrayMappings")]
    public List<ArrayRule> ArrayMappings { get; init; } = new();
}

public sealed record LegacyMappingRow
{
    [JsonPropertyName("outputPath")]
    public string OutputPath { get; init; } = string.Empty;

    [JsonPropertyName("sourceType")]
    public string SourceType { get; init; } = "path";

    [JsonPropertyName("sourceValue")]
    public string SourceValue { get; init; } = string.Empty;

    [JsonPropertyName("transformType")]
    public TransformType? TransformType { get; init; }

    [JsonPropertyName("defaultValue")]
    public string? DefaultValue { get; init; }
}

public sealed record LegacyMappingConfig
{
    [JsonPropertyName("rows")]
    public List<LegacyMappingRow> Rows { get; init; } = new();
}

public sealed record ConversionResult
{
    public object? Output { get; init; }
    public List<string> Errors { get; init; } = new();
}
