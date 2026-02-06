using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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

    internal static (object? Value, string? Error) ParsePayload(
        Stream stream,
        DataFormat format,
        Encoding? encoding = null,
        bool leaveOpen = true)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        using var reader = new StreamReader(
            stream,
            encoding ?? Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 1024,
            leaveOpen: leaveOpen);

        var text = reader.ReadToEnd();
        return ParsePayload(text, format);
    }

    internal static (object? Value, string? Error) ParsePayload(JsonNode? jsonNode, DataFormat format)
    {
        try
        {
            if (format != DataFormat.Json)
            {
                return (null, "JsonNode input is only supported for DataFormat.Json.");
            }

            return (JsonConverter.ParseJson(jsonNode), null);
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
            DataFormat.Xml => XmlConverter.FormatXml(value, pretty),
            DataFormat.Query => QueryStringConverter.FormatQueryString(value),
            _ => JsonSerializer.Serialize(
                value ?? new Dictionary<string, object?>(),
                new JsonSerializerOptions(JsonDefaults.Options) { WriteIndented = pretty })
        };
    }

    internal static void FormatPayload(
        object? value,
        DataFormat format,
        Stream stream,
        bool pretty,
        Encoding? encoding = null,
        bool leaveOpen = true)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        var payload = FormatPayload(value, format, pretty);
        using var writer = new StreamWriter(
            stream,
            encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            bufferSize: 1024,
            leaveOpen: leaveOpen);
        writer.Write(payload);
        writer.Flush();
    }
}
