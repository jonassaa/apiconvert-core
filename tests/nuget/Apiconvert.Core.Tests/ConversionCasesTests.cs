using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Linq;
using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;
using Xunit;

namespace Apiconvert.Core.Tests;

public sealed class ConversionCasesTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [Fact]
    public void ConversionCases_RunAll()
    {
        var casesRoot = LocateCasesRoot();
        var caseDirectories = Directory.GetDirectories(casesRoot)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.NotEmpty(caseDirectories);

        foreach (var caseDirectory in caseDirectories)
        {
            RunCase(caseDirectory);
        }
    }

    private static void RunCase(string caseDirectory)
    {
        var (rulesPath, inputPath, outputPath) = LoadCaseFiles(caseDirectory);
        var rulesJson = File.ReadAllText(rulesPath);
        var inputText = File.ReadAllText(inputPath);
        var expectedText = File.ReadAllText(outputPath);

        var rules = ConversionEngine.NormalizeConversionRules(rulesJson);
        var inputFormat = ParseFormatFromExtension(Path.GetExtension(inputPath));
        var outputFormat = ParseFormatFromExtension(Path.GetExtension(outputPath));

        object? inputValue = inputFormat.HasValue
            ? ParsePayloadOrThrow(inputText, inputFormat.Value, caseDirectory)
            : inputText;

        var result = ConversionEngine.ApplyConversion(inputValue, rules);

        Assert.True(result.Errors.Count == 0, BuildErrorMessage(caseDirectory, result.Errors));

        var actualText = outputFormat.HasValue
            ? ConversionEngine.FormatPayload(
                result.Output,
                outputFormat.Value,
                pretty: outputFormat.Value is DataFormat.Xml)
            : Convert.ToString(result.Output) ?? string.Empty;

        var normalizedActual = NormalizeOutput(actualText, outputFormat);
        var normalizedExpected = NormalizeOutput(expectedText, outputFormat);

        Assert.Equal(normalizedExpected, normalizedActual);
    }

    private static string BuildErrorMessage(string caseDirectory, IEnumerable<string> errors)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Conversion errors for case '{Path.GetFileName(caseDirectory)}':");
        foreach (var error in errors)
        {
            builder.AppendLine($"- {error}");
        }
        return builder.ToString();
    }

    private static object? ParsePayloadOrThrow(string text, DataFormat format, string caseDirectory)
    {
        var (value, error) = ConversionEngine.ParsePayload(text, format);
        if (error is null)
        {
            return value;
        }

        throw new Xunit.Sdk.XunitException(
            $"Failed to parse input for case '{Path.GetFileName(caseDirectory)}': {error}");
    }

    private static (string RulesPath, string InputPath, string OutputPath) LoadCaseFiles(string caseDirectory)
    {
        var rulesPath = Path.Combine(caseDirectory, "rules.json");
        var inputPath = GetSingleFile(caseDirectory, "input");
        var outputPath = GetSingleFile(caseDirectory, "output");

        if (!File.Exists(rulesPath))
        {
            throw new Xunit.Sdk.XunitException(
                $"Case '{Path.GetFileName(caseDirectory)}' is missing rules.json.");
        }

        EnsureExtensionSupported(inputPath, caseDirectory);
        EnsureExtensionSupported(outputPath, caseDirectory);

        return (rulesPath, inputPath, outputPath);
    }

    private static string GetSingleFile(string caseDirectory, string baseName)
    {
        var matches = Directory.GetFiles(caseDirectory, $"{baseName}.*");
        if (matches.Length == 0)
        {
            throw new Xunit.Sdk.XunitException(
                $"Case '{Path.GetFileName(caseDirectory)}' must contain {baseName}.<ext>.");
        }

        if (matches.Length > 1)
        {
            throw new Xunit.Sdk.XunitException(
                $"Case '{Path.GetFileName(caseDirectory)}' must contain exactly one {baseName}.<ext> file.");
        }

        return matches[0];
    }

    private static void EnsureExtensionSupported(string path, string caseDirectory)
    {
        var extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        if (extension is "json" or "xml" or "txt")
        {
            return;
        }

        throw new Xunit.Sdk.XunitException(
            $"Case '{Path.GetFileName(caseDirectory)}' uses unsupported extension '.{extension}'.");
    }

    private static DataFormat? ParseFormatFromExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".json" => DataFormat.Json,
            ".xml" => DataFormat.Xml,
            ".txt" => DataFormat.Query,
            _ => null
        };
    }

    private static string NormalizeOutput(string text, DataFormat? format)
    {
        var normalized = text.Replace("\r\n", "\n").Trim();
        if (format is DataFormat.Json)
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(normalized) ? "{}" : normalized);
            return JsonSerializer.Serialize(document.RootElement, JsonOptions);
        }

        if (format is DataFormat.Xml)
        {
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return string.Empty;
            }

            var document = XDocument.Parse(normalized, LoadOptions.None);
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true,
                NewLineHandling = NewLineHandling.Replace,
                NewLineChars = "\n"
            };

            using var writer = new StringWriter();
            using (var xmlWriter = XmlWriter.Create(writer, settings))
            {
                document.Save(xmlWriter);
            }

            return writer.ToString().Trim();
        }

        return normalized;
    }

    private static string LocateCasesRoot()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var candidate = Path.Combine(baseDirectory, "cases");
        if (Directory.Exists(candidate))
        {
            return candidate;
        }

        var current = new DirectoryInfo(baseDirectory);
        while (current is not null)
        {
            var testsRoot = Path.Combine(current.FullName, "tests", "cases");
            if (Directory.Exists(testsRoot))
            {
                return testsRoot;
            }

            current = current.Parent;
        }

        throw new Xunit.Sdk.XunitException(
            "Could not locate tests/cases. Ensure cases are copied to output or exist in the repository.");
    }
}
