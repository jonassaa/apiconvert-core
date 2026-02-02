using System.Text.Json;
using Apiconvert.Core.Rules;

namespace Apiconvert.Core.Converters;

internal static class PayloadConverter
{
    internal static (object? Value, string? Error) ParsePayload(string text, DataFormat format)
    {
        try
        {
            return format switch
            {
                DataFormat.Xml => (XmlConverter.ParseXml(text), null),
                DataFormat.Query => (QueryStringConverter.ParseQueryString(text), null),
                _ => (JsonConverter.ParseJson(text), null)
            };
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    internal static string FormatPayload(object? value, DataFormat format, bool pretty)
    {
        return format switch
        {
            DataFormat.Xml => XmlConverter.FormatXml(value),
            DataFormat.Query => QueryStringConverter.FormatQueryString(value),
            _ => JsonSerializer.Serialize(
                value ?? new Dictionary<string, object?>(),
                new JsonSerializerOptions(JsonDefaults.Options) { WriteIndented = pretty })
        };
    }
}
