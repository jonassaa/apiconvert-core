using Apiconvert.Core.Rules;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

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
    /// Normalizes raw rules into the canonical model and throws if validation fails.
    /// </summary>
    /// <param name="raw">Raw rules input (object or JSON-like model).</param>
    /// <returns>Normalized conversion rules.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="raw"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when rule normalization fails.</exception>
    public static ConversionRules NormalizeConversionRulesStrict(object? raw)
    {
        if (raw is null)
        {
            throw new ArgumentNullException(nameof(raw), "Rules input is required in strict mode.");
        }

        var rules = RulesNormalizer.NormalizeConversionRules(raw);
        if (rules.ValidationErrors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Rule normalization failed: {string.Join("; ", rules.ValidationErrors)}");
        }

        return rules;
    }

    /// <summary>
    /// Compiles raw rules into a reusable conversion plan.
    /// </summary>
    /// <param name="rawRules">Rules input (object or JSON-like model).</param>
    /// <returns>Compiled conversion plan.</returns>
    public static ConversionPlan CompileConversionPlan(object? rawRules)
    {
        var rules = NormalizeConversionRules(rawRules);
        return new ConversionPlan(rules);
    }

    /// <summary>
    /// Compiles raw rules into a reusable conversion plan and throws on validation errors.
    /// </summary>
    /// <param name="rawRules">Rules input (object or JSON-like model).</param>
    /// <returns>Compiled conversion plan.</returns>
    public static ConversionPlan CompileConversionPlanStrict(object? rawRules)
    {
        var rules = NormalizeConversionRulesStrict(rawRules);
        return new ConversionPlan(rules);
    }

    /// <summary>
    /// Computes a stable cache key for normalized rules.
    /// </summary>
    /// <param name="rawRules">Rules input (object or JSON-like model).</param>
    /// <returns>Stable cache key string for compiled plan reuse.</returns>
    public static string ComputeRulesCacheKey(object? rawRules)
    {
        var rules = NormalizeConversionRules(rawRules);
        return RulesCacheKey.Compute(rules);
    }

    /// <summary>
    /// Runs lint diagnostics over raw rules without mutating or executing conversion.
    /// </summary>
    /// <param name="rawRules">Rules input (object or JSON-like model).</param>
    /// <returns>Deterministic lint diagnostics with severity and fix guidance.</returns>
    public static List<RuleLintDiagnostic> LintRules(object? rawRules)
    {
        return RulesLinter.LintRules(rawRules);
    }

    /// <summary>
    /// Runs deterministic rule-doctor diagnostics by combining validation, lint, and optional runtime checks.
    /// </summary>
    /// <param name="rawRules">Rules input (object or JSON-like model).</param>
    /// <param name="sampleInputText">Optional sample input payload text for runtime diagnostics.</param>
    /// <param name="inputFormat">Input format for <paramref name="sampleInputText"/>. Defaults to JSON.</param>
    /// <param name="applySafeFixes">Reserved for future use. Current implementation returns preview-only fixes.</param>
    /// <returns>Rule-doctor report with ordered findings and safe-fix preview.</returns>
    public static RuleDoctorReport RunRuleDoctor(
        object? rawRules,
        string? sampleInputText = null,
        DataFormat inputFormat = DataFormat.Json,
        bool applySafeFixes = false)
    {
        var normalizedRules = NormalizeConversionRules(rawRules);
        var lintDiagnostics = LintRules(rawRules);
        var findings = new List<RuleDoctorFinding>();

        foreach (var validationError in normalizedRules.ValidationErrors)
        {
            findings.Add(new RuleDoctorFinding
            {
                Code = "ACV-LINT-001",
                Stage = "validation",
                Severity = RuleDoctorFindingSeverity.Error,
                RulePath = "rules",
                Message = validationError,
                Suggestion = "Fix schema/normalization errors before conversion."
            });
        }

        foreach (var diagnostic in lintDiagnostics)
        {
            findings.Add(new RuleDoctorFinding
            {
                Code = diagnostic.Code,
                Stage = "lint",
                Severity = diagnostic.Severity == RuleLintSeverity.Error
                    ? RuleDoctorFindingSeverity.Error
                    : RuleDoctorFindingSeverity.Warning,
                RulePath = diagnostic.RulePath,
                Message = diagnostic.Message,
                Suggestion = diagnostic.Suggestion
            });
        }

        if (!string.IsNullOrEmpty(sampleInputText))
        {
            var (value, parseError) = ParsePayload(sampleInputText, inputFormat);
            if (parseError is not null)
            {
                findings.Add(new RuleDoctorFinding
                {
                    Code = "ACV-DOCTOR-001",
                    Stage = "runtime",
                    Severity = RuleDoctorFindingSeverity.Error,
                    RulePath = "runtime.input",
                    Message = $"Failed to parse sample input: {parseError}",
                    Suggestion = "Pass sample input matching the selected format."
                });
            }
            else
            {
                var conversion = ApplyConversion(value, normalizedRules);
                foreach (var warning in conversion.Warnings)
                {
                    findings.Add(new RuleDoctorFinding
                    {
                        Code = "ACV-DOCTOR-010",
                        Stage = "runtime",
                        Severity = RuleDoctorFindingSeverity.Warning,
                        RulePath = "runtime",
                        Message = warning,
                        Suggestion = "Adjust rules or input sample to avoid runtime warnings."
                    });
                }

                foreach (var error in conversion.Errors)
                {
                    findings.Add(new RuleDoctorFinding
                    {
                        Code = "ACV-DOCTOR-011",
                        Stage = "runtime",
                        Severity = RuleDoctorFindingSeverity.Error,
                        RulePath = "runtime",
                        Message = error,
                        Suggestion = "Fix rule source paths, transforms, or branch expressions."
                    });
                }
            }
        }
        else
        {
            findings.Add(new RuleDoctorFinding
            {
                Code = "ACV-DOCTOR-100",
                Stage = "runtime",
                Severity = RuleDoctorFindingSeverity.Info,
                RulePath = "runtime",
                Message = "Runtime checks skipped (no sample input provided).",
                Suggestion = "Provide sample input to include conversion-time diagnostics."
            });
        }

        var safeFixPreview = lintDiagnostics
            .Select(diagnostic => $"{diagnostic.RulePath}: {diagnostic.Suggestion}")
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return new RuleDoctorReport
        {
            Findings = findings,
            HasErrors = findings.Any(finding => finding.Severity == RuleDoctorFindingSeverity.Error),
            CanApplySafeFixes = false,
            SafeFixPreview = safeFixPreview
        };
    }

    /// <summary>
    /// Applies conversion rules to the given input payload.
    /// </summary>
    /// <param name="input">Input payload (already parsed).</param>
    /// <param name="rawRules">Rules input (object or JSON-like model).</param>
    /// <param name="options">Optional execution options.</param>
    /// <returns>Conversion result containing output and errors.</returns>
    public static ConversionResult ApplyConversion(object? input, object? rawRules, ConversionOptions? options = null)
    {
        return MappingExecutor.ApplyConversion(input, rawRules, options);
    }

    /// <summary>
    /// Applies a compiled conversion plan to the given input payload.
    /// </summary>
    /// <param name="input">Input payload (already parsed).</param>
    /// <param name="plan">Compiled conversion plan.</param>
    /// <param name="options">Optional execution options.</param>
    /// <returns>Conversion result containing output and errors.</returns>
    public static ConversionResult ApplyConversion(object? input, ConversionPlan plan, ConversionOptions? options = null)
    {
        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        return plan.Apply(input, options);
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
    /// Parses a raw payload stream into a structured object for the specified format.
    /// </summary>
    /// <param name="stream">Raw payload stream.</param>
    /// <param name="format">Input format.</param>
    /// <param name="encoding">Text encoding used to read the stream. Defaults to UTF-8.</param>
    /// <param name="leaveOpen">Whether to leave the stream open after reading.</param>
    /// <returns>Parsed value and an optional error message.</returns>
    public static (object? Value, string? Error) ParsePayload(
        Stream stream,
        DataFormat format,
        Encoding? encoding = null,
        bool leaveOpen = true)
    {
        return PayloadConverter.ParsePayload(stream, format, encoding, leaveOpen);
    }

    /// <summary>
    /// Parses a JSON payload from a <see cref="JsonNode"/>.
    /// </summary>
    /// <param name="jsonNode">JSON payload node.</param>
    /// <param name="format">Input format. Must be <see cref="DataFormat.Json"/>.</param>
    /// <returns>Parsed value and an optional error message.</returns>
    public static (object? Value, string? Error) ParsePayload(JsonNode? jsonNode, DataFormat format = DataFormat.Json)
    {
        return PayloadConverter.ParsePayload(jsonNode, format);
    }

    /// <summary>
    /// Streams conversion over a JSON array payload, yielding one conversion result per array item.
    /// </summary>
    /// <param name="stream">Input JSON stream containing a top-level array.</param>
    /// <param name="rawRules">Rules input (object or JSON-like model).</param>
    /// <param name="options">Optional execution options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async stream of per-item conversion results.</returns>
    public static async IAsyncEnumerable<ConversionResult> StreamJsonArrayConversionAsync(
        Stream stream,
        object? rawRules,
        ConversionOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        var rules = NormalizeConversionRules(rawRules);
        var elements = JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(stream, cancellationToken: cancellationToken);
        await foreach (var element in elements.WithCancellation(cancellationToken))
        {
            var input = JsonConverter.ToObject(element);
            yield return MappingExecutor.ApplyConversion(input, rules, options);
        }
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

    /// <summary>
    /// Formats a structured payload and writes it to a stream.
    /// </summary>
    /// <param name="value">Structured payload.</param>
    /// <param name="format">Output format.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="pretty">Whether to use pretty formatting.</param>
    /// <param name="encoding">Text encoding used to write the stream. Defaults to UTF-8 (without BOM).</param>
    /// <param name="leaveOpen">Whether to leave the stream open after writing.</param>
    public static void FormatPayload(
        object? value,
        DataFormat format,
        Stream stream,
        bool pretty,
        Encoding? encoding = null,
        bool leaveOpen = true)
    {
        PayloadConverter.FormatPayload(value, format, stream, pretty, encoding, leaveOpen);
    }
}
