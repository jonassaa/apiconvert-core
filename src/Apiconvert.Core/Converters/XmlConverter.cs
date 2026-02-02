using System.Xml.Linq;

namespace Apiconvert.Core.Converters;

internal static class XmlConverter
{
    internal static object? ParseXml(string text)
    {
        var doc = XDocument.Parse(text, LoadOptions.PreserveWhitespace);
        if (doc.Root == null) return new Dictionary<string, object?>();
        var result = new Dictionary<string, object?>
        {
            [doc.Root.Name.LocalName] = ParseXmlElement(doc.Root)
        };
        return result;
    }

    internal static string FormatXml(object? value)
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

    private static object? ParseXmlElement(XElement element)
    {
        var hasElements = element.Elements().Any();
        var hasAttributes = element.Attributes().Any();
        var textValue = element.Nodes().OfType<XText>().Select(node => node.Value).FirstOrDefault();
        var hasText = !string.IsNullOrWhiteSpace(textValue);

        if (!hasElements && !hasAttributes)
        {
            return PrimitiveParser.ParsePrimitive(element.Value.Trim());
        }

        var obj = new Dictionary<string, object?>();
        foreach (var attribute in element.Attributes())
        {
            obj[$"@_{attribute.Name.LocalName}"] = PrimitiveParser.ParsePrimitive(attribute.Value);
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
            obj["#text"] = PrimitiveParser.ParsePrimitive(textValue.Trim());
        }

        return obj;
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
}
