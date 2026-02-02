using System.Globalization;

namespace Apiconvert.Core.Converters;

internal static class PrimitiveParser
{
    internal static object? ParsePrimitive(string value)
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
}
