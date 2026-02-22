using Apiconvert.Core.Rules;
using System.Diagnostics;
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
    /// Formats rules into deterministic canonical JSON for clean diffs and stable tooling output.
    /// </summary>
    /// <param name="rawRules">Rules input (object or JSON-like model).</param>
    /// <param name="pretty">Whether to pretty-print with stable indentation.</param>
    /// <returns>Canonical JSON string.</returns>
    public static string FormatConversionRules(object? rawRules, bool pretty = true)
    {
        return RulesFormatter.FormatCanonical(rawRules, pretty);
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
    /// Checks whether a rules pack is compatible with the requested runtime version.
    /// </summary>
    /// <param name="rawRules">Rules input (object or JSON-like model).</param>
    /// <param name="targetVersion">Target runtime semantic version (for example, 1.0.0).</param>
    /// <returns>Compatibility report with machine-readable diagnostics.</returns>
    public static RulesCompatibilityReport CheckCompatibility(object? rawRules, string targetVersion)
    {
        const string supportedVersionMin = "1.0.0";
        const string supportedVersionMax = "1.0.0";

        var diagnostics = new List<RulesCompatibilityDiagnostic>();
        var normalizedRules = NormalizeConversionRules(rawRules);

        var trimmedTargetVersion = (targetVersion ?? string.Empty).Trim();
        if (!Version.TryParse(trimmedTargetVersion, out var parsedTargetVersion))
        {
            diagnostics.Add(new RulesCompatibilityDiagnostic
            {
                Code = "ACV-COMP-001",
                Severity = RuleDoctorFindingSeverity.Error,
                Message = $"Invalid target version '{trimmedTargetVersion}'. Expected <major>.<minor>.<patch>.",
                Suggestion = "Pass a semantic version like 1.0.0."
            });
        }

        var schemaVersion = ExtractSchemaVersion(rawRules);
        Version? parsedSchemaVersion = null;
        if (string.IsNullOrWhiteSpace(schemaVersion))
        {
            diagnostics.Add(new RulesCompatibilityDiagnostic
            {
                Code = "ACV-COMP-002",
                Severity = RuleDoctorFindingSeverity.Warning,
                Message = "Rules do not declare schemaVersion. Compatibility is conservative.",
                Suggestion = "Add schemaVersion to your rules pack for strict compatibility checks."
            });
        }
        else if (!Version.TryParse(schemaVersion, out parsedSchemaVersion))
        {
            diagnostics.Add(new RulesCompatibilityDiagnostic
            {
                Code = "ACV-COMP-005",
                Severity = RuleDoctorFindingSeverity.Error,
                Message = $"Invalid schemaVersion '{schemaVersion}' in rules.",
                Suggestion = "Set schemaVersion using semantic version format (for example, 1.0.0)."
            });
        }

        var minVersion = Version.Parse(supportedVersionMin);
        var maxVersion = Version.Parse(supportedVersionMax);
        if (parsedTargetVersion is not null)
        {
            if (parsedTargetVersion < minVersion || parsedTargetVersion > maxVersion)
            {
                diagnostics.Add(new RulesCompatibilityDiagnostic
                {
                    Code = "ACV-COMP-003",
                    Severity = RuleDoctorFindingSeverity.Error,
                    Message = $"Target runtime version {trimmedTargetVersion} is outside supported range {supportedVersionMin} - {supportedVersionMax}.",
                    Suggestion = $"Use a target version within {supportedVersionMin} - {supportedVersionMax}."
                });
            }

            if (parsedSchemaVersion is not null && parsedSchemaVersion > parsedTargetVersion)
            {
                diagnostics.Add(new RulesCompatibilityDiagnostic
                {
                    Code = "ACV-COMP-004",
                    Severity = RuleDoctorFindingSeverity.Error,
                    Message = $"Rules schemaVersion {schemaVersion} requires runtime >= {schemaVersion}, but target is {trimmedTargetVersion}.",
                    Suggestion = "Upgrade target runtime or use rules compatible with the target version."
                });
            }
        }

        foreach (var validationError in normalizedRules.ValidationErrors)
        {
            diagnostics.Add(new RulesCompatibilityDiagnostic
            {
                Code = "ACV-COMP-006",
                Severity = RuleDoctorFindingSeverity.Warning,
                Message = $"Rules normalization warning: {validationError}",
                Suggestion = "Resolve rules validation issues before relying on compatibility checks."
            });
        }

        return new RulesCompatibilityReport
        {
            TargetVersion = trimmedTargetVersion,
            SchemaVersion = schemaVersion,
            SupportedRangeMin = supportedVersionMin,
            SupportedRangeMax = supportedVersionMax,
            IsCompatible = diagnostics.All(entry => entry.Severity != RuleDoctorFindingSeverity.Error),
            Diagnostics = diagnostics
        };
    }

    /// <summary>
    /// Bundles modular rules files with include directives into a single normalized rules object.
    /// </summary>
    /// <param name="entryRulesPath">Path to the entry rules JSON file.</param>
    /// <param name="options">Optional bundling options.</param>
    /// <returns>Bundled and normalized conversion rules.</returns>
    public static ConversionRules BundleRules(string entryRulesPath, RuleBundleOptions? options = null)
    {
        return RulesBundler.BundleRules(entryRulesPath, options);
    }

    /// <summary>
    /// Profiles a compiled conversion plan using deterministic report fields.
    /// </summary>
    /// <param name="rawRules">Rules input (object or JSON-like model).</param>
    /// <param name="inputs">Input samples used for repeated apply runs.</param>
    /// <param name="options">Optional profiling options.</param>
    /// <returns>Conversion profile report with compile and apply latency metrics.</returns>
    public static ConversionProfileReport ProfileConversionPlan(
        object? rawRules,
        IEnumerable<object?> inputs,
        ConversionProfileOptions? options = null)
    {
        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        var inputList = inputs.ToList();
        if (inputList.Count == 0)
        {
            throw new ArgumentException("At least one input sample is required.", nameof(inputs));
        }

        var profileOptions = options ?? new ConversionProfileOptions();
        var iterations = Math.Max(1, profileOptions.Iterations);
        var warmupIterations = Math.Max(0, profileOptions.WarmupIterations);

        var compileStopwatch = Stopwatch.StartNew();
        var plan = CompileConversionPlan(rawRules);
        compileStopwatch.Stop();

        for (var i = 0; i < warmupIterations; i++)
        {
            foreach (var input in inputList)
            {
                _ = plan.Apply(input);
            }
        }

        var runDurations = new List<double>(iterations * inputList.Count);
        for (var i = 0; i < iterations; i++)
        {
            foreach (var input in inputList)
            {
                var runStopwatch = Stopwatch.StartNew();
                _ = plan.Apply(input);
                runStopwatch.Stop();
                runDurations.Add(runStopwatch.Elapsed.TotalMilliseconds);
            }
        }

        var latency = BuildLatencyProfile(runDurations);
        return new ConversionProfileReport
        {
            PlanCacheKey = plan.CacheKey,
            CompileMs = compileStopwatch.Elapsed.TotalMilliseconds,
            WarmupIterations = warmupIterations,
            Iterations = iterations,
            TotalRuns = runDurations.Count,
            LatencyMs = latency
        };
    }

    private static ConversionLatencyProfile BuildLatencyProfile(List<double> values)
    {
        if (values.Count == 0)
        {
            return new ConversionLatencyProfile();
        }

        values.Sort();
        var sum = values.Sum();

        return new ConversionLatencyProfile
        {
            Min = Percentile(values, 0),
            P50 = Percentile(values, 50),
            P95 = Percentile(values, 95),
            P99 = Percentile(values, 99),
            Max = Percentile(values, 100),
            Mean = sum / values.Count
        };
    }

    private static double Percentile(List<double> sortedValues, int percentile)
    {
        if (sortedValues.Count == 0)
        {
            return 0;
        }

        var index = Math.Min(
            sortedValues.Count - 1,
            Math.Max(0, (int)Math.Ceiling((percentile / 100d) * sortedValues.Count) - 1));

        return sortedValues[index];
    }

    private static string? ExtractSchemaVersion(object? rawRules)
    {
        if (rawRules is null)
        {
            return null;
        }

        if (rawRules is string rawText)
        {
            try
            {
                using var doc = JsonDocument.Parse(rawText);
                return ExtractSchemaVersion(doc.RootElement);
            }
            catch
            {
                return null;
            }
        }

        if (rawRules is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!jsonElement.TryGetProperty("schemaVersion", out var schemaVersionElement))
            {
                return null;
            }

            return schemaVersionElement.ValueKind == JsonValueKind.String
                ? schemaVersionElement.GetString()
                : null;
        }

        try
        {
            var element = JsonSerializer.SerializeToElement(rawRules, JsonDefaults.Options);
            return ExtractSchemaVersion(element);
        }
        catch
        {
            return null;
        }
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
    public static IAsyncEnumerable<ConversionResult> StreamJsonArrayConversionAsync(
        Stream stream,
        object? rawRules,
        ConversionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return StreamConversionAsync(
            stream,
            rawRules,
            new StreamConversionOptions
            {
                InputKind = StreamInputKind.JsonArray,
                ErrorMode = StreamErrorMode.ContinueWithReport
            },
            options,
            cancellationToken);
    }

    /// <summary>
    /// Streams conversion over supported stream input shapes.
    /// </summary>
    /// <param name="stream">Input stream.</param>
    /// <param name="rawRules">Rules input (object or JSON-like model).</param>
    /// <param name="streamOptions">Stream adapter and error handling options.</param>
    /// <param name="options">Optional conversion execution options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async stream of per-item conversion results.</returns>
    public static async IAsyncEnumerable<ConversionResult> StreamConversionAsync(
        Stream stream,
        object? rawRules,
        StreamConversionOptions? streamOptions = null,
        ConversionOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        var rules = NormalizeConversionRules(rawRules);
        var adapterOptions = streamOptions ?? new StreamConversionOptions();
        var index = 0;

        await foreach (var item in ReadStreamItems(stream, adapterOptions, cancellationToken).WithCancellation(cancellationToken))
        {
            if (item.Error is not null)
            {
                if (adapterOptions.ErrorMode == StreamErrorMode.FailFast)
                {
                    throw new InvalidOperationException(item.Error);
                }

                yield return CreateStreamErrorResult(index, item.Error);
                index += 1;
                continue;
            }

            var conversion = MappingExecutor.ApplyConversion(item.Value, rules, options);
            if (adapterOptions.ErrorMode == StreamErrorMode.FailFast && conversion.Errors.Count > 0)
            {
                var firstError = conversion.Errors[0];
                throw new InvalidOperationException($"stream[{index}]: conversion failed: {firstError}");
            }

            if (adapterOptions.ErrorMode == StreamErrorMode.ContinueWithReport)
            {
                yield return AddStreamContext(conversion, index);
            }
            else
            {
                yield return conversion;
            }

            index += 1;
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

    private static async IAsyncEnumerable<(object? Value, string? Error)> ReadStreamItems(
        Stream stream,
        StreamConversionOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        switch (options.InputKind)
        {
            case StreamInputKind.Ndjson:
                await foreach (var item in ReadNdjsonItems(stream, options.Encoding, cancellationToken).WithCancellation(cancellationToken))
                {
                    yield return item;
                }
                yield break;
            case StreamInputKind.QueryLines:
                await foreach (var item in ReadQueryLineItems(stream, options.Encoding, cancellationToken).WithCancellation(cancellationToken))
                {
                    yield return item;
                }
                yield break;
            case StreamInputKind.XmlElements:
                await foreach (var item in ReadXmlElementItems(stream, options.XmlItemPath, cancellationToken).WithCancellation(cancellationToken))
                {
                    yield return item;
                }
                yield break;
            default:
                await foreach (var item in ReadJsonArrayItems(stream, cancellationToken).WithCancellation(cancellationToken))
                {
                    yield return item;
                }
                yield break;
        }
    }

    private static async IAsyncEnumerable<(object? Value, string? Error)> ReadJsonArrayItems(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var elements = JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(stream, cancellationToken: cancellationToken);
        await foreach (var element in elements.WithCancellation(cancellationToken))
        {
            yield return (JsonConverter.ToObject(element), null);
        }
    }

    private static async IAsyncEnumerable<(object? Value, string? Error)> ReadNdjsonItems(
        Stream stream,
        Encoding? encoding,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(
            stream,
            encoding ?? Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 1024,
            leaveOpen: true);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                yield break;
            }

            var trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            (object? Value, string? Error) result;
            try
            {
                result = (JsonConverter.ParseJson(trimmed), null);
            }
            catch (Exception ex)
            {
                result = (null, $"failed to parse NDJSON line: {ex.Message}");
            }

            yield return result;
        }
    }

    private static async IAsyncEnumerable<(object? Value, string? Error)> ReadQueryLineItems(
        Stream stream,
        Encoding? encoding,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(
            stream,
            encoding ?? Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 1024,
            leaveOpen: true);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                yield break;
            }

            var trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            (object? Value, string? Error) result;
            try
            {
                result = (QueryStringConverter.ParseQueryString(trimmed), null);
            }
            catch (Exception ex)
            {
                result = (null, $"failed to parse query record: {ex.Message}");
            }

            yield return result;
        }
    }

    private static async IAsyncEnumerable<(object? Value, string? Error)> ReadXmlElementItems(
        Stream stream,
        string? xmlItemPath,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(xmlItemPath))
        {
            throw new InvalidOperationException("XmlElements streaming requires StreamConversionOptions.XmlItemPath.");
        }

        var segments = xmlItemPath
            .Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            throw new InvalidOperationException("XmlElements streaming requires a non-empty XmlItemPath.");
        }

        string xmlText;
        using (var reader = new StreamReader(
                   stream,
                   Encoding.UTF8,
                   detectEncodingFromByteOrderMarks: true,
                   bufferSize: 1024,
                   leaveOpen: true))
        {
            xmlText = await reader.ReadToEndAsync(cancellationToken);
        }

        object? parsedXml = null;
        string? parseError = null;
        try
        {
            parsedXml = XmlConverter.ParseXml(xmlText);
        }
        catch (Exception ex)
        {
            parseError = $"failed to parse XML stream: {ex.Message}";
        }

        if (parseError is not null)
        {
            yield return (null, parseError);
            yield break;
        }

        foreach (var item in SelectXmlItems(parsedXml, segments))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return (item, null);
        }
    }

    private static IEnumerable<object?> SelectXmlItems(object? root, string[] pathSegments)
    {
        var current = new List<object?> { root };
        foreach (var segment in pathSegments)
        {
            var next = new List<object?>();
            foreach (var item in current)
            {
                if (item is not Dictionary<string, object?> obj)
                {
                    continue;
                }

                if (!obj.TryGetValue(segment, out var value))
                {
                    continue;
                }

                if (value is List<object?> list)
                {
                    next.AddRange(list);
                }
                else
                {
                    next.Add(value);
                }
            }

            current = next;
        }

        return current;
    }

    private static ConversionResult CreateStreamErrorResult(int index, string error)
    {
        var message = $"stream[{index}]: {error}";
        return new ConversionResult
        {
            Output = new Dictionary<string, object?>(),
            Errors = [message],
            Warnings = [],
            Trace = [],
            Diagnostics =
            [
                new ConversionDiagnostic
                {
                    Code = "ACV-STR-001",
                    RulePath = $"stream[{index}]",
                    Message = message,
                    Severity = ConversionDiagnosticSeverity.Error
                }
            ]
        };
    }

    private static ConversionResult AddStreamContext(ConversionResult result, int index)
    {
        var prefix = $"stream[{index}]";
        return new ConversionResult
        {
            Output = result.Output,
            Errors = result.Errors.Select(error => $"{prefix}: {error}").ToList(),
            Warnings = result.Warnings.Select(warning => $"{prefix}: {warning}").ToList(),
            Trace = result.Trace,
            Diagnostics = result.Diagnostics
                .Select(diagnostic => new ConversionDiagnostic
                {
                    Code = diagnostic.Code,
                    RulePath = $"{prefix}.{diagnostic.RulePath}",
                    Message = $"{prefix}: {diagnostic.Message}",
                    Severity = diagnostic.Severity
                })
                .ToList()
        };
    }
}
