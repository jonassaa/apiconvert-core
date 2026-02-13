using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Apiconvert.Core.Rules;

namespace Apiconvert.Core.Converters;

internal static partial class MappingExecutor
{
    private static object? ResolveSourceValue(
        object? root,
        object? item,
        ValueSource source,
        List<string> errors,
        string errorContext,
        int depth = 0)
    {
        if (depth > MaxConditionBranchDepth)
        {
            errors.Add($"{errorContext}: condition/source recursion limit exceeded.");
            return null;
        }

        switch (source.Type)
        {
            case "constant":
                return ParsePrimitive(source.Value ?? string.Empty);
            case "path":
                return ResolvePathValue(root, item, source.Path ?? string.Empty);
            case "merge":
                return ResolveMergeSourceValue(root, item, source);
            case "transform":
            {
                if (source.Transform == TransformType.Concat)
                {
                    var tokens = (source.Path ?? string.Empty)
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(token => token.Trim())
                        .Where(token => token.Length > 0);
                    var sb = new StringBuilder();
                    foreach (var token in tokens)
                    {
                        if (token.StartsWith("const:", StringComparison.OrdinalIgnoreCase))
                        {
                            sb.Append(token.Replace("const:", string.Empty, StringComparison.OrdinalIgnoreCase));
                            continue;
                        }
                        sb.Append(ResolvePathValue(root, item, token) ?? string.Empty);
                    }
                    return sb.ToString();
                }

                if (source.Transform == TransformType.Split)
                {
                    var splitBaseValue = ResolvePathValue(root, item, source.Path ?? string.Empty);
                    if (splitBaseValue == null)
                    {
                        return null;
                    }

                    var separator = source.Separator ?? " ";
                    if (separator.Length == 0)
                    {
                        return null;
                    }
                    var tokenIndex = source.TokenIndex ?? 0;
                    var trimAfterSplit = source.TrimAfterSplit ?? true;
                    var rawTokens = splitBaseValue
                        .ToString()
                        ?.Split(separator, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                    var tokens = trimAfterSplit
                        ? rawTokens.Select(token => token.Trim()).Where(token => token.Length > 0).ToArray()
                        : rawTokens;

                    if (tokenIndex < 0)
                    {
                        tokenIndex = tokens.Length + tokenIndex;
                    }

                    if (tokenIndex < 0 || tokenIndex >= tokens.Length)
                    {
                        return null;
                    }

                    return tokens[tokenIndex];
                }

                var baseValue = ResolvePathValue(root, item, source.Path ?? string.Empty);
                return ResolveTransform(baseValue, source.Transform ?? TransformType.ToLowerCase);
            }
            case "condition":
            {
                return ResolveConditionSourceValue(root, item, source, errors, errorContext, depth);
            }
            default:
                return null;
        }
    }

    private static object? ResolveConditionSourceValue(
        object? root,
        object? item,
        ValueSource source,
        List<string> errors,
        string errorContext,
        int depth)
    {
        var matched = EvaluateCondition(root, item, source.Expression, errors, errorContext, "condition expression");

        if (source.ConditionOutput == ConditionOutputMode.Match)
        {
            return matched;
        }

        if (matched)
        {
            return ResolveConditionBranchValue(
                root,
                item,
                source.TrueSource,
                source.TrueValue,
                errors,
                $"{errorContext} true branch",
                depth);
        }

        for (var index = 0; index < source.ElseIf.Count; index++)
        {
            var branch = source.ElseIf[index];
            var branchMatched = EvaluateCondition(
                root,
                item,
                branch.Expression,
                errors,
                $"{errorContext} elseIf[{index}]",
                "elseIf expression");
            if (!branchMatched)
            {
                continue;
            }

            return ResolveConditionBranchValue(
                root,
                item,
                branch.Source,
                branch.Value,
                errors,
                $"{errorContext} elseIf[{index}] branch",
                depth);
        }

        return ResolveConditionBranchValue(
            root,
            item,
            source.FalseSource,
            source.FalseValue,
            errors,
            $"{errorContext} false branch",
            depth);
    }

    private static bool EvaluateCondition(
        object? root,
        object? item,
        string? expression,
        List<string> errors,
        string errorContext,
        string label)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            errors.Add($"{errorContext}: {label} is required.");
            return false;
        }

        var evaluation = TryEvaluateConditionExpression(root, item, expression, out var matched, out var error);
        if (!evaluation)
        {
            errors.Add($"{errorContext}: invalid {label} \"{expression}\": {error}");
            return false;
        }

        return matched;
    }

    private static object? ResolveConditionBranchValue(
        object? root,
        object? item,
        ValueSource? branchSource,
        string? branchValue,
        List<string> errors,
        string errorContext,
        int depth)
    {
        if (branchSource != null)
        {
            return ResolveSourceValue(root, item, branchSource, errors, errorContext, depth + 1);
        }

        return ParsePrimitive(branchValue ?? string.Empty);
    }

    private static object? ResolveMergeSourceValue(object? root, object? item, ValueSource source)
    {
        var values = source.Paths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => ResolvePathValue(root, item, path))
            .ToList();

        var mode = source.MergeMode ?? MergeMode.Concat;

        return mode switch
        {
            MergeMode.FirstNonEmpty => values.FirstOrDefault(v => v != null && (v is not string s || s.Length > 0)),
            MergeMode.Array => values,
            _ => string.Join(source.Separator ?? string.Empty, values.Select(v => v?.ToString() ?? string.Empty))
        };
    }

    private static object? ResolvePathValue(object? root, object? item, string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (path == "$") return root;
        if (path.StartsWith("$.", StringComparison.Ordinal))
        {
            return GetValueByPath(root, path[2..]);
        }
        if (path.StartsWith("$[", StringComparison.Ordinal))
        {
            return GetValueByPath(root, path[1..]);
        }
        if (item != null)
        {
            return GetValueByPath(item, path);
        }
        return GetValueByPath(root, path);
    }

    private static object? ResolveTransform(object? value, TransformType transform)
    {
        return transform switch
        {
            TransformType.ToLowerCase => value is string lower ? lower.ToLowerInvariant() : value,
            TransformType.ToUpperCase => value is string upper ? upper.ToUpperInvariant() : value,
            TransformType.Number => value == null || (value is string s && s == string.Empty) ? value : ToNumber(value),
            TransformType.Boolean => value switch
            {
                bool b => b,
                string s => new[] { "true", "1", "yes", "y" }.Contains(s.ToLowerInvariant()),
                _ => value != null
            },
            _ => value
        };
    }

    private static object? GetValueByPath(object? input, string path)
    {
        if (input == null || string.IsNullOrWhiteSpace(path)) return null;
        var parts = path.Split('.').Select(part => part.Trim()).ToArray();
        object? current = input;
        foreach (var part in parts)
        {
            if (current == null) return null;
            var arrayMatch = Regex.Match(part, "^(\\w+)\\[(\\d+)\\]$");
            if (arrayMatch.Success)
            {
                var key = arrayMatch.Groups[1].Value;
                var index = int.Parse(arrayMatch.Groups[2].Value, CultureInfo.InvariantCulture);
                if (current is not Dictionary<string, object?> dict || !dict.TryGetValue(key, out var next)) return null;
                if (next is not List<object?> list || index >= list.Count) return null;
                current = list[index];
                continue;
            }

            if (Regex.IsMatch(part, "^\\d+$"))
            {
                if (current is not List<object?> list || !int.TryParse(part, out var index) || index >= list.Count) return null;
                current = list[index];
                continue;
            }

            if (current is not Dictionary<string, object?> obj || !obj.TryGetValue(part, out var value)) return null;
            current = value;
        }
        return current;
    }

    private static object? ParsePrimitive(string value)
    {
        return PrimitiveParser.ParsePrimitive(value);
    }

    private static double ToNumber(object? value)
    {
        if (value == null) return double.NaN;
        if (value is double d) return d;
        if (value is float f) return f;
        if (value is int i) return i;
        if (value is long l) return l;
        if (value is decimal m) return (double)m;
        if (value is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }
        try
        {
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return double.NaN;
        }
    }
}
