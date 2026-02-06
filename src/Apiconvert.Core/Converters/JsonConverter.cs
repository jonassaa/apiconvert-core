using System.Text.Json;
using System.Text.Json.Nodes;

namespace Apiconvert.Core.Converters;

internal static class JsonConverter
{
    internal static object? ParseJson(string text)
    {
        var document = JsonDocument.Parse(text);
        return ToObject(document.RootElement);
    }

    internal static object? ParseJson(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        var document = JsonDocument.Parse(node.ToJsonString());
        return ToObject(document.RootElement);
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
}
