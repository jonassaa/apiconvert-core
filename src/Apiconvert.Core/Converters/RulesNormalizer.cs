using System.Text.Json;
using Apiconvert.Core.Rules;

namespace Apiconvert.Core.Converters;

internal static class RulesNormalizer
{
    internal static ConversionRules NormalizeConversionRules(object? raw)
    {
        if (raw is ConversionRules rules)
        {
            return NormalizeRules(rules);
        }

        if (raw is string json)
        {
            try
            {
                var doc = JsonDocument.Parse(json);
                raw = doc.RootElement.Clone();
            }
            catch
            {
                return new ConversionRules();
            }
        }

        if (raw is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (TryDeserialize<ConversionRules>(element, out var parsedRules) && parsedRules != null && parsedRules.Version == 2)
                {
                    return NormalizeRules(parsedRules);
                }

                if (TryDeserialize<LegacyMappingConfig>(element, out var legacy) && legacy != null)
                {
                    return NormalizeLegacyRules(legacy);
                }
            }
        }

        return new ConversionRules();
    }

    private static ConversionRules NormalizeRules(ConversionRules rules)
    {
        return rules with
        {
            InputFormat = rules.InputFormat,
            OutputFormat = rules.OutputFormat,
            FieldMappings = rules.FieldMappings
                .Select(rule => rule with
                {
                    DefaultValue = rule.DefaultValue ?? string.Empty,
                    OutputPaths = rule.OutputPaths ?? new List<string>(),
                    Source = NormalizeValueSource(rule.Source)
                })
                .ToList(),
            ArrayMappings = rules.ArrayMappings
                .Select(mapping => mapping with
                {
                    CoerceSingle = mapping.CoerceSingle,
                    OutputPaths = mapping.OutputPaths ?? new List<string>(),
                    ItemMappings = mapping.ItemMappings
                        .Select(rule => rule with
                        {
                            DefaultValue = rule.DefaultValue ?? string.Empty,
                            OutputPaths = rule.OutputPaths ?? new List<string>(),
                            Source = NormalizeValueSource(rule.Source)
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    private static ConversionRules NormalizeLegacyRules(LegacyMappingConfig legacy)
    {
        var fieldMappings = legacy.Rows.Select(row =>
        {
            var sourceType = row.SourceType ?? "path";
            ValueSource source = sourceType switch
            {
                "constant" => new ValueSource { Type = "constant", Value = row.SourceValue },
                "transform" => new ValueSource
                {
                    Type = "transform",
                    Path = row.SourceValue,
                    Transform = row.TransformType ?? TransformType.ToLowerCase
                },
                _ => new ValueSource { Type = "path", Path = row.SourceValue }
            };

            return new FieldRule
            {
                OutputPath = row.OutputPath,
                Source = source,
                DefaultValue = row.DefaultValue ?? string.Empty
            };
        }).ToList();

        return new ConversionRules
        {
            Version = 2,
            InputFormat = DataFormat.Json,
            OutputFormat = DataFormat.Json,
            FieldMappings = fieldMappings,
            ArrayMappings = new List<ArrayRule>()
        };
    }

    private static bool TryDeserialize<T>(JsonElement element, out T? value)
    {
        try
        {
            value = JsonSerializer.Deserialize<T>(element.GetRawText(), JsonDefaults.Options);
            return value != null;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    private static string? NormalizeExpression(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return null;
        }

        return expression.Trim();
    }

    private static ValueSource NormalizeValueSource(ValueSource source)
    {
        return source with
        {
            Paths = source.Paths ?? new List<string>(),
            Expression = NormalizeExpression(source.Expression),
            TrueSource = source.TrueSource is null ? null : NormalizeValueSource(source.TrueSource),
            FalseSource = source.FalseSource is null ? null : NormalizeValueSource(source.FalseSource),
            ElseIf = (source.ElseIf ?? new List<ConditionElseIfBranch>())
                .Select(branch => branch with
                {
                    Expression = NormalizeExpression(branch.Expression),
                    Source = branch.Source is null ? null : NormalizeValueSource(branch.Source)
                })
                .ToList()
        };
    }
}
