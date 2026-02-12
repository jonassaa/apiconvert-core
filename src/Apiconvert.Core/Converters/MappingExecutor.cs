using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Apiconvert.Core.Rules;

namespace Apiconvert.Core.Converters;

internal static class MappingExecutor
{
    internal static ConversionResult ApplyConversion(object? input, object? rawRules)
    {
        var rules = RulesNormalizer.NormalizeConversionRules(rawRules);
        if (!rules.FieldMappings.Any() && !rules.ArrayMappings.Any())
        {
            return new ConversionResult
            {
                Output = input ?? new Dictionary<string, object?>(),
                Errors = new List<string>(),
                Warnings = new List<string>()
            };
        }

        var output = new Dictionary<string, object?>();
        var errors = new List<string>();
        var warnings = new List<string>();

        ApplyFieldMappings(input, null, rules.FieldMappings, output, errors, "Field");

        for (var index = 0; index < rules.ArrayMappings.Count; index++)
        {
            var arrayRule = rules.ArrayMappings[index];
            var value = ResolvePathValue(input, null, arrayRule.InputPath);
            var items = value as List<object?>;
            if (items == null && arrayRule.CoerceSingle && value != null)
            {
                items = new List<object?> { value };
            }

            if (items == null)
            {
                if (value == null)
                {
                    warnings.Add(
                        $"Array mapping skipped: inputPath \"{arrayRule.InputPath}\" not found (arrayMappings[{index}]).");
                }
                else
                {
                    errors.Add($"Array {index + 1}: input path did not resolve to an array ({arrayRule.InputPath}).");
                }
                continue;
            }

            var mappedItems = new List<object?>();
            foreach (var item in items)
            {
                var itemOutput = new Dictionary<string, object?>();
                ApplyFieldMappings(input, item, arrayRule.ItemMappings, itemOutput, errors, $"Array {index + 1} item");
                mappedItems.Add(itemOutput);
            }

            if (string.IsNullOrWhiteSpace(arrayRule.OutputPath))
            {
                errors.Add($"Array {index + 1}: output path is required.");
                continue;
            }

            var outputPath = NormalizeWritePath(arrayRule.OutputPath);
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                errors.Add($"Array {index + 1}: output path is required.");
                continue;
            }

            SetValueByPath(output, outputPath, mappedItems);
        }

        return new ConversionResult { Output = output, Errors = errors, Warnings = warnings };
    }

    private static void ApplyFieldMappings(
        object? root,
        object? item,
        IEnumerable<FieldRule> mappings,
        Dictionary<string, object?> output,
        List<string> errors,
        string label)
    {
        var index = 0;
        foreach (var rule in mappings)
        {
            index++;
            var writePaths = GetWritePaths(rule);
            if (writePaths.Count == 0)
            {
                errors.Add($"{label} {index}: output path is required.");
                continue;
            }

            var value = ResolveSourceValue(root, item, rule.Source);
            if ((value == null || (value is string str && string.IsNullOrEmpty(str))) && !string.IsNullOrEmpty(rule.DefaultValue))
            {
                value = ParsePrimitive(rule.DefaultValue);
            }

            foreach (var writePath in writePaths)
            {
                SetValueByPath(output, writePath, value);
            }
        }
    }

    private static object? ResolveSourceValue(object? root, object? item, ValueSource source)
    {
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
                if (source.Condition == null)
                {
                    return null;
                }
                var matched = EvaluateCondition(root, item, source.Condition);
                var resolved = matched ? source.TrueValue : source.FalseValue;
                return ParsePrimitive(resolved ?? string.Empty);
            }
            default:
                return null;
        }
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

    private static bool EvaluateCondition(object? root, object? item, ConditionRule condition)
    {
        var value = ResolvePathValue(root, item, condition.Path);
        var compareValue = condition.Value != null ? ParsePrimitive(condition.Value) : null;

        return condition.Operator switch
        {
            ConditionOperator.Exists => value != null,
            ConditionOperator.Equals => Equals(value, compareValue),
            ConditionOperator.NotEquals => !Equals(value, compareValue),
            ConditionOperator.Includes => Includes(value, compareValue),
            ConditionOperator.Gt => ToNumber(value) > ToNumber(compareValue),
            ConditionOperator.Lt => ToNumber(value) < ToNumber(compareValue),
            _ => false
        };
    }

    private static bool Includes(object? value, object? compareValue)
    {
        if (value is string text && compareValue is string compare)
        {
            return text.Contains(compare, StringComparison.Ordinal);
        }
        if (value is List<object?> list)
        {
            return list.Any(item => Equals(item, compareValue));
        }
        return false;
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

    private static void SetValueByPath(Dictionary<string, object?> target, string path, object? value)
    {
        var parts = path.Split('.').Select(part => part.Trim()).ToArray();
        var current = target;
        for (var index = 0; index < parts.Length; index++)
        {
            var part = parts[index];
            var isLast = index == parts.Length - 1;
            if (isLast)
            {
                current[part] = value;
                return;
            }

            if (!current.TryGetValue(part, out var next) || next is not Dictionary<string, object?> nested)
            {
                nested = new Dictionary<string, object?>();
                current[part] = nested;
            }
            current = nested;
        }
    }

    private static string NormalizeWritePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        if (path.StartsWith("$.", StringComparison.Ordinal))
        {
            return path[2..];
        }

        if (path == "$")
        {
            return string.Empty;
        }

        return path;
    }

    private static List<string> GetWritePaths(FieldRule rule)
    {
        var paths = new List<string>();

        if (!string.IsNullOrWhiteSpace(rule.OutputPath))
        {
            paths.Add(rule.OutputPath);
        }

        if (rule.OutputPaths.Count > 0)
        {
            paths.AddRange(rule.OutputPaths.Where(path => !string.IsNullOrWhiteSpace(path)));
        }

        return paths
            .Distinct(StringComparer.Ordinal)
            .ToList();
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
