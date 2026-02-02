using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Apiconvert.Core.Rules;

namespace Apiconvert.Core.Converters;

public static class ConversionEngine
{
    private static readonly Regex QueryKeyMatcher = new("([^\\[.\\]]+)|\\[(.*?)\\]", RegexOptions.Compiled);

    public static ConversionRules NormalizeConversionRules(object? raw)
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
                .Select(rule => rule with { DefaultValue = rule.DefaultValue ?? string.Empty })
                .ToList(),
            ArrayMappings = rules.ArrayMappings
                .Select(mapping => mapping with
                {
                    CoerceSingle = mapping.CoerceSingle,
                    ItemMappings = mapping.ItemMappings
                        .Select(rule => rule with { DefaultValue = rule.DefaultValue ?? string.Empty })
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
            value = JsonSerializer.Deserialize<T>(element.GetRawText(), JsonOptions);
            return value != null;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    public static ConversionResult ApplyConversion(object? input, object? rawRules)
    {
        var rules = NormalizeConversionRules(rawRules);
        if (!rules.FieldMappings.Any() && !rules.ArrayMappings.Any())
        {
            return new ConversionResult { Output = input ?? new Dictionary<string, object?>(), Errors = new List<string>() };
        }

        var output = new Dictionary<string, object?>();
        var errors = new List<string>();

        ApplyFieldMappings(input, null, rules.FieldMappings, output, errors, "Field");

        for (var index = 0; index < rules.ArrayMappings.Count; index++)
        {
            var arrayRule = rules.ArrayMappings[index];
            var value = GetValueByPath(input, arrayRule.InputPath);
            var items = value as List<object?>;
            if (items == null && arrayRule.CoerceSingle && value != null)
            {
                items = new List<object?> { value };
            }

            if (items == null)
            {
                errors.Add($"Array {index + 1}: input path did not resolve to an array ({arrayRule.InputPath}).");
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

            SetValueByPath(output, arrayRule.OutputPath, mappedItems);
        }

        return new ConversionResult { Output = output, Errors = errors };
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
            if (string.IsNullOrWhiteSpace(rule.OutputPath))
            {
                errors.Add($"{label} {index}: output path is required.");
                continue;
            }

            var value = ResolveSourceValue(root, item, rule.Source);
            if ((value == null || (value is string str && string.IsNullOrEmpty(str))) && !string.IsNullOrEmpty(rule.DefaultValue))
            {
                value = ParsePrimitive(rule.DefaultValue);
            }
            SetValueByPath(output, rule.OutputPath, value);
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

    private static object? ResolvePathValue(object? root, object? item, string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (path == "$" ) return root;
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

    private static object? ParsePrimitive(string value)
    {
        if (value == "true") return true;
        if (value == "false") return false;
        if (value == "null") return null;
        if (value == "undefined") return null;
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return number;
        }
        return value;
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

    public static (object? Value, string? Error) ParsePayload(string text, DataFormat format)
    {
        try
        {
            return format switch
            {
                DataFormat.Xml => (ParseXml(text), null),
                DataFormat.Query => (ParseQueryString(text), null),
                _ => (ParseJson(text), null)
            };
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public static string FormatPayload(object? value, DataFormat format, bool pretty)
    {
        return format switch
        {
            DataFormat.Xml => FormatXml(value),
            DataFormat.Query => FormatQueryString(value),
            _ => JsonSerializer.Serialize(
                value ?? new Dictionary<string, object?>(),
                new JsonSerializerOptions(JsonOptions) { WriteIndented = pretty })
        };
    }

    private static object? ParseJson(string text)
    {
        var document = JsonDocument.Parse(text);
        return ToObject(document.RootElement);
    }

    private static object? ParseXml(string text)
    {
        var doc = XDocument.Parse(text, LoadOptions.PreserveWhitespace);
        if (doc.Root == null) return new Dictionary<string, object?>();
        var result = new Dictionary<string, object?>
        {
            [doc.Root.Name.LocalName] = ParseXmlElement(doc.Root)
        };
        return result;
    }

    private static object? ParseXmlElement(XElement element)
    {
        var hasElements = element.Elements().Any();
        var hasAttributes = element.Attributes().Any();
        var textValue = element.Nodes().OfType<XText>().Select(node => node.Value).FirstOrDefault();
        var hasText = !string.IsNullOrWhiteSpace(textValue);

        if (!hasElements && !hasAttributes)
        {
            return ParsePrimitive(element.Value.Trim());
        }

        var obj = new Dictionary<string, object?>();
        foreach (var attribute in element.Attributes())
        {
            obj[$"@_{attribute.Name.LocalName}"] = ParsePrimitive(attribute.Value);
        }

        foreach (var child in element.Elements())
        {
            var childValue = ParseXmlElement(child);
            if (obj.TryGetValue(child.Name.LocalName, out var existing))
            {
                if (existing is List<object?> list)
                {
                    list.Add(childValue);
                }
                else
                {
                    obj[child.Name.LocalName] = new List<object?> { existing, childValue };
                }
            }
            else
            {
                obj[child.Name.LocalName] = childValue;
            }
        }

        if (hasText && textValue != null)
        {
            obj["#text"] = ParsePrimitive(textValue.Trim());
        }

        return obj;
    }

    private static string FormatXml(object? value)
    {
        if (value is not Dictionary<string, object?> root || root.Count == 0)
        {
            return new XDocument(new XElement("root")).ToString(SaveOptions.DisableFormatting);
        }

        if (root.Count == 1)
        {
            var pair = root.First();
            var element = BuildXmlElement(pair.Key, pair.Value);
            return new XDocument(element).ToString(SaveOptions.DisableFormatting);
        }

        var fallbackRoot = new XElement("root");
        foreach (var entry in root)
        {
            fallbackRoot.Add(BuildXmlElement(entry.Key, entry.Value));
        }
        return new XDocument(fallbackRoot).ToString(SaveOptions.DisableFormatting);
    }

    private static XElement BuildXmlElement(string name, object? value)
    {
        var element = new XElement(name);
        if (value is Dictionary<string, object?> obj)
        {
            foreach (var entry in obj)
            {
                if (entry.Key.StartsWith("@_", StringComparison.Ordinal))
                {
                    element.SetAttributeValue(entry.Key[2..], entry.Value);
                }
            }

            foreach (var entry in obj)
            {
                if (entry.Key.StartsWith("@_", StringComparison.Ordinal)) continue;
                if (entry.Key == "#text")
                {
                    if (entry.Value != null)
                    {
                        element.Value = entry.Value.ToString();
                    }
                    continue;
                }

                if (entry.Value is List<object?> list)
                {
                    foreach (var item in list)
                    {
                        element.Add(BuildXmlElement(entry.Key, item));
                    }
                }
                else
                {
                    element.Add(BuildXmlElement(entry.Key, entry.Value));
                }
            }
            return element;
        }

        if (value is List<object?> array)
        {
            foreach (var item in array)
            {
                element.Add(BuildXmlElement(name, item));
            }
            return element;
        }

        if (value != null)
        {
            element.Value = value.ToString();
        }
        return element;
    }

    private static Dictionary<string, object?> ParseQueryString(string text)
    {
        var trimmed = text.Trim();
        if (string.IsNullOrEmpty(trimmed)) return new Dictionary<string, object?>();
        var query = trimmed.StartsWith("?") ? trimmed[1..] : trimmed;
        var result = new Dictionary<string, object?>();
        var pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var splitIndex = pair.IndexOf('=');
            var rawKey = splitIndex >= 0 ? pair[..splitIndex] : pair;
            var rawValue = splitIndex >= 0 ? pair[(splitIndex + 1)..] : string.Empty;
            var key = Uri.UnescapeDataString(rawKey.Replace('+', ' '));
            var value = Uri.UnescapeDataString(rawValue.Replace('+', ' '));
            var path = ParseQueryKey(key);
            SetQueryValue(result, path, value);
        }
        return result;
    }

    private static List<object> ParseQueryKey(string key)
    {
        var parts = new List<object>();
        foreach (Match match in QueryKeyMatcher.Matches(key))
        {
            var token = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            if (string.IsNullOrEmpty(token)) continue;
            if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            {
                parts.Add(number);
            }
            else
            {
                parts.Add(token);
            }
        }
        if (parts.Count == 0)
        {
            parts.Add(key);
        }
        return parts;
    }

    private static void SetQueryValue(Dictionary<string, object?> target, List<object> path, string value)
    {
        object? current = target;
        object? parent = null;
        object? parentKey = null;

        Dictionary<string, object?> EnsureObjectContainer(object? valueToCheck)
        {
            if (valueToCheck is Dictionary<string, object?> obj) return obj;
            var next = new Dictionary<string, object?>();
            if (parent != null && parentKey != null)
            {
                if (parent is List<object?> list)
                {
                    list[(int)parentKey] = next;
                }
                else if (parent is Dictionary<string, object?> parentObj)
                {
                    parentObj[parentKey.ToString() ?? string.Empty] = next;
                }
            }
            return next;
        }

        List<object?> EnsureArrayContainer(object? valueToCheck)
        {
            if (valueToCheck is List<object?> list) return list;
            var next = new List<object?>();
            if (parent != null && parentKey != null)
            {
                if (parent is List<object?> parentList)
                {
                    parentList[(int)parentKey] = next;
                }
                else if (parent is Dictionary<string, object?> parentObj)
                {
                    parentObj[parentKey.ToString() ?? string.Empty] = next;
                }
            }
            return next;
        }

        for (var index = 0; index < path.Count; index++)
        {
            var segment = path[index];
            var isLast = index == path.Count - 1;
            var nextSegment = index + 1 < path.Count ? path[index + 1] : null;

            if (segment is int segmentIndex)
            {
                var arrayContainer = EnsureArrayContainer(current);
                if (isLast)
                {
                    var existing = arrayContainer.Count > segmentIndex ? arrayContainer[segmentIndex] : null;
                    if (existing == null)
                    {
                        EnsureListSize(arrayContainer, segmentIndex + 1);
                        arrayContainer[segmentIndex] = value;
                    }
                    else if (existing is List<object?> existingList)
                    {
                        existingList.Add(value);
                    }
                    else
                    {
                        arrayContainer[segmentIndex] = new List<object?> { existing, value };
                    }
                    continue;
                }

                parent = arrayContainer;
                parentKey = segmentIndex;
                EnsureListSize(arrayContainer, segmentIndex + 1);
                var existingValue = arrayContainer[segmentIndex];
                var shouldBeArray = nextSegment is int;
                if (existingValue == null || (shouldBeArray && existingValue is not List<object?>) || (!shouldBeArray && existingValue is not Dictionary<string, object?>))
                {
                    arrayContainer[segmentIndex] = shouldBeArray ? new List<object?>() : new Dictionary<string, object?>();
                }
                current = arrayContainer[segmentIndex];
                continue;
            }

            var objectContainer = EnsureObjectContainer(current);
            var key = segment.ToString() ?? string.Empty;
            if (isLast)
            {
                if (!objectContainer.TryGetValue(key, out var existing))
                {
                    objectContainer[key] = value;
                }
                else if (existing is List<object?> existingList)
                {
                    existingList.Add(value);
                }
                else
                {
                    objectContainer[key] = new List<object?> { existing, value };
                }
                continue;
            }

            parent = objectContainer;
            parentKey = key;
            objectContainer.TryGetValue(key, out var next);
            var shouldArray = nextSegment is int;
            if (next == null || (shouldArray && next is not List<object?>) || (!shouldArray && next is not Dictionary<string, object?>))
            {
                objectContainer[key] = shouldArray ? new List<object?>() : new Dictionary<string, object?>();
            }
            current = objectContainer[key];
        }
    }

    private static void EnsureListSize(List<object?> list, int size)
    {
        while (list.Count < size)
        {
            list.Add(null);
        }
    }

    private static string FormatQueryString(object? value)
    {
        if (value is not Dictionary<string, object?> obj)
        {
            throw new InvalidOperationException("Query output must be an object.");
        }

        var pairs = new List<(string Key, string Value)>();
        foreach (var key in obj.Keys.OrderBy(key => key, StringComparer.Ordinal))
        {
            AddQueryPair(pairs, key, obj[key]);
        }

        return string.Join("&", pairs.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
    }

    private static void AddQueryPair(List<(string Key, string Value)> pairs, string key, object? value)
    {
        if (value == null)
        {
            pairs.Add((key, string.Empty));
            return;
        }

        if (value is List<object?> list)
        {
            foreach (var item in list)
            {
                if (item is Dictionary<string, object?> || item is List<object?>)
                {
                    pairs.Add((key, JsonSerializer.Serialize(item, JsonOptions)));
                }
                else if (item == null)
                {
                    pairs.Add((key, string.Empty));
                }
                else
                {
                    pairs.Add((key, item.ToString() ?? string.Empty));
                }
            }
            return;
        }

        if (value is Dictionary<string, object?> obj)
        {
            foreach (var childKey in obj.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                var nextKey = string.IsNullOrEmpty(key) ? childKey : $"{key}.{childKey}";
                AddQueryPair(pairs, nextKey, obj[childKey]);
            }
            return;
        }

        if (string.IsNullOrEmpty(key)) return;
        pairs.Add((key, value.ToString() ?? string.Empty));
    }

    private static object? ToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(prop => prop.Name, prop => ToObject(prop.Value)),
            JsonValueKind.Array => element.EnumerateArray().Select(ToObject).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var longValue) ? longValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
