using Apiconvert.Core.Rules;

namespace Apiconvert.Core.Converters;

public static class ConversionEngine
{
    public static ConversionRules NormalizeConversionRules(object? raw)
    {
        return RulesNormalizer.NormalizeConversionRules(raw);
    }

    public static ConversionResult ApplyConversion(object? input, object? rawRules)
    {
        return MappingExecutor.ApplyConversion(input, rawRules);
    }

    public static (object? Value, string? Error) ParsePayload(string text, DataFormat format)
    {
        return PayloadConverter.ParsePayload(text, format);
    }

    public static string FormatPayload(object? value, DataFormat format, bool pretty)
    {
        return PayloadConverter.FormatPayload(value, format, pretty);
    }
}
