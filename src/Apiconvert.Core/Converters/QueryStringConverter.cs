using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Apiconvert.Core.Converters;

internal static class QueryStringConverter
{
    private static readonly Regex QueryKeyMatcher = new("([^\\[.\\]]+)|\\[(.*?)\\]", RegexOptions.Compiled);

    internal static Dictionary<string, object?> ParseQueryString(string text)
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

    internal static string FormatQueryString(object? value)
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
                    pairs.Add((key, JsonSerializer.Serialize(item, JsonDefaults.Options)));
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
}
