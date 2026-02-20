using System.Text.Json;
using Apiconvert.Core.Rules;

namespace Apiconvert.Core.Converters;

internal static class RulesNormalizer
{
    internal static ConversionRules NormalizeConversionRules(object? raw)
    {
        var validationErrors = new List<string>();

        if (raw is ConversionRules rules)
        {
            return NormalizeRules(rules, validationErrors);
        }

        if (raw is string json)
        {
            try
            {
                var doc = JsonDocument.Parse(json);
                raw = doc.RootElement.Clone();
            }
            catch (Exception ex)
            {
                validationErrors.Add($"rules: invalid JSON payload. {ex.Message}");
                return new ConversionRules { ValidationErrors = validationErrors };
            }
        }

        if (raw is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (TryDeserialize<ConversionRules>(element, out var parsedRules) && parsedRules != null)
                {
                    return NormalizeRules(parsedRules, validationErrors);
                }

                validationErrors.Add("rules: JSON payload could not be deserialized into ConversionRules.");
                return new ConversionRules { ValidationErrors = validationErrors };
            }

            validationErrors.Add($"rules: expected JSON object but received {element.ValueKind}.");
            return new ConversionRules { ValidationErrors = validationErrors };
        }

        if (raw != null)
        {
            validationErrors.Add($"rules: unsupported rules input type '{raw.GetType().FullName}'.");
            return new ConversionRules { ValidationErrors = validationErrors };
        }

        return new ConversionRules();
    }

    private static ConversionRules NormalizeRules(ConversionRules rules, List<string> validationErrors)
    {
        return rules with
        {
            InputFormat = rules.InputFormat,
            OutputFormat = rules.OutputFormat,
            Rules = NormalizeRuleNodes(rules.Rules, "rules", validationErrors),
            ValidationErrors = validationErrors
        };
    }

    private static List<RuleNode> NormalizeRuleNodes(
        IReadOnlyList<RuleNode>? nodes,
        string path,
        List<string> validationErrors)
    {
        var normalized = new List<RuleNode>();
        var inputNodes = nodes ?? Array.Empty<RuleNode>();

        for (var index = 0; index < inputNodes.Count; index++)
        {
            var node = inputNodes[index] ?? new RuleNode();
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

            switch (kind)
            {
                case "field":
                    normalized.Add(NormalizeFieldNode(node, nodePath, validationErrors));
                    break;
                case "array":
                    normalized.Add(NormalizeArrayNode(node, nodePath, validationErrors));
                    break;
                default:
                    normalized.Add(NormalizeBranchNode(node, nodePath, validationErrors));
                    break;
            }
        }

        return normalized;
    }

    private static RuleNode NormalizeFieldNode(RuleNode node, string nodePath, List<string> validationErrors)
    {
        var outputPaths = NormalizeOutputPaths(node.OutputPaths, nodePath, validationErrors);
        if (outputPaths.Count == 0)
        {
            validationErrors.Add($"{nodePath}: outputPaths is required.");
        }

        return node with
        {
            Kind = "field",
            OutputPaths = outputPaths,
            Source = NormalizeValueSource(node.Source ?? new ValueSource()),
            DefaultValue = node.DefaultValue ?? string.Empty
        };
    }

    private static RuleNode NormalizeArrayNode(RuleNode node, string nodePath, List<string> validationErrors)
    {
        var outputPaths = NormalizeOutputPaths(node.OutputPaths, nodePath, validationErrors);
        if (string.IsNullOrWhiteSpace(node.InputPath))
        {
            validationErrors.Add($"{nodePath}: inputPath is required.");
        }

        if (outputPaths.Count == 0)
        {
            validationErrors.Add($"{nodePath}: outputPaths is required.");
        }

        return node with
        {
            Kind = "array",
            InputPath = node.InputPath?.Trim() ?? string.Empty,
            OutputPaths = outputPaths,
            ItemRules = NormalizeRuleNodes(node.ItemRules, $"{nodePath}.itemRules", validationErrors)
        };
    }

    private static RuleNode NormalizeBranchNode(RuleNode node, string nodePath, List<string> validationErrors)
    {
        var expression = NormalizeExpression(node.Expression);
        if (expression == null)
        {
            validationErrors.Add($"{nodePath}: expression is required.");
        }

        var elseIf = new List<BranchElseIfRule>();
        var elseIfRules = node.ElseIf;
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
                Then = NormalizeRuleNodes(branch.Then, $"{branchPath}.then", validationErrors)
            });
        }

        return node with
        {
            Kind = "branch",
            Expression = expression,
            Then = NormalizeRuleNodes(node.Then, $"{nodePath}.then", validationErrors),
            ElseIf = elseIf,
            Else = NormalizeRuleNodes(node.Else, $"{nodePath}.else", validationErrors)
        };
    }

    private static List<string> NormalizeOutputPaths(
        IEnumerable<string>? paths,
        string nodePath,
        List<string> validationErrors)
    {
        var normalizedPaths = new List<string>();

        foreach (var rawPath in paths ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                continue;
            }

            var normalized = NormalizeWritePath(rawPath);
            if (normalized == "$")
            {
                validationErrors.Add($"{nodePath}: output path '$' is not supported.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(normalized))
            {
                continue;
            }

            if (!normalizedPaths.Contains(normalized, StringComparer.Ordinal))
            {
                normalizedPaths.Add(normalized);
            }
        }

        return normalizedPaths;
    }

    private static string NormalizeWritePath(string path)
    {
        if (path.StartsWith("$.", StringComparison.Ordinal))
        {
            return path[2..].Trim();
        }

        if (path == "$")
        {
            return "$";
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
            Type = (source.Type ?? string.Empty).Trim().ToLowerInvariant(),
            Path = source.Path?.Trim(),
            Paths = source.Paths ?? new List<string>(),
            Expression = NormalizeExpression(source.Expression),
            Separator = source.Separator,
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
