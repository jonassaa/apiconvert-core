using Apiconvert.Core.Rules;

namespace Apiconvert.Core.Converters;

/// <summary>
/// Entry point for applying conversion rules to payloads.
/// </summary>
public static class ConversionEngine
{
    /// <summary>
    /// Normalizes raw rules into the canonical conversion rules model.
    /// </summary>
    /// <param name="raw">Raw rules input (object or JSON-like model).</param>
    /// <returns>Normalized conversion rules.</returns>
    public static ConversionRules NormalizeConversionRules(object? raw)
    {
        return RulesNormalizer.NormalizeConversionRules(raw);
    }

    /// <summary>
    /// Applies conversion rules to the given input payload.
    /// </summary>
    /// <param name="input">Input payload (already parsed).</param>
    /// <param name="rawRules">Rules input (object or JSON-like model).</param>
    /// <returns>Conversion result containing output and errors.</returns>
    public static ConversionResult ApplyConversion(object? input, object? rawRules)
    {
        return MappingExecutor.ApplyConversion(input, rawRules);
    }

    /// <summary>
    /// Parses a raw text payload into a structured object for the specified format.
    /// </summary>
    /// <param name="text">Raw payload string.</param>
    /// <param name="format">Input format.</param>
    /// <returns>Parsed value and an optional error message.</returns>
    public static (object? Value, string? Error) ParsePayload(string text, DataFormat format)
    {
        return PayloadConverter.ParsePayload(text, format);
    }

    /// <summary>
    /// Formats a structured payload into a string for the specified format.
    /// </summary>
    /// <param name="value">Structured payload.</param>
    /// <param name="format">Output format.</param>
    /// <param name="pretty">Whether to use pretty formatting.</param>
    /// <returns>Formatted payload string.</returns>
    public static string FormatPayload(object? value, DataFormat format, bool pretty)
    {
        return PayloadConverter.FormatPayload(value, format, pretty);
    }
}
