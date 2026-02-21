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
        var fragments = NormalizeFragments(rules.Fragments, validationErrors);
        var fragmentResolver = new FragmentResolver(fragments, validationErrors);

        return rules with
        {
            InputFormat = rules.InputFormat,
            OutputFormat = rules.OutputFormat,
            Rules = NormalizeRuleNodes(rules.Rules, "rules", validationErrors, fragmentResolver),
            ValidationErrors = validationErrors
        };
    }

    private static List<RuleNode> NormalizeRuleNodes(
        IReadOnlyList<RuleNode>? nodes,
        string path,
        List<string> validationErrors,
        FragmentResolver fragmentResolver)
    {
        var normalized = new List<RuleNode>();
        var inputNodes = nodes ?? Array.Empty<RuleNode>();

        for (var index = 0; index < inputNodes.Count; index++)
        {
            var nodePath = $"{path}[{index}]";
            var node = fragmentResolver.ResolveUse(inputNodes[index] ?? new RuleNode(), nodePath);
            var kind = (node.Kind ?? string.Empty).Trim().ToLowerInvariant();

            if (kind.Length == 0)
            {
                validationErrors.Add($"{nodePath}: kind is required.");
                continue;
            }

            if (kind is not ("field" or "array" or "branch" or "map"))
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
                    normalized.Add(NormalizeArrayNode(node, nodePath, validationErrors, fragmentResolver));
                    break;
                case "map":
                    normalized.AddRange(ExpandMapNode(node, nodePath, validationErrors));
                    break;
                default:
                    normalized.Add(NormalizeBranchNode(node, nodePath, validationErrors, fragmentResolver));
                    break;
            }
        }

        return normalized;
    }

    private static RuleNode NormalizeFieldNode(RuleNode node, string nodePath, List<string> validationErrors)
    {
        var outputPaths = NormalizeOutputPaths(
            ResolveOutputPathAliases(node).Concat(node.OutputPaths),
            nodePath,
            validationErrors);
        if (outputPaths.Count == 0)
        {
            validationErrors.Add($"{nodePath}: outputPaths is required.");
        }

        var source = ResolveFieldSource(node, nodePath, validationErrors);

        return node with
        {
            Kind = "field",
            OutputPaths = outputPaths,
            Source = NormalizeValueSource(source),
            DefaultValue = node.DefaultValue ?? string.Empty
        };
    }

    private static IReadOnlyList<RuleNode> ExpandMapNode(
        RuleNode node,
        string nodePath,
        List<string> validationErrors)
    {
        if (node.Entries.Count == 0)
        {
            validationErrors.Add($"{nodePath}.entries: is required for map rule.");
            return Array.Empty<RuleNode>();
        }

        var expanded = new List<RuleNode>();
        for (var index = 0; index < node.Entries.Count; index++)
        {
            var entryPath = $"{nodePath}.entries[{index}]";
            var entry = node.Entries[index] ?? new RuleNode();
            expanded.Add(NormalizeFieldNode(entry with { Kind = "field" }, entryPath, validationErrors));
        }

        return expanded;
    }

    private static RuleNode NormalizeArrayNode(
        RuleNode node,
        string nodePath,
        List<string> validationErrors,
        FragmentResolver fragmentResolver)
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
            ItemRules = NormalizeRuleNodes(node.ItemRules, $"{nodePath}.itemRules", validationErrors, fragmentResolver)
        };
    }

    private static RuleNode NormalizeBranchNode(
        RuleNode node,
        string nodePath,
        List<string> validationErrors,
        FragmentResolver fragmentResolver)
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
                Then = NormalizeRuleNodes(branch.Then, $"{branchPath}.then", validationErrors, fragmentResolver)
            });
        }

        return node with
        {
            Kind = "branch",
            Expression = expression,
            Then = NormalizeRuleNodes(node.Then, $"{nodePath}.then", validationErrors, fragmentResolver),
            ElseIf = elseIf,
            Else = NormalizeRuleNodes(node.Else, $"{nodePath}.else", validationErrors, fragmentResolver)
        };
    }

    private static Dictionary<string, RuleNode> NormalizeFragments(
        IDictionary<string, RuleNode>? fragments,
        List<string> validationErrors)
    {
        var normalized = new Dictionary<string, RuleNode>(StringComparer.Ordinal);
        if (fragments == null)
        {
            return normalized;
        }

        foreach (var entry in fragments)
        {
            if (string.IsNullOrWhiteSpace(entry.Key))
            {
                validationErrors.Add("fragments: fragment name is required.");
                continue;
            }

            var name = entry.Key.Trim();
            if (normalized.ContainsKey(name))
            {
                validationErrors.Add($"fragments.{name}: duplicate fragment name.");
                continue;
            }

            normalized[name] = entry.Value ?? new RuleNode();
        }

        return normalized;
    }

    private sealed class FragmentResolver
    {
        private readonly IReadOnlyDictionary<string, RuleNode> _fragments;
        private readonly Dictionary<string, RuleNode> _cache = new(StringComparer.Ordinal);
        private readonly HashSet<string> _resolving = new(StringComparer.Ordinal);
        private readonly List<string> _validationErrors;

        internal FragmentResolver(
            IReadOnlyDictionary<string, RuleNode> fragments,
            List<string> validationErrors)
        {
            _fragments = fragments;
            _validationErrors = validationErrors;
        }

        internal RuleNode ResolveUse(RuleNode node, string nodePath)
        {
            if (string.IsNullOrWhiteSpace(node.Use))
            {
                return node;
            }

            var fragmentName = node.Use.Trim();
            var fragment = ResolveFragment(fragmentName, $"{nodePath}.use");
            if (fragment == null)
            {
                return node with { Use = null };
            }

            var merged = MergeRuleNodes(fragment, node);
            return merged with { Use = null };
        }

        private RuleNode? ResolveFragment(string name, string path)
        {
            if (_cache.TryGetValue(name, out var cached))
            {
                return cached;
            }

            if (!_fragments.TryGetValue(name, out var fragment))
            {
                _validationErrors.Add($"{path}: unknown fragment '{name}'.");
                return null;
            }

            if (_resolving.Contains(name))
            {
                _validationErrors.Add($"{path}: fragment '{name}' introduces a cycle.");
                return null;
            }

            _resolving.Add(name);
            var resolved = fragment;
            if (!string.IsNullOrWhiteSpace(fragment.Use))
            {
                resolved = ResolveUse(fragment, $"fragments.{name}");
            }
            _resolving.Remove(name);

            resolved = resolved with { Use = null };
            _cache[name] = resolved;
            return resolved;
        }

        private static RuleNode MergeRuleNodes(RuleNode fragment, RuleNode overrideNode)
        {
            return fragment with
            {
                Kind = string.IsNullOrWhiteSpace(overrideNode.Kind) ? fragment.Kind : overrideNode.Kind,
                OutputPaths = overrideNode.OutputPaths.Count > 0 ? overrideNode.OutputPaths : fragment.OutputPaths,
                Source = overrideNode.Source ?? fragment.Source,
                DefaultValue = string.IsNullOrEmpty(overrideNode.DefaultValue) ? fragment.DefaultValue : overrideNode.DefaultValue,
                InputPath = string.IsNullOrWhiteSpace(overrideNode.InputPath) ? fragment.InputPath : overrideNode.InputPath,
                ItemRules = overrideNode.ItemRules.Count > 0 ? overrideNode.ItemRules : fragment.ItemRules,
                CoerceSingle = overrideNode.CoerceSingle || fragment.CoerceSingle,
                Expression = string.IsNullOrWhiteSpace(overrideNode.Expression) ? fragment.Expression : overrideNode.Expression,
                Then = overrideNode.Then.Count > 0 ? overrideNode.Then : fragment.Then,
                ElseIf = overrideNode.ElseIf.Count > 0 ? overrideNode.ElseIf : fragment.ElseIf,
                Else = overrideNode.Else.Count > 0 ? overrideNode.Else : fragment.Else
            };
        }
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
            CustomTransform = source.CustomTransform?.Trim(),
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

    private static IEnumerable<string> ResolveOutputPathAliases(RuleNode node)
    {
        var outputPaths = new List<string>();
        outputPaths.AddRange(ReadStringOrStringArray(node.To));
        if (!string.IsNullOrWhiteSpace(node.OutputPath))
        {
            outputPaths.Add(node.OutputPath);
        }

        return outputPaths;
    }

    private static ValueSource ResolveFieldSource(RuleNode node, string nodePath, List<string> validationErrors)
    {
        if (node.Source is not null)
        {
            return node.Source;
        }

        var from = node.From?.Trim();
        if (node.Const.HasValue)
        {
            return new ValueSource
            {
                Type = "constant",
                Value = NormalizeConstValue(node.Const.Value)
            };
        }

        if (!string.IsNullOrWhiteSpace(node.As))
        {
            if (string.IsNullOrWhiteSpace(from))
            {
                validationErrors.Add($"{nodePath}: from is required when using as.");
                return new ValueSource();
            }

            var transform = ParseTransformAlias(node.As, nodePath, validationErrors);
            return new ValueSource
            {
                Type = "transform",
                Path = from,
                Transform = transform
            };
        }

        if (!string.IsNullOrWhiteSpace(from))
        {
            return new ValueSource
            {
                Type = "path",
                Path = from
            };
        }

        return new ValueSource();
    }

    private static string NormalizeConstValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => bool.TrueString.ToLowerInvariant(),
            JsonValueKind.False => bool.FalseString.ToLowerInvariant(),
            JsonValueKind.Null => string.Empty,
            _ => element.GetRawText()
        };
    }

    private static TransformType? ParseTransformAlias(
        string raw,
        string nodePath,
        List<string> validationErrors)
    {
        if (Enum.TryParse<TransformType>(raw, true, out var parsed))
        {
            return parsed;
        }

        validationErrors.Add($"{nodePath}: unsupported transform alias '{raw}'.");
        return null;
    }

    private static IEnumerable<string> ReadStringOrStringArray(JsonElement? element)
    {
        if (!element.HasValue)
        {
            return Array.Empty<string>();
        }

        return element.Value.ValueKind switch
        {
            JsonValueKind.String => new[] { element.Value.GetString() ?? string.Empty },
            JsonValueKind.Array => element.Value
                .EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString() ?? string.Empty)
                .ToArray(),
            _ => Array.Empty<string>()
        };
    }
}
