using System.Text.Json;
using Apiconvert.Core.Rules;

namespace Apiconvert.Core.Converters;

internal static class RulesNormalizer
{
    private static readonly HashSet<string> ForbiddenLegacyProperties = new(StringComparer.Ordinal)
    {
        "fieldMappings",
        "arrayMappings",
        "itemMappings",
        "outputPath"
    };

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
                var validationErrors = new List<string>();
                CollectLegacyPropertyErrors(element, "$", validationErrors);

                if (TryDeserialize<ConversionRules>(element, out var parsedRules) && parsedRules != null)
                {
                    return NormalizeRules(parsedRules, validationErrors);
                }

                if (validationErrors.Count > 0)
                {
                    return new ConversionRules { ValidationErrors = validationErrors };
                }
            }
        }

        return new ConversionRules();
    }

    private static ConversionRules NormalizeRules(ConversionRules rules, List<string>? seedErrors = null)
    {
        var validationErrors = seedErrors ?? new List<string>();

        return rules with
        {
            Version = rules.Version <= 0 ? 2 : rules.Version,
            InputFormat = rules.InputFormat,
            OutputFormat = rules.OutputFormat,
            Rules = NormalizeRuleNodes(rules.Rules ?? new List<RuleNode>(), "rules", validationErrors),
            ValidationErrors = validationErrors
        };
    }

    private static List<RuleNode> NormalizeRuleNodes(
        List<RuleNode> nodes,
        string path,
        List<string> validationErrors)
    {
        var normalized = new List<RuleNode>();

        for (var index = 0; index < nodes.Count; index++)
        {
            var node = nodes[index] ?? new RuleNode();
            var nodePath = $"{path}[{index}]";
            var kind = (node.Kind ?? string.Empty).Trim().ToLowerInvariant();

            if (kind.Length == 0)
            {
                validationErrors.Add($"{nodePath}: kind is required.");
                continue;
            }

            if (kind is not ("field" or "array" or "branch"))
            {
                validationErrors.Add($"{nodePath}: unsupported kind '{node.Kind}'.");
                continue;
            }

            if (kind == "field")
            {
                var outputPaths = NormalizeOutputPaths(node.OutputPaths);
                if (outputPaths.Count == 0)
                {
                    validationErrors.Add($"{nodePath}: outputPaths is required.");
                }

                normalized.Add(node with
                {
                    Kind = kind,
                    OutputPaths = outputPaths,
                    Source = NormalizeValueSource(node.Source ?? new ValueSource()),
                    DefaultValue = node.DefaultValue ?? string.Empty
                });
                continue;
            }

            if (kind == "array")
            {
                var outputPaths = NormalizeOutputPaths(node.OutputPaths);
                if (string.IsNullOrWhiteSpace(node.InputPath))
                {
                    validationErrors.Add($"{nodePath}: inputPath is required.");
                }

                if (outputPaths.Count == 0)
                {
                    validationErrors.Add($"{nodePath}: outputPaths is required.");
                }

                normalized.Add(node with
                {
                    Kind = kind,
                    InputPath = node.InputPath?.Trim() ?? string.Empty,
                    OutputPaths = outputPaths,
                    ItemRules = NormalizeRuleNodes(node.ItemRules ?? new List<RuleNode>(), $"{nodePath}.itemRules", validationErrors)
                });
                continue;
            }

            var expression = NormalizeExpression(node.Expression);
            if (expression == null)
            {
                validationErrors.Add($"{nodePath}: expression is required.");
            }

            var elseIf = new List<BranchElseIfRule>();
            var elseIfRules = node.ElseIf ?? new List<BranchElseIfRule>();
            for (var elseIfIndex = 0; elseIfIndex < elseIfRules.Count; elseIfIndex++)
            {
                var branch = elseIfRules[elseIfIndex] ?? new BranchElseIfRule();
                var branchPath = $"{nodePath}.elseIf[{elseIfIndex}]";
                var branchExpression = NormalizeExpression(branch.Expression);
                if (branchExpression == null)
                {
                    validationErrors.Add($"{branchPath}: expression is required.");
                }

                elseIf.Add(branch with
                {
                    Expression = branchExpression,
                    Then = NormalizeRuleNodes(branch.Then ?? new List<RuleNode>(), $"{branchPath}.then", validationErrors)
                });
            }

            normalized.Add(node with
            {
                Kind = kind,
                Expression = expression,
                Then = NormalizeRuleNodes(node.Then ?? new List<RuleNode>(), $"{nodePath}.then", validationErrors),
                ElseIf = elseIf,
                Else = NormalizeRuleNodes(node.Else ?? new List<RuleNode>(), $"{nodePath}.else", validationErrors)
            });
        }

        return normalized;
    }

    private static List<string> NormalizeOutputPaths(IEnumerable<string>? paths)
    {
        return (paths ?? Array.Empty<string>())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(NormalizeWritePath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static string NormalizeWritePath(string path)
    {
        if (path.StartsWith("$.", StringComparison.Ordinal))
        {
            return path[2..].Trim();
        }

        if (path == "$")
        {
            return string.Empty;
        }

        return path.Trim();
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

    private static void CollectLegacyPropertyErrors(JsonElement element, string path, List<string> errors)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var propertyPath = $"{path}.{property.Name}";
                    if (ForbiddenLegacyProperties.Contains(property.Name))
                    {
                        errors.Add($"{propertyPath}: legacy property '{property.Name}' is not supported; use rules[] with outputPaths/itemRules.");
                    }

                    CollectLegacyPropertyErrors(property.Value, propertyPath, errors);
                }
                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    CollectLegacyPropertyErrors(item, $"{path}[{index}]", errors);
                    index++;
                }
                break;
        }
    }
}
